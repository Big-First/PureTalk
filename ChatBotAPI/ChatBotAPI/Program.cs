using System.Net.WebSockets;
using System.Text;
using ChatBotAPI.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ChatBotService>();
var app = builder.Build();

app.UseWebSockets();
app.Map("/chat", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    var chatService = context.RequestServices.GetRequiredService<ChatBotService>();
    await ChatHandler(webSocket, chatService);
});

app.Run();

async Task ChatHandler(WebSocket webSocket, ChatBotService chatService)
{
    var buffer = new byte[4096];

    while (webSocket.State == WebSocketState.Open)
    {
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        if (result.MessageType == WebSocketMessageType.Close)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Conexão fechada", CancellationToken.None);
            return;
        }

        if (result.MessageType == WebSocketMessageType.Text)
        {
            var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
            string responseMessage;
            try
            {
                responseMessage = chatService.GetResponse(receivedMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao gerar resposta: {ex}");
                responseMessage = "Ocorreu um erro ao processar a mensagem.";
            }

            var responseBytes = Encoding.UTF8.GetBytes(responseMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}