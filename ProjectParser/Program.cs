using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Mono.Options;

namespace ProjectParser
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var showHelp = false;
            var browseForProjectFile = false;
            var projectOutputFile = "";
            var remove = "";
            var options = new OptionSet
            {
                {"b|browse", "Browse for project file to open", b => browseForProjectFile = b != null},
                {"o|output=", "Write output to file", o => projectOutputFile = o },
                {"r|remove=", "Remove file from setup project if it contains this string", r => remove = r },
                {"h|help", "Show help", h => showHelp = h != null}
            };

            List<string> extra;
            try
            {
                extra = options.Parse(args);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
            var projectFile = extra.FirstOrDefault();

            if (showHelp)
            {
                Console.WriteLine($"Usage: [OPTIONS] projectfile.vdproj");
                Console.WriteLine("Options");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }
            

            if (browseForProjectFile || String.IsNullOrWhiteSpace(projectFile))
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "Select Project";
                ofd.DefaultExt = "vdproj";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    projectFile = ofd.FileName;
                }
            }

            if (File.Exists(projectFile))
            {
                var doc = ParseProject(projectFile);
                if (!String.IsNullOrWhiteSpace(remove))
                {
                    Remove(doc, remove);
                }
                if(!String.IsNullOrWhiteSpace(projectOutputFile))
                {
                    File.WriteAllLines(projectOutputFile, CreateProjectFile(doc, 0));
                    //foreach (var line in CreateProjectFile(doc, 0).Take(50))
                    //{
                    //    Console.WriteLine(line);
                    //}
                }
            }

            if (Debugger.IsAttached)
            {
                Console.ReadLine();
            }
        }

        public static void Remove(XDocument doc, string remove)
        {
            var elements = doc.XPathSelectElements("//SourcePath")
                .Where(d => d.Value.Contains(remove));

            foreach (var element in elements)
            {
                var name = DecodeXmlName(element.Parent.Name.ToString());
                var msmKey = name.Substring(name.IndexOf(':') + 1);
                var folder = element.Parent.XPathSelectElement("Folder")
                    .Value;
                folder = folder.Substring(folder.IndexOf(':') + 1);

                // MSM Key
                doc.XPathSelectElements("//MsmKey")
                    .Where(d => d.Value.EndsWith(msmKey))
                    .Select(d => d.Parent)
                    .Remove()
                    ;

                // Folder
                //folder.Dump();
                var folderParent = doc.XPathSelectElements("//Folders")
                    .Where(f => f.Nodes().OfType<XElement>().Any(n => n.Name.ToString().Contains(folder)))
                    //.Dump()
                    ;
                folderParent.Nodes()
                    .OfType<XElement>()
                    .Where(xe => xe.Name.ToString().Contains(folder))
                    //.Dump()
                    .Remove();
            }
            elements
                .Select(e => e.Parent)
                .Remove();
        }

        static string EncodeXmlName(string name)
        {
            return XmlConvert.EncodeName(name).Replace(":", "_x003A_");
        }

        static string DecodeXmlName(string xmlName)
        {
            return XmlConvert.DecodeName(xmlName)?.Replace("_x003A_", ":");
        }

        public static IEnumerable<string> CreateProjectFile(XContainer xContainer, int depth)
        {
            List<string> lines = new List<string>();

            var pad = Math.Max(0,(depth - 2)*4);

            var xElement = xContainer as XElement;
            var nodes = xContainer.Nodes();
            if (xElement != null)
            {
                if ((xElement.HasElements && depth > 1)
                    || String.IsNullOrWhiteSpace(xElement.Value))
                {
                    pad+=4;
                }
                var line = "".PadLeft(pad) + "\"" + DecodeXmlName(xElement.Name.ToString()) + "\"";
                if (!xElement.HasElements)
                {
                    if (String.IsNullOrWhiteSpace(xElement.Value))
                    {
                        line += Environment.NewLine + "".PadLeft(pad) + "{" + Environment.NewLine
                                + "".PadLeft(pad) + "}";
                    }
                    else
                    {
                        line += $" = \"{xElement.Value}\"";
                    }
                    
                }
                lines.Add(line);
            }
            if (xContainer.Nodes().OfType<XElement>().Any())
            {
                if (xElement != null)
                {
                    lines.Add("".PadLeft(pad) + "{");
                }
                foreach (var xNode in xContainer.Nodes().OfType<XElement>())
                {
                    lines.AddRange(CreateProjectFile(xNode, depth + 1));
                }
                if (xElement != null)
                {
                    lines.Add("".PadLeft(pad) + "}");
                }
            }

            return lines;
        }

        public static XDocument ParseProject(string fileName)
        {
            var doc = new XDocument();
            var lines = File.ReadAllLines(fileName);
            var stack = new Stack<XContainer>();
            stack.Push(doc);
            foreach (var line in lines)
            {
                var current = stack.Peek();
                if (line.Trim() == "{")
                {
                    stack.Push(current.LastNode as XContainer);
                }
                else if (line.Trim() == "}")
                {
                    stack.Pop();
                }
                else
                {
                    var elements = Regex.Match(line, @"(\s*)""([^""]+)""");
                    if(elements.Length < line.Length)
                    {
                        elements = Regex.Match(line, @"(\s*)""([^""]+)""\s*=\s*""(.*)""");
                    }
                    var name = EncodeXmlName(elements.Groups[2].Value);
                    var value = elements.Groups.Count > 3 ? elements.Groups[3].Value : null;
                    var newElement = new XElement(name, value);
                    current.Add(newElement);
                }
            }


            return doc;
        }
    }
}
