import React, { useEffect, useRef, useState } from "react";
import type { Chat, CreateChatRequest, Message, SendMessageRequest } from "../types/index";
import { useChatContext } from "../contexts/ChatContext";
import { extractErrorMessages } from "../services/HttpService";
import { useErrorAndLoading } from "../hooks/useErrorAndLoading";
import { useAuth } from "../contexts/AuthContext";
import { useHubMethods } from "../hooks/useHubMethos";
import { useSignalR } from "../contexts/HubContext";

interface ChatBoxProps {
  chat: Chat | null;
}

const ChatBox: React.FC<ChatBoxProps> = ({ chat }) => {
  const [newMessage, setNewMessage] = useState("");
  const [messages, setMessages] = useState<Message[]>([]);
  const { getNicknameById, dispatch } = useChatContext();
  const { errors, updateErrors } = useErrorAndLoading();
  const messageEndRef = useRef<HTMLDivElement>(null);
  const messagesContainerRef = useRef<HTMLDivElement>(null);
  const [showScrollButton, setShowScrollButton] = useState<boolean>(false);
  const { getUserId } = useAuth();
  const { subscribe, unsubscribe, connection } = useSignalR();
  const { leaveGroup, createNewChat, sendMessage} = useHubMethods();
  const chatRef = useRef(chat);
  const isFirstScrollRef = useRef(true);
  
  useEffect(() => {

    if (messages.length > 0) {
      if (isFirstScrollRef.current) {
        requestAnimationFrame(() => scrollToBottom());
        isFirstScrollRef.current = false;
      } else {
        requestAnimationFrame(() => scrollToBottom("smooth"));
      }
    }
  }, [messages]);

  // triggered on chat select change, runs every chat selection
  useEffect(()=> {

    // update the chat reference
    chatRef.current = chat;
    
    // reset
    isFirstScrollRef.current = true;
    
    // keep the dummy message in the messages array and don't clear it
    if (messages.length == 0 || messages[0].messageId !== -1) {
      // update messages
      setMessages(chat?.messages ?? []);
    }

  }, [chat]);

  // called when the user presses the leave-group button
  const handleLeaveGroup = async () => {
    if (!chat || !chat.chatId) return;

    try {
      // SignalR method that invokes the leave process
      await leaveGroup(chat.chatId);

      // remove the chat from on-going chat list
      dispatch({ type: "removeOnChat", payload: chat.chatId });

      // clear the selected chat
      dispatch({type: "setSelectedChat", payload: null})

    } catch (err) {
      const errors: string[] = extractErrorMessages(err);
      updateErrors(errors);
    }
  };

  // optimistic update the Ui
  const applyOptimisticUi = (msgContent: string, date: string) => {
    const userId = getUserId();
    if (!userId || !chat) return;

    // dummy message
    const dummyMessage: Message = {
      chatId: -1,
      messageId: -1,
      content: msgContent,
      senderId: userId,
      sentAt: date
    };

    // set local state
    setMessages([dummyMessage]);

    // dummy chat
    const dummyChat: Chat = {
      ...chat,
      chatId: -1,
      lastUpdated: new Date(date)
    };

    // remove from offChats and add to onChats in context
    const otherUserId = chat.participants.find(p => p !== userId)!;
    dispatch({ type: "removeOffChatByParticipantId", payload: otherUserId });
    dispatch({ type: "addOnChat", payload: dummyChat });
  };

  const reverseOptimisticUi = () => {
    if (chat === null) return;
    
    chat.lastUpdated = null;
    dispatch({ type: "addOffChat", payload: chat });
    dispatch({ type: "removeOnChat", payload: -1})
    setMessages([]);
  }

  // callback called when log-in user sends a message
  const handleSendMessage = async () => {
    // if no content, or not chat selected, return
    if  (!newMessage.trim() || !chat) return;
    let chatId: number | null = null;
    // capture the current date
    const date:string = new Date().toISOString();

    // selected chat is a dummy chat with no id, prepare request
    if (!chat.chatId) {
      // prepare the request 
      const request: CreateChatRequest = {
        name: "",
        participantsIds: chat.participants,
        isGroup: false,
        updatedAt: date
      };

      try {
        // optimistically update the Ui
        applyOptimisticUi(newMessage, date);

        // invoke "CreateChat"
        chatId = await createNewChat(request);
        const updatedChat: Chat = {
          ...chat,
          chatId: chatId,
          lastUpdated: new Date(date)
        }

        dispatch({ type: "replaceDummyChat", payload: updatedChat});
        dispatch({ type: "setSelectedChat", payload: updatedChat});
      } catch (error) {
        reverseOptimisticUi();
        const errors: string[] = extractErrorMessages(error);
        updateErrors(errors);
      }
    }

    // prepare to send message
    const sendMessageRequest: SendMessageRequest = {
      chatId: (chatId || chat.chatId)!,
      content: newMessage,
      sentAt: date
    };
    try {

      // invokes "sendMessage" 
      await sendMessage(sendMessageRequest);
      setNewMessage("");

    } catch (error) {
      const errors: string[] = extractErrorMessages(error);
      updateErrors(errors);
    }
  };
  
  // called to check if the user scrolled has up enough to display the scroll button.
  const isAtBottom = (): boolean => {
    const container = messagesContainerRef.current;
    if (!container) return false;
    const threshold = 80;
    const { clientHeight, scrollHeight, scrollTop } = container;
    return (scrollHeight - clientHeight - scrollTop) < threshold;
  };

  // SignalR callback method triggered whenever new message arrives
  const handleIncomingMessage = (message: Message) => {

    if (!chatRef.current || !chatRef.current.chatId) return;

    if (message.chatId === chatRef.current.chatId) {

      setMessages(prev => {
        if (prev.length > 0 && prev[0].messageId === -1) {
          // replace dummy with real message
          return [message];
        } else {
          // append normally
          return [...prev, message];
        }
      });
    }
  };

  useEffect(()=> {

    // subscription startup function
    const setupHub = async () => {
      if (!connection) return;
      subscribe("ReceiveMessage", handleIncomingMessage);
    }

    setupHub();
    return ()=> {
      unsubscribe("ReceiveMessage", handleIncomingMessage);
    }
  }, [connection]);

  const scrollToBottom = (behavior: ScrollBehavior = "auto") => {
    messageEndRef.current?.scrollIntoView({ behavior });
  };

  const handleScroll = () => {
    const container = messagesContainerRef.current;
    if (!container) return;

    if (isAtBottom()) setShowScrollButton(false);
    else setShowScrollButton(true);
  };

  if (!chat) return <div style={{ padding: 20 }}>Select a chat or an available user to start messaging</div>;
  return (
    <div className="chat-box">
      {/* Messages area */}
      <div
        ref={messagesContainerRef}
        onScroll={handleScroll}
        className="messages-container"
      >
        {messages.map((msg) => {
          const isSent = getUserId() === msg.senderId;
          const wrapperClass = `message-wrapper ${isSent ? "message-sent" : "message-received"}`;
          {/* message */}
          return (
            <div key={msg.messageId} className={wrapperClass}>
              <div className="message-bubble">
                {chat.isGroup && !isSent
                  ? `${getNicknameById(msg.senderId)?? "You"}: ${msg.content}`
                  : msg.content }
              </div>
              <div className="message-timestamp">
                {new Date(msg.sentAt).toLocaleTimeString()}
              </div>
            </div>
          );
        })}
        <div ref={messageEndRef} style={{ height: 0 }} />
      </div>

      {/* scroll down button */}
      {showScrollButton && (
        <button className="scroll-bottom-btn" onClick={() => scrollToBottom("smooth")}>
          ↓ Scroll to bottom
        </button>
      )}
      {errors.map((msg, index) => <p key={index} style={{ color: "red" }}>{msg}</p>)}
      {/* Input area */}
      <div className="input-area">
        <input
          type="text"
          value={newMessage}
          onChange={(e) => setNewMessage(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && handleSendMessage()}
          placeholder="Type a message..."
        />
        <button onClick={handleSendMessage}>Send</button>
        {chat.isGroup && <button onClick={handleLeaveGroup}>Leave group</button>}
      </div>
    </div>

  );
};

export default ChatBox;
