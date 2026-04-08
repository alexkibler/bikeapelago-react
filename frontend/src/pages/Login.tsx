import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Bike, Lock, User, ArrowRight, Loader2 } from 'lucide-react';
import { useAuthStore } from '../store/authStore';

const Login = () => {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();
  const { login, isValid } = useAuthStore();

  // If already authenticated, go home
  useEffect(() => {
    if (isValid) navigate('/');
  }, [isValid, navigate]);

  const performLogin = async (identity: string, pass: string) => {
    setLoading(true);
    setError('');

    try {
      const res = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ identity, password: pass }),
      });
      if (!res.ok) {
        const errData = await res.json().catch(() => ({})) as { message?: string };
        throw new Error(errData.message ?? 'Invalid credentials.');
      }
      const data = await res.json() as { token: string; record: Parameters<typeof login>[1] };
      login(data.token, data.record);
      navigate('/');
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Invalid credentials.');
    } finally {
      setLoading(false);
    }
  };

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    await performLogin(username, password);
  };

  const handleAutofill = () => {
    setUsername('testuser');
    setPassword('Password');
    void performLogin('testuser', 'Password');
  };

  return (
    <div className="min-h-[80vh] flex items-center justify-center p-6">
      <div className="w-full max-w-md">
        <div className="text-center mb-10">
          <div className="inline-flex items-center justify-center w-16 h-16 rounded-3xl bg-orange-600/10 border border-orange-500/20 mb-6 group hover:scale-110 transition-transform duration-500">
            <Bike className="w-8 h-8 text-orange-500 group-hover:rotate-12 transition-transform" />
          </div>
          <h1 className="text-4xl font-black text-white italic uppercase tracking-tighter mb-2">Bikeapelago</h1>
        </div>

        <form onSubmit={handleLogin} className="space-y-4">
          {error && (
            <div className="p-4 bg-red-500/10 border border-red-500/20 rounded-2xl text-red-500 text-sm font-bold text-center">
              {error}
            </div>
          )}

          <div className="relative group">
            <User className="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-neutral-600 group-focus-within:text-orange-500 transition-colors" />
            <input
              type="text"
              placeholder="Username"
              id="login-username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              className="w-full bg-neutral-900 border border-neutral-800 rounded-2xl py-4 pl-12 pr-4 text-white focus:outline-none focus:border-orange-500/50 transition-all placeholder:text-neutral-700 font-medium"
              required
            />
          </div>

          <div className="relative group">
            <Lock className="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-neutral-600 group-focus-within:text-orange-500 transition-colors" />
            <input
              type="password"
              placeholder="Password"
              id="login-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full bg-neutral-900 border border-neutral-800 rounded-2xl py-4 pl-12 pr-4 text-white focus:outline-none focus:border-orange-500/50 transition-all placeholder:text-neutral-700 font-medium"
              required
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            id="login-submit"
            className="w-full btn btn-orange btn-lg h-16 rounded-2xl font-black text-lg uppercase tracking-widest gap-3 shadow-xl shadow-orange-600/20 items-center justify-center flex"
          >
            {loading ? (
              <Loader2 className="w-6 h-6 animate-spin" />
            ) : (
              <>
                Ignition
                <ArrowRight className="w-5 h-5" />
              </>
            )}
          </button>
        </form>

        <div className="mt-8 text-center text-sm text-neutral-600">
          <p>Temporary E2E Mode: Use <button 
            type="button"
            onClick={handleAutofill}
            className="text-orange-500/80 font-mono hover:text-orange-400 hover:underline transition-all cursor-pointer bg-transparent border-none p-0"
          >
            testuser:Password
          </button></p>
        </div>
      </div>
    </div>
  );
};

export default Login;
