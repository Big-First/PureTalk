namespace PuretalkApplication.Models;

public class UserProfile
{
    public UserProfile(){}
    public string id { get; set; }
    public string name { get; set; }
    public string image { get; set; }
    public int ddNumber { get; set; }
    public int foneNumber { get; set; }

    public UserProfile(string id, string name, string image, int ddNumber, int foneNumber)
    {
        this.id = id;
        this.name = name;
        this.image = image;
        this.ddNumber = ddNumber;
        this.foneNumber = foneNumber;
    }
}