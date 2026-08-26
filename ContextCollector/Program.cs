using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;

partial class Program
{
    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    static readonly string CaminhoBanco = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ContextCollector",
        "contexto.db");

    static readonly string Ambiente = Environment.MachineName;

    static void InicializarBanco()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CaminhoBanco)!);

        using var connection = new SqliteConnection($"Data Source={CaminhoBanco}");
        connection.Open();
        var command = connection.CreateCommand();

        command.CommandText = """
            CREATE TABLE IF NOT EXISTS eventos (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                ambiente TEXT NOT NULL,
                processo TEXT NOT NULL,
                inicio TEXT NOT NULL,
                fim TEXT NOT NULL,
                duracao_segundos REAL NOT NULL,
                rede TEXT
            );
            """;

        command.ExecuteNonQuery();

        Console.WriteLine($"Banco: {CaminhoBanco}");
        Console.WriteLine($"Ambiente: {Ambiente}");
    }

    static string ObterRede()
    {
        try
        {
            var iface = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up
                                  && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            return iface?.Name ?? "desconhecida";
        }
        catch
        {
            return "erro";
        }
    }

    static void GerarEvento(string processo, DateTime inicio, DateTime fim, double duracao)
    {
        using var connection = new SqliteConnection($"Data Source={CaminhoBanco}");
        connection.Open();
        var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO eventos (ambiente, processo, inicio, fim, duracao_segundos, rede)
            VALUES ($ambiente, $processo, $inicio, $fim, $duracao, $rede)
            """;

        command.Parameters.AddWithValue("$ambiente", Ambiente);
        command.Parameters.AddWithValue("$processo", processo);
        command.Parameters.AddWithValue("$inicio", inicio.ToString("o"));
        command.Parameters.AddWithValue("$fim", fim.ToString("o"));
        command.Parameters.AddWithValue("$duracao", duracao);
        command.Parameters.AddWithValue("$rede", ObterRede());

        command.ExecuteNonQuery();
    }

    static void Main()
    {
        InicializarBanco();

        string nomeAnterior = "";
        DateTime inicioAnterior = DateTime.Now;

        Console.CancelKeyPress += (sender, e) =>
        {
            if (nomeAnterior != "")
            {
                DateTime fim = DateTime.Now;
                GerarEvento(nomeAnterior, inicioAnterior, fim, (fim - inicioAnterior).TotalSeconds);
                Console.WriteLine("Último evento gravado.");
            }
        };

        while (true)
        {
            try
            {
                IntPtr handle = GetForegroundWindow();
                GetWindowThreadProcessId(handle, out uint pid);

                int pidInt = (int)pid;

                Process localById = Process.GetProcessById(pidInt);
                string nomeProcesso = localById.ProcessName;

                if (nomeProcesso != nomeAnterior)
                {
                    DateTime agora = DateTime.Now;

                    if (nomeAnterior != "")
                    {
                        double duracao = (agora - inicioAnterior).TotalSeconds;
                        Console.WriteLine($"{nomeAnterior} || {inicioAnterior:HH:mm:ss} -> {agora:HH:mm:ss} || {duracao:F0}s");
                        GerarEvento(nomeAnterior, inicioAnterior, agora, duracao);
                    }

                    nomeAnterior = nomeProcesso;
                    inicioAnterior = agora;
                }
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Processo não encontrado");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Thread.Sleep(1000);
        }
    }
}
