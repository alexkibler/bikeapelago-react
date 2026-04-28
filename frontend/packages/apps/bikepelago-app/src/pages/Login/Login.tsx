import { useEffect, useState } from 'react';
import type { ReactElement } from 'react';

import { ArrowRight, Bike, Lock, User } from 'lucide-react';
import { Link, useLocation, useNavigate } from 'react-router-dom';

import { Button } from '@bikeapelago/shared-ui-components';
import { RhfInput, useForm } from '@bikeapelago/shared-ui-form';

import { useLoginPost } from '../../operations/authentication';
import { useAuthStore } from '../../store/authStore';
import type { LoginForm } from './types';

export function Login(): ReactElement {
  const formMethods = useForm<LoginForm>();
  const [error, setError] = useState('');
  const navigate = useNavigate();
  const { login, isValid } = useAuthStore();

  const loginRequest = useLoginPost();

  const location = useLocation();
  const registerMessage = (location.state as Record<string, unknown> | null)
    ?.message as string | undefined;

  // If already authenticated, go home
  useEffect(() => {
    if (isValid) void navigate('/');
  }, [isValid, navigate]);

  const handleLogin = (data: LoginForm) => {
    loginRequest.mutate(data, {
      onSuccess: (responseData) => {
        login(responseData.token, responseData.record);
        const returnTo = (location.state as Record<string, unknown> | null)
          ?.returnTo as string | undefined;
        void navigate(returnTo ?? '/');
      },
      onError: (err) => {
        setError(err instanceof Error ? err.message : 'Invalid credentials.');
      },
    });
  };

  const handleAutofill = () => {
    handleLogin({
      identity: 'testuser',
      password: 'Password',
    });
  };

  return (
    <div className='min-h-[80vh] flex items-center justify-center p-6'>
      <div className='w-full max-w-md'>
        <div className='text-center mb-10'>
          <div className='inline-flex items-center justify-center w-16 h-16 rounded-3xl bg-orange-600/10 border border-orange-500/20 mb-6 group hover:scale-110 transition-transform duration-500'>
            <Bike className='w-8 h-8 text-orange-500 group-hover:rotate-12 transition-transform' />
          </div>
          <h1 className='text-4xl font-black text-[var(--color-text-hex)] italic uppercase tracking-tighter mb-2'>
            Bikeapelago
          </h1>
        </div>

        <form
          onSubmit={formMethods.handleSubmit(handleLogin)}
          className='space-y-4'
        >
          {registerMessage && !error && (
            <div className='p-4 bg-emerald-500/10 border border-emerald-500/20 rounded-2xl text-emerald-500 text-sm font-bold text-center'>
              {registerMessage}
            </div>
          )}

          {error && (
            <div className='p-4 bg-red-500/10 border border-red-500/20 rounded-2xl text-red-500 text-sm font-bold text-center'>
              {error}
            </div>
          )}

          <RhfInput
            leftIcon={
              <User className='absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-neutral-600 group-focus-within:text-orange-500 transition-colors' />
            }
            formMethods={formMethods}
            name='identity'
            placeholder='Username'
            required
          />
          <RhfInput
            leftIcon={
              <Lock className='absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-neutral-600 group-focus-within:text-orange-500 transition-colors' />
            }
            formMethods={formMethods}
            name='password'
            type='password'
            placeholder='Password'
            required
          />

          <Button
            type='submit'
            isLoading={loginRequest.isPending}
            id='login-submit'
          >
            Ignition
            <ArrowRight className='w-5 h-5' />
          </Button>
        </form>

        <div className='mt-8 text-center text-sm space-y-2'>
          <p className='text-[var(--color-text-subtle-hex)]'>
            New to the archipelago?{' '}
            <Link
              to='/register'
              className='text-orange-500 hover:text-orange-600 font-bold transition-colors'
            >
              Self-Register
            </Link>
          </p>
          <p className='text-[var(--color-text-subtle-hex)]'>
            Not sure what this is?{' '}
            <Link
              to='/about'
              className='text-orange-500 hover:text-orange-600 font-bold transition-colors'
            >
              Learn more
            </Link>
          </p>
        </div>

        <div className='mt-12 flex flex-col items-center gap-6'>
          <div className='h-px w-24 bg-gradient-to-r from-transparent via-[var(--color-border-hex)] to-transparent' />

          <div className='flex flex-col items-center gap-2'>
            <span className='text-[10px] font-black text-[var(--color-text-subtle-hex)] uppercase tracking-[0.3em]'>
              Fast Track
            </span>
            <button
              type='button'
              onClick={handleAutofill}
              className='group relative px-6 py-3 rounded-2xl bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] hover:border-orange-500/40 transition-all duration-500 overflow-hidden shadow-sm hover:shadow-orange-500/10'
            >
              <div className='absolute inset-0 bg-orange-500/5 opacity-0 group-hover:opacity-100 transition-opacity duration-500' />
              <div className='absolute -bottom-px left-1/2 -translate-x-1/2 w-12 h-px bg-gradient-to-r from-transparent via-orange-500/50 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-500' />

              <div className='flex items-center gap-3'>
                <code className='text-sm font-mono text-orange-500 font-bold group-hover:text-orange-600 transition-colors duration-300'>
                  testuser:Password
                </code>
                <div className='w-1.5 h-1.5 rounded-full bg-orange-500/30 group-hover:bg-orange-500 transition-all' />
              </div>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
