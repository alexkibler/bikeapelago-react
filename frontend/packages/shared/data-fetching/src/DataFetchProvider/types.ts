export type TDataFetchProviderContext = {
  token?: string | null;
  handleUnauthorized: () => void;
};

export type DataFetchProviderProps = {
  token?: string | null;
  handleUnauthorized: () => void;
};
