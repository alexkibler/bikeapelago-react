import * as signalR from '@microsoft/signalr';

import {
  type ArchipelagoStatus,
  useArchipelagoStore,
} from '../store/archipelagoStore';

class ArchipelagoClient {
  private connection: signalR.HubConnection | null = null;
  private startingPromise: Promise<signalR.HubConnection> | null = null;
  private currentSessionId: string | null = null;

  private async getOrCreateConnection(): Promise<signalR.HubConnection> {
    if (
      this.connection &&
      this.connection.state === signalR.HubConnectionState.Connected
    ) {
      return this.connection;
    }

    if (this.startingPromise) {
      return this.startingPromise;
    }

    this.startingPromise = (async () => {
      try {
        const conn = new signalR.HubConnectionBuilder()
          .withUrl('/hubs/archipelago')
          .withAutomaticReconnect()
          .build();

        conn.on(
          'OnStatusUpdate',
          (update: { status: string; error?: string }) => {
            console.log('ArchipelagoHub Status Update:', update);
            useArchipelagoStore
              .getState()
              .setStatus(update.status as ArchipelagoStatus);
            if (update.error)
              useArchipelagoStore.getState().setError(update.error);
          },
        );

        conn.on('OnLocationsUpdate', (update: { locationIds: number[] }) => {
          const store = useArchipelagoStore.getState();
          // Merge with existing locations to avoid overwriting the full list with partial updates
          const current = store.checkedLocationIds;
          const merged = [...new Set([...current, ...update.locationIds])];
          store.setCheckedLocations(merged);
        });

        conn.on(
          'OnItemsUpdate',
          (update: { items: { id: number; name: string }[] }) => {
            const store = useArchipelagoStore.getState();
            store.setReceivedItems(update.items);
          },
        );

        conn.on(
          'OnChatMessage',
          (message: { text: string; type: string; timestamp: string }) => {
            useArchipelagoStore.getState().addMessage({
              text: message.text,
              // @ts-expect-error message.type exists but type is string not ChatMessageType
              type: message.type,
            });
          },
        );

        conn.on('OnSyncRequired', async () => {
          console.log('[ArchipelagoClient] Sync required, triggering refresh');
          const { triggerSync } = (
            await import('../store/gameStore')
          ).useGameStore.getState();
          triggerSync();
        });

        await conn.start();
        this.connection = conn;
        return conn;
      } finally {
        this.startingPromise = null;
      }
    })();

    return this.startingPromise;
  }

  async connect(
    sessionId: string,
    url: string,
    slotName: string,
    password?: string,
  ) {
    if (
      this.connection?.state === signalR.HubConnectionState.Connected &&
      this.currentSessionId === sessionId &&
      useArchipelagoStore.getState().status === 'connected'
    ) {
      return; // Already connecting/connected to this session
    }

    try {
      const conn = await this.getOrCreateConnection();
      this.currentSessionId = sessionId;

      // Join the session group in SignalR
      await conn.invoke('JoinSession', sessionId);

      // Tell the backend to connect to actual Archipelago
      await conn.invoke(
        'ConnectToArchipelago',
        sessionId,
        url,
        slotName,
        password || null,
      );
    } catch (err: unknown) {
      console.error('SignalR Hub Connection Failed:', err);
      useArchipelagoStore.getState().setStatus('error');
      const errorMessage = err instanceof Error ? err.message : 'SignalR error';
      useArchipelagoStore.getState().setError(errorMessage);
    }
  }

  async say(text: string) {
    if (!this.connection || !this.currentSessionId) return;
    try {
      await this.connection.invoke('SendMessage', this.currentSessionId, text);
    } catch (err) {
      console.error('Failed to send chat message:', err);
    }
  }

  async disconnect() {
    if (!this.connection || !this.currentSessionId) return;
    try {
      await this.connection.invoke('Disconnect', this.currentSessionId);
      await this.connection.invoke('LeaveSession', this.currentSessionId);
    } catch (err) {
      console.error('Failed to disconnect from hub:', err);
    }
  }
}

export const archipelago = new ArchipelagoClient();
