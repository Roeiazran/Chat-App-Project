import React from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";

type PrivateRouteProps = {
  children: React.ReactNode;
};

const PrivateRoute: React.FC<PrivateRouteProps> = ({ children }) => {
  const { isTokenValid } = useAuth();

  if (!isTokenValid()) {
     return <Navigate to="/login" />;
  }

  return <>{children}</>;
};

export default PrivateRoute;
