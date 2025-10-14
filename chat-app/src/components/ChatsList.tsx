import React from "react";
import type { Chat } from "../types/index";
import { useChatContext } from "../contexts/ChatContext";
import { useAuth } from "../contexts/AuthContext";
import { updateLastVisited } from "../services/HttpService";

interface ChatsListProps {
  chats: Chat[];
}

const ChatsList: React.FC<ChatsListProps> = ({ chats }) => {
  const { getIsOnlineById, getNicknameById, dispatch } = useChatContext();
  const { getUserId } = useAuth();
  const currentUserId = getUserId();

  // called whenever logged-in user selects a chat from the list
  const handleSelectChat = async (chat: Chat) => {
    // if the selected chat has an unread count, reset it
    if (chat.chatId && chat.unreadCount) {

      // update the chat's last visited time asynchronously
      dispatch({ type: "resetUnreadCount", payload: chat.chatId });
      await updateLastVisited(chat.chatId);
    }
    dispatch({ type: "setSelectedChat", payload: chat });
  };

  return (
    <div className="chats-list">
    {chats.map((chat, index) => {
      const otherUserId = chat.participants.find(u => u !== currentUserId);

      return (
        <div
          key={chat.chatId ?? `potential-${index}`}
          onClick={() => handleSelectChat(chat)}
          className="chat-item"
        >
          <div>
            <div style={{ display: "flex", alignItems: "center", gap: "5px" }}>
              <span>
                {chat.chatName.length > 0 ? chat.chatName : getNicknameById(otherUserId)}
              </span>
              {!chat.isGroup && (
                <span
                  className="online-dot"
                  style={{ backgroundColor: getIsOnlineById(otherUserId) ? "green" : "#ccc" }}
                />
              )}
            </div>
            <div style={{ fontSize: "12px", color: "#666" }}>
              {chat.lastUpdated ? chat.lastUpdated.toLocaleString() : ""}
            </div>
          </div>

          {chat.unreadCount > 0 && (
            <span className="unread-badge">{chat.unreadCount}</span>
          )}
        </div>
      );
    })}
  </div>
  );
};

export default ChatsList;
