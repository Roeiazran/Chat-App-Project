import { createContext, useContext, useEffect, useRef, type ReactNode, useReducer, type Dispatch } from "react";
import type { User, Chat, Message, CreateChatResponse, ChatState, ChatAction } from "../types/index";
import { extractErrorMessages, getChats, getMessages, updateLastVisited } from "../services/HttpService";
import { chatReducer } from "../reducers/ChatReducer";
import { useAuth } from "./AuthContext";
import { useSignalR } from "./HubContext";

interface ChatContextType extends ChatState {
  dispatch: Dispatch<ChatAction>;
  getNicknameById: (id:number | undefined) => string | undefined;
  getIsOnlineById: (id: number | undefined) => boolean | undefined;
}

const initialState: ChatState = {
  onChats: [],
  offChats: [],
  selectedChat: null,
  users: []
};

const ChatContext = createContext<ChatContextType | undefined>(undefined);

export const ChatProvider = ({ children }: { children: ReactNode }) => {
  const [state, dispatch] = useReducer(chatReducer, initialState);
  const selectedChatRef = useRef<Chat | null>(state.selectedChat);
  const { getUserId } = useAuth();
  const { subscribe, unsubscribe, connection } = useSignalR();

  const getNicknameById = (id: number | undefined) => {
    if (id === undefined) return undefined;
    return state.users.find((u: User) => u.userId === id)?.nickname;
  };

  const getIsOnlineById = (id: number | undefined) => {
    if (id === undefined) return undefined;
    return state.users.find((u: User) => u.userId === id)?.isOnline;
  };

  // SignalR callback triggered whenever a new chat is created
  const handleNewChatCreated = (data: CreateChatResponse) => {
    const { chat, creatorId } = data;
    const userId = getUserId();
    if (!userId) return null;
    const newChat: Chat = {
        ...chat,
        lastUpdated: new Date(chat.lastUpdated!)
    }
    
    if (!chat.isGroup) { // private chat

      // remove the chat from the off list by the the other user id
      const otherUserId = chat.participants.find(p => p !== userId)!;
      dispatch({ type: "removeOffChatByParticipantId", payload: otherUserId });
    } else { // group chat

      // for the creator, select the chat
      if (userId === creatorId) { 
        dispatch({ type: "setSelectedChat", payload: newChat });
      }
    }
    
    // for everyone add the group to on going chats list
    dispatch({ type: "addOnChat", payload: newChat });
  };

  // SignalR callback method triggered whenever a new user registers in the app
  const handleNewRegister = (user: User) => {

    const userId = getUserId();
    if (!userId) return;

    // prepare the new chat to insert into the off-going chats
    const chat: Chat = {
      chatId: null,
      chatName: "",
      lastUpdated: null,
      participants: [user.userId, userId],
      isGroup: false,
      unreadCount: 0
    };

    // update the off chats of the new register
    dispatch({ type: "addOffChat", payload: chat });

    // insert new user to the users list
    dispatch( {type: "addUser", payload: user });
  };

  // SignalR callback triggered whenever new message arrives
  const handleIncomingMessage = async (message: Message) => {
    
    // append the message to the chat's message list
    dispatch({ type: "appendMessage", payload: {chatId: message.chatId, message}});

    // if the chat is new, no need to update the date or sort the chats
    if (selectedChatRef.current?.lastUpdated?.getTime() !== new Date(message.sentAt).getTime()) {
      // update chat's last updated date and re-order the chats
      dispatch({ type: "UpdateAndSortChat", payload : { chatId: message.chatId, date: new Date(message.sentAt) }});
    }

    if (selectedChatRef.current?.chatId !== message.chatId) {
      // increment the unread counter
      dispatch({ type: "incrementUnreadCounter", payload: message.chatId });
    }

    if (selectedChatRef.current?.chatId === message.chatId) {
      // update the chat last visited date, for unread count correctness
      await updateLastVisited(message.chatId);
    }

  };

  // SignalR callback triggered whenever user changes it's online status
  const handleUserOnlineStatusChange = (user:User) => {
    dispatch({ type: "updateUserStatus", 
      payload: {userId: user.userId, status: user.isOnline }});
  };

    // init ref
  useEffect(() => {
    selectedChatRef.current = state.selectedChat;
  }, [state.selectedChat]);

  // fired once, on mount
  useEffect(() => {

    // called to fetch the app's initial state
    const fetchDashboard = async () => {
      
      try {
        // make api call and fetch data
        const data = await getChats();
        let { onGoingChats, offGoingChats, users } = data;

        // convert time to readable string
        onGoingChats = onGoingChats.map((c: Chat) => {
          return { ...c, lastUpdated: new Date(c.lastUpdated!)} 
        });
  
        // convert null last-updates date to empty string
        offGoingChats = offGoingChats.map((c: Chat) => {
          return { ...c, lastUpdated: null}
        });
        
        // set initial state
        dispatch({ type: "setInitialState", 
          payload: { 
            onChats: onGoingChats, 
            offChats: offGoingChats, 
            users
        }});

        // fetch each chat's messages asynchronously 
        onGoingChats.forEach(async (chat: Chat) => {
          try {
            const messages = await getMessages(chat.chatId!);
            
            dispatch({
              type: "updateChatMessages",
              payload: { chatId: chat.chatId!, messages },
            });

          } catch (err) {
            console.warn(`Could not fetch messages for chat ${chat.chatId}`, err);
          }
        });

      } catch(err) {
        const errors: string[] = extractErrorMessages(err);
        console.error(errors.join(", "));
      }
    };

    fetchDashboard();
  }, []);

  // Subscribing on mount
  useEffect(() => {
      
      const setupHub = async () => {
        if (!connection) return;
        subscribe("NewChatCreated", handleNewChatCreated);
        subscribe("ReceiveMessage", handleIncomingMessage);
        subscribe("UserRegister", handleNewRegister);
        subscribe("UserOnlineStatusChanged", handleUserOnlineStatusChange);
      };
  
      setupHub();
      return () => {
        unsubscribe("NewChatCreated", handleNewChatCreated);
        unsubscribe("ReceiveMessage", handleIncomingMessage);
        unsubscribe("UserRegister", handleNewRegister);
        unsubscribe("UserOnlineStatusChanged", handleUserOnlineStatusChange);
      };
  }, [connection]);

  return (
    <ChatContext.Provider value={{ 
      ...state,
      dispatch,
      getNicknameById,
      getIsOnlineById
      }}>
      {children}
    </ChatContext.Provider>
  );
};

export const useChatContext = () => {
  const context = useContext(ChatContext);
  if (!context) throw new Error("useChatContext must be used inside ChatProvider");
  return context;
};
