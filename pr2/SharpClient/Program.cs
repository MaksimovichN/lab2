using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SharpClient
{
    class Program
    {
        static Encoding cp866 = Encoding.GetEncoding("CP866");

        enum Messages
        {
            M_INIT      =0,
            M_EXIT      =1,
            M_GETDATA   =2,
            M_NODATA    =3,
            M_DATA      =4,
            M_CONFIRM   =5
        };

        enum Members
        {
            M_BROKER = 0,
            M_ALL = 10,
            M_USER = 100
        };

        public struct MsgHeader
        {
            public int m_To;
            public int m_From;
            public int m_Type;
            public int m_Size;
            public MsgHeader(int m_To, int m_From, int m_Type, int m_Size)
            {
                this.m_To = m_To;
                this.m_From = m_From;
                this.m_Type = m_Type;
                this.m_Size = m_Size;
            }

            public void Send(ref Socket s)
            {
                s.Send(BitConverter.GetBytes(this.m_To), sizeof(int), SocketFlags.None);
                s.Send(BitConverter.GetBytes(this.m_From), sizeof(int), SocketFlags.None);
                s.Send(BitConverter.GetBytes(this.m_Type), sizeof(int), SocketFlags.None);
                s.Send(BitConverter.GetBytes(this.m_Size), sizeof(int), SocketFlags.None);
            }
        };

        public class Message
        {
            public MsgHeader m_Header;
            public string m_Data;
            public Message(int To, int From, int Type = (int)Messages.M_DATA, string str = "")
            {
                m_Header = new MsgHeader(To, From, Type, str.Length);

                m_Data = str;
            }

            public void Send(Message msg, ref Socket s)
            {
                msg.m_Header.Send(ref s);
                if (msg.m_Header.m_Size != 0)
                {
                    s.Send(BitConverter.GetBytes(msg.m_Data.Length), sizeof(int), SocketFlags.None);
                    s.Send(cp866.GetBytes(msg.m_Data), msg.m_Data.Length, SocketFlags.None);
                }
            }

            public int ReceiveHeaders(Socket s)
            {
                byte[] b = new byte[sizeof(int)];
                s.Receive(b, sizeof(int), SocketFlags.None);
                return BitConverter.ToInt32(b, 0);
            }

            public int Receive(Message msg, ref Socket s)
            {
                msg.m_Header.m_To = ReceiveHeaders(s);
                msg.m_Header.m_From = ReceiveHeaders(s);
                msg.m_Header.m_Type = ReceiveHeaders(s);
                msg.m_Header.m_Size = ReceiveHeaders(s);
                if (msg.m_Header.m_Size != 0)
                {
                    byte[] b = new byte[msg.m_Header.m_Size];
                    s.Receive(b, msg.m_Header.m_Size, SocketFlags.None);
                    msg.m_Data = cp866.GetString(b, 0, msg.m_Header.m_Size);
                }
                return msg.m_Header.m_Type;
            }

            public void SendMessage(ref Socket s, int To, int From, int Type = (int)Messages.M_DATA, string str = "")
            {
                Message msg = new Message(To, From, Type, str);
                Send(msg, ref s);
            }
        };

        public static void ReceiveMsg(int ClientID, Message m)
        {
            for(; ; )
            {
                int nPort = 12345;
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), nPort);
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                s.Connect(endPoint);
                m.SendMessage(ref s, (int)Members.M_BROKER, ClientID, (int)Messages.M_GETDATA);
                if(m.Receive(m, ref s) == (int)Messages.M_GETDATA)
                    Console.WriteLine(m.m_Data);
                s.Disconnect(true);
            }
        }


        static void Main(string[] args)
        {
            int nPort = 12345;
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), nPort);
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.Connect(endPoint);
            Message m = new Message((int)Members.M_BROKER, 0, (int)Messages.M_INIT);
            m.SendMessage(ref s, (int)Members.M_BROKER, 0, (int)Messages.M_INIT);
            int ClientID = m.m_Header.m_To;
            s.Disconnect(true);
            Thread t = new Thread(() => ReceiveMsg(ClientID, m));
            t.Start();
            int MsgType, Prov, IdRec;
            string Data1, Data2;
            for (; ; )
            {
                Console.WriteLine( "Отправить сообщение - 0; Выйти -1: ");
                Data1 = Console.ReadLine();
                Prov = Convert.ToInt32(Data1);
                switch (Prov)
                {
                    case 0:
                        {
                            Console.WriteLine("Личное - 0; Широковещательное - 1: ");
                            Data1 = Console.ReadLine();
                            MsgType = Convert.ToInt32(Data1);
                            Console.WriteLine("Введите текст сообщения: ");
                            Data2 = Console.ReadLine();
                            switch (MsgType)
                            {
                                case 0:
                                    {
                                        Console.WriteLine("Введите получателя: ");
                                        Data1 = Console.ReadLine();
                                        IdRec = Convert.ToInt32(Data1);
                                        s.Connect(endPoint);
                                        m.SendMessage(ref s, IdRec, ClientID, (int)Messages.M_DATA, Data2);
                                        s.Disconnect(true);
                                        break;
                                    }
                                case 1:
                                    {
                                        s.Connect(endPoint);
                                        m.SendMessage(ref s, (int)Members.M_ALL, ClientID, (int)Messages.M_DATA, Data2);
                                        s.Disconnect(true);
                                        break;
                                    }
                            }
                            break;
                        }
                    case 1:
                        {
                            s.Connect(endPoint);
                            m.SendMessage(ref s, (int)Members.M_BROKER, ClientID, (int)Messages.M_EXIT);
                            s.Disconnect(false);
                            return;
                        }
                }
            }
        }
    }
}
