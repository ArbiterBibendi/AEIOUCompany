using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;

namespace AEIOU_Company;
public static class TTS
{
    static readonly int OUT_BUFFER_SIZE = 8192; // text
    public static readonly int IN_BUFFER_SIZE = 8388608; // 8MB pcm audio
    static float[] floatBuffer = new float[IN_BUFFER_SIZE];
    private static NamedPipeClientStream _namedPipeClientStream;
    private static StreamWriter _streamWriter;
    private static BinaryWriter _binaryWriter;
    private static BinaryReader _binaryReader;
    private static bool _initialized = false;
    public static void Init()
    {
        StartSpeakServer();
        try
        {
            _namedPipeClientStream = new NamedPipeClientStream("AEIOUCOMPANYMOD");
            _streamWriter = new StreamWriter(_namedPipeClientStream, Encoding.UTF8, OUT_BUFFER_SIZE, true);
            _binaryWriter = new BinaryWriter(_namedPipeClientStream, Encoding.UTF8, true);
            _binaryReader = new BinaryReader(_namedPipeClientStream, Encoding.UTF8, true);
        }
        catch (IOException e)
        {
            Plugin.LogError(e);
        }
        ConnectToSpeakServer();
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
            SendMsg(message, "msgA");
        }
        catch (IOException e)
        {
            Plugin.LogError("Speak" + e);
        }
    }
    public static float[] SpeakToMemory(string message)
    {
        if (!_initialized)
        {
            Plugin.LogError("Tried to speak before initializing TTS!");
            return default(float[]);
        }

        SendMsg(message, "msg");
        ClearSamplesBuffer();
        int msgLength = _binaryReader.ReadInt32();
        for (int i = 0; i < msgLength; i++)
        {
            byte[] bytes = _binaryReader.ReadBytes(2);
            float nextSample = (float)BitConverter.ToInt16(bytes, 0) / 32767f; // convert half to single
            if (i < floatBuffer.Length) // if audio clip is larger than buffer, we still want to consume every byte on the stream
            {
                floatBuffer[i] = nextSample;
            }
        }
        Plugin.Log($"END");
        return floatBuffer;
    }

    private static void SendMsg(string message, string prefix) // prefix msgA or msg
    {
        Plugin.Log($"Sending: {prefix}={message}]");
        try
        {
            _streamWriter.WriteLine(); // write empty line to test if pipe is unbroken
            _streamWriter.Flush();
        }
        catch (IOException e)
        {
            if (e.Message.Contains("Pipe is broken"))
            {
                StartSpeakServer();
                ConnectToSpeakServer();
            }
            else
            {
                Plugin.LogError(e);
            }
        }
        catch(Exception e)
        {
            Plugin.LogError(e);
        }
        _streamWriter.WriteLine($"{prefix}={message}]"); // ] to close off any accidentally opened talk commands
        _streamWriter.Flush();
    }

    private static void ConnectToSpeakServer()
    {
        Plugin.Log("ConnectingToPipe");
        try
        {
            _namedPipeClientStream.Connect(7500);
        }
        catch (TimeoutException)
        {
            Plugin.LogError("Unable to connect to SpeakServer after timeout");
        }
        catch(IOException)
        {
            Plugin.LogError("IOException while trying to ConnectToSpeakServer");
        }
    }

    private static void ClearSamplesBuffer()
    {
        for (int i = 0; i < floatBuffer.Length; i++)
        {
            floatBuffer[i] = 0f;
        }
    }

    private static void StartSpeakServer()
    {
        if (CheckForAEIOUSPEAKProcess())
        {
            Process[] processes = Process.GetProcessesByName("AEIOUSpeak");
            foreach (Process process in processes)
            {
                process.Close();
            }
        }
        string directory = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath.Replace($"{PluginInfo.PLUGIN_NAME}.dll", "");
        Plugin.Log(directory + "AEIOUSpeak.exe");
        Process speakServerProcess = Process.Start(directory + "AEIOUSpeak.exe");
        Plugin.Log("Started Speak Server");
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