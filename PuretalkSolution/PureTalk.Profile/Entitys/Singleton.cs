namespace PureTalk.Profile.Entitys;

public class Singleton
{
    static Singleton? _instance { get; set; }
    public string srcMongo = "mongodb://mplopes:3702959@127.0.0.1:27017/";
    public static Singleton? Instance()
    {
        if (_instance == null) _instance = new Singleton();
        return _instance;
    }
}