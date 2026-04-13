import { Link, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';
import { LogOut } from 'lucide-react';
import ThemeToggle from './ThemeToggle';

const Header = () => {
  const navigate = useNavigate();
  const { user, logout } = useAuthStore();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

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
        {user && (
          <button
            onClick={handleLogout}
            className="p-2 rounded-lg transition-all duration-200 hover:bg-[var(--color-error-hex)]/10 text-[var(--color-text-muted-hex)] hover:text-[var(--color-error-hex)]"
            aria-label="Logout"
            title="Logout"
          >
            <LogOut className="w-5 h-5" />
          </button>
        )}
      </div>
    </nav>
  );
};

export default Header;
