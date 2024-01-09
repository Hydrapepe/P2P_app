using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

class MyServer : WebSocketBehavior
{
    private static List<MyServer> clients = new List<MyServer>();
    private static Dictionary<string, MyServer> userDictionary = new Dictionary<string, MyServer>();
    protected override void OnOpen()
    {
        base.OnOpen();
        clients.Add(this);

        Console.WriteLine("Добро пожаловать в чат!");
    }

protected override void OnMessage(MessageEventArgs e)
{
    var message = e.Data;

    if (message.StartsWith("REGISTER:"))
    {
        var userName = message.Substring("REGISTER:".Length);
        userDictionary[userName] = this;
        UpdateUserList();
    }
    else if (message.StartsWith("PRIVATE:"))
    {
        var parts = message.Substring("PRIVATE:".Length).Split(':');
        var sender = parts[0];
        var recipient = parts[1];
        var content = parts[2];

        if (userDictionary.TryGetValue(recipient, out var recipientClient))
        {
            recipientClient.Send($"{sender} (приватно): {content}");
        }
    }
    else if (message.StartsWith("FILE:"))
    {
        var parts = message.Substring("FILE:".Length).Split(':');
        var sender = parts[0];
        var recipient = parts[1];
        var fileName = parts[2];
        var fileContent = parts[3];

        if (userDictionary.TryGetValue(recipient, out var recipientClient))
        {
            // Отправляем файл получателю
            recipientClient.Send($"FILE:{sender}:{recipient}:{fileName}:{fileContent}");
        }
    }
}

    private void UpdateUserList()
    {
        var userList = string.Join(";", userDictionary.Keys);
        foreach (var client in clients)
        {
            client.Send("USERLIST:" + userList);
        }
    }

    protected override void OnClose(CloseEventArgs e)
    {
        base.OnClose(e);
        clients.Remove(this);

        var userName = userDictionary.FirstOrDefault(x => x.Value == this).Key;
        if (userName != null)
        {
            userDictionary.Remove(userName);
            UpdateUserList();
        }

        Console.WriteLine($"Пользователь {ID} вышел из чата");
    }
}

internal abstract class MasterServer
{
    private static void Main()
    {
        var wssv = new WebSocketServer("wss://127.0.0.1:7777")
        {
            SslConfiguration = new ServerSslConfiguration(
                new X509Certificate2("certificate.pfx", "123"),
                true,
                SslProtocols.Tls12,
                false
            )
        };
        wssv.AddWebSocketService<MyServer>("/myServer");
        wssv.Start();
        Console.WriteLine("WebSocket Server Started");
        Console.ReadKey();
        wssv.Stop();
    }
}