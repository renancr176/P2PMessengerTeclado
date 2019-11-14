using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace P2PMessengerTeclado
{
    class Program
    {
        private static string IpServidor;
        private static int PortaServidor;

        public static Cliente Cliente { get; private set; }

        static void Main(string[] args)
        {
            Console.WriteLine("P2PMessengerTeclado");

            string porta;
            do
            {
                Console.WriteLine("Informe o IP do servidor");

                IpServidor = Console.ReadLine();

                if (!ValidaIp(IpServidor))
                {
                    Console.Clear();
                    Console.WriteLine("P2PMessengerTeclado");
                }

            } while (!ValidaIp(IpServidor));

            do
            {
                Console.WriteLine("Informe a porta do servidor.");

                porta = Console.ReadLine();

                if (!int.TryParse(porta, out PortaServidor))
                {
                    Console.Clear();
                    Console.WriteLine("P2PMessengerTeclado");
                }

            } while (!int.TryParse(porta, out PortaServidor));


            Cliente = new Cliente(IPAddress.Parse(IpServidor), PortaServidor);

            Cliente.Iniciar().Wait();
        }

        private static bool ValidaIp(string ip)
        {
            var regex = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");

            return regex.IsMatch(ip);
        }
    }

    public class Cliente
    {
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken => _cancellationTokenSource.Token;

        public bool Rodando { get; private set; }

        public IPAddress IpServidor { get; private set; }
        public int PortaServidor { get; private set; }

        public Cliente(IPAddress ipServidor, int portaServidor)
        {
            IpServidor = ipServidor;
            PortaServidor = portaServidor;
        }

        public async Task Iniciar()
        {
            if (!Rodando)
            {
                Rodando = true;
                _cancellationTokenSource = new CancellationTokenSource();

                var client = new TcpClient();

                while (!_cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    if (client.Connected)
                    {
                        Console.Clear();

                        NetworkStream ns = client.GetStream();

                        while (!_cancellationToken.IsCancellationRequested && client.Connected)
                        {
                            var opcao = -1;
                            Mensagem mensagem;
                            string jsonStr;
                            byte[] msg;

                            do
                            {
                                Console.Clear();
                                Console.WriteLine("P2PMessengerTeclado\n");
                                Console.WriteLine("Escolha uma das opções.");

                                foreach (var tipoMensagem in Enum.GetValues(typeof(TipoMensagemEnum))
                                    .Cast<TipoMensagemEnum>())
                                {
                                    Console.WriteLine($"{(int)tipoMensagem} - {tipoMensagem.GetDescription()}");
                                }

                                var op = Console.ReadLine();

                                if (!int.TryParse(op, out opcao)
                                || Enum.GetValues(typeof(TipoMensagemEnum))
                                    .Cast<TipoMensagemEnum>()
                                    .All(e => e != (TipoMensagemEnum)opcao))
                                {
                                    Console.Clear();
                                    Console.WriteLine("P2PMessengerTeclado\n");
                                    Console.WriteLine("Opção inválida.");
                                }

                            } while (Enum.GetValues(typeof(TipoMensagemEnum))
                                         .Cast<TipoMensagemEnum>()
                                         .All(e => e != (TipoMensagemEnum)opcao));

                            switch ((TipoMensagemEnum)opcao)
                            {
                                case TipoMensagemEnum.Mensagem:
                                    Console.Clear();
                                    Console.WriteLine("P2PMessengerTeclado\n");
                                    Console.WriteLine("Digite a mensagem.");

                                    var texto = Console.ReadLine();

                                    mensagem = new Mensagem(texto);

                                    jsonStr = JsonConvert.SerializeObject(mensagem);

                                    msg = Encoding.Default.GetBytes(jsonStr);

                                    ns.Write(msg, 0, msg.Length);
                                    break;
                                case TipoMensagemEnum.Arquivo:
                                    Console.Clear();
                                    Console.WriteLine("P2PMessengerTeclado\n");
                                    Console.WriteLine("Informe o caminho da imagem.");

                                    var caminho = Console.ReadLine();

                                    var caminhoValido = false;

                                    try
                                    {
                                        Path.GetFullPath(caminho);

                                        caminhoValido = true;
                                    }
                                    catch (Exception e)
                                    {

                                    }

                                    if (caminhoValido
                                    && File.Exists(caminho)
                                    && (caminho.EndsWith(".jpg") || caminho.EndsWith(".png")))
                                    {
                                        FileInfo fileInfo = new FileInfo(caminho);

                                        byte[] imagem = new byte[fileInfo.Length];

                                        // Load a filestream and put its content into the byte[]
                                        using (FileStream fs = fileInfo.OpenRead())
                                        {
                                            fs.Read(imagem, 0, imagem.Length);
                                        }

                                        var arquivo = new Arquivo(fileInfo.Name, imagem);

                                        mensagem = new Mensagem(arquivo);

                                        jsonStr = JsonConvert.SerializeObject(mensagem);

                                        msg = Encoding.Default.GetBytes(jsonStr);

                                        ns.Write(msg, 0, msg.Length);
                                    }
                                    else
                                    {
                                        Console.Clear();
                                        if (!caminhoValido)
                                        {
                                            Console.WriteLine("Caminho inválido.");
                                        }
                                        else if (!File.Exists(caminho))
                                        {
                                            Console.WriteLine("Arquivo inexistente.");
                                        }
                                        else
                                        {
                                            Console.WriteLine("O arquivo não é uma imagem válida, informe uma imagem JPG ou PNG.");
                                        }

                                        Console.WriteLine("Precione ENTER para continuar.");
                                        Console.ReadLine();
                                    }
                                    break;
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            client.Connect(IpServidor, PortaServidor);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }
            }
        }
    }

    public class Mensagem
    {
        public TipoMensagemEnum TipoMensagem { get; private set; }
        public string Texto { get; private set; }
        public Arquivo Arquivo { get; private set; }

        public Mensagem(string texto)
        {
            TipoMensagem = TipoMensagemEnum.Mensagem;
            Texto = texto;
        }

        public Mensagem(Arquivo arquivo)
        {
            TipoMensagem = TipoMensagemEnum.Arquivo;
            Arquivo = arquivo;
        }
    }

    public class Arquivo
    {
        public string Nome { get; private set; }
        public string ArquivoTexto { get; private set; }

        public Arquivo(string nome, byte[] imagem)
        {
            Nome = nome;
            ArquivoTexto = Convert.ToBase64String(imagem);
        }
    }

    public static class EnumExtensao
    {
        public static string GetDescription(this Enum en)
        {
            Type type = en.GetType();

            MemberInfo[] memInfo = type.GetMember(en.ToString());

            if (memInfo != null && memInfo.Length > 0)

            {

                object[] attrs = memInfo[0].GetCustomAttributes(
                    typeof(DescriptionAttribute),

                    false);

                if (attrs != null && attrs.Length > 0)

                    return ((DescriptionAttribute)attrs[0]).Description;

            }

            return en.ToString();
        }
    }

    public enum TipoMensagemEnum
    {
        [Description("Mensagem")]
        Mensagem,
        [Description("Imagem")]
        Arquivo
    }
}
