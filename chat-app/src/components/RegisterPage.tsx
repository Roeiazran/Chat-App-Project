// src/pages/RegisterPage.tsx
import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { extractErrorMessages, register } from "../services/HttpService";
import { useErrorAndLoading } from "../hooks/useErrorAndLoading";
import { useAuth } from "../contexts/AuthContext";
const RegisterPage: React.FC = () => {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [nickname, setNickname] = useState("");
  const { loading, errors, appendError, clearErrors, startLoad, finishLoad, updateErrors } = useErrorAndLoading();
  const navigate = useNavigate();
  const { isTokenValid, setToken } = useAuth();

  // validate the user password
  const isValidPassword = (password: string): boolean => {
    
    let valid = true;

    if (password.length < 8) {
      appendError("Password must be at least 8 characters long.");
      valid = false;
    } 
    if (!/[a-z]/.test(password)) 
    {
      appendError("Password must include at least one lowercase letter.");
      valid = false;
    }
    if (!/[A-Z]/.test(password)) {
      appendError("Password must include at least one uppercase letter.");
      valid = false;
    }
    if (!/\d/.test(password)) {
      appendError("Password must include at least one number.");
      valid = false;
    }
    if (/\s/.test(password)) {
      appendError("Password cannot contain spaces.");
      valid = false;
    }
    if (!/[!@#$%^&*()_+\[\]{};':"\\|,.<>\/?-]/.test(password)) {
      appendError("Password must include at least one special character.");
      valid = false;
    }
    return valid;
  }

  // if the user is logged-in navigate from this page
  useEffect(()=> {
  
    if (isTokenValid()) {
      navigate("/chats");
    }

  }, [navigate, isTokenValid]);


  // called when the user presses the register button
  const handleRegister = async (e?: React.FormEvent) => {

    e?.preventDefault();
    clearErrors();
  
    if (!username || !password) {
      appendError("Username and password are required.")
      return;
    }
    
    if (!isValidPassword(password)) return;

    try {

      startLoad();

      // make an api call to register and store the token
      const token = await register(username, password, nickname);
      
      setToken(token);

      // navigate to homepage
      navigate("/chats");
      
    } catch (err) {
      const errors: string[] = extractErrorMessages(err);
      updateErrors(errors);
    } finally {
      finishLoad();
    }
  };

  return (
    <form onSubmit={handleRegister} style={{ padding: 20 }}>
      <h1>Register</h1>

      {/* Username input */}
      <input
        placeholder="Username"
        value={username}
        onChange={(e) => setUsername(e.target.value)}
      />

      {/* Password input */}
      <input
        placeholder="Password"
        type="password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
      />

      {/* Nickname input */}
      <input
        placeholder="Nickname"
        value={nickname}
        onChange={(e) => setNickname(e.target.value)}
      />

      {/* Register button */}
      <button type="submit" disabled={loading}>
        {loading ? "Registering..." : "Register"}
      </button>

      {/* Error message */}
      {errors.map((msg, index) => <p key={index} style={{ color: "red" }}>{msg}</p>)}
    </form>
  );
};

export default RegisterPage;
