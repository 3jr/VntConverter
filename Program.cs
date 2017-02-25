using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vntConverter
{
    class Program
    {
        static bool waitForOutput = false;

        static void debugOutput(string s)
        {
            Console.WriteLine(s);
            Program.waitForOutput = true;
        }

        static DateTime? parseTime(string s)
        {
            DateTime t;
            if (!DateTime.TryParseExact(s, "yyyyMMdd\\THHmmss", null, System.Globalization.DateTimeStyles.AssumeLocal, out t))
            {
                debugOutput("ERROR while parsing DateTime: " + s);
                return null;
            }
            return t;
        }

        static void writeFile(string filename, DateTime? created, DateTime? modified, string body, string summary, string categories, string clazz)
        {
            File.WriteAllText(Path.ChangeExtension(filename, ".txt"),
                (created == null ? "" : $"Creation time: {created.ToString()}\r\n")
                + (modified == null ? "" : $"Last Modified: {modified.ToString()}\r\n")
                + (clazz == null ? "" : $"Class: {clazz.ToString()}\r\n")
                + (categories == null ? "" : $"Categories: {categories.ToString()}\r\n")
                + (summary == null ? "" : $"Summary: {summary.ToString()}\r\n")
                + "\r\n" + body.Replace("\n", "\r\n"),
                new UTF8Encoding(false)
            );
        }

        static void Main(string[] args)
        {
            try
            {
                foreach (var file in args)
                {
                    bool withinNote = false;
                    DateTime? created = null;
                    DateTime? modified = null;
                    string body = null;
                    string summary = null;
                    string categories = null;
                    string clazz = null;
                    foreach (var line in File.ReadAllText(file).Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var lineParts = line.Split(new[] { ':' }, 2);
                        if (lineParts.Length < 2)
                        {
                            throw new Exception("Invalid Format 1");
                        }
                        var options = lineParts[0].Split(';');
                        var content = lineParts[1];

                        var tag = options[0];

                        if (tag == "BEGIN")
                        {
                            if (withinNote)
                            {
                                // TODO error
                            }
                            if (body != null)
                            {
                                // TODO write previous node
                            }
                            withinNote = true;
                        }

                        if (!withinNote)
                        {
                            // TODO error
                        }

                        if (tag != "BODY" && options.Length > 1)
                        {
                            debugOutput("Unexpected option " + options[1] + " after tag " + tag);
                        }

                        switch (tag)
                        {
                            case "BEGIN":
                                if (content != "VNOTE")
                                {
                                    debugOutput("ERROR unexpected BEGIN:" + content);
                                }
                                break;
                            case "END":
                                if (content != "VNOTE")
                                {
                                    debugOutput("ERROR unexpected END:" + content);
                                }
                                break;
                            case "VERSION":
                                if (content != "1.1")
                                {
                                    debugOutput("Unexpected version: " + content);
                                }
                                break;
                            case "BODY":
                                bool hasCharSet = false;
                                bool hasEncoding = false;
                                foreach (var setting in options.Skip(1))
                                {
                                    var parts = setting.Split(new[] { '=' }, 2);
                                    if (parts.Length < 2)
                                    {
                                        // TODO error
                                        continue;
                                    }
                                    var key = parts[0];
                                    var value = parts[1];
                                    switch (key)
                                    {
                                        case "CHARSET":
                                            hasCharSet = true;
                                            if (value != "UTF-8")
                                            {
                                                debugOutput("unexpected charset: " + value);
                                            }
                                            break;
                                        case "ENCODING":
                                            hasEncoding = true;
                                            if (value != "QUOTED-PRINTABLE")
                                            {
                                                debugOutput("unexpected encoding: " + value);
                                            }
                                            break;
                                        default:
                                            debugOutput("unexpected setting in body: " + setting);
                                            break;
                                    }
                                }
                                if (!hasCharSet)
                                {
                                    debugOutput("No charset specified");
                                }
                                if (!hasEncoding)
                                {
                                    debugOutput("No encoding specified");
                                }
                                var utf8 = new List<byte>();
                                string escape = null;
                                foreach (var c in content)
                                {
                                    if (escape != null)
                                    {
                                        escape += c;
                                        if (!(('0' <= c && c <= '9') || ('A' <= c && c <= 'F') || ('a' <= c && c <= 'f')))
                                        {
                                            debugOutput("Invalid escape sequence");
                                            escape = null;
                                        }
                                        if (escape.Length == 2)
                                        {
                                            utf8.Add((byte)Convert.ToInt32(escape, 16));
                                            escape = null;
                                        }
                                    }
                                    else if (c == '=')
                                    {
                                        escape = "";
                                    }
                                    else
                                    {
                                        utf8.Add((byte)c);
                                    }
                                }

                                body = Encoding.UTF8.GetString(utf8.ToArray());

                                break;
                            case "DCREATED":
                                created = parseTime(content);
                                break;
                            case "LAST-MODIFIED":
                                modified = parseTime(content);
                                break;
                            case "CATEGORIES":
                                categories = content;
                                break;
                            case "CLASS":
                                clazz = content;
                                break;
                            case "SUMMARY":
                                summary = content;
                                break;
                            default:
                                debugOutput("Unexpected tag: " + tag);
                                break;
                        }

                        if (tag == "END")
                        {
                            if (!withinNote)
                            {
                                // TODO error
                            }
                            withinNote = false;
                        }
                    }
                    if (withinNote)
                    {
                        // TODO error
                    }
                    writeFile(file, created, modified, body, summary, categories, clazz);
                }
            }
            catch (Exception e)
            {
                debugOutput(e.ToString());
            }
            finally
            {
                if (waitForOutput)
                {
                    Console.ReadKey();
                }
            }
        }
    }
}
