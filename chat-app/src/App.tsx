import { BrowserRouter as Router, Routes, Route, Navigate } from "react-router-dom";
import LoginPage from "./components/LoginPage";
import RegisterPage from "./components/RegisterPage";
import ChatsPage from "./components/ChatsPage";
import MessagesReport from "./components/MessagesReport";
import PrivateRoute from "./components/PrivateRoute";
import PrivateLayout from "./components/PrivateLayout";
import { ChatProvider } from "./contexts/ChatContext";
import CreateGroupChat from "./components/CreateGroupChat";
import "./style/global.css"

function App() {

  return (
    <Router>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />

        <Route 
          element={
          <PrivateRoute>
            <ChatProvider>
            <PrivateLayout />
            </ChatProvider>
          </PrivateRoute>
          }
        >
          <Route path="/chats" element={ <ChatsPage />} />
          <Route path="/reports" element={<MessagesReport />} />
          <Route path="/chat/new" element={<CreateGroupChat />} />
          <Route path="/" element={<Navigate to="/chats" replace />} />
        </Route>
      </Routes>
    </Router>
  );
}

export default App;
