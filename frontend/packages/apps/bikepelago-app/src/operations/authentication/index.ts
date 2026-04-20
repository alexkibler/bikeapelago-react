import {
  useApiPostFactory,
  useMutation,
} from '@bikeapelago/shared-data-fetching';
import type {
  LoginInput,
  LoginOutput,
} from './types';


export function useLoginPost() {
  const loginRequest = useApiPostFactory<'/api/auth/login', LoginInput, LoginOutput>('/api/auth/login')

  return useMutation({
    mutationFn: async (body: LoginInput) => await loginRequest({
      body,
    }),
  })
}
