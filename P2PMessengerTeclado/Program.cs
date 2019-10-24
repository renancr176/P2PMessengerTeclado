using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace P2PMessengerTeclado
{
    public class Program
    {
        private static string IpServidor;
        private static int PortaServidor;

        public static Cliente Cliente { get; private set; }

        static void Main(string[] args)
        {
            if (args.Length == 2 && ValidaIp(args[0])
            && int.TryParse(args[1], out int p))
            {
                Console.WriteLine(string.Join("\n", args));
            }
            else
            {
                Console.WriteLine("P2PMessenger - Teclado");

                string porta;
                do
                {

                    Console.WriteLine("Informe o IP do servidor.");

                    IpServidor = Console.ReadLine();

                    if (!ValidaIp(IpServidor))
                    {
                        Console.Clear();
                        Console.WriteLine("P2PMessenger - Teclado");
                    }

                } while (!ValidaIp(IpServidor));

                do
                {
                    Console.WriteLine("Informe a porta do servidor.");

                    porta = Console.ReadLine();

                    if (!int.TryParse(porta, out PortaServidor))
                    {
                        Console.Clear();
                        Console.WriteLine("P2PMessenger - Teclado Chat");
                    }

                } while (!int.TryParse(porta, out PortaServidor));
            }

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
                    if (client.Connected)
                    {
                        Console.Clear();

                        NetworkStream ns = client.GetStream();

                        while (!_cancellationToken.IsCancellationRequested && client.Connected)
                        {
                            Console.Clear();
                            Console.WriteLine("P2PMessenger - Teclado");

                            var mensagem = Console.ReadLine();

                            var msg = Encoding.Default.GetBytes(mensagem);

                            ns.Write(msg, 0, msg.Length);
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
}
