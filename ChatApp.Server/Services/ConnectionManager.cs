namespace ChatApp.Server.Services;

/// <summary>
/// Mapping users to their connections using their ids.
/// </summary>
public class ConnectionManager
{
    private readonly object _lock = new();
    private readonly Dictionary<int, HashSet<string>> _userConnections = new();

    /// <summary>
    /// Add the user connection to the corresponding userId entry in the user's connections dictionary
    /// /// Uses lock to prevent race condition from multiple requests.
    /// </summary>
    public void AddConnection(int userId, string connectionId)
    {
        lock(_lock)
        {
            if (!_userConnections.ContainsKey(userId))
                _userConnections[userId] = new HashSet<string>();
            _userConnections[userId].Add(connectionId);
        }
    }

    /// <summary>
    /// Remove the user connection from the user's connections dictionary
    /// </summary>
    public void RemoveConnection(int userId, string connectionId)
    {
        lock(_lock)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                connections.Remove(connectionId);

                if (connections.Count == 0) 
                {
                    _userConnections.Remove(userId);
                }
            }
        }
    }


    /// <summary>
    /// Get all current connections for a user
    /// </summary>
    public HashSet<string> GetConnections(int userId)
    {
        // prevents other threads from modifying the dictionary
        lock (_lock)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                // return a copy to prevent external modification
                return new HashSet<string>(connections);
            }
            return new HashSet<string>();
        }   
    }

    /// <summary>
    /// Get all logged-in users ids
    /// </summary>
    public List<int> GetOnlineUsersIds() {
        lock (_lock) return _userConnections.Keys.ToList();
    }

}