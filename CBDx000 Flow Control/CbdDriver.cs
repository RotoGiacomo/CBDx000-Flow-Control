using System.IO.Ports;

namespace CBDx000FlowControl
{
    internal class CbdDriver
    {
        SerialPort Com = new();
        readonly object CompleteMon = new();
        string[] SheetsData = Array.Empty<string>();
        const int SHEET_WINDOW = 5;
        int StatusCounter = 0;
        int SheetSent = 0;
        int SheetInStack = 0;
        bool WaitingForOrder = false;

        // booklet is not sent all at once to avoid the intervention of XON, XOFF flow control
        int PrintMore(string[] SheetsInBook, int StackedSheets, int SentSheets)
        {
            int SendNow = 0;
            int SheetToBeSent = SheetsInBook.Length - SentSheets;
            int FlyingSheets = SentSheets - StackedSheets;
            int CanSendNow = SHEET_WINDOW - FlyingSheets;
            if (SheetToBeSent > 0)
            {
                SendNow = (SheetToBeSent < CanSendNow) ? SheetToBeSent : CanSendNow;
                if (SendNow > 0)
                {
                    for (int f = 0; f != SendNow; f++)
                    {
                        Com.WriteLine(SheetsInBook[SentSheets++]);
                        Console.WriteLine($"Sent sheet {SentSheets} of {SheetsInBook.Length}");
                    }
                }
            }
            return SendNow;
        }

        void Port_ReceivedEvent(object sender, SerialDataReceivedEventArgs e)
        {
            string reply = Com.ReadLine();
            if (reply.Length < 2)
                return;

            switch (reply[0])
            {
                // -----------------------------------------------------------------------------------------------
                // reading errors
                // -----------------------------------------------------------------------------------------------
                case ':':
                    if (WaitingForOrder)
                        break;

                    Console.WriteLine($"Reading Error");
                    Com.Write("@R");
                    WaitingForOrder = true;
                    break;
                // -----------------------------------------------------------------------------------------------
                // order acknoledge
                // -----------------------------------------------------------------------------------------------
                case '@':
                    WaitingForOrder = false;
                    break;
                // -----------------------------------------------------------------------------------------------
                // Stack occupation
                // -----------------------------------------------------------------------------------------------
                case 'S':
                    char[] terms = { '=', '+' };
                    int IndOfTerm = reply.IndexOfAny(terms, 1);
                    if (Int32.TryParse(reply[2..IndOfTerm], out SheetInStack))
                        Console.WriteLine($"Sheets in the stacker = {SheetInStack}");

                    if (SheetSent < SheetInStack)
                    {
                        Console.WriteLine("Cheques already present in the stack");
                        lock (CompleteMon)
                            Monitor.PulseAll(CompleteMon);
                        return;
                    }

                    SheetSent += PrintMore(SheetsData, SheetInStack, SheetSent);

                    if (SheetsData.Length == SheetInStack)
                    {
                        Console.WriteLine($"{SheetInStack} cheques printed");
                        lock (CompleteMon)
                            Monitor.PulseAll(CompleteMon);
                    }
                    break;
                // -----------------------------------------------------------------------------------------------
                // Status
                // -----------------------------------------------------------------------------------------------
                default:
                    UInt32 Status = Convert.ToUInt32(reply[0..8], 16);

                    if (WaitingForOrder)
                        break;

                    bool IsLow = reply.IndexOf('P', 8) != -1;
                    bool IsDouble = reply.IndexOf('D', 8) != -1;

                    // try to catch all sources of error
                    bool IsFailure = false;
                    IsFailure |= reply.IndexOf('E', 8) != -1;
                    IsFailure |= reply.IndexOf('F', 8) != -1;
                    IsFailure |= reply.IndexOf('J', 8) != -1;
                    IsFailure |= reply.IndexOf('W', 8) != -1;
                    IsFailure |= reply.IndexOf('O', 8) != -1;
                    IsFailure |= reply.IndexOf('T', 8) != -1;
                    IsFailure |= reply.IndexOf('U', 8) != -1;
                    IsFailure |= reply.IndexOf('V', 8) != -1;
                    if (IsFailure)
                    {
                        Console.WriteLine($"Print failure! Status:{reply}");
                        lock (CompleteMon)
                            Monitor.PulseAll(CompleteMon);
                        break;
                    }

                    // Periodically request the number of sheets parked in the stacker
                    // this "order" is not buffered and is executed as soon as it is received
                    if (IsLow)
                    {
                        Console.WriteLine($"Paper low");
                        Com.WriteLine("@R");
                        WaitingForOrder = true;
                    }
                    else if (IsDouble)
                    {
                        Console.WriteLine($"Double feed");
                        Com.WriteLine("@R");
                        WaitingForOrder = true;
                    } else if ((StatusCounter++ % 5) == 0)
                    {
                        Com.WriteLine("@S");
                    }
                    break;
            }
        
        }


        public void Run(string ComName, string[] Head, string[] SheetsDataToPrint)
        {
            SheetsData = SheetsDataToPrint;
            
            Console.WriteLine($"Printing {SheetsData.Length} sheets, CBD is connected to {ComName}");
            // serial port
            Com = new SerialPort(ComName, 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One)
            {
                Handshake = System.IO.Ports.Handshake.None, // Handshake managed by software
                WriteTimeout = (int)int.MaxValue            //  SerialPort.InfiniteTimeout;
            };
            Com.DataReceived += new SerialDataReceivedEventHandler(Port_ReceivedEvent);
            Com.Open();

            Com.Write("@C");
            Thread.Sleep(1000);

            // send init
            foreach( var line in Head )
                Com.WriteLine(line);

            Com.WriteLine("OQN");   // enable automatic status transmission every second
            Com.WriteLine("OBN");   // do not bind automatically when front cover is printed
            if (!Com.IsOpen)
            {
                Console.WriteLine($"Cannot open serial port");
                return;
            }

            // Wait here until CompleteMon is signalled
            lock (CompleteMon)
                Monitor.Wait(CompleteMon);

            // is the booklet complete?
            if (SheetsData.Length == SheetInStack)
            {
                Console.WriteLine($"Ejecting booklet");
                Com.WriteLine("E"); // eject to the client
            }
            else
            {
                Console.WriteLine($"Capturing Booklet");
                Com.WriteLine("R"); // capture in box
            }
        }


    }
}

