using System;
using System.IO.Pipes;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

class Program
{
    static ConcurrentDictionary<string, Dictionary<string, int>> fileWordCounts = new();
    static ConcurrentDictionary<string, object> fileLocks = new(); // one lock per file

    static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: Master <pipe1> <pipe2>");
            return;
        }

        string pipe1 = args[0];
        string pipe2 = args[1];

        Thread t1 = new Thread(() => HandlePipe(pipe1));
        Thread t2 = new Thread(() => HandlePipe(pipe2));
        t1.Start();
        t2.Start();
        t1.Join();
        t2.Join();

        Console.WriteLine("\nFinal Result ");
        foreach (var fileEntry in fileWordCounts)
        {
            foreach (var wordEntry in fileEntry.Value)
            {
                Console.WriteLine($"{fileEntry.Key}:{wordEntry.Key}:{wordEntry.Value}");
            }
        }
    }

    static void HandlePipe(string pipeName)
    {
        using var server = new NamedPipeServerStream(pipeName, PipeDirection.In);
        Console.WriteLine($"Waiting for connection on pipe: {pipeName}");
        server.WaitForConnection();

        using var reader = new StreamReader(server);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            var parts = line.Split(':');
            if (parts.Length != 3) continue;

            string filename = parts[0];
            string word = parts[1];

           
            var lockObj = fileLocks.GetOrAdd(filename, _ => new object());

            lock (lockObj)
            {
                var wordCounts = fileWordCounts.GetOrAdd(filename, _ => new Dictionary<string, int>());

                if (wordCounts.ContainsKey(word))
                    wordCounts[word]++;
                else
                    wordCounts[word] = 1;
            }
        }
    }
}
