import React from "react";
import { Link, useNavigate } from "react-router-dom";
import { useChatContext } from "../contexts/ChatContext"; 

import { useAuth } from "../contexts/AuthContext";

const NavBar: React.FC = () => {
  const navigate = useNavigate();
  const { dispatch } = useChatContext();
  const { clearToken } = useAuth();
  
  // called when the user the presses logout button
  const handleLogout = () => {

    // clear state 
    dispatch({type: "setInitialState", payload: { onChats: [], offChats: [], users: [] }});
    dispatch({type: "setSelectedChat", payload: null });

    // navigate and remove token
    clearToken();
    navigate("/login");
  };


  return (
    <nav
      style={{
        display: "flex",
        gap: 10,
        padding: 10,
        borderBottom: "1px solid #ccc",
      }}
    >
      <Link to="/chats">Chats</Link>
      <Link to="/reports">Reports</Link>
      <Link to="/chat/new">New chat</Link>
      <button onClick={handleLogout} style={{ marginLeft: "auto" }}>
        Logout
      </button>
    </nav>
  );
};

export default NavBar;
