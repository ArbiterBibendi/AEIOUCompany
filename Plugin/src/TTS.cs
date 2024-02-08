using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AEIOU_Company;
static class TTS
{
    public readonly struct SpeechData
    {
        public readonly int PlayerId;
        public readonly float[] AudioData;
        public readonly float AudioLengthInSeconds;

        public SpeechData(int playerPlayerId, float[] audioData, float audioLengthInSeconds)
        {
            PlayerId = playerPlayerId;
            AudioData = audioData;
            AudioLengthInSeconds = audioLengthInSeconds;
        }
    }

    private const int OUT_BUFFER_SIZE = 8192; // text
    public const int IN_BUFFER_SIZE = 8388608; // 8MB pcm audio

    private static readonly float[] audioFloatBuffer = new float[IN_BUFFER_SIZE];
    private static byte[] audioByteBuffer = new byte[IN_BUFFER_SIZE * 2];
    private static NamedPipeClientStream _namedPipeClientStream;
    private static StreamWriter _streamWriter;
    private static BinaryReader _binaryReader;
    private static bool _initialized = false;

    public static void Init()
    {
        StartSpeakServer();
        try
        {
            _namedPipeClientStream = new NamedPipeClientStream("AEIOUCOMPANYMOD");
            _streamWriter = new StreamWriter(_namedPipeClientStream, Encoding.UTF8, OUT_BUFFER_SIZE, true);
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

    public static SpeechData SpeakToMemory(int playerId, string message, float volumeScale = 1f)
    {
        if (!_initialized)
        {
            Plugin.LogError("Tried to speak before initializing TTS!");
            return default;
        }
        message = message.Replace("\r", "").Replace("\n", "");

        SendMsg(message, "msg");

        int msgLength = _binaryReader.ReadInt32();
        if (msgLength > audioByteBuffer.Length) { audioByteBuffer = new byte[msgLength]; }

        Array.Clear(audioFloatBuffer, 0, audioFloatBuffer.Length);
        int lastNonZeroValueIndex = 0;

        _binaryReader.Read(audioByteBuffer, 0, msgLength);
        for (int i = 0; i < Math.Min(msgLength / 2, audioFloatBuffer.Length); i++)
        {
            float nextSample = volumeScale * ((float)BitConverter.ToInt16(audioByteBuffer, i * 2) / 32767f); // convert half -1 to 1 float
            audioFloatBuffer[i] = nextSample;
            if (nextSample != 0f) { lastNonZeroValueIndex = i; }
        }

        var currentAudioLengthInSeconds = (float)lastNonZeroValueIndex / (float)11025; // 11025 hz

        Plugin.Log($"END");
        return new SpeechData(playerId, audioFloatBuffer, currentAudioLengthInSeconds);
    }

    private static void SendMsg(string message, string prefix) // prefix msgA or msg
    {
        if (!_namedPipeClientStream.IsConnected)
        {
            StartSpeakServer();
            ConnectToSpeakServer();
        }

        message = $"{prefix}=[:np]{message}]";
        Plugin.Log($"Sending: {message}");
        try
        {
            _streamWriter.WriteLine(message);
            _streamWriter.Flush();
        }
        catch (Exception e)
        {
            Plugin.LogError(e);
        }
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
        catch (IOException)
        {
            Plugin.LogError("IOException while trying to ConnectToSpeakServer");
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