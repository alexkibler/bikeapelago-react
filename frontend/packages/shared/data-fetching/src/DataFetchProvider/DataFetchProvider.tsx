import type { PropsWithChildren } from "react";
import { createContext, useContext, useMemo } from "react";
import type { TDataFetchProviderContext, DataFetchProviderProps } from './types';
import {
  QueryClient,
  QueryClientProvider,
} from '@tanstack/react-query'

const DataFetchProviderContext = createContext<TDataFetchProviderContext | undefined>(
  undefined,
);

export function DataFetchProvider({ children, handleUnauthorized, token }: PropsWithChildren<DataFetchProviderProps>) {
  const queryClient = useMemo(() => new QueryClient(), [token]);

  const value = useMemo(
    () => ({
      handleUnauthorized,
      token,
    }),
    [handleUnauthorized, token],
  );

  return (
    <QueryClientProvider client={queryClient}>
      <DataFetchProviderContext.Provider value={value}>
        {children}
      </DataFetchProviderContext.Provider>
    </QueryClientProvider>
  );
}

export function useDataFetchProviderCtx(): TDataFetchProviderContext {
  const context = useContext(DataFetchProviderContext);
  if (!context) {
    throw new Error("useDataFetchProviderCtx must be used within a DataFetchProvider");
  }

  return context;
}

