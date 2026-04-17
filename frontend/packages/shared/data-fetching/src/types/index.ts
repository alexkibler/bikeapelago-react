export type ExtractPathParamsBase<T extends string> =
  T extends `${string}:${infer Param}/${infer Rest}`
    ? { [K in Param | keyof ExtractPathParamsBase<Rest>]: string }
    : T extends `${string}:${infer Param}`
      ? { [K in Param]: string }
      : // eslint-disable-next-line @typescript-eslint/no-empty-object-type
        {};

export type ExtractPathParams<T extends string> =
  T extends `${string}:${string}` ? ExtractPathParamsBase<T> : void;

export type JSONValue =
  | string
  | number
  | boolean
  | null
  | { [key: string]: JSONValue }
  | JSONValue[];

type QueryRequestCallbackArgsDefiner<
  TSearchParams extends Record<string, string> | void | undefined,
> = TSearchParams extends void
  ? { searchParams?: TSearchParams }
  : { searchParams: TSearchParams };

export type QueryRequestCallbackArgs<
  TSearchParams extends Record<string, string> | void | undefined,
> = Pick<QueryRequestCallbackArgsDefiner<TSearchParams>, 'searchParams'> & {
  signal?: AbortSignal;
};

export type QueryRequestCallback<
  TResponse,
  TSearchParams extends Record<string, string> | void | undefined,
> = (
  url: string,
  args: QueryRequestCallbackArgs<TSearchParams>,
) => Promise<TResponse>;

export type QueryRequestFactoryCallbackArgs<
  TPath extends string,
  TSearchParams extends Record<string, string> | void | undefined,
> =
  ExtractPathParams<TPath> extends void
    ? {
        pathParams?: ExtractPathParams<TPath>;
      } & QueryRequestCallbackArgs<TSearchParams>
    : {
        pathParams: ExtractPathParams<TPath>;
      } & QueryRequestCallbackArgs<TSearchParams>;

export type QueryRequestFactoryCallback<
  TPath extends string,
  TResponse,
  TSearchParams extends Record<string, string> | void | undefined,
> = (
  args: QueryRequestFactoryCallbackArgs<TPath, TSearchParams>,
) => Promise<TResponse>;

type MutationRequestCallbackSearchParamsDefiner<
  TSearchParams extends Record<string, string> | void | undefined,
> = TSearchParams extends void
  ? { searchParams?: TSearchParams }
  : { searchParams: TSearchParams };

export type MutationRequestCallbackArgs<
  TBody extends JSONValue,
  TSearchParams extends Record<string, string> | void | undefined,
> = Pick<
  MutationRequestCallbackSearchParamsDefiner<TSearchParams>,
  'searchParams'
> & {
  body: TBody;
  signal?: AbortSignal;
};

export type MutationRequestFactoryCallbackArgs<
  TPath extends string,
  TBody extends JSONValue,
  TSearchParams extends Record<string, string> | void | undefined,
> =
  ExtractPathParams<TPath> extends void
    ? {
        pathParams?: ExtractPathParams<TPath>;
      } & MutationRequestCallbackArgs<TBody, TSearchParams>
    : {
        pathParams: ExtractPathParams<TPath>;
      } & MutationRequestCallbackArgs<TBody, TSearchParams>;

export type MutationRequestCallback<
  TBody extends JSONValue,
  TResponse,
  TSearchParams extends Record<string, string> | void | undefined,
> = (
  url: string,
  args: MutationRequestCallbackArgs<TBody, TSearchParams>,
) => Promise<TResponse>;

export type MutationRequestFactoryCallback<
  TPath extends string,
  TBody extends JSONValue,
  TResponse,
  TSearchParams extends Record<string, string> | void | undefined,
> = (
  args: MutationRequestFactoryCallbackArgs<TPath, TBody, TSearchParams>,
) => Promise<TResponse>;
