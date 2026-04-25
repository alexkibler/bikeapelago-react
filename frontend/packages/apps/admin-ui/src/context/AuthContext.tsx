import React, { createContext, useContext, useState } from 'react';

export interface User {
  id: string;
  userName?: string;
  username?: string;
  name?: string;
  email?: string;
  avatar?: string;
  roles: string[];
}

interface AuthContextType {
  user: User | null;
  token: string | null;
  loading: boolean;
  login: (token: string, user: unknown) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const isRecord = (value: unknown): value is Record<string, unknown> =>
  typeof value === 'object' && value !== null;

const normalizeUser = (value: unknown): User | null => {
  if (!isRecord(value) || typeof value.id !== 'string') {
    return null;
  }

  const roles = Array.isArray(value.roles)
    ? value.roles.filter((role): role is string => typeof role === 'string')
    : [];

  return {
    id: value.id,
    userName:
      typeof value.userName === 'string'
        ? value.userName
        : typeof value.username === 'string'
          ? value.username
          : undefined,
    username: typeof value.username === 'string' ? value.username : undefined,
    name: typeof value.name === 'string' ? value.name : undefined,
    email: typeof value.email === 'string' ? value.email : undefined,
    avatar: typeof value.avatar === 'string' ? value.avatar : undefined,
    roles,
  };
};

const parseStoredUser = (raw: string): User | null => {
  try {
    return normalizeUser(JSON.parse(raw) as unknown);
  } catch {
    return null;
  }
};

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [user, setUser] = useState<User | null>(() => {
    const savedUser = localStorage.getItem('admin_user');
    return savedUser ? parseStoredUser(savedUser) : null;
  });
  const [token, setToken] = useState<string | null>(
    localStorage.getItem('admin_token'),
  );
  const [loading] = useState(false);

  const login = (nextToken: string, userData: unknown) => {
    const normalizedUser = normalizeUser(userData);

    localStorage.setItem('admin_token', nextToken);
    localStorage.setItem('admin_user', JSON.stringify(userData));
    setToken(nextToken);
    setUser(normalizedUser);
  };

  const logout = () => {
    localStorage.removeItem('admin_token');
    localStorage.removeItem('admin_user');
    setToken(null);
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, token, loading, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
