//
// ProcessorSample.cs
// This file is part of Ghostscript.NET.Samples project
//
// Author: Josip Habjan (habjan@gmail.com, http://www.linkedin.com/in/habjan) 
// Copyright (c) 2013-2016 by Josip Habjan. All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using Ghostscript.NET.Processor;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Ghostscript.NET.Samples
{
    public class ProcessorSample4 : ISample
    {
        public void Start()
        {
            // gs -dSAFER -dBATCH -dNOPAUSE -dNOPROMPT --permit-file-read=W:/Projects/sites/library.visyond.gov/80/lib/CS/Ghostscript.NET/test/ -c '(W:/Projects/sites/library.visyond.gov/80/lib/CS/Ghostscript.NET/test/test.pdf) (r) file runpdfbegin pdfpagecount = quit'

            string inputFile = @"../../../test/test.pdf";
            string outputFile = @"../../../test/output\page-%04d.png";

            GhostscriptStdIO stdioCb = new GhostscriptViewerStdIOHandler();

            try
            {
                using (GhostscriptProcessor ghostscript = new GhostscriptProcessor())
                {
                    ghostscript.Processing += new GhostscriptProcessorProcessingEventHandler(ghostscript_Processing);

                    List<string> switches = new List<string>();
                    //switches.Add("-empty");
                    switches.Add("-dSAFER");
                    switches.Add("-dBATCH");
                    switches.Add("-dNOPAUSE");
                    switches.Add("-dNOPROMPT");

                    // report the page count as per https://stackoverflow.com/questions/4826485/ghostscript-pdf-total-pages :
                    //
                    // of course this spells trouble when the `outputFile` path itself contains double quotes   :-(
                    // Also you cannot feed this baby *relative* paths as the GhostScript DLL will consider its own location as directory '.':
                    string ap = Path.GetFullPath(inputFile).Replace("\\", "/");
                    //  as per https://stackoverflow.com/questions/50730501/ggt-an-output-file-with-a-count-of-pdf-pages-for-each-file-with-ghostscript#answer-61310660 :
                    // (make sure **all** paths havee forward slashes here, as they must match **exactly**!)
#if false // oddly enough --permit-file-read doesn't fly while -I does, but I've seen this same flaky behaviour on the commandline...   :-(
                    switches.Add($"--permit-file-read={ Path.GetDirectoryName(ap).Replace("\\", "/") }");
#else
                    switches.Add($"-I{ Path.GetDirectoryName(ap).Replace("\\", "/") }");
#endif
                    switches.Add("-c");
                    switches.Add($"({ap}) (r) file runpdfbegin pdfpagecount = quit");

                    if (!File.Exists(ap))
                    {
                        throw new ApplicationException($"input file does not exist: {inputFile}{ (inputFile != ap ? $" --> {ap}" : "") }");
                    }

                    Console.WriteLine("CMD: {0}", String.Join(" ", switches.ToArray()));

                    ghostscript.Process(switches.ToArray(), stdioCb);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Exception: {ex}");
            }
            finally
            {
                Console.WriteLine(stdioCb.ToString());
            }
        }

        void ghostscript_Processing(object sender, GhostscriptProcessorProcessingEventArgs e)
        {
            Console.WriteLine($"{e.CurrentPage} / { (e.TotalPages == 0 ? @"<unknown>" : e.TotalPages.ToString()) }");
        }
    }

    internal class GhostscriptViewerStdIOHandler : GhostscriptStdIO
    {
        private StringBuilder _outputMessages = new StringBuilder();
        private StringBuilder _errorMessages = new StringBuilder();

        public GhostscriptViewerStdIOHandler() : base(true, true, true)
        {
        }

        public override void StdIn(out string input, int count)
        {
            input = string.Empty;
        }

        public override void StdOut(string output)
        {
            lock (_outputMessages)
            {
                _outputMessages.Append(output);
            }
        }

        public override void StdError(string error)
        {
            lock (_errorMessages)
            {
                _errorMessages.Append(error);
            }
        }

        public override string ToString()
        {
            return $"STDOUT:\n{_outputMessages}\nSTDERR:\n{_errorMessages}";
        }
    }
}
