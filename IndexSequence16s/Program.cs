using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace IndexSequence16s
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("ERROR: Not enough arguments given");
                Console.WriteLine("Correct usage: {0}.exe [FASTA File] [Output (Index file)]", System.AppDomain.CurrentDomain.FriendlyName);
                Console.ReadLine();
            }
            else
            {
                try
                {
                    // Open FASTA file
                    //FileStream file = new FileStream(args[0], FileMode.Open, FileAccess.Read);
                    StreamReader reader = new StreamReader(args[0]);

                    MatchCollection collection;
                    string line;
                    long position = 0;

                    // index [DNA Sequence, Offset start, Offset end]
                    List<Tuple<string, long>> index = new List<Tuple<string, long>>();
                    
                    // Begin indexing
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Find all DNA sequences in line
                        collection = Regex.Matches(line, @"(NR_\d+.\d+)");

                        if(collection.Count > 0)
                        {
                            // Add all found DNA sequences to index List
                            foreach(Match DNA in collection)
                            {
                                index.Add(Tuple.Create(DNA.Value, position));
                            }
                        }
                        position = position + line.Length + 1;
                    }
                    reader.Close();

                    // Write to index file
                    using (FileStream file = new FileStream(args[1], FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        using (StreamWriter writer = new StreamWriter(file))
                        {
                            foreach (var sequence in index)
                            {
                                writer.WriteLine(sequence.Item1 + " " + sequence.Item2);
                            }
                        }
                    }
                       
                }
                catch(Exception e)
                {
                    Console.WriteLine("ERROR: {0}", e.Message);
                    Console.ReadLine();
                }
                
            }
        }
    }
}
