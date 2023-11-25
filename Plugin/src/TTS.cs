using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;

namespace AEIOU_Company;
public static class TTS
{
    static readonly int BUFFER_SIZE = 8192;
    private static NamedPipeClientStream _namedPipeClientStream;
    private static StreamWriter _streamWriter;
    private static bool _initialized = false;
    public static void Init()
    {
        StartSpeakServer();
        try
        {
            _namedPipeClientStream = new NamedPipeClientStream("AEIOUCOMPANYMOD");
            _streamWriter = new StreamWriter(_namedPipeClientStream, Encoding.UTF8, BUFFER_SIZE, true);
        }
        catch (IOException e)
        {
            Plugin.LogError(e);
        }
        _initialized = true;
    }
    public static void Speak(string message)
    {
        if (!_initialized)
        {
            Plugin.LogError("Tried to speak before initializing TTS!");
            return;
        }
        try
        {
            if (!_namedPipeClientStream.IsConnected)
            {
                Plugin.Log("ConnectingToPipe");
                _namedPipeClientStream.Connect();
            }
            Plugin.Log($"Sending: msgA={message}]");
            _streamWriter.WriteLine($"msgA={message}]"); // ] to close off any accidentally opened talk commands
            _streamWriter.Flush();
        }
        catch (IOException e)
        {
            Plugin.LogError("Speak" + e);
        }
    }
    private static void StartSpeakServer()
    {
        if (CheckForAEIOUSPEAKProcess())
        {
            return;
        }
        string directory = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath.Replace($"{PluginInfo.PLUGIN_NAME}.dll", "");
        Plugin.Log(directory + "AEIOUSpeak.exe");
        Process speakServerProcess = Process.Start(directory + "AEIOUSpeak.exe");
        if (speakServerProcess == null)
        {
            Plugin.LogError("Failed to start Speak Server");
        }
    }
    private static bool CheckForAEIOUSPEAKProcess()
    {
        Process[] processes = Process.GetProcessesByName("AEIOUSpeak");
        if (processes.Length > 0)
        {
            return true;
        }
        return false;
    }
}