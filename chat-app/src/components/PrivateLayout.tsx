import React from "react";
import { Outlet } from "react-router-dom";
import Navbar from "./NavBar";

const PrivateLayout: React.FC = () => {

  return (
    <div>
      <Navbar />
      <div>
        <Outlet />
      </div>
    </div>
  );
  
};

export default PrivateLayout;
