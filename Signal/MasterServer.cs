using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;
using System.Data.SQLite;

class MyServer : WebSocketBehavior
{
    private static List<MyServer> clients = new List<MyServer>();
    private static Dictionary<string, MyServer> userDictionary = new Dictionary<string, MyServer>();
    private static SQLiteConnection dbConnection;

    static MyServer()
    {
        // Инициализация подключения к базе данных SQLite
        dbConnection = new SQLiteConnection("Data Source=P2PDatabase.db;Version=3;");
        dbConnection.Open();

        // Создание таблицы для пользователей, если её нет
        using (var cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Users (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserName TEXT UNIQUE);", dbConnection))
        {
            cmd.ExecuteNonQuery();
        }

        // Создание таблицы для сообщений, если её нет
        using (var cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Messages (Id INTEGER PRIMARY KEY AUTOINCREMENT, Sender TEXT, Recipient TEXT, Content TEXT, Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP);", dbConnection))
        {
            cmd.ExecuteNonQuery();
        }
    }

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

            // Сохранение сообщения в базу данных
            SaveMessage(sender, recipient, content);
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

    private void SaveMessage(string sender, string recipient, string content)
    {
        using (var cmd = new SQLiteCommand("INSERT INTO Messages (Sender, Recipient, Content, Timestamp) VALUES (@Sender, @Recipient, @Content, CONVERT_TZ(@Timestamp, '+00:00', '+03:00'));", dbConnection))
        {
            cmd.Parameters.AddWithValue("@Sender", sender);
            cmd.Parameters.AddWithValue("@Recipient", recipient);
            cmd.Parameters.AddWithValue("@Content", content);
            cmd.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow);

            cmd.ExecuteNonQuery();
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