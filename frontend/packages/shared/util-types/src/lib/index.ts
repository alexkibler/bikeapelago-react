export type MaybePromise<T> = T | Promise<T>;

export type Values<T> = T[keyof T];
