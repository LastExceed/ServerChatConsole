using System;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using Tools;

namespace ConsoleClient
{
    class Program
    {
        TcpClient tcpclnt = new TcpClient();
        BinaryWriter writer;
        BinaryReader reader;
        ulong guid;

        static void Main(string[] args)
        {
            Program program = new Program();
            program.setup();
        }

        public void setup()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("enter server ip");
            Console.ForegroundColor = ConsoleColor.White;
            string ip = Console.ReadLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("choose a name");
            Console.ForegroundColor = ConsoleColor.White;
            byte[] nameBytes = Encoding.UTF8.GetBytes(Console.ReadLine());

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("connecting...");
            tcpclnt.Connect(ip, 12345);
            writer = new BinaryWriter(tcpclnt.GetStream());
            reader = new BinaryReader(tcpclnt.GetStream());
            Console.WriteLine("connection established");

            Console.WriteLine("setting up chat_bot...");
            writer.Write(0x11); //version packet
            writer.Write(3); //version 0.03

            reader.ReadUInt32();//packet ID (join packet: 16
            reader.ReadUInt32();//unknown
            guid = reader.ReadUInt64(); //GUID
            reader.ReadBytes(0x1168);//junk

            writer.Write(0); //packet ID
            byte[] uncompressed = File.ReadAllBytes("c:/dev/StreamReader/entityData.dat");
            uncompressed[0] = (byte)guid;
            Array.Copy(nameBytes, 0, uncompressed, 0x1122, nameBytes.Length);
            byte[] compressed = zlib.Compress(uncompressed);
            writer.Write((uint)compressed.Length);
            writer.Write(compressed);

            Console.WriteLine("chat bot online");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("======================");
            Console.ForegroundColor = ConsoleColor.White;

            Thread Thread_packetSeeker = new Thread(new ThreadStart(lookForPackets));
            Thread_packetSeeker.Start();
            Thread Thread_antiTO = new Thread(new ThreadStart(antiTimeOut));
            Thread_antiTO.Start();
            Thread Thread_InputReader = new Thread(new ThreadStart(readInput));
            Thread_InputReader.Start();  
        }

        public void antiTimeOut()
        {
            while (true)
            {
                byte[] empty = new byte[16];
                empty[0] = (byte)guid;
                byte[] emptyC = zlib.Compress(empty);
                writer.Write(0);
                writer.Write((uint)emptyC.Length);
                writer.Write(emptyC);
                Thread.Sleep(5000);
            }
        }

        public void readInput()
        {
            while (true)
            {
                string message = Console.ReadLine();
                byte[] data = Encoding.Unicode.GetBytes(message);
                writer.Write((uint)0xA);
                writer.Write(data.Length / 2);
                writer.Write(data);
            }
        }

        public void lookForPackets()
        {
            uint packetID;
            int size;
            byte[] compressed;
            byte[] uncompressed;
            ulong sender;
            int length;
            string message;

            while (true)
            {
                packetID = reader.ReadUInt32();
                switch (packetID)
                {
                    case 0:
                        size = reader.ReadInt32();
                        compressed = reader.ReadBytes(size);
                        uncompressed = zlib.Uncompress(compressed);
                        break;

                    case 2:
                        break;

                    case 4:
                        size = reader.ReadInt32();
                        compressed = reader.ReadBytes(size);
                        break;

                    case 5:
                        reader.ReadUInt32(); //day
                        reader.ReadUInt32(); //time
                        break;

                    case 10:
                        sender = reader.ReadUInt64();
                        length = reader.ReadInt32();
                        message = Encoding.Unicode.GetString(reader.ReadBytes(length * 2));
                        if (sender == 0)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.WriteLine(message);
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        else if (sender == guid)
                        {
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.Write(sender + ": ");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine(message);
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write(sender + ": ");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine(message);
                        }
                        break;

                    case 15:
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write("mapseed: " + reader.ReadUInt32());
                        Console.ForegroundColor = ConsoleColor.White;
                        break;

                    default:
                        //DebugLog("Unkonw opcode:   " + ID);
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write("mapseed: " + reader.ReadUInt32());
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                }
            }
        }
    }
}
