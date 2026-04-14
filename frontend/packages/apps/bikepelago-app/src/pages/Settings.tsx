import React, { useState } from 'react';
import {
  User,
  Lock,
  Trash2,
  AlertTriangle,
  Check,
  Loader2,
  ShieldAlert,
  Save,
  ArrowLeft,
} from 'lucide-react';
import { useAuthStore, getToken } from '../store/authStore';
import { useNavigate } from 'react-router-dom';
import { useSessions } from '../hooks/useSessions';
import { toast } from '../store/toastStore';

const Settings = () => {
  const navigate = useNavigate();
  const { user, updateUser } = useAuthStore();
  const { deleteAllSessions } = useSessions();

  // Username State
  const [newUsername, setNewUsername] = useState(user?.username || '');
  const [isUpdatingUsername, setIsUpdatingUsername] = useState(false);
  const [usernameSuccess, setUsernameSuccess] = useState(false);

  // Password State
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [isUpdatingPassword, setIsUpdatingPassword] = useState(false);
  const [passwordError, setPasswordError] = useState('');
  const [passwordSuccess, setPasswordSuccess] = useState(false);

  // Delete Sessions State
  const [deleteStep, setDeleteStep] = useState(0); // 0: initial, 1: first confirmation, 2: final confirmation/loading
  const [isDeletingAll, setIsDeletingAll] = useState(false);

  const handleUpdateUsername = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!user?.id || !newUsername || newUsername === user.username) return;

    setIsUpdatingUsername(true);
    setUsernameSuccess(false);
    try {
      const token = getToken();
      const res = await fetch(`/api/users/${user.id}`, {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ username: newUsername }),
      });
      if (!res.ok) throw new Error('Failed to update username');

      const updatedUser = await res.json();
      updateUser(updatedUser);
      setUsernameSuccess(true);
      setTimeout(() => setUsernameSuccess(false), 3000);
    } catch {
      toast.error('Failed to update username. It might be taken.');
    } finally {
      setIsUpdatingUsername(false);
    }
  };

  const handleUpdatePassword = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!user?.id || !newPassword) return;
    if (newPassword !== confirmPassword) {
      setPasswordError('Passwords do not match');
      return;
    }

    setIsUpdatingPassword(true);
    setPasswordError('');
    setPasswordSuccess(false);
    try {
      const token = getToken();
      const res = await fetch(`/api/users/${user.id}`, {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ password: newPassword }),
      });
      if (!res.ok) throw new Error('Failed to update password');

      setPasswordSuccess(true);
      setNewPassword('');
      setConfirmPassword('');
      setTimeout(() => setPasswordSuccess(false), 3000);
    } catch {
      setPasswordError('Failed to update password.');
    } finally {
      setIsUpdatingPassword(false);
    }
  };

  const handleDeleteAll = async () => {
    if (deleteStep < 2) {
      setDeleteStep(deleteStep + 1);
      return;
    }

    setIsDeletingAll(true);
    try {
      await deleteAllSessions();
      setDeleteStep(0);
      toast.success('All sessions deleted successfully.');
    } catch {
      toast.error('Failed to delete sessions.');
      setDeleteStep(0);
    } finally {
      setIsDeletingAll(false);
    }
  };

  return (
    <div className='max-w-3xl mx-auto py-12 px-6'>
      <header className='mb-12 flex items-center justify-between'>
        <div>
          <button
            onClick={() => navigate(-1)}
            className='flex items-center gap-2 text-[var(--color-text-muted-hex)] hover:text-[var(--color-text-hex)] transition-colors mb-4 group'
          >
            <ArrowLeft className='w-4 h-4 group-hover:-translate-x-1 transition-transform' />
            Back
          </button>
          <h1 className='text-4xl font-black text-[var(--color-text-hex)] italic uppercase tracking-tighter'>
            Settings
          </h1>
          <p className='text-[var(--color-text-muted-hex)] mt-2'>
            Manage your account and data.
          </p>
        </div>
      </header>

      <div className='space-y-6'>
        {/* Username Section */}
        <section className='bg-[var(--color-surface-hex)] border border-[var(--color-border-hex)] rounded-3xl p-8 backdrop-blur-sm'>
          <div className='flex items-center gap-4 mb-8'>
            <div className='p-3 bg-orange-500/10 rounded-2xl text-orange-500'>
              <User className='w-6 h-6' />
            </div>
            <div>
              <h2 className='text-xl font-bold text-[var(--color-text-hex)]'>
                Account Identifier
              </h2>
              <p className='text-sm text-[var(--color-text-muted-hex)]'>
                How you are identified across the pelago.
              </p>
            </div>
          </div>

          <form onSubmit={handleUpdateUsername} className='space-y-4 max-w-md'>
            <div>
              <label className='block text-[10px] font-black uppercase tracking-widest text-[var(--color-text-muted-hex)] mb-2'>
                Username
              </label>
              <div className='relative'>
                <input
                  type='text'
                  value={newUsername}
                  onChange={(e) => setNewUsername(e.target.value)}
                  className='w-full bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] rounded-xl px-4 py-3 text-[var(--color-text-hex)] font-medium focus:outline-none focus:border-orange-500 transition-colors placeholder:text-[var(--color-text-subtle-hex)]'
                  placeholder='Enter new username'
                />
                {usernameSuccess && (
                  <div className='absolute right-3 top-1/2 -translate-y-1/2 text-green-500 flex items-center gap-1 text-xs font-bold animate-in fade-in slide-in-from-right-2'>
                    <Check className='w-4 h-4' />
                    Updated
                  </div>
                )}
              </div>
            </div>
            <button
              type='submit'
              disabled={isUpdatingUsername || newUsername === user?.username}
              className='btn btn-orange btn-md w-full sm:w-auto rounded-xl gap-2 disabled:opacity-50'
            >
              {isUpdatingUsername ? (
                <Loader2 className='w-4 h-4 animate-spin' />
              ) : (
                <Save className='w-4 h-4' />
              )}
              Update Username
            </button>
          </form>
        </section>

        {/* Password Section */}
        <section className='bg-[var(--color-surface-hex)] border border-[var(--color-border-hex)] rounded-3xl p-8 backdrop-blur-sm'>
          <div className='flex items-center gap-4 mb-8'>
            <div className='p-3 bg-blue-500/10 rounded-2xl text-blue-500'>
              <Lock className='w-6 h-6' />
            </div>
            <div>
              <h2 className='text-xl font-bold text-[var(--color-text-hex)]'>
                Security
              </h2>
              <p className='text-sm text-[var(--color-text-muted-hex)]'>
                Keep your account secure with a strong password.
              </p>
            </div>
          </div>

          <form onSubmit={handleUpdatePassword} className='space-y-4 max-w-md'>
            <div className='grid grid-cols-1 md:grid-cols-2 gap-4'>
              <div>
                <label className='block text-[10px] font-black uppercase tracking-widest text-[var(--color-text-muted-hex)] mb-2'>
                  New Password
                </label>
                <input
                  type='password'
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  className='w-full bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] rounded-xl px-4 py-3 text-[var(--color-text-hex)] font-medium focus:outline-none focus:border-blue-500 transition-colors placeholder:text-[var(--color-text-subtle-hex)]'
                  placeholder='••••••••'
                />
              </div>
              <div>
                <label className='block text-[10px] font-black uppercase tracking-widest text-[var(--color-text-muted-hex)] mb-2'>
                  Confirm Password
                </label>
                <input
                  type='password'
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  className='w-full bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] rounded-xl px-4 py-3 text-[var(--color-text-hex)] font-medium focus:outline-none focus:border-blue-500 transition-colors placeholder:text-[var(--color-text-subtle-hex)]'
                  placeholder='••••••••'
                />
              </div>
            </div>

            {passwordError && (
              <p className='text-red-500 text-xs font-bold'>{passwordError}</p>
            )}

            <div className='flex items-center gap-4'>
              <button
                type='submit'
                disabled={isUpdatingPassword || !newPassword}
                className='w-full sm:w-auto px-6 py-3 rounded-xl bg-[var(--color-surface-alt-hex)] border border-[var(--color-border-hex)] text-[var(--color-text-hex)] font-bold hover:border-blue-500/50 transition-all flex items-center justify-center gap-2 disabled:opacity-50'
              >
                {isUpdatingPassword ? (
                  <Loader2 className='w-4 h-4 animate-spin' />
                ) : (
                  <Check className='w-4 h-4' />
                )}
                Change Password
              </button>
              {passwordSuccess && (
                <span className='text-green-500 text-xs font-bold flex items-center gap-1 animate-in fade-in'>
                  <Check className='w-4 h-4' />
                  Password updated successfully
                </span>
              )}
            </div>
          </form>
        </section>

        {/* Danger Zone */}
        <section className='bg-red-500/5 border border-red-500/20 rounded-3xl p-8 backdrop-blur-sm mt-12'>
          <div className='flex items-center gap-4 mb-8'>
            <div className='p-3 bg-red-500/10 rounded-2xl text-red-500'>
              <ShieldAlert className='w-6 h-6' />
            </div>
            <div>
              <h2 className='text-xl font-bold text-[var(--color-text-hex)] underline decoration-red-500/30'>
                Danger Zone
              </h2>
              <p className='text-sm text-[var(--color-text-muted-hex)]'>
                Irreversible actions that affect your data.
              </p>
            </div>
          </div>

          <div className='bg-[var(--color-surface-hex)] border border-[var(--color-border-hex)] rounded-2xl p-6 flex flex-col md:flex-row items-center justify-between gap-6'>
            <div className='flex-1'>
              <h3 className='text-[var(--color-text-hex)] font-bold mb-1'>
                Delete All Game Sessions
              </h3>
              <p className='text-[var(--color-text-muted-hex)] text-sm'>
                This will permanently remove every game session, including all
                nodes, routes, and progress. This action is non-recoverable.
              </p>
            </div>

            <div className='shrink-0'>
              {deleteStep === 0 && (
                <button
                  onClick={() => setDeleteStep(1)}
                  className='px-6 py-3 bg-red-600 hover:bg-red-500 text-white font-black uppercase tracking-widest text-xs rounded-xl shadow-lg shadow-red-900/20 transition-all active:scale-95 flex items-center gap-2'
                >
                  <Trash2 className='w-4 h-4' />
                  Delete All Sessions
                </button>
              )}

              {deleteStep === 1 && (
                <div className='flex flex-col items-center gap-3 animate-in fade-in zoom-in-95 duration-200'>
                  <p className='text-red-500 text-[10px] font-black uppercase tracking-[0.2em] animate-pulse'>
                    Are you absolutely sure?
                  </p>
                  <div className='flex gap-2'>
                    <button
                      onClick={() => setDeleteStep(0)}
                      className='px-4 py-2 border border-[var(--color-border-hex)] text-[var(--color-text-muted-hex)] hover:text-[var(--color-text-hex)] rounded-lg text-xs font-bold transition-colors'
                    >
                      Cancel
                    </button>
                    <button
                      onClick={() => setDeleteStep(2)}
                      className='px-4 py-2 bg-red-600 text-white rounded-lg text-xs font-black uppercase tracking-widest hover:bg-red-700 transition-colors'
                    >
                      Yes, Proceed
                    </button>
                  </div>
                </div>
              )}

              {deleteStep === 2 && (
                <div className='flex flex-col items-center gap-3 animate-in fade-in zoom-in-95 duration-200'>
                  <div className='flex items-center gap-2 text-red-500 mb-1'>
                    <AlertTriangle className='w-4 h-4' />
                    <p className='text-[10px] font-black uppercase tracking-[0.2em]'>
                      Final Confirmation Required
                    </p>
                  </div>
                  <div className='flex gap-2'>
                    <button
                      onClick={() => setDeleteStep(0)}
                      className='px-4 py-2 border border-[var(--color-border-hex)] text-[var(--color-text-muted-hex)] hover:text-[var(--color-text-hex)] rounded-lg text-xs font-bold transition-colors'
                    >
                      Actually, Stop
                    </button>
                    <button
                      onClick={handleDeleteAll}
                      disabled={isDeletingAll}
                      className='px-4 py-2 bg-white text-red-600 rounded-lg text-xs font-black uppercase tracking-widest hover:bg-red-50 transition-colors flex items-center gap-2'
                    >
                      {isDeletingAll ? (
                        <Loader2 className='w-4 h-4 animate-spin' />
                      ) : (
                        <ShieldAlert className='w-4 h-4' />
                      )}
                      I Understand, Wipe Everything
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        </section>
      </div>
    </div>
  );
};

export default Settings;
