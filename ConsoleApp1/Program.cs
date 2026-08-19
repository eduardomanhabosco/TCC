using System.Runtime.InteropServices;
using System;
using System.Diagnostics;
partial class Program
{
    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    static void Main()
    {
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
// ver se é o caso de ver todos abertos ou só o em atenção
