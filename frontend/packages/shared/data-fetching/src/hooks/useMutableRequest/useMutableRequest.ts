import { useCallback } from 'react';
import { useDataFetchProviderCtx } from '../../DataFetchProvider';
import type { JSONValue, MutationRequestFactoryCallbackArgs, MutationRequestCallbackArgs, MutationRequestCallback, MutationRequestFactoryCallback } from '../../types';
import { compile } from 'path-to-regexp';

type MethodType = 'POST' | 'PUT' | 'PATCH' | 'DELETE'

function useApiMutableRequest<
  TBody extends JSONValue,
  TResponse,
  TSearchParams extends Record<string, string> | void | undefined = void
>(method: MethodType): MutationRequestCallback<TBody, TResponse, TSearchParams> {
  const { handleUnauthorized, token } = useDataFetchProviderCtx();

  const request: MutationRequestCallback<TBody, TResponse, TSearchParams> = useCallback(async (url: string, {
    body,
    searchParams: _searchParams,
    signal
  }: MutationRequestCallbackArgs<TBody, TSearchParams>) => {
    const headers = {
      'Content-Type': 'application/json',
      ...(token ? { 'Authorization': `Bearer ${token}` } : {})
    } as const;

    const res = await fetch(url, {
      body: JSON.stringify(body),
      method,
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
  }, [handleUnauthorized, token, method]);

  return request
}

function buildMutableRequestFactory(method: MethodType) {
  return function useApiMutableFactory<
    TPath extends string,
    TBody extends JSONValue,
    TResponse,
    TSearchParams extends Record<string, string> | void | undefined = void
  >(path: TPath): MutationRequestFactoryCallback<
    TPath,
    TBody,
    TResponse,
    TSearchParams
  > {
    const mutationRequest = useApiMutableRequest<
      TBody,
      TResponse,
      TSearchParams
    >(method);

    const requestCallback: MutationRequestFactoryCallback<
      TPath,
      TBody,
      TResponse,
      TSearchParams
    > = useCallback(async ({
      body,
      pathParams,
      searchParams,
      signal,
    }: MutationRequestFactoryCallbackArgs<TPath, TBody, TSearchParams>) => {
      let requestPath = path.toString();

      if (pathParams) {
        const toPath = compile(requestPath);
        requestPath = toPath(pathParams);
      }

      return await mutationRequest(requestPath, {
        body,
        searchParams,
        signal
      });

    }, [path, mutationRequest]);

    return requestCallback;
  }
}

export const useApiPostFactory = buildMutableRequestFactory('POST');
export const useApiPatchFactory = buildMutableRequestFactory('PATCH');
export const useApiPutFactory = buildMutableRequestFactory('PUT');
export const useApiDeleteFactory = buildMutableRequestFactory('DELETE');
