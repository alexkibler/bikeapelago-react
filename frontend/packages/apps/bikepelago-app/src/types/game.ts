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

export interface PathPoint {
  lat: number;
  lon: number;
  alt?: number;
}

export interface RideStats {
  distanceMeters: number;
  elevationGainMeters: number;
  durationSeconds: number;
  avgSpeedKph?: number;
}

export interface NewlyCheckedNode {
  id: string;
  apLocationId?: number;
  lat: number;
  lon: number;
}

export interface FitAnalysisResult {
  path: PathPoint[];
  stats: RideStats;
  newlyCheckedNodes: NewlyCheckedNode[];
}
