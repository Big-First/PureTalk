using Microsoft.AspNetCore.Mvc;
using Core;
using Data;
using Entitys;
using Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddSingleton<Server>();
builder.Services.AddHostedService<ListenerService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configuração do ambiente de desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware para WebSockets
app.UseWebSockets();

// Instância do servidor
new InicializaBD().InitializeMongo(new DBContext(Singleton.Instance().srcMongo, "UserProfile"));
var server = app.Services.GetRequiredService<Server>();

// Endpoint WebSocket (Separado do HTTP)

app.MapPost("/Profile",async ([FromBody] UserProfile _user) =>
{
    return server.AddUser(_user);
});

app.Run("http://0.0.0.0:5500");