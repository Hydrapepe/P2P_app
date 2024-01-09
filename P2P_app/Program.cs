namespace P2P_app;
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Включение стилей пользовательского интерфейса
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        // Запрос у пользователя ввода никнейма через диалоговое окно
        var nickname = Prompt.ShowDialog("Введите никнейм ", "Вход");
        // Запуск приложения чата с указанным никнеймом, если ввод не пустой
        if (!string.IsNullOrEmpty(nickname)) Application.Run(new ChatForm(nickname));
        // Добавление обработчика события при выходе из приложения для корректного завершения потока приложения
        Application.ApplicationExit += (sender, e) => { Application.ExitThread(); };
    }
}