import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App";
import { AuthProvider } from "./contexts/AuthContext";
import { SignalRProvider } from "./contexts/HubContext";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <AuthProvider>
      <SignalRProvider>
      <App />
      </SignalRProvider>
    </AuthProvider>
  </React.StrictMode>
);
