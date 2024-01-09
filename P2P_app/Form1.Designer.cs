namespace P2P_app;
partial class ChatForm 
{ 
    private ListBox usersListBox;
    private TextBox chatTextBox;
    private RichTextBox fileModuleTextBox;
    private Button sendFileButton;
    private string nickname;
    private Label usersLabel;
    private Label chatLabel;
    private Label fileLabel;
    private TextBox additionalTextBox;
    private Button additionalButton;

        private System.ComponentModel.IContainer components = null;
    private void InitializeComponent()
    {
        this.usersListBox = new ListBox();
        this.chatTextBox = new TextBox();
        this.fileModuleTextBox = new RichTextBox();
        this.sendFileButton = new Button();
        this.additionalTextBox = new TextBox();
        this.additionalButton = new Button();

        this.usersLabel = new Label() { Text = "Список пользователей:", Location = new System.Drawing.Point(13, 0), Size = new System.Drawing.Size(150, 20) };
        this.chatLabel = new Label() { Text = "Чат:", Location = new System.Drawing.Point(220, 0), Size = new System.Drawing.Size(150, 20) };
        this.fileLabel = new Label() { Text = "Отправка файла:", Location = new System.Drawing.Point(430, 0), Size = new System.Drawing.Size(150, 20) };

        // Users ListBox
        this.usersListBox.Location = new System.Drawing.Point(13, 20);
        this.usersListBox.Size = new System.Drawing.Size(200, 400);
        this.usersListBox.SelectedIndexChanged += new EventHandler(this.UsersListBox_SelectedIndexChanged);

        // Chat TextBox
        this.chatTextBox.Location = new System.Drawing.Point(220, 20);
        this.chatTextBox.Multiline = true;
        this.chatTextBox.ScrollBars = ScrollBars.Vertical; // Добавление вертикального скроллбара
        this.chatTextBox.Size = new System.Drawing.Size(200, 400);

        additionalTextBox.Location = new System.Drawing.Point(220, 425); // Расположение под chatTextBox
        this.additionalTextBox.Size = new System.Drawing.Size(200, 20);
        this.additionalTextBox.Enter += AdditionalTextBox_Enter;
        additionalTextBox.Leave += AdditionalTextBox_Leave;
        additionalTextBox.Text = "Введите сообщение..."; // Установка значения по умолчанию

        // File Module TextBox
        this.fileModuleTextBox = new RichTextBox();
        this.fileModuleTextBox.Location = new System.Drawing.Point(430, 20);
        this.fileModuleTextBox.Multiline = true;
        this.fileModuleTextBox.Size = new System.Drawing.Size(200, 365);
        this.fileModuleTextBox.Text = "Выберите файл для отправки или перетащите его сюда";
        this.fileModuleTextBox.SelectAll();
        this.fileModuleTextBox.SelectionAlignment = HorizontalAlignment.Center;
        this.fileModuleTextBox.DeselectAll();
        this.fileModuleTextBox.AllowDrop = true;
        this.fileModuleTextBox.DragEnter += new DragEventHandler(this.FileModuleTextBox_DragEnter);
        this.fileModuleTextBox.DragDrop += new DragEventHandler(this.FileModuleTextBox_DragDrop);
        this.fileModuleTextBox.Click += new EventHandler(this.FileModuleTextBox_Click);
        this.fileModuleTextBox.ReadOnly = true;

        // Send File Button
        this.sendFileButton.Location = new System.Drawing.Point(430, 385);
        this.sendFileButton.Size = new System.Drawing.Size(200, 23);
        this.sendFileButton.Text = "Отправить файл";
        this.sendFileButton.Click += new EventHandler(this.SendFileButton_Click);


        this.additionalButton.Location = new System.Drawing.Point(430, 425); // Расположение под additionalTextBox
        this.additionalButton.Size = new System.Drawing.Size(200,23);
        this.additionalButton.Text = "Отправить сообщение";
        this.additionalButton.Click += new EventHandler(this.AdditionalButton_Click);



        // ChatForm
        this.ClientSize = new System.Drawing.Size(800, 450);
        this.Controls.Add(usersLabel);
        this.Controls.Add(chatLabel);
        this.Controls.Add(fileLabel);
        this.Controls.Add(this.usersListBox);
        this.Controls.Add(this.chatTextBox);
        this.Controls.Add(this.fileModuleTextBox);
        this.Controls.Add(this.sendFileButton);
        this.Controls.Add(additionalTextBox);
        this.Controls.Add(additionalButton);
        this.Text = $"Здравствуйте, {nickname}";
    }
}
public static class Prompt
{
    public static string ShowDialog(string text, string caption)
    {
        Form prompt = new Form()
        {
            Width = 500,
            Height = 150,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Text = caption,
            StartPosition = FormStartPosition.CenterScreen
        };
        Label textLabel = new Label() { Left = 50, Top = 20, Text = text, Size = new System.Drawing.Size(150, 20) };
        TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 400 };
        Button confirmation = new Button() { Text = "Ok", Left = 350, Width = 100, Top = 70, DialogResult = DialogResult.OK };
        confirmation.Click += (sender, e) => { prompt.Close(); };
        prompt.Controls.Add(textBox);
        prompt.Controls.Add(confirmation);
        prompt.Controls.Add(textLabel);
        prompt.AcceptButton = confirmation;
        return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
    }
}