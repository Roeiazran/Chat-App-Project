import api from "../api/api";
import type { 
  Message, 
  HourReports,
  GetChatsReturnType
} from "../types/index";

interface ErrorType {
  message?: string;
  errors?: string[];
}

export const extractErrorMessages = (err: any): string[] => {
  const defaultMessage = "Something went wrong. Please try again.";

  // axios response object
  const response = err.response;
  const errorData: ErrorType | undefined = response?.data;

  // no response (network error, timeout, etc.)
  if (!response || !errorData) {
    return [defaultMessage];
  }

  // if backend sent validation errors array
  if (errorData.errors && errorData.errors.length > 0) {
    return errorData.errors;
  }

  // if backend sent single message
  if (errorData.message) {
    return [errorData.message];
  }

  // handle based on status code
  switch (response.status) {
    case 400:
      return ["Bad request. Please check your input."];
    case 401:
      return ["Unauthorized. Please log in again."];
    case 403:
      return ["Forbidden. You don’t have permission."];
    case 404:
      return ["Resource not found."];
    case 500:
      return ["Server error. Please try again later."];
    default:
      return [defaultMessage];
  }
};

export const register = async (username: string, password: string, nickname: string): Promise<string> => {
  const res = await api.post("/user/register", { username, password, nickname });
  return res.data.token;
};

export const login = async (username: string, password: string): Promise<string> => {
  const res = await api.post("/user/login", { username, password });
  return res.data.token;
};

export const getChats = async (): Promise<GetChatsReturnType> => {
  const res = await api.get<GetChatsReturnType>("/chat/");
  return res.data;
};

export const getMessages = async (chatId: number): Promise<Message[]> => {
  const res = await api.get(`/chat/${chatId}/messages`);
  return res.data.messages;
};

export const getMessagesReport = async (date: string): Promise<HourReports[]> => {
  const res = await api.get(`/chat/report?date=${date}`);
  console.log(res.data.reports);
  return res.data.reports;
};

export const refreshToken = async (): Promise<string> => {
  const res = await api.post("/user/refresh");
  return res.data.token;
}

export const updateLastVisited = async (chatId: number) => {
  try {
    await api.post(`/chat/${chatId}/updateLastVisited?date=${new Date().toISOString()}`);
  } catch (error) {
    console.error(error); 
  }
};