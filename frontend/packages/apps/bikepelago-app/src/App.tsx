import { useEffect } from 'react';

import { App as CapApp } from '@capacitor/app';
import { Capacitor } from '@capacitor/core';
import {
  Navigate,
  Route,
  BrowserRouter as Router,
  Routes,
} from 'react-router-dom';

import { DataFetchProvider } from '@bikeapelago/shared-data-fetching';

import './App.css';
import Layout from './components/layout/Layout';
import { useFitFileOpen } from './hooks/useFitFileOpen';
import { resetStores } from './lib/resetStores';
import About from './pages/About';
import AthleteProfile from './pages/AthleteProfile';
import FitImport from './pages/FitImport';
import GameView from './pages/GameView';
import Home from './pages/Home';
import { Login } from './pages/Login';
import NewGame from './pages/NewGame';
import Register from './pages/Register';
import SessionSetup from './pages/SessionSetup';
import Settings from './pages/Settings';
import YamlCreator from './pages/YamlCreator';
import { handleUnauthorized, useAuthStore } from './store/authStore';

function PrivateRoute({ children }: { children: React.ReactNode }) {
  const isValid = useAuthStore((s) => s.isValid);
  return isValid ? <>{children}</> : <Navigate to='/login' replace />;
}

// Rendered inside <Router> so hooks that call useNavigate (useFitFileOpen) work correctly.
function AppRoutes() {
  useFitFileOpen();

  return (
    <Routes>
      <Route path='/login' element={<Login />} />
      <Route path='/register' element={<Register />} />
      <Route path='/about' element={<About />} />
      {/* Public — page handles its own auth guard to preserve the pending file */}
      <Route path='/fit-import' element={<FitImport />} />
      <Route
        path='/'
        element={
          <PrivateRoute>
            <Home />
          </PrivateRoute>
        }
      />
      <Route
        path='/game/:id'
        element={
          <PrivateRoute>
            <GameView />
          </PrivateRoute>
        }
      />
      <Route
        path='/new-game'
        element={
          <PrivateRoute>
            <NewGame />
          </PrivateRoute>
        }
      />
      <Route
        path='/setup-session'
        element={
          <PrivateRoute>
            <SessionSetup />
          </PrivateRoute>
        }
      />
      <Route
        path='/yaml-creator'
        element={
          <PrivateRoute>
            <YamlCreator />
          </PrivateRoute>
        }
      />
      <Route
        path='/athlete'
        element={
          <PrivateRoute>
            <AthleteProfile />
          </PrivateRoute>
        }
      />
      <Route
        path='/settings'
        element={
          <PrivateRoute>
            <Settings />
          </PrivateRoute>
        }
      />
    </Routes>
  );
}

function App() {
  const token = useAuthStore((s) => s.token);

  // On Android, the hardware back button exits the app by default when Capacitor
  // handles it. Override it to navigate within the web history stack instead.
  useEffect(() => {
    if (!Capacitor.isNativePlatform()) return;
    const listenerPromise = CapApp.addListener(
      'backButton',
      ({ canGoBack }) => {
        if (canGoBack) {
          window.history.back();
        } else {
          void CapApp.exitApp();
        }
      },
    );
    return () => {
      void listenerPromise.then((handle) => handle.remove());
    };
  }, []);

  return (
    <Router>
      <DataFetchProvider
        handleUnauthorized={() => {
          resetStores();
          handleUnauthorized();
        }}
        token={token ?? null}
        baseUrl={import.meta.env.VITE_PUBLIC_API_URL as string | undefined}
      >
        <Layout>
          <AppRoutes />
        </Layout>
      </DataFetchProvider>
    </Router>
  );
}

export default App;
