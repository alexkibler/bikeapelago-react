export type LoginInput = {
  identity: string;
  password: string;
};

export type User = {
  id: string;
  username: string;
  name: string;
  weight: number;
  avatar: string | null;
  email: string | null;
};

export type LoginOutput = {
  token: string;
  record: User;
};
