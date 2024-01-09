using System.Net.WebSockets;
using System.Text;
namespace P2P_app;
public class P2PClient
{
    private readonly ClientWebSocket socket;
    private readonly string serverAddress;

    public P2PClient(string serverAddress)
    {
        this.serverAddress = serverAddress;
        this.socket = new ClientWebSocket();
    }

    public async Task ConnectAsync()
    {
        await socket.ConnectAsync(new Uri(serverAddress), CancellationToken.None);
        // Реализуйте логику отправки регистрационных данных на сервер
    }

    public async Task SendMessageAsync(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task<string> ReceiveMessageAsync()
    {
        var buffer = new byte[1024];
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        if (result.MessageType == WebSocketMessageType.Text)
        {
            return Encoding.UTF8.GetString(buffer, 0, result.Count);
        }
        else if (result.MessageType == WebSocketMessageType.Close)
        {
            // Если получено закрытие соединения, вернуть null или пустую строку, в зависимости от вашей логики
            return null;
        }
        else
        {
            // Обработка других типов сообщений, если необходимо
            return null;
        }
    }

    public async Task CloseAsync()
    {
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
    }
    public async Task RegisterAsync(string nickname)
    {
        var registerMessage = $"REGISTER:{nickname}";
        await SendMessageAsync(registerMessage);
    }
}