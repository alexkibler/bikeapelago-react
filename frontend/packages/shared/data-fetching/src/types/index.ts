
export type ExtractPathParamsBase<T extends string> =
  T extends `${string}:${infer Param}/${infer Rest}`
    ? { [K in Param | keyof ExtractPathParamsBase<Rest>]: string }
    : T extends `${string}:${infer Param}`
    ? { [K in Param]: string }
    : {};

export type ExtractPathParams<T extends string> =
  T extends `${string}:${string}`
    ? ExtractPathParamsBase<T>
    : void;

type QueryRequestCallbackArgsDefiner<
  TSearchParams extends Record<string, string> | void | undefined,
> = TSearchParams extends void ?
  { searchParams?: TSearchParams; }
  : { searchParams: TSearchParams; }


export type QueryRequestCallbackArgs<
  TSearchParams extends Record<string, string> | void | undefined,
> = Pick<
  QueryRequestCallbackArgsDefiner<TSearchParams>,
  'searchParams'
> & {
  signal?: AbortSignal
}

export type QueryRequestCallback<
  TSearchParams extends Record<string, string> | void | undefined,
  TResponse
> = (url: string, args: QueryRequestCallbackArgs<TSearchParams>) => Promise<TResponse>

export type QueryRequestFactoryCallbackArgs<
  TPath extends string,
  TSearchParams extends Record<string, string> | void | undefined
> = ExtractPathParams<TPath> extends void ?
  {
    pathParams?: ExtractPathParams<TPath>;
  } & QueryRequestCallbackArgs<TSearchParams>
  : {
      pathParams: ExtractPathParams<TPath>;
    } & QueryRequestCallbackArgs<TSearchParams>;

export type QueryRequestFactoryCallback<
  TPath extends string,
  TSearchParams extends Record<string, string> | void | undefined,
  TResponse
> = (args: QueryRequestFactoryCallbackArgs<TPath, TSearchParams>) => Promise<TResponse>
