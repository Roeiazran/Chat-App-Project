import { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from "react";
import { useAuth } from "./AuthContext";
import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";

interface SignalRContextType {
  connection: HubConnection | null;
  subscribe: <T>(event: string, cb: (data: T) => void) => void;
  unsubscribe: <T>(event: string, cb: (data: T) => void) => void;
}

const SignalRContext = createContext<SignalRContextType | undefined>(undefined);

export const SignalRProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const { token } = useAuth();
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const reconnectingRef = useRef(false);

  useEffect(() => {
    const init = async () => {
      if (!token || reconnectingRef.current) return;

      // lock to prevent race conditions
      reconnectingRef.current = true;

      try {
        if (connection) await connection.stop();

        const conn = new HubConnectionBuilder()
          .withUrl("http://localhost:5050/chatHub", { accessTokenFactory: () => token })
          .withAutomaticReconnect()
          .build();

        await conn.start();
        setConnection(conn);
      } catch {
        // on error, set connection to null so children unsubscribe
        setConnection(null);
      } finally {
        // free the lock
        reconnectingRef.current = false;
      }
    };

    init();
  }, [token]);

  // no valid token, stop the connection
  useEffect(() => {
    if (!token && connection) {
      connection.stop();
      setConnection(null);
    }
  }, [token, connection]);

  // generic subscribe, called with event and a callback method.
  const subscribe = useCallback(<T,>(event: string, cb: (data: T) => void) => {
    if (!connection) return;
    connection.on(event, cb);
  }, [connection]);
  
  const unsubscribe = useCallback(<T,>(event: string, cb: (data: T) => void) => {
    if (!connection) return;
    connection.off(event, cb);
  }, [connection]);

  return (
    <SignalRContext.Provider value={{ connection, subscribe, unsubscribe }}>
      {children}
    </SignalRContext.Provider>
  );
};

export const useSignalR = () => {
  const context = useContext(SignalRContext);
  if (!context) throw new Error("useSignalR must be used within SignalRProvider");
  return context;
};
