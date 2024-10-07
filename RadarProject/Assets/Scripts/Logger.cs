using System;
using System.IO;
using UnityEngine;

public class Logger
{
    private static string filePath;

    public static void SetFilePath(object message)
    {
        string path = Path.Combine(Application.persistentDataPath, "Logs");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        
        // Time stamp in a valid format (does not contain /)
        filePath = Path.Combine(path, $"Simulation_{DateTime.Now:yyyyMMdd_HHmmss}.log");

        using StreamWriter writer = new(filePath, false);
        writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
    }

    public static void Log(object message)
    {
        if (filePath == null) return;

        using StreamWriter writer = new(filePath, true);
        writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
    }
}
