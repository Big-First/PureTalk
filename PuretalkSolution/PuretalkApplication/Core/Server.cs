using PuretalkApplication.Models;

namespace PuretalkApplication.Core;

public class Server
{
    public Server(){}
    public Queue<UserRegister> _UserRegisters = new ();
    private Queue<UserRegister> _levelOrderQueue = new Queue<UserRegister>();
    public event EventHandler<UserRegister>? UserRegisterAdded;
    
    public int AddUser(UserRegister newBlock)
    {
        _UserRegisters.Enqueue(newBlock);
        UserRegisterAdded?.Invoke(this, newBlock);
        return _UserRegisters.Contains(newBlock)? StatusCodes.Status200OK: StatusCodes.Status400BadRequest;
    }
}