namespace P2P_app;
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var nickname = Prompt.ShowDialog("¬ведите никнейм ", "¬ход");
        if (!string.IsNullOrEmpty(nickname)) Application.Run(new ChatForm(nickname));
        Application.ApplicationExit += (sender, e) => { Application.ExitThread(); };
    }
}