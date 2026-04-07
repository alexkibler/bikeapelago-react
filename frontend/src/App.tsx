import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Layout from './components/layout/Layout';
import Home from './pages/Home';
import GameView from './pages/GameView';
import SessionSetup from './pages/SessionSetup';
import YamlCreator from './pages/YamlCreator';
import AthleteProfile from './pages/AthleteProfile';
import NewGame from './pages/NewGame';
import Login from './pages/Login';
import './App.css';

function App() {
  return (
    <Router>
      <Layout>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/login" element={<Login />} />
          <Route path="/game/:id" element={<GameView />} />
          <Route path="/new-game" element={<NewGame />} />
          <Route path="/setup-session" element={<SessionSetup />} />
          <Route path="/yaml-creator" element={<YamlCreator />} />
          <Route path="/athlete" element={<AthleteProfile />} />
        </Routes>
      </Layout>
    </Router>
  );
}

export default App;
