import { useCallback } from 'react';
import { useDataFetchProviderCtx } from '../../DataFetchProvider';
import type { QueryRequestFactoryCallbackArgs, QueryRequestCallbackArgs, QueryRequestCallback, QueryRequestFactoryCallback } from '../../types';
import { compile } from 'path-to-regexp';

export function useApiGet<
  TSearchParams extends Record<string, string> | void | undefined,
  TResponse
>(): QueryRequestCallback<TSearchParams, TResponse> {
  const { handleUnauthorized, token } = useDataFetchProviderCtx();

  const request: QueryRequestCallback<TSearchParams, TResponse> = useCallback(async (url: string, {
    searchParams: _searchParams,
    signal
  }: QueryRequestCallbackArgs<TSearchParams>) => {
    const headers = {
      'Content-Type': 'application/json',
      ...(token ? { 'Authorization': `Bearer ${token}` } : {})
    } as const;

    const res = await fetch(url, {
      headers,
      signal,
    });

    if (!res.ok) {
      if (res.status === 401) {
        handleUnauthorized();
        // brute force short circuit
        return undefined as unknown as TResponse;
      }
      throw new Error(`Request Failed`);
    }

    const data = await res.json() as TResponse;

    return data
  }, [handleUnauthorized, token]);

  return request
}

export function useApiGetFactory<
  TPath extends string,
  TSearchParams extends Record<string, string> | void | undefined,
  TResponse
>(path: TPath): QueryRequestFactoryCallback<
  TPath,
  TSearchParams,
  TResponse
> {
  const getRequest = useApiGet<
    TSearchParams,
    TResponse
  >();

  const requestCallback: QueryRequestFactoryCallback<
    TPath,
    TSearchParams,
    TResponse
  > = useCallback(async ({
    pathParams,
    searchParams,
    signal,
  }: QueryRequestFactoryCallbackArgs<TPath, TSearchParams>) => {
    let requestPath = path.toString();

    if (pathParams) {
      const toPath = compile(requestPath);
      requestPath = toPath(pathParams);
    }

    return await getRequest(requestPath, {
      searchParams,
      signal
    });

  }, [path, getRequest]);

  return requestCallback;
}
