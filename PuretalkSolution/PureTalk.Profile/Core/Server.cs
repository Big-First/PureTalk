using Models;

namespace Core;

public class Server
{
    public Server(){}
    public Queue<UserProfile> _UserRegisters = new ();
    private Queue<UserProfile> _levelOrderQueue = new Queue<UserProfile>();
    public event EventHandler<UserProfile>? UserProfileAdded;
    
    public int AddUser(UserProfile newBlock)
    {
        _UserRegisters.Enqueue(newBlock);
        UserProfileAdded?.Invoke(this, newBlock);
        return _UserRegisters.Contains(newBlock)? StatusCodes.Status200OK: StatusCodes.Status400BadRequest;
    }
}