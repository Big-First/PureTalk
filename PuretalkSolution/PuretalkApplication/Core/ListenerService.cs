using PuretalkApplication.Data;
using PuretalkApplication.Entitys;
using PuretalkApplication.Models;
using PuretalkApplication.Repositorys;

namespace PuretalkApplication.Core;

public class ListenerService : BackgroundService
{
    private readonly Server _server;
    private readonly ILogger<ListenerService> _logger;
    DBContext _db { get; set; }
    
    public ListenerService(Server server, ILogger<ListenerService> logger)
    {
        _server = server;
        _logger = logger;
        _server.UserRegisterAdded += OnBlockAdded;
    }

    private async void OnBlockAdded(object? sender, UserRegister user)
    {
        Console.WriteLine($"{nameof(OnBlockAdded)} >> {DateTime.Now}");
        if (_db == null) _db = new DBContext(Singleton.Instance().srcMongo, "UserRegister");
        var register = await new ApplicationRepository(_db).InsetObject(_db,user, CancellationToken.None);
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Serviço de monitoramento de Registros iniciado.");
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _server.UserRegisterAdded -= OnBlockAdded;
        return base.StopAsync(cancellationToken);
    }
}