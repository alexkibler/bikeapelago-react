import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Layout from './components/layout/Layout';
import Home from './pages/Home';
import GameView from './pages/GameView';
import SessionSetup from './pages/SessionSetup';
import YamlCreator from './pages/YamlCreator';
import AthleteProfile from './pages/AthleteProfile';
import NewGame from './pages/NewGame';
import { Login } from './pages/Login';
import Register from './pages/Register';
import Settings from './pages/Settings';
import { handleUnauthorized, useAuthStore } from './store/authStore';
import { DataFetchProvider } from '@bikeapelago/shared-data-fetching';
import './App.css';

function PrivateRoute({ children }: { children: React.ReactNode }) {
  const isValid = useAuthStore((s) => s.isValid);
  return isValid ? <>{children}</> : <Navigate to="/login" replace />;
}

function App() {
  const token = useAuthStore(s => s.token)

  return (
    <Router>
      <DataFetchProvider handleUnauthorized={handleUnauthorized} token={token ?? null}>
        <Layout>
          <Routes>
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />
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
