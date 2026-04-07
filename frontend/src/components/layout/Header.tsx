import React from 'react';
import { Link } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';

const Header = () => {
  const { user } = useAuthStore();

  return (
    <nav className="max-w-screen-xl h-12 mx-auto flex items-center justify-between px-4">
      <div className="flex items-center">
        <Link to="/" className="flex items-center">
          <span className="text-orange-600 uppercase font-extrabold text-lg tracking-tight italic">
            bikeapelago
          </span>
        </Link>
      </div>

      {user && (
        <div className="flex items-center gap-4">
          <span className="hidden text-sm font-medium text-neutral-300 sm:block">
            {user.name || user.username}
          </span>
        </div>
      )}
    </nav>
  );
};

export default Header;
