using System.Runtime.InteropServices;
using System;
using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic;
using System.Runtime.Intrinsics.X86;
partial class Program
{
    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    
    const string ambiente = "pc";
    static void InicializarBanco()
    {
        using var connection = new SqliteConnection("Data Source=contexto.db");
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
        
    }
    
    static void GerarEvento(string processo, DateTime inicio, DateTime fim, double duracao)
    {
        using var connection = new SqliteConnection("Data Source=contexto.db");
        connection.Open();
        var command = connection.CreateCommand();
        
        command.CommandText = """
            INSERT INTO eventos (ambiente, processo, inicio, fim, duracao_segundos)
            VALUES ($ambiente, $processo, $inicio, $fim, $duracao)
        """;
        
        command.Parameters.AddWithValue("$ambiente", ambiente);
        command.Parameters.AddWithValue("$processo", processo);
        command.Parameters.AddWithValue("$inicio", inicio.ToString("o"));
        command.Parameters.AddWithValue("$fim", fim.ToString("o"));
        command.Parameters.AddWithValue("$duracao", duracao);

        command.ExecuteNonQuery();


    }
    static void Main()
    {
        InicializarBanco();
        string nomeAnterior = "";
        DateTime inicioAnterior = DateTime.Now;
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

// Anota os dois no decisoes.md: você vai precisar de uma lista de processos ignorados, e esses são os dois primeiros. Provavelmente vai crescer — LockApp, ShellExperienceHost, ApplicationFrameHost costumam aparecer também.
// O último registro nunca fecha — quando você mata o programa, o intervalo em aberto se perde. Por enquanto tudo bem; quando for pro SQLite, vale pensar em capturar o encerramento (pesquisa Console.CancelKeyPress). Não faz agora.
