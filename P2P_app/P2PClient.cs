using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Net.WebSockets;
using System.Text;

namespace P2P_app
{
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
            // Implement the logic for sending registration data to the server if needed
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

            return result.MessageType switch
            {
                WebSocketMessageType.Text => Encoding.UTF8.GetString(buffer, 0, result.Count),
                WebSocketMessageType.Close => null,
                _ => null
            };
        }

        public async Task CloseAsync()
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
        }

        public void SetTlsVersion(SslProtocols protocols, string certificatePath, string certificatePassword)
        {
            var sslOptions = new SslClientAuthenticationOptions { EnabledSslProtocols = protocols, };
            socket.Options.AddSubProtocol("wss");
            socket.Options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            // Load the certificate from file
            var certificate = new X509Certificate2(certificatePath, certificatePassword);
            // Add the certificate to the options
            socket.Options.ClientCertificates = new X509CertificateCollection { certificate };
            if (socket.Options.ClientCertificates.Count > 0) sslOptions.ClientCertificates = socket.Options.ClientCertificates;
            socket.Options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
        }
        public async Task RegisterAsync(string nickname)
        {
            var registerMessage = $"REGISTER:{nickname}";
            await SendMessageAsync(registerMessage);
        }
    }
}
