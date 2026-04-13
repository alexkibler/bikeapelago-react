export interface GameSession {
  id: string;
  name: string | null;
  ap_seed_name: string | null;
  ap_server_url: string | null;
  ap_slot_name: string | null;
  mode: string;
  status: string;
}
