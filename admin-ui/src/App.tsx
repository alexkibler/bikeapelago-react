import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import { ProtectedRoute } from './components/layout/ProtectedRoute';
import { DashboardLayout } from './components/layout/DashboardLayout';
import { Login } from './pages/Login';
import { DataExplorer } from './pages/DataExplorer';
import { AnalyticsDashboard } from './pages/AnalyticsDashboard';
import { UserManagement } from './pages/UserManagement';

function App() {
  return (
    <Router>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<Login />} />
          
          <Route path="/" element={
            <ProtectedRoute>
              <DashboardLayout />
            </ProtectedRoute>
          }>
            <Route index element={<Navigate to="/analytics" replace />} />
            <Route path="analytics" element={<AnalyticsDashboard />} />
            <Route path="explorer" element={<DataExplorer />} />
            <Route path="users" element={<UserManagement />} />
            <Route path="*" element={<div className="text-zinc-500 font-bold p-12 text-center uppercase tracking-widest bg-zinc-900/10 border border-dashed border-zinc-800 rounded-3xl">404 - Page Not Found</div>} />
          </Route>
        </Routes>
      </AuthProvider>
    </Router>
  );
}

export default App;
