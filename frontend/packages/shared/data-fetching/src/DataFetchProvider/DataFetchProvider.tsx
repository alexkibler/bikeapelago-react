import type { PropsWithChildren } from 'react';
import { createContext, useContext, useMemo } from 'react';

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

import type {
  DataFetchProviderProps,
  TDataFetchProviderContext,
} from './types';

const DataFetchProviderContext = createContext<
  TDataFetchProviderContext | undefined
>(undefined);

export function DataFetchProvider({
  children,
  handleUnauthorized,
  token,
}: PropsWithChildren<DataFetchProviderProps>) {
  // eslint-disable-next-line react-hooks/exhaustive-deps -- change client wholesale when token changes
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
    throw new Error(
      'useDataFetchProviderCtx must be used within a DataFetchProvider',
    );
  }

  return context;
}
