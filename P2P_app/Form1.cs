namespace P2P_app;
public partial class ChatForm : Form
{
    private P2PClient _p2PClient;

    public ChatForm(string nickname)
    {
        this.nickname = nickname;
        InitializeComponent();
        Load += ChatForm_Load;
    }

    private void ChatForm_Load(object sender, EventArgs e)
    {
        ConnectToP2PServer(); // ������ ���������� � P2P-��������
        ReceiveMessagesFromP2P(); // ������ ������ ��������� �� P2P-�������
    }
    private void FileModuleTextBox_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
    }
    private void FileModuleTextBox_DragDrop(object sender, DragEventArgs e)
    {
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        foreach (var file in files) this.fileModuleTextBox.Text += file + Environment.NewLine;
    }
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        Application.Exit();
    }
    private void SendFileButton_Click(object sender, EventArgs e)
    {
        string selectedUser = usersListBox.SelectedItem as string;
        if (!string.IsNullOrEmpty(selectedUser) && selectedUser != nickname)
        {
            string fileName = Path.GetFileName(fileModuleTextBox.Text);
            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileModuleTextBox.Text))
            {
                // ������ ����������� �����
                string fileContent = Convert.ToBase64String(File.ReadAllBytes(fileModuleTextBox.Text));

                // �������� �����
                string message = $"FILE:{nickname}:{selectedUser}:{fileName}:{fileContent}";
                SendMessageToP2P(message);

                // ���������� ���������� � ����� � ����
                chatTextBox.AppendText($"�� ��������� ���� '{fileName}' ������������ {selectedUser}\n");
            }
            else
            {
                MessageBox.Show(@"�������� ���� ��� ��������.");
            }
        }
        else if (selectedUser == nickname)
        {
            MessageBox.Show(@"������ ��������� ���� ������ ����.");
        }
        else
        {
            MessageBox.Show(@"�������� ������������ ��� �������� �����.");
        }
    }
    private void FileModuleTextBox_Click(object sender, EventArgs e)
    {
        var openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            this.fileModuleTextBox.Text = openFileDialog.FileName;
        }
    }

    private async void ConnectToP2PServer()
    {
        _p2PClient = new P2PClient("ws://127.0.0.1:7777/myServer");
        await _p2PClient.ConnectAsync();
        await _p2PClient.RegisterAsync(nickname);
    }

    private async Task SendMessageToP2P(string message)
    {
        await _p2PClient.SendMessageAsync(message);
    }

    private async void ReceiveMessagesFromP2P()
    {
        Thread.Sleep(1111);
        while (true)
        {
            var message = await _p2PClient.ReceiveMessageAsync();
            if (message != null)
            {
                if (message.StartsWith("USERLIST:"))
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        UpdateUserList(message.Substring("USERLIST:".Length));
                    });
                }
                else if (message.StartsWith("FILE:"))
                {
                    var parts = message.Substring("FILE:".Length).Split(':');
                    var sender = parts[0];
                    var recipient = parts[1];
                    var fileName = parts[2];
                    var fileContent = parts[3];

                    if (recipient == nickname)
                    {
                        this.Invoke((MethodInvoker)async delegate
                        {
                            var result = MessageBox.Show($"�� ������ ������� ���� �� ������������ '{sender}', ����: '{fileName}'?", "������ �� ���������� �����", MessageBoxButtons.YesNo);

                            if (result == DialogResult.Yes)
                            {
                                // �������� ������������, ���� ��������� ����
                                SaveFileDialog saveFileDialog = new SaveFileDialog();
                                saveFileDialog.FileName = fileName;
                                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                                {
                                    // ��������� ���� �� ����
                                    File.WriteAllBytes(saveFileDialog.FileName, Convert.FromBase64String(fileContent));
                                    chatTextBox.AppendText($"���� �������� �� ����: {saveFileDialog.FileName}\n");
                                }
                            }
                            else
                            {
                                // ��������� ����������� ����������� � ���, ��� ���� ��������
                                await SendMessageToP2P($"FILE_REJECTED:{nickname}:{sender}:{fileName}");
                                chatTextBox.AppendText($"�� ��������� ���� �� ������������ '{sender}', ����: '{fileName}'\n");
                            }
                        });
                    }
                }
                else if (message.StartsWith("FILE_REJECTED:"))
                {
                    var parts = message.Substring("FILE_REJECTED:".Length).Split(':');
                    var sender = parts[0];
                    var fileName = parts[1];
                    this.Invoke((MethodInvoker)delegate
                    {
                        // ���������� ����������� ����������� � ���, ��� ���� ��������
                        chatTextBox.AppendText($"������������ '{sender}' �������� ��� ���� '{fileName}'\n");
                    });
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        // ���������� ��������� ��������� � ����
                        chatTextBox.AppendText($"{message}\n");
                    });
                }
            }
        }
    }
    private void UpdateUserList(string userList)
    {
        // ����������� ������ ';' � �������� �����������
        var users = userList.Split(';');
        // �������� listBox
        usersListBox.Items.Clear();
        // �������� ������� ������������ � listBox
        foreach (var user in users)
        {
            usersListBox.Items.Add(user);
        }
    }
    private async void DisconnectFromP2PServer()
    {
        await _p2PClient.CloseAsync();
    }

    private void UsersListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        var selectedUser = usersListBox.SelectedItem.ToString();
        chatLabel.Text = @"��� c: " + selectedUser;
    }

    // ���������� ������� ��� ����� � additionalTextBox
    private void AdditionalTextBox_Enter(object sender, EventArgs e)
    {
        var textBox = (TextBox)sender;
        if (textBox.Text == @"������� ���������...") textBox.Text = "";
    }

    // ���������� ������� ��� ������ �� additionalTextBox
    private void AdditionalTextBox_Leave(object sender, EventArgs e)
    {
        var textBox = (TextBox)sender;
        if (string.IsNullOrWhiteSpace(textBox.Text)) textBox.Text = @"������� ���������...";
    }

    private void AdditionalButton_Click(object sender, EventArgs e)
    {
        var selectedUser = usersListBox.SelectedItem as string;
        if (!string.IsNullOrEmpty(selectedUser) && selectedUser != nickname)
        {
            var message = $"PRIVATE:{nickname}:{selectedUser}:{additionalTextBox.Text}";
            SendMessageToP2P(message);
            // ���������� ������������ ��������� � ����
            chatTextBox.AppendText($"�� (�������� {selectedUser}): {additionalTextBox.Text}\n");
            chatTextBox.AppendText("\n"); // ������ ������
            additionalTextBox.Text = @"������� ���������...";
        }
        else if (selectedUser == nickname) MessageBox.Show(@"������ ��������� ��������� ������ ����.");
        else MessageBox.Show(@"�������� ������������ ��� �������� ���������� ���������.");
    }
}