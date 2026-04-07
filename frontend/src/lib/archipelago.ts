import { Client, itemsHandlingFlags } from '@airbreather/archipelago.js';
import { useArchipelagoStore } from '../store/archipelagoStore';
import type { ChatMessage } from '../store/archipelagoStore';

class ArchipelagoClient {
  private client: Client;

  constructor() {
    this.client = new Client();
    this.setupListeners();
  }

  private setupListeners() {
    // Socket events (connection status)
    this.client.socket.on('connected', () => {
      console.log('Archipelago: Connected & Authenticated');
      useArchipelagoStore.getState().setStatus('connected');
      useArchipelagoStore.getState().setError(null);
      
      const checked = this.client.room.checkedLocations;
      useArchipelagoStore.getState().setCheckedLocations(checked);
      
      useArchipelagoStore.getState().addMessage({
        text: `Connected to Archipelago as ${this.client.players.self.alias}`,
        type: 'system'
      });
    });

    this.client.socket.on('disconnected', () => {
      console.log('Archipelago: Disconnected');
      useArchipelagoStore.getState().setStatus('disconnected');
    });

    this.client.socket.on('connectionRefused', (packet) => {
      const errorMsg = packet.errors?.join(', ') || 'Connection refused';
      console.error('Archipelago Connection Refused:', errorMsg);
      useArchipelagoStore.getState().setStatus('error');
      useArchipelagoStore.getState().setError(errorMsg);
      useArchipelagoStore.getState().addMessage({
        text: `Connection Refused: ${errorMsg}`,
        type: 'error'
      });
    });

    // Message events (chat and items)
    this.client.messages.on('message', (text, nodes) => {
      // Find out if it's a special type based on nodes or event type if we used specific listeners
      // For simplicity, we just use the global 'message' but can refine with specific listeners
      // itemSent, itemCheated, etc are available on client.messages
      useArchipelagoStore.getState().addMessage({ text, type: 'system' });
    });

    this.client.messages.on('chat', (message, player) => {
      useArchipelagoStore.getState().addMessage({ 
        text: `${player.alias}: ${message}`, 
        type: 'player' 
      });
    });

    this.client.messages.on('itemSent', (text) => {
      useArchipelagoStore.getState().addMessage({ text, type: 'item' });
    });

    // Room events (location checks)
    this.client.room.on('locationsChecked', (locations) => {
      locations.forEach(locId => {
        useArchipelagoStore.getState().addCheckedLocation(locId);
      });
    });
  }

  async connect(url: string, slotName: string, password?: string) {
    if (this.client.authenticated) {
        return;
    }

    useArchipelagoStore.getState().setStatus('connecting');
    useArchipelagoStore.getState().setError(null);

    // Clean up URL: remove http/https if present
    let cleanUrl = url.replace(/^(ws:\/\/|wss:\/\/)/, '');
    
    try {
      // login handles both socket connection and authentication
      await this.client.login(cleanUrl, slotName, 'Bikeapelago', {
        password: password || '',
        items: itemsHandlingFlags.all,
        slotData: true
      });
    } catch (err: any) {
      console.error('Login failed:', err);
      // login error might have been handled by connectionRefused already, but just in case
      if (useArchipelagoStore.getState().status !== 'error') {
        useArchipelagoStore.getState().setStatus('error');
        useArchipelagoStore.getState().setError(err.message || 'Failed to connect');
      }
    }
  }

  say(text: string) {
    if (this.client.authenticated) {
      this.client.messages.say(text);
    }
  }

  disconnect() {
    this.client.socket.disconnect();
    useArchipelagoStore.getState().setStatus('disconnected');
  }
}

export const archipelago = new ArchipelagoClient();
