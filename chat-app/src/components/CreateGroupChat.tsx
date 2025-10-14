import React, { useState } from "react";
import type { User, CreateChatRequest } from "../types/index";
import { useChatContext } from "../contexts/ChatContext";
import { extractErrorMessages } from "../services/HttpService";
import { useNavigate } from "react-router-dom";
import { useErrorAndLoading } from "../hooks/useErrorAndLoading";
import { useHubMethods } from "../hooks/useHubMethos";
import { useAuth } from "../contexts/AuthContext";

const CreateGroupChat: React.FC = () => {
    const [chatName, setChatName] = useState<string>("");
    const [participants, setParticipants] = useState<number[]>([]);
    const { 
        errors, 
        loading, 
        clearErrors, 
        updateErrors, 
        appendError, 
        startLoad, 
        finishLoad
    } = useErrorAndLoading();
    const { users } = useChatContext();
    const navigate = useNavigate();
    const { createNewChat } = useHubMethods();
    const { getUserId } = useAuth();
    
    // called to insert and remove participants ids from state
    const toggleParticipant = (id: number) => {
        setParticipants(prev=> 
            prev.includes(id)? prev.filter(p => p !== id) : [...prev, id] 
        );
    };

    // called when the user hits submit
    const submitRequest = async () => {

        try {
            const userId = getUserId();
            if (userId == null) return;

            // prepare the request chat
            const request: CreateChatRequest = {
                name: chatName,
                participantsIds: [...participants, userId ],
                isGroup: true,
                updatedAt: new Date().toISOString()
            };
            
            startLoad();

            // invokes "CreateChat"
            await createNewChat(request);

            clearErrors();
            navigate("/chats");
        } catch (err: any) {
            const messages: string[] = extractErrorMessages(err);
            updateErrors(messages);
        } finally {
            finishLoad();
        }
    };

    // called before submitRequest
    const validateInput = () => {

        clearErrors();

        // chat name isn't empty
        if (chatName.trim().length === 0) {
            appendError("Group name is required");
            return;
        } 
        // at least one participant in a group
        else if (participants.length < 1) {
            appendError("At least one participant must be selected")
            return;
        }

        submitRequest();
    };

    return (
        <div>
            <h2>{"Create Chat"}</h2>
            <label >
                Chat Name
            </label>
            {/* name input */}
            <input
                type="text"
                value={chatName}
                onChange={e => setChatName(e.target.value)}
                />
            {/* users check list */}
            <div>
                {users.map((u: User) => (
                    <label key={u.userId} >
                    <input
                        type="checkbox"
                        onChange={()=>toggleParticipant(u.userId)}
                        />
                    {u.nickname}
                    </label>
                    
                ))}
            </div>
            
            {/* submit bottom + error */}
            <button onClick={validateInput}>
                {loading ? "Creating..." : "Create"}
            </button>
            {errors.map((msg, index) => <p key={index} style={{ color: "red" }}>{msg}</p>)}    
        </div>
    )
}

export default CreateGroupChat;