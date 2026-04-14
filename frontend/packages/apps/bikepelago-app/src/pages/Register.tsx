import React, { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { Lock, User, ArrowRight, Loader2, Mail, UserPlus } from 'lucide-react';
import { useAuthStore } from '../store/authStore';

const Register = () => {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [name, setName] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();
  const { isValid } = useAuthStore();

  // If already authenticated, go home
  useEffect(() => {
    if (isValid) navigate('/');
  }, [isValid, navigate]);

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      const res = await fetch('/api/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password, name }),
      });

      if (!res.ok) {
        const errData = (await res.json().catch(() => ({}))) as {
          message?: string;
        };
        throw new Error(errData.message ?? 'Registration failed.');
      }

      // After registration, redirect to login
      navigate('/login', {
        state: { message: 'Registration successful! Please log in.' },
      });
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Registration failed.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className='min-h-[80vh] flex items-center justify-center p-6'>
      <div className='w-full max-w-md'>
        <div className='text-center mb-10'>
          <div className='inline-flex items-center justify-center w-16 h-16 rounded-3xl bg-orange-600/10 border border-orange-500/20 mb-6 group hover:scale-110 transition-transform duration-500'>
            <UserPlus className='w-8 h-8 text-orange-500 group-hover:rotate-12 transition-transform' />
          </div>
          <h1 className='text-4xl font-black text-[var(--color-text-hex)] italic uppercase tracking-tighter mb-2'>
            Join the Race
          </h1>
          <p className='text-[var(--color-text-subtle-hex)] font-medium'>
            Create your Bikeapelago account
          </p>
        </div>

        <form onSubmit={handleRegister} className='space-y-4'>
          {error && (
            <div className='p-4 bg-red-500/10 border border-red-500/20 rounded-2xl text-red-500 text-sm font-bold text-center'>
              {error}
            </div>
          )}

          <div className='relative group'>
            <User className='absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-neutral-600 group-focus-within:text-orange-500 transition-colors' />
            <input
              type='text'
              placeholder='Display Name (optional)'
              value={name}
              onChange={(e) => setName(e.target.value)}
              className='w-full bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] rounded-2xl py-4 pl-12 pr-4 text-[var(--color-text-hex)] focus:outline-none focus:border-orange-500/50 transition-all placeholder:text-[var(--color-text-subtle-hex)] font-medium'
            />
          </div>

          <div className='relative group'>
            <Mail className='absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-neutral-600 group-focus-within:text-orange-500 transition-colors' />
            <input
              type='text'
              placeholder='Username'
              id='register-username'
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              className='w-full bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] rounded-2xl py-4 pl-12 pr-4 text-[var(--color-text-hex)] focus:outline-none focus:border-orange-500/50 transition-all placeholder:text-[var(--color-text-subtle-hex)] font-medium'
              required
            />
          </div>

          <div className='relative group'>
            <Lock className='absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-neutral-600 group-focus-within:text-orange-500 transition-colors' />
            <input
              type='password'
              placeholder='Password'
              id='register-password'
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className='w-full bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] rounded-2xl py-4 pl-12 pr-4 text-[var(--color-text-hex)] focus:outline-none focus:border-orange-500/50 transition-all placeholder:text-[var(--color-text-subtle-hex)] font-medium'
              required
            />
          </div>

          <button
            type='submit'
            disabled={loading}
            id='register-submit'
            className='w-full h-16 rounded-2xl bg-[var(--color-primary-hex)] text-white font-black text-lg uppercase tracking-widest gap-3 shadow-xl shadow-orange-600/20 items-center justify-center flex hover:bg-[var(--color-primary-hover-hex)] transition-all active:scale-[0.98] disabled:opacity-50'
          >
            {loading ? (
              <Loader2 className='w-6 h-6 animate-spin' />
            ) : (
              <>
                Register
                <ArrowRight className='w-5 h-5' />
              </>
            )}
          </button>
        </form>

        <div className='mt-8 text-center text-sm'>
          <p className='text-[var(--color-text-subtle-hex)]'>
            Already have an account?{' '}
            <Link
              to='/login'
              className='text-orange-500 hover:text-orange-600 font-bold transition-colors'
            >
              Log In
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
};

export default Register;
