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

namespace Ghostscript.NET.Samples
{
    public class ProcessorSample1 : ISample
    {
        public void Start()
        {
            string inputFile = @"..\..\..\test.pdf";
            string outputFile = @"..\..\..\output\page-%04d.png";

            const int pageFrom = 1;
            const int pageTo = 50;
            const int dpi = 300;            // 96 for screen when you don't want zoom or OCR-viable render output
            const bool highQualityAntiAliasedOutput = true;

            using (GhostscriptProcessor ghostscript = new GhostscriptProcessor())
            {
                ghostscript.Processing += new GhostscriptProcessorProcessingEventHandler(ghostscript_Processing);

                List<string> switches = new List<string>();
                //switches.Add("-empty");
                switches.Add("-dSAFER");
                switches.Add("-dBATCH");
                switches.Add("-dNOPAUSE");
                switches.Add("-dNOPROMPT");
                switches.Add($"-dFirstPage={pageFrom}");
                switches.Add($"-dLastPage={pageTo}");
                switches.Add($"-sPageList={pageFrom}-");            // overrides FirstPage and LastPage when used...
                switches.Add("-sDEVICE=png16m");                    // PNG 24bit color output: https://ghostscript.com/doc/current/Devices.htm
                switches.Add($"-r{dpi}");                              
                switches.Add($"-dMaxBitmap={ /* assume A3+50% page size @ 4 bytes per pixel */ (int)(12 * 16 * dpi * dpi * 4 * 1.5) }");
                if (highQualityAntiAliasedOutput)
                {
                    switches.Add("-dTextAlphaBits=4");                  // 4 = best quality: https://www.ghostscript.com/doc/9.52/Use.htm#Rendering_parameters
                    switches.Add("-dGraphicsAlphaBits=4");
                    switches.Add("-dAlignToPixels=0");
                }
                else
                {
                    switches.Add("-dTextAlphaBits=1");                  // 4 = best quality: https://www.ghostscript.com/doc/9.52/Use.htm#Rendering_parameters
                    switches.Add("-dGraphicsAlphaBits=1");
                    switches.Add("-dAlignToPixels=1");
                }
                switches.Add("-dPrinted=false");                    // always treat output device as a screen instead of as a printer for annotations display, etc.
                switches.Add($"-sOutputFile={outputFile}");

#if false  // doesn't work   :-(
                // also report the page count as per https://stackoverflow.com/questions/4826485/ghostscript-pdf-total-pages :
                switches.Add("-q");
                switches.Add("-c");
                // of course this spells trouble when the `outputFile` path itself contains double quotes   :-(
                // Aalso you cannot feed this baby *relative* paths as the GhostScript DLL will consider its own location as directory '.':
                string ap = Path.GetFullPath(inputFile).Replace("\\", "/");
                switches.Add($"\"({ap}) (r) file runpdfbegin pdfpagecount =\"");
#else
                string ap = Path.GetFullPath(inputFile).Replace("\\", "/");
#endif
                switches.Add("-c");
                switches.Add("30000000 setvmthreshold");

                switches.Add(@"-f");
                switches.Add(ap /* inputFile -- just to make sure both parts of the GS command point at exactly the same file */);

                if (!File.Exists(ap))
                {
                    throw new ApplicationException($"input file does not exist: {inputFile}{ (inputFile != ap ? $" --> {ap}" : "") }");
                }

                Console.WriteLine("CMD: {0}", String.Join(" ", switches.ToArray()));

                ghostscript.Process(switches.ToArray());
            }
        }

        void ghostscript_Processing(object sender, GhostscriptProcessorProcessingEventArgs e)
        {
            Console.WriteLine($"{e.CurrentPage} / { (e.TotalPages == 0 ? @"<unknown>" : e.TotalPages.ToString()) }");
        }

        private void Start2()
        {
            string inputFile = @"E:\__test_data\i1.pdf";

            GhostscriptPipedOutput gsPipedOutput = new GhostscriptPipedOutput();

            string outputPipeHandle = "%handle%" + int.Parse(gsPipedOutput.ClientHandle).ToString("X2");

            using (GhostscriptProcessor processor = new GhostscriptProcessor())
            {
                //"C:\Program Files\gs\gs9.15\bin\gswin64.exe" -sDEVICE=tiff24nc -r300 -dNOPAUSE -dBATCH -sOutputFile="Invoice 1_%03ld.tiff" "Invoice 1.pdf"
            
                List<string> switches = new List<string>();
                switches.Add("-empty");
                switches.Add("-dQUIET");
                switches.Add("-dSAFER");
                switches.Add("-dBATCH");
                switches.Add("-dNOPAUSE");
                switches.Add("-dNOPROMPT");
                switches.Add("-dPrinted");
                //switches.Add("-sDEVICE=pdfwrite");
                switches.Add("-sDEVICE=tiff24nc");
                switches.Add("-sOutputFile=" + outputPipeHandle);
                switches.Add("-f");
                switches.Add(inputFile);

                try
                {
                    processor.Process(switches.ToArray());

                    byte[] rawDocumentData = gsPipedOutput.Data;
                    var memStream = new MemoryStream(rawDocumentData);
                    //var image = new Bitmap(memStream);
                    //image.Save(@"Invocie 1.tiff");
                    //if (writeToDatabase)
                    //{
                    //    Database.ExecSP("add_document", rawDocumentData);
                    //}
                    //else if (writeToDisk)
                    //{
                    //    File.WriteAllBytes(@"E:\gss_test\output\test_piped_output.pdf", rawDocumentData);
                    //}
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    gsPipedOutput.Dispose();
                    gsPipedOutput = null;
                }
            }
        }
    }
}
