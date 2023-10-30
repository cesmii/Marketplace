namespace FilterLinesWith
{
    internal class Program
    {
        #pragma warning disable CS8602

        static void Main(string[] args)
        {
            string? strLine = "";
            bool bContinue = true;
            bool bFileFound = false;
            int cTotalWrittenLines =  0;
            int cTotalReadLines = 0;
            int cTotalIgnored = 0;
            int cIgnore = args.Length;

            string[]? astrIgnore = null;
            int[]? aiIgnored = null;

            // A file to read - from the 1st Arg
            System.IO.FileStream? fsInput = null;
            System.IO.StreamReader? fstream = null; 

            if (args.Length > 0)
            {
                if (File.Exists(args[0])) 
                {
                    bFileFound = true;
                    fsInput = new System.IO.FileStream(args[0],
                                  System.IO.FileMode.Open,
                                  System.IO.FileAccess.Read);
                    fstream = new System.IO.StreamReader(fsInput, System.Text.Encoding.UTF8);
                }

                if (bFileFound)
                {
                    astrIgnore = new string[cIgnore - 1];
                    aiIgnored = new int[cIgnore - 1];
                    for (int iArg = 1; iArg < cIgnore; iArg++)
                    {
                        astrIgnore[iArg - 1] = args[iArg];
                        aiIgnored[iArg - 1] = 0;
                    }

                    while (bContinue)
                    {
                        strLine = fstream.ReadLine();
                        int count = (strLine == null) ? -1 : strLine.Length;
                        if (count > 0)
                        {
                            cTotalReadLines++;

                            bool bWriteThis = true;
                            if (cIgnore > 0)
                            {
                                for (int iCheck = 0; iCheck < (cIgnore - 1); iCheck++)
                                {
                                    #pragma warning disable CS8602
                                    if (strLine.Contains(astrIgnore[iCheck]))
                                    {
                                        bWriteThis = false;
                                        aiIgnored[iCheck]++;
                                        cTotalIgnored++;
                                        break;
                                    }
                                }
                            }

                            if (bWriteThis)
                            {
                                Console.WriteLine($"{strLine}");
                                cTotalWrittenLines++;
                            }
                        }

                        bContinue = (strLine != null);
                    }

                    if (cIgnore == 0)
                    {
                        Console.WriteLine($"IgnoreLinesWith '<<nothing ignored>>': Ignored 0 and wrote {cTotalWrittenLines} lines.");
                    }
                    else
                    {
                        Console.WriteLine($"IgnoreLinesWith {cIgnore} items: Ignored {cTotalIgnored} lines and wrote {cTotalWrittenLines} lines.");
                        for (int iCheck = 0; iCheck < (cIgnore - 1); iCheck++)
                        {
                            Console.WriteLine($"IgnoreLinesWith #{iCheck + 1}: '{astrIgnore[iCheck]}': Ignored {aiIgnored[iCheck]} times.");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"IgnoreLinesWith: Cannot find input file {args[0]}");
                }
            }
        }
    }
}