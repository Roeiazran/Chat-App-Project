import React, { useEffect, useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { extractErrorMessages, login } from "../services/HttpService";
import { useErrorAndLoading } from "../hooks/useErrorAndLoading";
import { useAuth } from "../contexts/AuthContext";


const LoginPage: React.FC = () => {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const { setToken, isTokenValid} = useAuth();
  const { errors, loading, clearErrors, startLoad, finishLoad, appendError} = useErrorAndLoading();
  const navigate = useNavigate();

  // if the user is logged-in navigate from this page
  useEffect(()=> {

    if (isTokenValid()) {
    navigate("/chats");
    }
  }, [isTokenValid, navigate]);

  // called when the user presses the login button
  const handleLogin = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    clearErrors();
    startLoad();

    try {

      // make an api call
      const token = await login(username, password);
      
      setToken(token);
      
      navigate("/chats");
    } catch (err: any) {
      
      const errors: string[] = extractErrorMessages(err);
      appendError(errors[0]);
    } finally {
      finishLoad();
    }
  };

  return (
    <form onSubmit={handleLogin} style={{ padding: 20 }}>
      <h1>Login</h1>

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

      {/* Register button */}
      <button type="submit" disabled={loading}>
        {loading ? "Logging..." : "Login"}
      </button>

      {/* Error message */}
      {errors.map((msg, index) => <p key={index} style={{ color: "red" }}>{msg}</p>)}
       <p style={{ marginTop: 10 }}>
        Don't have an account?{" "}
        <Link to="/register" style={{ color: "blue", textDecoration: "underline" }}>
          Register here
        </Link>
      </p>
    </form>
  );
};

export default LoginPage;
