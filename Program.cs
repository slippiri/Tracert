using System;
using System.Net;
using System.Net.Sockets;

class Program
{
    static void Main(string[] args)
    {
        Console.Write("tracert ");
        string ipAddress = Console.ReadLine();

        if (!IPAddress.TryParse(ipAddress, out _))
        {
            Console.WriteLine("Неверный IP-адрес.");
            return;
        }

        int maxHops = 30;
        int timeout = 3000; 
        ushort identifier = 1234; 
        ushort sequenceNumber = 1; 

        Console.WriteLine($"Трассировка маршрута к {ipAddress} с максимальным числом прыжков {maxHops}:");

        using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp))
        {
            socket.ReceiveTimeout = timeout;

            for (int ttl = 1; ttl <= maxHops; ttl++)
            {
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, ttl);

                Console.Write($"{ttl}\t");

                IPAddress replyAddress = null;
                for (int i = 0; i < 3; i++) 
                {
                    byte[] packet = CreateIcmpPacket(identifier, sequenceNumber);
                    sequenceNumber++; 

                    DateTime startTime = DateTime.Now;
                    socket.SendTo(packet, new IPEndPoint(IPAddress.Parse(ipAddress), 0));

                    byte[] buffer = new byte[1024];
                    EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

                    try
                    {
                        int bytesReceived = socket.ReceiveFrom(buffer, ref remoteEndPoint);
                        TimeSpan roundTripTime = DateTime.Now - startTime;
                        replyAddress = ((IPEndPoint)remoteEndPoint).Address;

                        if (buffer[20] == 11 || buffer[20] == 0) 
                        {
                            Console.Write($"{roundTripTime.TotalMilliseconds} ms\t");
                        }
                    }
                    catch (SocketException)
                    {
                        Console.Write("*\t");
                    }
                }

                if (replyAddress != null)
                {
                    Console.WriteLine($"{replyAddress}");
                }
                else
                {
                    Console.WriteLine("Время ожидания истекло.");
                }

                if (replyAddress != null && replyAddress.ToString() == ipAddress)
                {
                    Console.WriteLine("Трассировка завершена.");
                    break;
                }
            }
        }
    }

    static byte[] CreateIcmpPacket(ushort identifier, ushort sequenceNumber)
    {
        byte[] packet = new byte[64];
        packet[0] = 8; 
        packet[1] = 0;
        BitConverter.GetBytes((ushort)0).CopyTo(packet, 2); 
        BitConverter.GetBytes(identifier).CopyTo(packet, 4); 
        BitConverter.GetBytes(sequenceNumber).CopyTo(packet, 6); 

        ushort checksum = CalculateChecksum(packet);
        BitConverter.GetBytes(checksum).CopyTo(packet, 2);

        return packet;
    }

    static ushort CalculateChecksum(byte[] buffer)
    {
        int length = buffer.Length;
        int i = 0;
        long sum = 0;

        while (length > 1)
        {
            sum += BitConverter.ToUInt16(buffer, i);
            i += 2;
            length -= 2;
        }

        if (length > 0)
            sum += buffer[i];

        sum = (sum >> 16) + (sum & 0xFFFF);
        sum += (sum >> 16);
        return (ushort)(~sum);
    }
}