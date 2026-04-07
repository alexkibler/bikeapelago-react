import React from 'react';
import { Link } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';
import ThemeToggle from './ThemeToggle';

const Header = () => {
  const { user } = useAuthStore();

  return (
    <nav className="max-w-screen-xl h-12 mx-auto flex items-center justify-between px-6">
      <div className="flex items-center">
        <Link to="/" className="flex items-center">
          <span className="text-[var(--color-primary-hex)] uppercase font-extrabold text-lg tracking-tight italic">
            bikeapelago
          </span>
        </Link>
      </div>

      <div className="flex items-center gap-4">
        {user && (
          <span className="hidden text-sm font-medium text-[var(--color-text-muted-hex)] sm:block">
            {user.name || user.username}
          </span>
        )}
        <ThemeToggle />
      </div>
    </nav>
  );
};

export default Header;
