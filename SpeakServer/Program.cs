using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq.Expressions;
using System.Text;
using SharpTalk;

namespace Speak
{
    class Program
    {
        static bool _shouldCloseConnection = false;
        static bool _shouldCloseServer = false;

        static NamedPipeServerStream namedPipeServerStream = null;
        static StreamReader streamReader = null;
        static BinaryWriter binaryWriter = null;
        static FonixTalkEngine tts = null;

        static readonly string MESSAGE_PREFIX = "msg=";
        static readonly string MESSAGE_ALOUD_PREFIX = "msgA=";
        static void Main()
        {
            namedPipeServerStream = new NamedPipeServerStream("AEIOUCOMPANYMOD");
            streamReader = new StreamReader(namedPipeServerStream, Encoding.UTF8, true, 8192, true);
            binaryWriter = new BinaryWriter(namedPipeServerStream, Encoding.UTF8, true);
            tts = new FonixTalkEngine();

            namedPipeServerStream.WaitForConnection();
            Console.WriteLine("Connected");

            ListenForMessages();

            if (namedPipeServerStream.IsConnected)
            {
                namedPipeServerStream.Disconnect();
            }
            tts.Sync();
        }
        static void ListenForMessages()
        {
            while (!_shouldCloseConnection)
            {
                if (!namedPipeServerStream.IsConnected)
                {
                    _shouldCloseConnection = true;
                    break;
                }
                try
                {
                    string line = streamReader.ReadLine();
                    Console.WriteLine(line);
                    HandleMessage(line);
                }
                catch (IOException e)
                {
                    Console.WriteLine($"Broken: {e.Message}");
                    namedPipeServerStream.Disconnect();
                    continue;
                }
            }
        }
        static void HandleMessage(string line)
        {
            if (line == null || line == "")
            {
                return;
            }
            else if (line == "exit")
            {
                _shouldCloseConnection = true;
                return;
            }
            else if (line.StartsWith(MESSAGE_PREFIX, StringComparison.Ordinal))
            {
                Console.WriteLine("Stream");
                string message = line.Substring(MESSAGE_PREFIX.Length);
                byte[] buffer = tts.SpeakToMemory(message);
                binaryWriter.Write((int)buffer.Length); // buffer.Length/2 num samples
                namedPipeServerStream.Write(buffer, 0, buffer.Length);
                binaryWriter.Flush();
            }
            else if (line.StartsWith(MESSAGE_ALOUD_PREFIX, StringComparison.Ordinal))
            {
                Console.WriteLine("Speaker");
                string message = line.Substring(MESSAGE_ALOUD_PREFIX.Length);
                tts.Speak(message);
            }
        }
    }
}
