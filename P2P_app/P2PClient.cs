using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Net.WebSockets;
using System.Text;
namespace P2P_app;
public class P2PClient(string serverAddress)
{
    private readonly ClientWebSocket _socket = new();     // Экземпляр клиента WebSocket

    public async Task ConnectAsync()
    {
        await _socket.ConnectAsync(new Uri(serverAddress), CancellationToken.None);
        // Реализация логики отправки данных регистрации на сервер, если необходимо
    }
    // Отправка сообщения серверу асинхронно
    public async Task SendMessageAsync(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        await _socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }
    // Получение сообщения от сервера асинхронно
    public async Task<string?> ReceiveMessageAsync()
    {
        var buffer = new byte[1024];
        var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        return result.MessageType switch
        {
            WebSocketMessageType.Text => Encoding.UTF8.GetString(buffer, 0, result.Count),
            WebSocketMessageType.Close => null,
            _ => null
        };
    }
    // Закрытие соединения с сервером асинхронно
    public async Task CloseAsync()
    {
        await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
    }
    // Установка версии TLS, пути к сертификату и пароля для безопасного обмена данными
    public void SetTlsVersion(SslProtocols protocols, string certificatePath, string certificatePassword)
    {
        var sslOptions = new SslClientAuthenticationOptions { EnabledSslProtocols = protocols };

        // Добавление подпротокола WebSocket для безопасного обмена
        _socket.Options.AddSubProtocol("wss");

        // Принятие любого удаленного сертификата для упрощения (не рекомендуется для продакшена)
        _socket.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;

        // Загрузка сертификата из файла
        var certificate = new X509Certificate2(certificatePath, certificatePassword);

        // Добавление сертификата в параметры
        _socket.Options.ClientCertificates = new X509CertificateCollection { certificate };

        // Установка клиентских сертификатов, если они доступны
        if (_socket.Options.ClientCertificates.Count > 0) sslOptions.ClientCertificates = _socket.Options.ClientCertificates;
    }

    // Регистрация на сервере асинхронно с использованием предоставленного псевдонима
    public async Task RegisterAsync(string nickname)
    {
        var registerMessage = $"REGISTER:{nickname}";
        await SendMessageAsync(registerMessage);
    }
}