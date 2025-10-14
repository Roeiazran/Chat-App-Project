import type { ChatAction, ChatState } from "../types";

export const chatReducer = (state: ChatState, action: ChatAction): ChatState => {
    switch(action.type) {
        case "setInitialState":
            return {
                ...state,
                onChats: action.payload.onChats,
                offChats: action.payload.offChats,
                users: action.payload.users
            }
        case "removeOffChatByParticipantId":
            return { 
                ...state,
                offChats: state.offChats.filter(c => !c.participants.includes(action.payload))
            };
        case "addOnChat":
            return {
                ...state,
                onChats: [action.payload, ...state.onChats]
            };
        case "removeOnChat":
            return {
                ...state,
                onChats: state.onChats.filter(c => c.chatId !== action.payload)
            };
        case "addOffChat":
            return {
                ...state,
                offChats: [...state.offChats, action.payload]
            };
        case "setSelectedChat":
            return {
                ...state,
                selectedChat: action.payload
            }
        case "addUser":
            return {
                ...state,
                users: [...state.users, action.payload]
            };
        case "incrementUnreadCounter":
            return {
                ...state,
                onChats: state.onChats.map(c =>
                    c.chatId === action.payload ?
                    { ...c, unreadCount: c.unreadCount + 1 }
                     : c
                )
            };
        case "updateUserStatus":
            return {
                ...state,
                users: state.users.map(u => 
                    u.userId === action.payload.userId ?
                    { ...u, isOnline: action.payload.status }
                    : u
                )
            };
        case "resetUnreadCount":
            return {
                ...state,
                onChats: state.onChats.map(c => c.chatId === action.payload ?
                    { ...c, unreadCount: 0}
                     : c
                )
            };
        case "UpdateAndSortChat":
            const updated = state.onChats.map(c =>
                    c.chatId == action.payload.chatId ?
                    { ...c, lastUpdated: action.payload.date }
                     : c
                ).sort((a, b) => {
                    const timeA = a.lastUpdated ? new Date(a.lastUpdated).getTime() : 0;
                    const timeB = b.lastUpdated ? new Date(b.lastUpdated).getTime() : 0;
                    return timeB - timeA;
                });
            return {
                ...state,
                onChats: updated
            };
        case "updateChatMessages":
            return {
                ...state,
                onChats: state.onChats.map(c =>
                c.chatId === action.payload.chatId
                    ? { ...c, messages: action.payload.messages }
                    : c
                ),
            };
        case "appendMessage":
            return {
                ...state,
                onChats: state.onChats.map(c =>
                    c.chatId === action.payload.chatId
                    ? { ...c, messages: [...c.messages??  [], action.payload.message]}
                    : c
                )
            };
        case "replaceDummyChat":
            return {
                ...state,
                onChats: state.onChats.map(c =>
                    c.chatId === -1 ? action.payload : c
                )
            };
        default:
            return state;
    }
}