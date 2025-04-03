namespace PuretalkApplication.Models;

public class UserRegister
{
    public UserRegister(){}
    public string id { get; set; }
    public string mail { get; set; }
    public string password { get; set; }

    public UserRegister(string id, string mail, string password)
    {
        this.id = id;
        this.mail = mail;
        this.password = password;
    }
}