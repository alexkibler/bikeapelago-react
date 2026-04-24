export type NodeState = 'Hidden' | 'Available' | 'Checked';

export interface MapNode {
  id: string;
  name: string;
  lat: number;
  lon: number;
  state: NodeState;
  ap_arrival_location_id: number;
  ap_precision_location_id: number;
  region_tag: string;
  is_arrival_checked: boolean;
  is_precision_checked: boolean;
  has_been_relocated: boolean;
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
  connection_mode: string; // "archipelago" | "singleplayer"
  transport_mode: string; // "bike" | "walk"
  progression_mode: string;
  north_pass_received: boolean;
  east_pass_received: boolean;
  south_pass_received: boolean;
  west_pass_received: boolean;
  radius_step: number;
  macguffins_required: number;
  macguffins_collected: number;
  status: 'SetupInProgress' | 'Active' | 'Completed' | 'Archived';
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
  apArrivalLocationId?: number;
  apPrecisionLocationId?: number;
  arrivalChecked: boolean;
  precisionChecked: boolean;
  lat: number;
  lon: number;
}

export interface FitAnalysisResult {
  path: PathPoint[];
  stats: RideStats;
  newlyCheckedNodes: NewlyCheckedNode[];
}
