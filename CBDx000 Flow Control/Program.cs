using CBDx000FlowControl;
using System.IO;
using System.IO.Ports;

class App
{
    static void Main(string[] args)
    {


        string ComName = args.Length == 0 ? "COM114" : args[0];
        string FileName1 = args.Length < 2 ? "booklet.txt" : args[1];
        string FileName2 = args.Length < 3 ? "head.txt" : args[2];

        List<string> Sheets = new();
        List<string> Head = new();

        // read the booklet data from file
        using (StreamReader sm = File.OpenText(FileName1))
        {
            while (true)
            {
                string? line = sm.ReadLine();
                if (line == null)
                    break;
                if (line.Length != 0)
                {
                    Sheets.Add(line);
                }

            }
        }

        // read initialization from file (cheque dimension, printer initialization, etc..)
        using (StreamReader sm = File.OpenText(FileName2))
        {
            while (true)
            {
                string? line = sm.ReadLine();
                if (line == null)
                    break;
                if (line.Length != 0)
                {
                    Head.Add(line);
                }

            }
        }

        CbdDriver Cbd = new();
        Cbd.Run(ComName, Head.ToArray(), Sheets.ToArray());



    }
}






