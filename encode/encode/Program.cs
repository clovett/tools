// Copyright © Microsoft Corporation
// All Rights Reserved.

using System;
using System.IO;
using System.Text;

internal class Program
{
    string encoding = null;
    string newEncoding = null;
    string path = null;

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: encode <options> filename");
        Console.WriteLine("-e name  specify the name of the encoding to use ");
        Console.WriteLine("-r name  specify new encoding to re-encode the file");
    }

    private bool ParseArgs(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg[0] == '-')
            {
                switch (arg)
                {
                    case "-e":
                        this.encoding = args[++i];
                        break;
                    case "-r":
                        this.newEncoding = args[++i];
                        break;
                    default:
                        Console.WriteLine("Unknown option: " + arg);
                        return false;
                }
                continue;
            }
            if (!string.IsNullOrEmpty(this.path))
            {
                Console.WriteLine("Too many arguments: " + arg);
                return false;
            }
            if (File.Exists(arg))
            {
                this.path = arg;
            }
            else
            {
                Console.WriteLine("File not found: " + arg);
                return false;
            }
        }
        if (string.IsNullOrEmpty(this.newEncoding))
        {
            Console.WriteLine("No -r encoding specified.");
            return false;
        }
        if (string.IsNullOrEmpty(this.path))
        {
            Console.WriteLine("No file specified.");
            return false;
        }
        return true;
    }

    internal void Run()
    {
        StreamReader streamReader = null;
        if (encoding != null)
        {
            streamReader = new StreamReader(path, Encoding.GetEncoding(encoding), detectEncodingFromByteOrderMarks: true);
        }
        else
        {
            streamReader = new StreamReader(path, detectEncodingFromByteOrderMarks: true);
        }

        string unicode = streamReader.ReadToEnd();
        streamReader.Close();
        
        Console.WriteLine("Re-encoding: " + newEncoding);
        using (StreamWriter streamWriter = new StreamWriter(path, append: false, Encoding.GetEncoding(newEncoding)))
        {
            streamWriter.Write(unicode);
        }
    }

    [STAThread]
    private static void Main(string[] args)
    {
        Program p = new Program();
        if (p.ParseArgs(args))
        {
            p.Run();
        }
        else
        {
            PrintUsage();
        }   
    }

}
