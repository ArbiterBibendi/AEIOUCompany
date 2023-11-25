using System;
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
        static StreamWriter streamWriter = null;
        static FonixTalkEngine tts = null;

        static readonly string MESSAGE_PREFIX = "msg=";
        static readonly string MESSAGE_ALOUD_PREFIX = "msgA=";
        static void Main()
        {
            namedPipeServerStream = new NamedPipeServerStream("AEIOUCOMPANYMOD");
            streamReader = new StreamReader(namedPipeServerStream, Encoding.UTF8, true, 8192, true);
            streamWriter = new StreamWriter(namedPipeServerStream, Encoding.UTF8, 8192, true);
            tts = new FonixTalkEngine();

            while (!_shouldCloseServer)
            {
                namedPipeServerStream.WaitForConnection();
                Console.WriteLine("Connected");

                ListenForMessages();

                namedPipeServerStream.Disconnect();
                if (Process.GetProcessesByName("Lethal Company.exe").Length == 0)
                {
                    _shouldCloseServer = true;
                }
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
                    namedPipeServerStream.Flush();
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
                tts.SpeakToStream(namedPipeServerStream, message);
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
