import { useCallback } from 'react';
import { useDataFetchProviderCtx } from '../../DataFetchProvider';
import type { QueryRequestFactoryCallbackArgs, QueryRequestCallbackArgs, QueryRequestCallback, QueryRequestFactoryCallback } from '../../types';
import { compile } from 'path-to-regexp';

export function useApiGet<
  TResponse,
  TSearchParams extends Record<string, string> | void | undefined = void
>(): QueryRequestCallback<TResponse, TSearchParams> {
  const { handleUnauthorized, token, baseUrl } = useDataFetchProviderCtx();

  const request: QueryRequestCallback<TResponse, TSearchParams> = useCallback(async (url: string, {
    searchParams: _searchParams,
    signal
  }: QueryRequestCallbackArgs<TSearchParams>) => {
    url = baseUrl ? `${baseUrl}${url}` : url;
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
  }, [handleUnauthorized, token, baseUrl]);

  return request
}

export function useApiGetFactory<
  TPath extends string,
  TResponse,
  TSearchParams extends Record<string, string> | void | undefined = void
>(path: TPath): QueryRequestFactoryCallback<
  TPath,
  TResponse,
  TSearchParams
> {
  const getRequest = useApiGet<
    TResponse,
    TSearchParams
  >();

  const requestCallback: QueryRequestFactoryCallback<
    TPath,
    TResponse,
    TSearchParams
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
