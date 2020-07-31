using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace Search16s
{
    class Parser
    {
        /// <summary>
        /// The constructor of the Parser class
        /// </summary>
        /// <param name="source">The location of the FASTA file</param>
        public Parser(string source)
        {
            //src of possible extensions: https://en.wikipedia.org/wiki/FASTA_format#FASTA_file
            if (source.Contains("fasta") || source.Contains("fna") || source.Contains("ffn") || source.Contains("faa") || source.Contains("frn"))
            {
                try
                {
                    file = new FileStream(source, FileMode.Open, FileAccess.Read);
                    reader = new StreamReader(file);

                    index = new Dictionary<string, long>();

                    // Get number of lines in file && validate file
                    string sequence = "";
                    string[] DNA = new string[2];

                    int currentLine = 1;
                    bool running = true;
                    while (!reader.EndOfStream && running)
                    {
                        numLines++;

                        sequence = reader.ReadLine();
                        DNA[currentLine - 1] = sequence;

                        // Validate DNA Metadata
                        if (!Regex.IsMatch(DNA[0], @">[a-zA-Z][a-zA-Z]_\d+.\d.+"))
                        {
                            SetError(Error.Bad_File);
                            break;
                        }

                        if (currentLine == 2)
                        {
                            // Validate nucleobases(?)
                            foreach (char character in DNA[1])
                            {
                                // Check if the character is A-Z or a-z using ASCII table
                                //Src: https://en.wikipedia.org/wiki/FASTA_format#Sequence_representation
                                if (!isValidNucleoBase(character))
                                {
                                    SetError(Error.Bad_File);
                                    running = false;
                                    break;
                                }
                            }

                            DNA[0] = "";
                            DNA[1] = "";
                            sequence = "";
                            currentLine = 0;
                        }
                        currentLine++;
                    }
                    if (GetLastError() == Error.none)
                    {
                        if (numLines == 0)
                        {
                            SetError(Error.Empty_File);
                        }
                        else Reset();
                    }
                }
                catch (Exception)
                {
                    SetError(Error.Cant_Read_File);
                }
            }
            else
            {
                SetError(Error.Wrong_File_Format);
            }

        }
        ~Parser()
        {
            reader.Dispose();
            file.Dispose();
        }
        /// <summary>
        /// Retrieves a DNA sequence by line number
        /// </summary>
        /// <param name="lineNum">The line number to start searching from</param>
        /// <param name="numSequences">The number of DNA sequences to retrieve</param>
        /// <returns></returns>
        public string GetDNASequencesByLine(int lineNum, int numSequences)
        {
            Reset();

            string output = "";
            if (file.CanRead)
            {
                // Skip unwanted lines
                if (SkipToLine(lineNum))
                {
                    // Get sequences
                    for (int i = 0; i < numSequences * 2; i++) output += reader.ReadLine() + "\n";
                }
            }
            return output;
        }
        /// <summary>
        /// Retrieves a specific DNA sequence by its ID
        /// </summary>
        /// <param name="sequenceID">The sequence ID of the target DNA. This can be a partial string and not the full ID of the DNA</param>
        /// <param name="useIndex">Specifices whether to use the given index file or not (if applicable)</param>
        /// <returns>The metadata and composition of the DNA</returns>
        public string GetDNABySequenceID(string sequenceID, bool useIndex = false)
        {
            Reset();

            string output = "";
            string line = "";

            if(useIndex)
            {
                if (index.ContainsKey(sequenceID))
                {
                    file.Seek(index[sequenceID], SeekOrigin.Begin);
                }
                else SetError(Error.Sequence_Not_Found);
            }
            
            // Make sure that the sequence was found if using indexing
            if(lastError == Error.none)
            {
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    if (line.Contains(">" + sequenceID))
                    {
                        // Get DNA metadata
                        output = line + "\n";
                        // Get composition of DNA
                        output += reader.ReadLine();
                        break;
                    }
                }
                if (output.Length == 0) SetError(Error.Sequence_Not_Found);
            }

            return output;
        }


        /// <summary>
        /// Retrieves all Sequence IDs that contain a given Nucleobase.
        /// </summary>
        /// <param name="nucleoBases">The Nucleobase to search for.</param>
        /// <returns>List of Sequence IDs that contain the given Nucleobase.</returns>
        public List<string> GetSequenceIDSByNucleobases(string nucleoBases, bool containsWildcard = false)
        {
            Reset();
            List<string> sequenceIDS = new List<string>();
            string line;
            string sequenceID = "";

            if(containsWildcard)
            {
                // Replace all wildcards with [a-zA-Z]*
                nucleoBases = nucleoBases.Replace("*", "[a-zA-Z]*");
            }

            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();

                // If it's the DNA metadata
                if(Regex.IsMatch(line, @">[a-zA-Z][a-zA-Z]_\d+.\d.+"))
                {
                    // Store it for use later
                    sequenceID = line;
                }
                else
                {
                    // Check given nucleobases to make sure that they are all valid
                    if(!containsWildcard)
                    {
                        foreach (char nucleoBase in nucleoBases)
                        {
                            if (!isValidNucleoBase(nucleoBase))
                            {
                                SetError(Error.Bad_Nucleobase);
                                break;
                            }
                        }
                        //ACTG[a-zA-Z]*GTAC[a-zA-Z]*CA
                    }

                    // It's the compositon of the DNA (nucleobases)
                    if(isOkay() && Regex.IsMatch(line, nucleoBases))
                    {
                        //AAGTCGAGCGATGGCGC
                        // DNA contains nucleobases that we are looking for
                        // now we need to find how many DNA there are for this composition
                        MatchCollection foundSequenceIDS = Regex.Matches(sequenceID, @"(NR_\d+.\d+)");

                        if(foundSequenceIDS.Count > 0)
                        {
                            // Get all sequence IDS found and add them to the List sequenceIDS
                            foreach(Match sequence in foundSequenceIDS)
                            {
                                sequenceIDS.Add(sequence.Value);
                            }
                        }
                    }
                }
            }
            return sequenceIDS;
        }

        /// <summary>
        /// Retrieves Sequence IDs by a meta-data search.
        /// </summary>
        /// <param name="metaData">The meta-data to search for.</param>
        /// <returns>List of all Sequences that have the contain the meta-data.</returns>
        public List<string> GetSequenceIDSByMetaData(string metaData)
        {
            Reset();
            List<string> sequenceIDS = new List<string>();

            string line;

            while(!reader.EndOfStream)
            {
                line = reader.ReadLine();

                if(Regex.IsMatch(line, string.Format(@"\b{0}\b", metaData), RegexOptions.IgnoreCase))
                {
                    MatchCollection foundSequenceIDS = Regex.Matches(line, @"(NR_\d+.\d+)");

                    if (foundSequenceIDS.Count > 0)
                    {
                        // Get all sequence IDS found and add them to the List sequenceIDS
                        foreach (Match sequence in foundSequenceIDS)
                        {
                            sequenceIDS.Add(sequence.Value);
                        }
                    }
                }
            }

            return sequenceIDS;
        }

        /// <summary>
        /// Skips to a specific line in the file.
        /// Used for internal purposes only.
        /// </summary>
        /// <param name="lineNum">The line number to jump to</param>
        /// <returns>True if successfuly jumped to target line, else false.</returns>
        private bool SkipToLine(int lineNum)
        {
            if (file.CanRead)
            {
                if (lineNum > numLines)
                {
                    SetError(Error.Invalid_Line_Number);
                    return false;
                }
                try
                {
                    for (int i = 1; i < lineNum; i++)
                        reader.ReadLine();
                }
                catch
                {
                    SetError(Error.Cant_Read_Line);
                }
            }
            return true;
        }

        /// <summary>
        /// Determines whether or not the parser object is ready for usage.
        /// </summary>
        /// <returns>True if the parser object is ready to be used, else false.</returns>
        public bool isOkay()
        {
            if (file != null && reader != null && !errorTriggered)
                return true;
            else return false;
        }
        /// <summary>
        /// Resets the FileStream file and StreamReader reader back to their starting positions.
        /// This should be used before attempting to read from the file.
        /// </summary>
        private void Reset()
        {
            reader.DiscardBufferedData();
            file.Seek(0, SeekOrigin.Begin);
            SetError(Error.none);
            errorTriggered = false;
        }

        private bool isValidNucleoBase(char character)
        {
            if (((int)character > 90 || (int)character < 65) && ((int)character > 122 || (int)character < 97))
                return false;
            else return true;
        }

        /// <summary>
        /// Sets the Index file for the Parser to use.
        /// </summary>
        /// <param name="filePath">The location of the Index file.</param>
        public void SetIndexFile(string filePath)
        {
            try
            {
                string[] entries = File.ReadAllLines(filePath);

                string[] line;
                long offset = 0;

                // Begin indexing
                foreach(string entry in entries)
                {
                    // Split the current line using space
                    // This results in giving us both the SequenceID and offset
                    line = entry.Split(' ');

                    if(line.Count() == 2)
                    {
                        // Convert the offset from string to long
                        if (Int64.TryParse(line[1], out offset))
                        {
                            // Add the current SequenceID and offset to index
                            index.Add(line[0], offset);
                        }
                        else
                        {
                            SetError(Error.Couldnt_Load_Index_Entry);
                            break;
                        }
                    }
                    else
                    {
                        SetError(Error.Couldnt_Load_Index_Entry);
                        break;
                    }
                }
            }
            catch(Exception)
            {
                SetError(Error.Couldnt_Set_Index_File);
            }
        }

        /// <summary>
        /// Sets the current state or error of the parser.
        /// </summary>
        /// <param name="error"></param>
        private void SetError(Error error)
        {
            lastError = error;
            errorTriggered = true;
        }

        /// <summary>
        /// Retrieves the last error encountered by the parser
        /// </summary>
        /// <returns>The error encountered</returns>
        public Error GetLastError()
        {
            return lastError;
        }

        /// <summary>
        /// Retrieves the last error encountered by the parser
        /// </summary>
        /// <returns>Last error encountered</returns>
        public String GetLastErrorAsString()
        {
            string output = "Error: ";
            switch (lastError)
            {
                case Error.Invalid_Line_Number:
                    {
                        output += string.Format("Invalid line number entered. Maximum is {0:n0}", numLines);
                        break;
                    }
                case Error.Cant_Read_Line:
                    {
                        output += "Could not read line.";
                        break;
                    }
                case Error.Sequence_Not_Found:
                    {
                        output += "Specific sequence requested was not found.";
                        break;
                    }
                case Error.Wrong_File_Format:
                    {
                        output += "Wrong DNA file given, must be of FASTA format.";
                        break;
                    }
                case Error.Cant_Read_File:
                    {
                        output += "Could not read FASTA file, please make sure it exists and adequate permissions are set.";
                        break;
                    }
                case Error.Bad_File:
                    {
                        output += "FASTA file contains missing information or is corrupt. Possible line of fault: " + numLines;
                        break;
                    }
                case Error.Empty_File:
                    {
                        output += "FASTA file seems to be empty.";
                        break;
                    }
                case Error.Couldnt_Set_Index_File:
                    {
                        output += "Could not set Index file. Please make sure it exists.";
                        break;
                    }
                case Error.Couldnt_Load_Index_Entry:
                    {
                        output += "Failed to load an Index Entry. Please check Index file and make sure that it is valid.";
                        break;
                    }
                case Error.Bad_Nucleobase:
                    {
                        output += "Bad Nucleobase given. Please make sure that all given Nucleobases range from A to Z.";
                        break;
                    }
                case Error.none:
                    {
                        output += "None.";
                        break;
                    }
            }
            return output;
        }

        /// <summary>
        /// Retrieves the number of lines in the file.
        /// </summary>
        /// <returns>Number of lines in file</returns>
        public int GetNumberOfLines()
        {
            return numLines;
        }

        /// <summary>
        /// List of all possible errors that a parser can encounter
        /// </summary>
        public enum Error
        {
            none,
            Invalid_Line_Number,
            Cant_Read_Line,
            Sequence_Not_Found,
            Wrong_File_Format,
            Cant_Read_File,
            Bad_File,
            Empty_File,
            Couldnt_Set_Index_File,
            Couldnt_Load_Index_Entry,
            Bad_Nucleobase
        }

        private FileStream file = null;
        private StreamReader reader = null;
        private bool errorTriggered = false; // Did the Parser object encounter an error?
        private Error lastError = Error.none; // The last Error encountered by the Parser object
        private int numLines = 0; // The number of lines in the file
        private Dictionary<string, long> index; // Index of the FASTA file
    }

}
