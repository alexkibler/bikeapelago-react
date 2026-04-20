export type SessionUniqueWhere = { id: string };

export type SessionUpdateInput = {
  ap_server_url: string;
  ap_slot_name: string;
}

export type SessionCreateDataInput = {
  user: string;
  name: string;
  status: string;
  radius: number;
  center_lat: number;
  center_lon: number;
  ap_server_url?: string;
  ap_slot_name?: string;
  mode: string;
}
