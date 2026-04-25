import { useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { Capacitor } from '@capacitor/core';
import { App as CapApp } from '@capacitor/app';
import Layout from './components/layout/Layout';
import Home from './pages/Home';
import GameView from './pages/GameView';
import SessionSetup from './pages/SessionSetup';
import YamlCreator from './pages/YamlCreator';
import AthleteProfile from './pages/AthleteProfile';
import NewGame from './pages/NewGame';
import { Login } from './pages/Login';
import Register from './pages/Register';
import About from './pages/About';
import Settings from './pages/Settings';
import { handleUnauthorized, useAuthStore } from './store/authStore';
import { DataFetchProvider } from '@bikeapelago/shared-data-fetching';
import './App.css';

function PrivateRoute({ children }: { children: React.ReactNode }) {
  const isValid = useAuthStore((s) => s.isValid);
  return isValid ? <>{children}</> : <Navigate to="/login" replace />;
}

function App() {
  const token = useAuthStore(s => s.token);

  // On Android, the hardware back button exits the app by default when Capacitor
  // handles it. Override it to navigate within the web history stack instead.
  useEffect(() => {
    if (!Capacitor.isNativePlatform()) return;
    const listenerPromise = CapApp.addListener('backButton', ({ canGoBack }) => {
      if (canGoBack) {
        window.history.back();
      } else {
        CapApp.exitApp();
      }
    });
    return () => {
      listenerPromise.then(handle => handle.remove());
    };
  }, []);

  return (
    <Router>
      <DataFetchProvider handleUnauthorized={handleUnauthorized} token={token ?? null}>
        <Layout>
          <Routes>
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />
            <Route path="/about" element={<About />} />
            <Route path="/" element={<PrivateRoute><Home /></PrivateRoute>} />
            <Route path="/game/:id" element={<PrivateRoute><GameView /></PrivateRoute>} />
            <Route path="/new-game" element={<PrivateRoute><NewGame /></PrivateRoute>} />
            <Route path="/setup-session" element={<PrivateRoute><SessionSetup /></PrivateRoute>} />
            <Route path="/yaml-creator" element={<PrivateRoute><YamlCreator /></PrivateRoute>} />
            <Route path="/athlete" element={<PrivateRoute><AthleteProfile /></PrivateRoute>} />
            <Route path="/settings" element={<PrivateRoute><Settings /></PrivateRoute>} />
          </Routes>
        </Layout>
      </DataFetchProvider>
    </Router>
  );
}

export default App;
