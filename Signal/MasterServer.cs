using System.Data.SQLite;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using WebSocketSharp.Net;
using WebSocketSharp.Server;
namespace Signal;
internal class MyServer : WebSocketBehavior
{
    private static readonly List<MyServer> Clients = new List<MyServer>();
    private static readonly Dictionary<string, MyServer> UserDictionary = new Dictionary<string, MyServer>();
    private static readonly SQLiteConnection DbConnection;

    static MyServer()
    {
        // Инициализация подключения к базе данных SQLite
        DbConnection = new SQLiteConnection("Data Source=P2PDatabase.db;Version=3;");
        DbConnection.Open();

        // Создание таблицы для пользователей, если её нет
        using (var cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Users (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserName TEXT UNIQUE);", DbConnection))
        {
            cmd.ExecuteNonQuery();
        }

        // Создание таблицы для сообщений, если её нет
        using (var cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Messages (Id INTEGER PRIMARY KEY AUTOINCREMENT, Sender TEXT, Recipient TEXT, Content TEXT, Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP);", DbConnection))
        {
            cmd.ExecuteNonQuery();
        }
    }
    protected override void OnOpen()
    {
        base.OnOpen();
        Clients.Add(this);
        Console.WriteLine("Добро пожаловать в чат!");
    }
    
    protected override void OnMessage(MessageEventArgs e)
    {
        var message = e.Data;
        if (message.StartsWith("REGISTER:"))
        {
            // Регистрация пользователя
            var userName = message["REGISTER:".Length..];
            UserDictionary[userName] = this;
            UpdateUserList();
        }
        else if (message.StartsWith("PRIVATE:"))
        {
            // Обработка приватного сообщения
            var parts = message["PRIVATE:".Length..].Split(':');
            var sender = parts[0];
            var recipient = parts[1];
            var content = parts[2];
            // Отправка приватного сообщения получателю
            if (UserDictionary.TryGetValue(recipient, out var recipientClient)) recipientClient.Send($"{sender} (приватно): {content}");
            // Сохранение сообщения в базу данных
            SaveMessage(sender, recipient, content);
        }
        else if (message.StartsWith("FILE:"))
        {
            // Обработка файла
            var parts = message["FILE:".Length..].Split(':');
            var sender = parts[0];
            var recipient = parts[1];
            var fileName = parts[2];
            var fileContent = parts[3];
            // Отправка файла получателю
            if (UserDictionary.TryGetValue(recipient, out var recipientClient)) recipientClient.Send($"FILE:{sender}:{recipient}:{fileName}:{fileContent}");
        }
    }
    private static void SaveMessage(string sender, string recipient, string content)
    {
        using var cmd = new SQLiteCommand("INSERT INTO Messages (Sender, Recipient, Content) VALUES (@Sender, @Recipient, @Content);", DbConnection);
        cmd.Parameters.AddWithValue("@Sender", sender);
        cmd.Parameters.AddWithValue("@Recipient", recipient);
        cmd.Parameters.AddWithValue("@Content", content);

        cmd.ExecuteNonQuery();
    }
    private static void UpdateUserList()
    {
        // Обновление списка пользователей для всех клиентов
        var userList = string.Join(";", UserDictionary.Keys);
        foreach (var client in Clients)
        {
            client.Send("USERLIST:" + userList);
        }
    }

    protected override void OnClose(CloseEventArgs e)
    {
        base.OnClose(e);
        Clients.Remove(this);
        // Обработка закрытия соединения (выход пользователя)
        var userName = UserDictionary.FirstOrDefault(x => x.Value == this).Key;
        if (userName != null)
        {
            UserDictionary.Remove(userName);
            UpdateUserList();
        }
        Console.WriteLine($"Пользователь {ID} вышел из чата");
    }
}
internal abstract class MasterServer
{
    private static void Main()
    {
        // Запуск сервера WebSocket
        var wssv = new WebSocketServer("wss://127.0.0.1:7777")
        {
            SslConfiguration = new ServerSslConfiguration(
                new X509Certificate2("certificate.pfx", "123"),
                true,
                SslProtocols.Tls12,
                false
            )
        };
        // Добавление службы WebSocket для обработки сообщений
        wssv.AddWebSocketService<MyServer>("/myServer");
        // Запуск сервера
        wssv.Start();
        Console.WriteLine("WebSocket Server Started");
        Console.ReadKey();
        wssv.Stop();
    }
}