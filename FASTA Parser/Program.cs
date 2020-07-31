using System;
using System.Collections.Generic;
using System.IO;

namespace Search16s
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] arguments = new string[3];

            arguments[0] = "Finding a sequence by ordinal position.\n\t Arguments: [line number] and [number of sequences]\n";
            arguments[1] = "Sequential access to a specific sequence by sequence-id.\n\t Arguments: [Sequence ID]\n";
            arguments[2] = "Sequential access to find a set of sequence-ids given in a query file,\n\t   and writing the output to a specified result file.\n\t Arguments: [Query file] [Output file]";


            if (args.Length < 2)
            {
                // Show the user the correct usage of the program
                Console.WriteLine("Usage: {0} -level[n] [DNA File.fasta] [Arguments]\nOptions for n:\n\t", System.AppDomain.CurrentDomain.FriendlyName);

                // Display all possible arguments to the user
                for (int i = 0; i < arguments.Length; i++)
                {
                    string output = string.Format("\t{0}: ", i + 1);
                    output = string.Concat(output, arguments[i]);
                    Console.WriteLine(output);
                }

                // Give an example
                Console.WriteLine("\n\tExample: {0}.exe -level1 16s.fasta 273 3", System.AppDomain.CurrentDomain.FriendlyName);
            }
            else
            {
                Parser parser = new Parser(args[1]);

                if (parser.isOkay())
                {
                    switch (args[0])
                    {
                        case "-level1":
                            {
                                if (args.Length != 4)
                                {
                                    Console.WriteLine("Missing/Incorrect arguments.\nCorrect usage: {0}.exe -level1 [DNA File.fasta] [line number] [number of sequences to get]", System.AppDomain.CurrentDomain.FriendlyName);
                                    return;
                                }

                                int lineNum;
                                if (Int32.TryParse(args[2], out lineNum))
                                {
                                    if (lineNum > 0 && lineNum % 2 != 0)
                                    {
                                        int numSequences;
                                        if (Int32.TryParse(args[3], out numSequences) && numSequences > 0)
                                        {
                                            string sequences = parser.GetDNASequencesByLine(lineNum, numSequences);

                                            if (parser.isOkay())
                                            {
                                                Console.WriteLine(sequences);
                                            }
                                            else Console.WriteLine(parser.GetLastErrorAsString());
                                        }
                                        else Console.WriteLine("ERROR: Please enter in a valid number of sequences to get");
                                    }
                                    else Console.WriteLine("ERRO: Line number MUST be an odd number and greater than 0");
                                }
                                else Console.WriteLine("ERROR: Please enter in a valid line number");
                                break;
                            }
                        case "-level2":
                            {
                                if (args.Length != 3)
                                {
                                    Console.WriteLine("Missing/Incorrect arguments.\nCorrect usage: {0}.exe -level2 filename.fasta [Sequence ID]", System.AppDomain.CurrentDomain.FriendlyName);
                                }
                                else
                                {
                                    string sequence = parser.GetDNABySequenceID(args[2]);

                                    if (parser.isOkay())
                                    {
                                        Console.WriteLine(sequence);
                                    }
                                    else if (parser.GetLastError() == Parser.Error.Sequence_Not_Found)
                                    {
                                        Console.WriteLine("Error, sequence {0} not found.", args[2]);
                                    }
                                }
                                break;
                            }
                        case "-level3":
                            {
                                if (args.Length != 4)
                                {
                                    Console.WriteLine("Missing/Incorrent arguments.\nCorrect usage: {0}.exe -level3 filename.fasta [Query file] [Output file]", System.AppDomain.CurrentDomain.FriendlyName);
                                }
                                else
                                {
                                    try
                                    {
                                        string[] lines = File.ReadAllLines(args[2]);

                                        string output = "";

                                        string temp = "";

                                        // Attempt to get all sequences in the file
                                        foreach (string line in lines)
                                        {
                                            temp = parser.GetDNABySequenceID(line);
                                            if (parser.isOkay())
                                            {
                                                output += temp + "\n";
                                            }
                                            else if (parser.GetLastError() == Parser.Error.Sequence_Not_Found) Console.WriteLine("Error, sequence {0} not found.", line);
                                        }

                                        // Write sequences to output file
                                        File.WriteAllText(args[3], output);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e.Message);
                                    }
                                }
                                break;
                            }
                        case "-level4":
                            {
                                if (args.Length != 5)
                                {
                                    Console.WriteLine("Missing/Incorrent arguments.\nCorrect usage: {0}.exe -level4 filename.fasta [Index File] [Query File] [Output file]", System.AppDomain.CurrentDomain.FriendlyName);
                                }
                                else
                                {
                                    try
                                    {
                                        parser.SetIndexFile(args[2]);

                                        if (parser.isOkay())
                                        {
                                            string[] lines = File.ReadAllLines(args[3]);

                                            string output = "";

                                            string temp = "";

                                            // Attempt to get all sequences in the file
                                            foreach (string line in lines)
                                            {
                                                temp = parser.GetDNABySequenceID(line, true);
                                                if (parser.isOkay())
                                                {
                                                    output += temp + "\n";
                                                }
                                                else if (parser.GetLastError() == Parser.Error.Sequence_Not_Found) Console.WriteLine("Error, sequence {0} not found.", line);
                                            }

                                            // Write sequences to output file
                                            File.WriteAllText(args[4], output);
                                        }
                                        else Console.WriteLine(parser.GetLastErrorAsString());
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e.Message);
                                    }
                                }
                                break;
                            }
                        case "-level5":
                            {
                                if (args.Length != 3)
                                {
                                    Console.WriteLine("Missing/Incorrent arguments.\nCorrect usage: {0}.exe -level5 filename.fasta [Nucleobase series]", System.AppDomain.CurrentDomain.FriendlyName);
                                }
                                else
                                {
                                    try
                                    {
                                        if (parser.isOkay())
                                        {
                                            List<string> sequenceIDS = parser.GetSequenceIDSByNucleobases(args[2]);

                                            if(parser.isOkay())
                                            {
                                                foreach (string sequenceID in sequenceIDS)
                                                {
                                                    Console.WriteLine(sequenceID);
                                                }
                                            }
                                            else Console.WriteLine(parser.GetLastErrorAsString());
                                        }
                                        else Console.WriteLine(parser.GetLastErrorAsString());
                                    }
                                    catch(Exception e)
                                    {
                                        Console.WriteLine(e.Message);
                                    }
                                }
                                break;
                            }
                        case "-level6":
                            {
                                if (args.Length != 3)
                                {
                                    Console.WriteLine("Missing/Incorrent arguments.\nCorrect usage: {0}.exe -level6 filename.fasta [Meta-data]", System.AppDomain.CurrentDomain.FriendlyName);
                                }
                                else
                                {
                                    try
                                    {
                                        if (parser.isOkay())
                                        {
                                            List<string> sequenceIDS = parser.GetSequenceIDSByMetaData(args[2]);

                                            if (parser.isOkay())
                                            {
                                                foreach (string sequenceID in sequenceIDS)
                                                {
                                                    Console.WriteLine(sequenceID);
                                                }
                                            }
                                            else Console.WriteLine(parser.GetLastErrorAsString());
                                        }
                                        else Console.WriteLine(parser.GetLastErrorAsString());
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e.Message);
                                    }
                                }
                                break;
                            }
                        case "-level7":
                            {
                                if (args.Length != 3)
                                {
                                    Console.WriteLine("Missing/Incorrent arguments.\nCorrect usage: {0}.exe -level7 filename.fasta [Nucleobase series]", System.AppDomain.CurrentDomain.FriendlyName);
                                }
                                else
                                {
                                    try
                                    {
                                        if (parser.isOkay())
                                        {
                                            List<string> sequenceIDS = parser.GetSequenceIDSByNucleobases(args[2], true);

                                            if (parser.isOkay())
                                            {
                                                foreach (string sequenceID in sequenceIDS)
                                                {
                                                    Console.WriteLine(sequenceID);
                                                }
                                            }
                                            else Console.WriteLine(parser.GetLastErrorAsString());
                                        }
                                        else Console.WriteLine(parser.GetLastErrorAsString());
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e.Message);
                                    }
                                }
                                break;
                            }
                        default:
                            {
                                Console.WriteLine("ERROR: Invalid level entered");
                                break;
                            }

                    }
                }
                else Console.WriteLine("ERROR: Failed to initialise Parser object, Parser states: \n{0}", parser.GetLastErrorAsString());
            }
        }
    }
}
