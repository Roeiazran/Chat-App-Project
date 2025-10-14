import { useSignalR } from "../contexts/HubContext";
import type { SendMessageRequest, CreateChatRequest} from "../types/index";
export const useHubMethods = () => {
  const { connection } = useSignalR();

  // called from chat box whenever a user presses the Leave group button
  const leaveGroup = async (chatId: number) => {
    if (!connection) throw new Error("Hub connection not initialized.");
    await connection.invoke("LeaveGroup", { chatId });
  };

  // called from chat box whenever a user sends new message
  const sendMessage = async (request: SendMessageRequest) => {
    if (!connection) throw new Error("Hub connection not initialized.");
    await connection.invoke("SendMessage", request);
  };

  // called from create chat page, whenever user submits a request to create group chat
  const createNewChat = async (request: CreateChatRequest): Promise<number> => {
    if (!connection) throw new Error("Hub connection not initialized.");
    return await connection.invoke<number>("CreateChat", request);
  };

  return { leaveGroup, sendMessage, createNewChat };
};