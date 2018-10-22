using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace DevMaid.Commands
{
    public static class Geral
    {
        public static void CsvToClass(string inputFile = @"./arquivo.csv")
        {
            if (!File.Exists(inputFile))
            {
                throw new System.ArgumentException("Can`t find the input file");
            }

            string[] lines = File.ReadAllLines(inputFile);

            /*
             * select column_name, data_type, is_nullable from information_schema.columns where table_name = '';
             */
            foreach (string line in lines)
            {
                // Console.WriteLine(line);
                var quebrando = line.Split(",");
                if (quebrando.Length <= 0 || string.IsNullOrEmpty(line) || quebrando[0].Contains("column_name"))
                {
                    continue;
                }

                var tipos = new Dictionary<string, string>
                {
                    { "bigint" , "long"  },
                    { "binary" , "byte[]"  },
                    { "bit" , "bool"  },
                    { "char" , "string"  },
                    { "date" , "DateTime"  },
                    { "datetime" , "DateTime"  },
                    { "datetime2" , "DateTime"  },
                    { "datetimeoffset" , "DateTimeOffset"  },
                    { "decimal" , "decimal"  },
                    { "float" , "float"  },
                    { "image" , "byte[]"  },
                    { "int" , "int"  },
                    { "money" , "decimal"  },
                    { "nchar" , "char"  },
                    { "ntext" , "string"  },
                    { "numeric" , "decimal"  },
                    { "nvarchar" , "string"  },
                    { "real" , "double"  },
                    { "smalldatetime" , "DateTime"  },
                    { "smallint" , "short"  },
                    { "smallmoney" , "decimal"  },
                    { "text" , "string"  },
                    { "time" , "TimeSpan"  },
                    { "timestamp" , "DateTime"  },
                    { "tinyint" , "byte"  },
                    { "uniqueidentifier" , "Guid"  },
                    { "\"character varying\"", "string" },
                    { "character", "string" }
                };

                var nulo = quebrando[2] == "YES" ? "?" : "";
                var tabelainfo = new { coluna = quebrando[0], tipo = tipos.GetValueOrDefault(quebrando[1].Trim()), Nulo = nulo };
                // quebrando[2] = quebrando[2].Replace("'","''");

                var strbuild = new StringBuilder();

                strbuild.Append($"[Column(\"{tabelainfo.coluna}\")]");
                strbuild.Append("\n");
                strbuild.Append($"public {tabelainfo.tipo}");
                if (tabelainfo.tipo != "string")
                {
                    strbuild.Append($"{tabelainfo.Nulo}");
                }
                strbuild.Append($" {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tabelainfo.coluna)} " + "{ get; set; }");
                strbuild.Append("\n");


                using (System.IO.StreamWriter file = new StreamWriter(@"./saida.class", true))
                {
                    // Console.WriteLine(template);
                    file.WriteLine(strbuild.ToString());
                    //file.WriteLine("\n");
                }
            }
        }

        public static async Task TableToClass(string connectionString, string tableName)
        {
            var tableColumns = await GetColumnsInfo(connectionString, tableName);
            if (tableColumns.Count <= 0)
            {
                throw new System.ArgumentException("Erro ao obter informações da tabela.");
            }

            foreach (var tableColumn in tableColumns)
            {
                var tiposDoBanco = new Dictionary<string, string>
                {
                    { "bigint" , "long"  },
                    { "binary" , "byte[]"  },
                    { "bit" , "bool"  },
                    { "char" , "string"  },
                    { "date" , "DateTime"  },
                    { "datetime" , "DateTime"  },
                    { "datetime2" , "DateTime"  },
                    { "datetimeoffset" , "DateTimeOffset"  },
                    { "decimal" , "decimal"  },
                    { "float" , "float"  },
                    { "image" , "byte[]"  },
                    { "int" , "int"  },
                    { "money" , "decimal"  },
                    { "nchar" , "char"  },
                    { "ntext" , "string"  },
                    { "numeric" , "decimal"  },
                    { "nvarchar" , "string"  },
                    { "real" , "double"  },
                    { "smalldatetime" , "DateTime"  },
                    { "smallint" , "short"  },
                    { "smallmoney" , "decimal"  },
                    { "text" , "string"  },
                    { "time" , "TimeSpan"  },
                    { "timestamp" , "DateTime"  },
                    { "tinyint" , "byte"  },
                    { "uniqueidentifier" , "Guid"  },
                    { "\"character varying\"", "string" },
                    { "character", "string" },
                    {"integer", "int"}
                };

                var strbuild = new StringBuilder();

                strbuild.Append($"[Column(\"{tableColumn.column_name}\")]");
                strbuild.Append("\n");
                var tipo = tiposDoBanco.GetValueOrDefault(tableColumn.data_type as string);
                strbuild.Append($"public {tipo}");
                if (tipo != "string")
                {
                    var nulo = tableColumn.is_nullable == "YES" ? "?" : "";
                    strbuild.Append($"{ nulo }");
                }
                strbuild.Append($" {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tableColumn.column_name)} " + "{ get; set; }");
                strbuild.Append("\n");


                using (System.IO.StreamWriter file = new StreamWriter(@"./tabela.class", true))
                {
                    file.WriteLine(strbuild.ToString());
                }
            }
        }

        public static string GetConnectionString(string host, string db, string user, SecureString password)
        {
            var strPassword = SecureStringToString(password);
            if (string.IsNullOrEmpty(db))
            {
                throw new ArgumentException("Miss database name.");
            }
            else if (string.IsNullOrEmpty(user))
            {
                throw new ArgumentException("Miss user name.");
            }
            else if (string.IsNullOrEmpty(strPassword))
            {
                throw new ArgumentException("Miss password.");
            }
            if (string.IsNullOrEmpty(host))
            {
                host = "localhost";
            }
            return $"Host={host};Username={user};Password={strPassword};Database={db}";
        }

        public static async Task<List<dynamic>> GetColumnsInfo(string connectionString, string tableName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Miss Connections String.");
            }
            var sqlQuery = $@"SELECT column_name, data_type, is_nullable FROM information_schema.columns where table_name = '{tableName}';";
            // var connectionString = "Host=baasu.db.elephantsql.com;Username=wzemlogc;Password=Izzk4VtPDnkz0y5gdgWzH6WL6Vf6vyXc;Database=wzemlogc";

            var parametros = new List<NpgsqlParameter>();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                using (var command = new NpgsqlCommand())
                {
                    command.CommandText = sqlQuery;
                    command.Connection = conn;
                    command.Parameters.AddRange(parametros.ToArray());
                    var lista = new List<dynamic>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                        foreach (IDataRecord record in reader as IEnumerable)
                        {
                            var expando = new ExpandoObject() as IDictionary<string, object>;
                            int i = 0;
                            foreach (var name in names)
                            {
                                expando.Add(name, record.IsDBNull(i) ? null : record[name]);
                                i++;
                            }
                            lista.Add(expando);
                        }
                    }
                    return lista;
                }
            }
        }

        public static SecureString GetConsoleSecurePassword()
        {
            Console.Write("Password: ");
            SecureString pwd = new SecureString();
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == ConsoleKey.Backspace)
                {
                    pwd.RemoveAt(pwd.Length - 1);
                    Console.Write("\b \b");
                }
                else
                {
                    pwd.AppendChar(i.KeyChar);
                    Console.Write("*");
                }
            }
            return pwd;
        }

        public static String SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        public static void CombineMultipleFilesIntoSingleFile(string inputDirectoryPathWithPattern, string outputFilePath = null)
        {
            if (String.IsNullOrWhiteSpace(outputFilePath))
            {
                outputFilePath = Path.Join(Directory.GetCurrentDirectory(), "outputfile.txt");
            }

            var pattern = Path.GetFileName(inputDirectoryPathWithPattern);
            var directory = Path.GetDirectoryName(inputDirectoryPathWithPattern);
            var GetAllFileText = string.Empty;
            var currentEncoding = Encoding.UTF8;

            var inputFilePaths = Directory.GetFiles(directory, pattern);
            Console.WriteLine("Number of files: {0}.", inputFilePaths.Length);

            if (!inputFilePaths.Any())
            {
                throw new Exception("Files not Found");
            }

            foreach (var inputFilePath in inputFilePaths)
            {
                currentEncoding = GetCurrentFileEncoding(inputFilePath);
                GetAllFileText += File.ReadAllText(inputFilePath, currentEncoding);
                GetAllFileText += Environment.NewLine;

                Console.WriteLine("The file {0} has been processed.", inputFilePath);
            }

            File.WriteAllText(outputFilePath, $"{GetAllFileText}", currentEncoding);
        }

        public static Encoding GetCurrentFileEncoding(string filePath)
        {
            // using (StreamReader sr = new StreamReader(filePath, true))
            // {
            //     while (sr.Peek() >= 0)
            //     {
            //         sr.Read();
            //     }
            //     //Test for the encoding after reading, or at least after the first read.
            //     return sr.CurrentEncoding;
            // }

            // Read the BOM
            var bom = new byte[4];
            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            // Analyze the BOM
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
            return Encoding.ASCII;
        }

        // Function to detect the encoding for UTF-7, UTF-8/16/32 (bom, no bom, little
        // & big endian), and local default codepage, and potentially other codepages.
        // 'taster' = number of bytes to check of the file (to save processing). Higher
        // value is slower, but more reliable (especially UTF-8 with special characters
        // later on may appear to be ASCII initially). If taster = 0, then taster
        // becomes the length of the file (for maximum reliability). 'text' is simply
        // the string with the discovered encoding applied to the file.
        public static Encoding detectTextEncoding(string filename, out String text, int taster = 0)
        {
            byte[] b = File.ReadAllBytes(filename);

            //////////////// First check the low hanging fruit by checking if a
            //////////////// BOM/signature exists (sourced from http://www.unicode.org/faq/utf_bom.html#bom4)
            if (b.Length >= 4 && b[0] == 0x00 && b[1] == 0x00 && b[2] == 0xFE && b[3] == 0xFF) { text = Encoding.GetEncoding("utf-32BE").GetString(b, 4, b.Length - 4); return Encoding.GetEncoding("utf-32BE"); }  // UTF-32, big-endian 
            else if (b.Length >= 4 && b[0] == 0xFF && b[1] == 0xFE && b[2] == 0x00 && b[3] == 0x00) { text = Encoding.UTF32.GetString(b, 4, b.Length - 4); return Encoding.UTF32; }    // UTF-32, little-endian
            else if (b.Length >= 2 && b[0] == 0xFE && b[1] == 0xFF) { text = Encoding.BigEndianUnicode.GetString(b, 2, b.Length - 2); return Encoding.BigEndianUnicode; }     // UTF-16, big-endian
            else if (b.Length >= 2 && b[0] == 0xFF && b[1] == 0xFE) { text = Encoding.Unicode.GetString(b, 2, b.Length - 2); return Encoding.Unicode; }              // UTF-16, little-endian
            else if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF) { text = Encoding.UTF8.GetString(b, 3, b.Length - 3); return Encoding.UTF8; } // UTF-8
            else if (b.Length >= 3 && b[0] == 0x2b && b[1] == 0x2f && b[2] == 0x76) { text = Encoding.UTF7.GetString(b, 3, b.Length - 3); return Encoding.UTF7; } // UTF-7


            //////////// If the code reaches here, no BOM/signature was found, so now
            //////////// we need to 'taste' the file to see if can manually discover
            //////////// the encoding. A high taster value is desired for UTF-8
            if (taster == 0 || taster > b.Length) taster = b.Length;    // Taster size can't be bigger than the filesize obviously.


            // Some text files are encoded in UTF8, but have no BOM/signature. Hence
            // the below manually checks for a UTF8 pattern. This code is based off
            // the top answer at: https://stackoverflow.com/questions/6555015/check-for-invalid-utf8
            // For our purposes, an unnecessarily strict (and terser/slower)
            // implementation is shown at: https://stackoverflow.com/questions/1031645/how-to-detect-utf-8-in-plain-c
            // For the below, false positives should be exceedingly rare (and would
            // be either slightly malformed UTF-8 (which would suit our purposes
            // anyway) or 8-bit extended ASCII/UTF-16/32 at a vanishingly long shot).
            int i = 0;
            bool utf8 = false;
            while (i < taster - 4)
            {
                if (b[i] <= 0x7F) { i += 1; continue; }     // If all characters are below 0x80, then it is valid UTF8, but UTF8 is not 'required' (and therefore the text is more desirable to be treated as the default codepage of the computer). Hence, there's no "utf8 = true;" code unlike the next three checks.
                if (b[i] >= 0xC2 && b[i] <= 0xDF && b[i + 1] >= 0x80 && b[i + 1] < 0xC0) { i += 2; utf8 = true; continue; }
                if (b[i] >= 0xE0 && b[i] <= 0xF0 && b[i + 1] >= 0x80 && b[i + 1] < 0xC0 && b[i + 2] >= 0x80 && b[i + 2] < 0xC0) { i += 3; utf8 = true; continue; }
                if (b[i] >= 0xF0 && b[i] <= 0xF4 && b[i + 1] >= 0x80 && b[i + 1] < 0xC0 && b[i + 2] >= 0x80 && b[i + 2] < 0xC0 && b[i + 3] >= 0x80 && b[i + 3] < 0xC0) { i += 4; utf8 = true; continue; }
                utf8 = false; break;
            }
            if (utf8 == true)
            {
                text = Encoding.UTF8.GetString(b);
                return Encoding.UTF8;
            }


            // The next check is a heuristic attempt to detect UTF-16 without a BOM.
            // We simply look for zeroes in odd or even byte places, and if a certain
            // threshold is reached, the code is 'probably' UF-16.          
            double threshold = 0.1; // proportion of chars step 2 which must be zeroed to be diagnosed as utf-16. 0.1 = 10%
            int count = 0;
            for (int n = 0; n < taster; n += 2) if (b[n] == 0) count++;
            if (((double)count) / taster > threshold) { text = Encoding.BigEndianUnicode.GetString(b); return Encoding.BigEndianUnicode; }
            count = 0;
            for (int n = 1; n < taster; n += 2) if (b[n] == 0) count++;
            if (((double)count) / taster > threshold) { text = Encoding.Unicode.GetString(b); return Encoding.Unicode; } // (little-endian)


            // Finally, a long shot - let's see if we can find "charset=xyz" or
            // "encoding=xyz" to identify the encoding:
            for (int n = 0; n < taster - 9; n++)
            {
                if (
                    ((b[n + 0] == 'c' || b[n + 0] == 'C') && (b[n + 1] == 'h' || b[n + 1] == 'H') && (b[n + 2] == 'a' || b[n + 2] == 'A') && (b[n + 3] == 'r' || b[n + 3] == 'R') && (b[n + 4] == 's' || b[n + 4] == 'S') && (b[n + 5] == 'e' || b[n + 5] == 'E') && (b[n + 6] == 't' || b[n + 6] == 'T') && (b[n + 7] == '=')) ||
                    ((b[n + 0] == 'e' || b[n + 0] == 'E') && (b[n + 1] == 'n' || b[n + 1] == 'N') && (b[n + 2] == 'c' || b[n + 2] == 'C') && (b[n + 3] == 'o' || b[n + 3] == 'O') && (b[n + 4] == 'd' || b[n + 4] == 'D') && (b[n + 5] == 'i' || b[n + 5] == 'I') && (b[n + 6] == 'n' || b[n + 6] == 'N') && (b[n + 7] == 'g' || b[n + 7] == 'G') && (b[n + 8] == '='))
                    )
                {
                    if (b[n + 0] == 'c' || b[n + 0] == 'C') n += 8; else n += 9;
                    if (b[n] == '"' || b[n] == '\'') n++;
                    int oldn = n;
                    while (n < taster && (b[n] == '_' || b[n] == '-' || (b[n] >= '0' && b[n] <= '9') || (b[n] >= 'a' && b[n] <= 'z') || (b[n] >= 'A' && b[n] <= 'Z')))
                    { n++; }
                    byte[] nb = new byte[n - oldn];
                    Array.Copy(b, oldn, nb, 0, n - oldn);
                    try
                    {
                        string internalEnc = Encoding.ASCII.GetString(nb);
                        text = Encoding.GetEncoding(internalEnc).GetString(b);
                        return Encoding.GetEncoding(internalEnc);
                    }
                    catch { break; }    // If C# doesn't recognize the name of the encoding, break.
                }
            }


            // If all else fails, the encoding is probably (though certainly not
            // definitely) the user's local codepage! One might present to the user a
            // list of alternative encodings as shown here: https://stackoverflow.com/questions/8509339/what-is-the-most-common-encoding-of-each-language
            // A full list can be found using Encoding.GetEncodings();
            text = Encoding.Default.GetString(b);
            return Encoding.Default;
        }
    }
}