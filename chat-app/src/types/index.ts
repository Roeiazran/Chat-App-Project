// src/types/index.ts

export type ChatAction =
  | { type: "setInitialState"; payload: { onChats: Chat[], offChats: Chat[], users: User[] } }
  | { type: "removeOffChatByParticipantId"; payload: number }
  | { type: "addOnChat"; payload: Chat }
  | { type: "removeOnChat"; payload: number }
  | { type: "addOffChat"; payload: Chat }
  | { type: "setSelectedChat"; payload: Chat | null }
  | { type: "addUser"; payload: User }
  | { type: "incrementUnreadCounter"; payload: number }
  | { type: "resetUnreadCount"; payload: number }
  | { type: "updateUserStatus"; payload: { userId: number, status: boolean }}
  | { type: "UpdateAndSortChat"; payload: { chatId: number, date: Date }}
  | { type: "updateChatMessages"; payload: {chatId: number, messages: Message[] }}
  | { type: "appendMessage"; payload: { chatId: number, message: Message }}
  | { type: "replaceDummyChat"; payload: Chat }


export interface ChatState {
  onChats: Chat[];
  offChats: Chat[];
  selectedChat: Chat | null;
  users: User[];
}

export interface Message {
  chatId: number;
  messageId: number;
  senderId: number;
  content: string;
  sentAt: string;
}

export interface AllUsersResponse {
  users: User[];
  userId: number;
}

export interface HourReports {
  hour: number;
  avgMessageLength: number;
}

export interface User {
  userId: number;
  nickname: string;
  isOnline: boolean;
}

export interface Chat {
  chatId: number | null;
  chatName: string;
  lastUpdated: Date | null;
  participants: number[];
  isGroup: boolean;
  unreadCount: number;
  messages?: Message[]
}

export interface SendMessageRequest {
  chatId: number;
  content: string;
  sentAt: string;
}

export interface CreateChatResponse {
  chat: Chat;
  creatorId: number;
}
export interface CreateChatRequest {
  name: string;
  participantsIds: number[];
  isGroup: boolean;
  updatedAt: string;
}

export interface UserLeftGroupNotification {
  chatId: number;
  userId: number;
}

export interface GetChatsReturnType {
  onGoingChats: Chat[];
  offGoingChats: Chat[];
  users: User[];
}

export interface TokenPayload {
  userId: number;
  nickname: string;
  exp: number;
}