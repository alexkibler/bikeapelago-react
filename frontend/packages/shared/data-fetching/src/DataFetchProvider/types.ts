export type TDataFetchProviderContext = {
  token?: string | null;
  handleUnauthorized: () => void;
  baseUrl?: string;
};

export type DataFetchProviderProps = {
  token?: string | null;
  handleUnauthorized: () => void;
  baseUrl?: string;
};
