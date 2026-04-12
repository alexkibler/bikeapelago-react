export type NodeState = 'Hidden' | 'Available' | 'Checked';

export interface MapNode {
  id: string;
  name: string;
  lat: number;
  lon: number;
  state: NodeState;
  apLocationId: number;
}

export interface GameSession {
  id: string;
  name?: string;
  ap_seed_name?: string;
  ap_slot_name: string;
  ap_server_url?: string;
  center_lat?: number;
  center_lon?: number;
  radius?: number;
  received_item_ids?: number[];
}
