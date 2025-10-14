import React, { createContext, useCallback, useContext, useEffect, useState, type ReactNode} from "react";
import { jwtDecode } from "jwt-decode";
import type { TokenPayload } from "../types/index";
import { refreshToken } from "../services/HttpService";

type AuthContextType = {
    token: string | null;
    setToken: (token: string) => void;
    clearToken: () => void;
    isTokenValid: () => boolean;
    getUserId: () => number | null;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
    const [token, setTokenState] = useState<string | null> (() => localStorage.getItem("token"));

    const setToken = (token: string) => {
        localStorage.setItem("token", token);
        setTokenState(token);
    };

    const clearToken = () => {
        localStorage.removeItem("token");
        setTokenState(null);
    }

    // get the token decoded payload
    const getTokenPayload = useCallback((): TokenPayload | null => {

        if (!token) return null;

        let decoded;
        try {
            decoded = jwtDecode<TokenPayload>(token);
        } catch (err) {
            return null;
        }

        if (!decoded.userId || !decoded.nickname || !decoded.exp) {
            return null;
        }

        return { userId: Number(decoded.userId), nickname: decoded.nickname, exp: decoded.exp };
    }, [token])

    // check if the token expired
    const isTokenValid = useCallback((): boolean => {

        const tokenPayload = getTokenPayload();
        if (!tokenPayload) return false;

        if (new Date(tokenPayload.exp * 1000) >= new Date()) {
            return true;
        }
        return false;

    }, [getTokenPayload]);

    // get the user id from the token
    const getUserId = useCallback((): number | null => {
        const tokenPayload = getTokenPayload();
        if (!tokenPayload) return null;

        return tokenPayload.userId;
    }, [token, getTokenPayload]);

    
    useEffect(() => {
        if (!token) return;

        const payload = getTokenPayload();
        if (!payload) return;
        
        // subtract from expiration in ms the current date and 1 minute in ms
        let msUntilExpiry = payload.exp * 1000 - Date.now() - 1 * 60 * 1000;
        msUntilExpiry = Math.max(msUntilExpiry, 0);

        // set timer to refresh the token
        const timeout = setTimeout(async () => {
            try {
                const newToken = await refreshToken();
                // trigger the next token refresh
                if (newToken) setToken(newToken);
            }  catch {
                clearToken();
            }
        }, msUntilExpiry);
        
        // clear the timer for the next refresh and for when the user logs out
        return () => clearTimeout(timeout);

    }, [token, getTokenPayload, setToken, clearToken]);

    return (
        <AuthContext.Provider value={{ token, setToken, clearToken, isTokenValid, getUserId }}>
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = () => {
    const context = useContext(AuthContext);
    if (!context) throw new Error("useAuth must be used within AuthProvider");
    return context;
};