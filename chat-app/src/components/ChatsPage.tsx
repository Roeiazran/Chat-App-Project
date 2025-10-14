import React from "react";
import ChatsList from "./ChatsList";
import ChatBox from "./ChatBox";
import { useChatContext } from "../contexts/ChatContext";

const ChatsPage: React.FC = () => {
  const { onChats, offChats, selectedChat } = useChatContext();

  return (
    <div style={{ display: "flex", height: "90vh" }}>
      {/* ChatsList */}
      <div style={{ width: 250, borderRight: "1px solid #ccc" }}>
        <ChatsList
          chats={onChats}
        />
      </div>

      {/* ChatBox */}
      <div style={{ flex: 1, borderLeft: "1px solid #ccc" }}>
        <ChatBox
          chat={selectedChat}
        />
      </div>

      <div style={{ width: 250, borderLeft: "1px solid #ccc" }}>
        <ChatsList
          chats={offChats}
        />
      </div>

    </div>
  );
};

export default ChatsPage;
