using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: AgentB <directory> <pipeName>");
            return;
        }

        string dirPath = args[0];
        string pipeName = args[1];

        Thread thread = new Thread(() => ScanAndSend(dirPath, pipeName));
        thread.Start();
        thread.Join();
    }

    static void ScanAndSend(string path, string pipeName)
    {
        if (!Directory.Exists(path))
        {
            Console.WriteLine($"Directory not found: {path}");
            return;
        }

        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
        client.Connect();

        using var writer = new StreamWriter(client) { AutoFlush = true };

        foreach (var file in Directory.GetFiles(path, "*.txt"))
        {
            string[] words = File.ReadAllText(file).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                writer.WriteLine($"{Path.GetFileName(file)}:{word.ToLower()}:1");
            }
        }
    }
}
