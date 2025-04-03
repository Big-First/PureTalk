using Data;
using Entitys;
using Models;
using PureTalk.Profile.Repositorys;

namespace Core;

public class ListenerService : BackgroundService
{
    private readonly Server _server;
    private readonly ILogger<ListenerService> _logger;
    DBContext _db { get; set; }
    
    public ListenerService(Server server, ILogger<ListenerService> logger)
    {
        _server = server;
        _logger = logger;
        _server.UserProfileAdded += OnBlockAdded;
    }

    private async void OnBlockAdded(object? sender, UserProfile user)
    {
        if (_db == null) _db = new DBContext(Singleton.Instance().srcMongo, "UserProfile");
        var register = await new ApplicationRepository(_db).InsetObject(_db,user, CancellationToken.None);
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Serviço de monitoramento de Registros iniciado.");
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _server.UserProfileAdded -= OnBlockAdded;
        return base.StopAsync(cancellationToken);
    }
}