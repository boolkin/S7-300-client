/*=============================================================================|
|  PROJECT Sharp7                                                        1.0.0 |
|==============================================================================|
|  Copyright (C) 2013, Davide Nardella                                         |
|  All rights reserved.                                                        |
|==============================================================================|
|  SNAP7 is free software: you can redistribute it and/or modify               |
|  it under the terms of the Lesser GNU General Public License as published by |
|  the Free Software Foundation, either version 3 of the License, or           |
|  (at your option) any later version.                                         |
|                                                                              |
|  It means that you can distribute your commercial software linked with       |
|  SNAP7 without the requirement to distribute the source code of your         |
|  application and without the requirement that your application be itself     |
|  distributed under LGPL.                                                     |
|                                                                              |
|  SNAP7 is distributed in the hope that it will be useful,                    |
|  but WITHOUT ANY WARRANTY; without even the implied warranty of              |
|  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the               |
|  Lesser GNU General Public License for more details.                         |
|                                                                              |
|  You should have received a copy of the GNU General Public License and a     |
|  copy of Lesser GNU General Public License along with Snap7.                 |
|  If not, see  http://www.gnu.org/licenses/                                   |
|==============================================================================|
|                                                                              |
|  Client Example                                                              |
|                                                                              |
|=============================================================================*/
using System;
using System.Runtime.InteropServices;
using System.Text;
using Sharp7;
using System.Net;
using System.Net.Sockets;

class ClientDemo
{
    static S7Client Client;
    static byte prevByte = 0;

    //------------------------------------------------------------------------------
    // Check error (simply writes an header)
    //------------------------------------------------------------------------------
    static bool Check(int Result, string FunctionPerformed)
    {
        Console.WriteLine();
        Console.WriteLine("+-----------------------------------------------------");
        Console.WriteLine("| " + FunctionPerformed);
        Console.WriteLine("+-----------------------------------------------------");
        if (Result == 0)
        {
            int ExecTime = Client.ExecTime();
            Console.WriteLine("| Result         : OK");
            Console.WriteLine("| Execution time : " + ExecTime.ToString() + " ms"); //+ Client.getex->ExecTime());
            Console.WriteLine("+-----------------------------------------------------");
        }
        else
        {
            Console.WriteLine("| ERROR !!! \n");
            if (Result < 0)
                Console.WriteLine("| Library Error (-1)\n");
            else
                Console.WriteLine("| " + Client.ErrorText(Result));
            Console.WriteLine("+-----------------------------------------------------\n");
        }
        return Result == 0;
    }


    //-------------------------------------------------------------------------  
    // PLC connection
    //-------------------------------------------------------------------------  
    static bool PlcConnect(string Address, int Rack, int Slot)
    {
        int res = Client.ConnectTo(Address, Rack, Slot);
        if (Check(res, "UNIT Connection"))
        {
            int Requested = Client.RequestedPduLength();
            int Negotiated = Client.NegotiatedPduLength();
            Console.WriteLine("  Connected to   : " + Address + " (Rack=" + Rack.ToString() + ", Slot=" + Slot.ToString() + ")");
            Console.WriteLine("  PDU Requested  : " + Requested.ToString());
            Console.WriteLine("  PDU Negotiated : " + Negotiated.ToString());
        }
        return res == 0;
    }

    static void ReadData()
    {
        byte[] Buffer = new byte[1];
        // Read one byte mb20
        int result = Client.ReadArea(S7Consts.S7AreaMK, 0, 20, 1, S7Consts.S7WLByte, Buffer);

        if (result == 0)
        {
            SendByUDP(Buffer, "192.168.1.100", 5550);
            if (prevByte != Buffer[0])
            {
                System.IO.File.AppendAllText("C:\\dat\\CRM-stop.csv", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ";" + prevByte.ToString() + ";" + Buffer[0].ToString() + Environment.NewLine);
                prevByte = Buffer[0];
            }
        }
        else
        {
            Console.WriteLine("  ERROR : " + result);
        }
    }

    private static void SendByUDP(byte[] datagram, string IPaddr, int remotePort)
    {
        IPAddress remoteIPAddress = IPAddress.Parse(IPaddr);
        // Создаем UdpClient
        UdpClient sender = new UdpClient();
        // Создаем endPoint по информации об удаленном хосте
        IPEndPoint endPoint = new IPEndPoint(remoteIPAddress, remotePort);
        try
        {
            sender.Send(datagram, datagram.Length, endPoint);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Возникло исключение UDP: " + ex.ToString() + "\n  " + ex.Message);
            System.IO.File.WriteAllText("C:\\logs\\_udp_err" + DateTime.Now.ToString("HHmmss") + ".txt", "UDP err: " + ex.ToString() + "\n " + ex.Message);
        }
        finally
        {
            // Закрыть соединение
            sender.Close();
        }
    }

    //-------------------------------------------------------------------------  
    // Main                                  
    //-------------------------------------------------------------------------  
    public static void Main(string[] args)
    {
        int Rack = 0, Slot = 2; // default for S7300
        System.IO.Directory.CreateDirectory("C:\\dat\\");
        if (!System.IO.File.Exists("C:\\dat\\CRM-stop.csv"))
        {
            System.IO.File.WriteAllText("C:\\dat\\CRM-stop.csv", "Date-Time;Prev;Now" + Environment.NewLine);
        }
        // Client creation
        Client = new S7Client();
        // Try Connection
        if (PlcConnect("192.168.1.5", Rack, Slot))
        {
            while (true)
            {
                ReadData();
                System.Threading.Thread.Sleep(5000);
            }

            Client.Disconnect();
        }
        Console.ReadKey();
    }
}
