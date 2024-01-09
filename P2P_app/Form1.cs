// Импорт пространства имен для работы с безопасностью
using System.Security.Authentication;
// Определение пространства имен и класса
namespace P2P_app;
// Частичный класс формы чата
public partial class ChatForm : Form
{
    // Приватное поле для хранения экземпляра P2P-клиента
    private P2PClient? _p2PClient;

    // Конструктор формы, принимающий никнейм пользователя
    public ChatForm(string nickname)
    {
        this.nickname = nickname;
        // Инициализация компонентов формы
        InitializeComponent();
        // Подписка на событие загрузки формы
        Load += ChatForm_Load!;
    }

    // Метод, вызываемый при загрузке формы
    private void ChatForm_Load(object sender, EventArgs e)
    {
        // Запуск соединения с P2P-сервером
        ConnectToP2PServer();
        // Запуск приема сообщений от P2P-сервера
        ReceiveMessagesFromP2P();
    }

    // Обработчик события DragEnter для TextBox, позволяющий определить, можно ли сбросить файл
    private void FileModuleTextBox_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
    }

    // Обработчик события DragDrop для TextBox, обрабатывающий сброс файлов
    private void FileModuleTextBox_DragDrop(object sender, DragEventArgs e)
    {
        // Получение списка путей к сброшенным файлам
        var files = (string[])e.Data!.GetData(DataFormats.FileDrop)!;
        // Добавление имен файлов в TextBox
        foreach (var file in files) fileModuleTextBox.Text += file + Environment.NewLine;
    }

    // Переопределение метода закрытия формы
    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        // Асинхронное закрытие P2P-клиента
        await _p2PClient!.CloseAsync();
        // Вызов базового метода закрытия формы
        base.OnFormClosing(e);
        // Завершение работы приложения
        Application.Exit();
    }

    // Обработчик нажатия кнопки отправки файла
    private void SendFileButton_Click(object sender, EventArgs e)
    {
        // Получение выбранного пользователя из списка
        var selectedUser = usersListBox.SelectedItem as string;
        // Проверка, что пользователь выбран и не является отправителем
        if (!string.IsNullOrEmpty(selectedUser) && selectedUser != nickname)
        {
            // Получение имени файла и его содержимого
            var fileName = Path.GetFileName(fileModuleTextBox.Text);
            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileModuleTextBox.Text))
            {
                // Чтение содержимого файла в Base64
                var fileContent = Convert.ToBase64String(File.ReadAllBytes(fileModuleTextBox.Text));
                // Формирование сообщения с информацией о файле
                var message = $"FILE:{nickname}:{selectedUser}:{fileName}:{fileContent}";
                // Отправка сообщения с файлом
                _ = SendMessageToP2P(message);
                // Отображение информации о файле в чате
                chatTextBox.AppendText($"Вы отправили файл '{fileName}' пользователю {selectedUser}\n");
            }
            else MessageBox.Show(@"Выберите файл для отправки.");
        }
        else if (selectedUser == nickname) MessageBox.Show(@"Нельзя отправить файл самому себе.");
        else MessageBox.Show(@"Выберите пользователя для отправки файла.");
    }

    // Обработчик события клика на TextBox для выбора файла
    private void FileModuleTextBox_Click(object sender, EventArgs e)
    {
        var openFileDialog = new OpenFileDialog();
        // Отображение диалога выбора файла
        if (openFileDialog.ShowDialog() == DialogResult.OK) fileModuleTextBox.Text = openFileDialog.FileName;
    }

    // Асинхронное соединение с P2P-сервером
    private async void ConnectToP2PServer()
    {
        // Создание экземпляра P2P-клиента с указанием адреса сервера
        _p2PClient = new P2PClient("wss://127.0.0.1:7777/myServer");
        // Установка версии TLS и передача сертификата и пароля
        _p2PClient.SetTlsVersion(SslProtocols.Tls12, "certificate.pfx", "123");
        // Асинхронное подключение к серверу
        await _p2PClient.ConnectAsync();
        // Регистрация пользователя на сервере
        await _p2PClient.RegisterAsync(nickname);
    }

    // Асинхронная отправка сообщения через P2P-сервер
    private async Task SendMessageToP2P(string message) => await _p2PClient!.SendMessageAsync(message);

    // Асинхронный метод приема сообщений от P2P-сервера
    private async void ReceiveMessagesFromP2P()
    {
        // Пауза перед началом приема сообщений
        Thread.Sleep(200);
        // Бесконечный цикл приема сообщений
        while (true)
        {
            // Асинхронное получение сообщения от P2P-сервера
            var message = await _p2PClient?.ReceiveMessageAsync()!;
            if (message == null) continue;
            // Обработка сообщения в зависимости от его типа
            if (message.StartsWith("USERLIST:"))
            {
                // Обновление списка пользователей на форме
                Invoke((MethodInvoker)delegate
                {
                    UpdateUserList(message["USERLIST:".Length..]);
                });
            }
            else if (message.StartsWith("FILE:"))
            {
                // Обработка сообщения с файлом
                var parts = message["FILE:".Length..].Split(':');
                var sender = parts[0];
                var recipient = parts[1];
                var fileName = parts[2];
                var fileContent = parts[3];

                // Проверка, что файл адресован текущему пользователю
                if (recipient == nickname)
                {
                    // Обновление интерфейса в основном потоке
                    Invoke((MethodInvoker)async delegate
                    {
                        // Вывод запроса на скачивание файла
                        var result = MessageBox.Show($@"Вы хотите скачать файл от пользователя '{sender}', файл: '{fileName}'?", @"Запрос на скачивание файла", MessageBoxButtons.YesNo);

                        if (result == DialogResult.Yes)
                        {
                            // Диалог сохранения файла
                            var saveFileDialog = new SaveFileDialog
                            {
                                FileName = fileName
                            };
                            if (saveFileDialog.ShowDialog() != DialogResult.OK) return;
                            // Сохранение файла на диск
                            await File.WriteAllBytesAsync(saveFileDialog.FileName, Convert.FromBase64String(fileContent));
                            chatTextBox.AppendText($"Файл сохранен по пути: {saveFileDialog.FileName}\n");
                        }
                        else
                        {
                            // Уведомление отправителю о том, что файл отклонен
                            await SendMessageToP2P($"FILE_REJECTED:{nickname}:{sender}:{fileName}");
                            chatTextBox.AppendText($"Вы отклонили файл от пользователя '{sender}', файл: '{fileName}'\n");
                        }
                    });
                }
            }
            else if (message.StartsWith("FILE_REJECTED:"))
            {
                // Обработка отклоненного файла
                var parts = message["FILE_REJECTED:".Length..].Split(':');
                var sender = parts[0];
                var fileName = parts[1];
                Invoke((MethodInvoker)delegate
                {
                    // Отображение уведомления отправителю о том, что файл отклонен
                    chatTextBox.AppendText($"Пользователь '{sender}' отклонил ваш файл '{fileName}'\n");
                });
            }
            else
            {
                // Отображение приватного сообщения в чате
                Invoke((MethodInvoker)delegate
                {
                    chatTextBox.AppendText($"{message}\n");
                });
            }
        }
    }
    // Метод обновления списка пользователей на форме
    private void UpdateUserList(string userList)
    {
        // Разделение строки с пользователями на массив и обновление списка на форме
        var users = userList.Split(';');
        usersListBox.Items.Clear();
        foreach (var user in users)
        {
            usersListBox.Items.Add(user);
        }
    }

    // Обработчик события выбора пользователя в списке
    private void UsersListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        // Отображение текущего выбранного пользователя в метке чата
        var selectedUser = usersListBox.SelectedItem?.ToString();
        chatLabel.Text = @"Чат c: " + selectedUser;
    }

    // Обработчик события входа в TextBox для дополнительного сообщения
    private void AdditionalTextBox_Enter(object sender, EventArgs e)
    {
        // Очистка текста, если он равен заданной строке
        var textBox = (TextBox)sender;
        if (textBox.Text == @"Введите сообщение...") textBox.Text = "";
    }

    // Обработчик события выхода из TextBox для дополнительного сообщения
    private void AdditionalTextBox_Leave(object sender, EventArgs e)
    {
        // Восстановление заданной строки, если TextBox пуст
        var textBox = (TextBox)sender;
        if (string.IsNullOrWhiteSpace(textBox.Text)) textBox.Text = @"Введите сообщение...";
    }

    // Обработчик нажатия кнопки отправки приватного сообщения
    private void AdditionalButton_Click(object sender, EventArgs e)
    {
        // Получение выбранного пользователя из списка
        var selectedUser = usersListBox.SelectedItem as string;
        // Проверка, что пользователь выбран и не является отправителем
        if (!string.IsNullOrEmpty(selectedUser) && selectedUser != nickname)
        {
            // Формирование сообщения с приватным текстом
            var message = $"PRIVATE:{nickname}:{selectedUser}:{additionalTextBox.Text}";
            // Отправка приватного сообщения
            _ = SendMessageToP2P(message);
            // Отображение отправленного сообщения в чате
            chatTextBox.AppendText($"Вы (приватно {selectedUser}): {additionalTextBox.Text}\n");
            chatTextBox.AppendText("\n"); // Разрыв строки
            // Сброс текста TextBox для дополнительного сообщения
            additionalTextBox.Text = @"Введите сообщение...";
        }
        else if (selectedUser == nickname) MessageBox.Show(@"Нельзя отправить сообщение самому себе.");
        else MessageBox.Show(@"Выберите пользователя для отправки приватного сообщения.");
    }
}