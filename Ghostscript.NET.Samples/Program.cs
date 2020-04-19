//
// Program.cs
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
using Ghostscript.NET.Viewer;

namespace Ghostscript.NET.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Ghostscript.NET Samples");

            if (!GhostscriptVersionInfo.IsGhostscriptInstalled)
            {
                throw new Exception("You don't have Ghostscript installed on this machine!");
            }

            ISample sample = null;

            int.TryParse(args.Length >= 1 ? args[0] : "", out int choice);

            for (choice = 1; ; choice++)
            {
                switch (choice)
                {
                    case 1:
                        sample = new GetInkCoverageSample();
                        break;

                    case 2:
                        sample = new ProcessorSample1();
                        break;

                    case 3:
                        sample = new ProcessorSample2();
                        break;

                    case 4:
                        sample = new FindInstalledGhostscriptVersionsSample();
                        break;

                    case 5:
                        sample = new RunMultipleInstancesSample();
                        break;

                    case 6:
                        sample = new ViewerSample();
                        break;

                    case 7:
                        sample = new RasterizerSample1();
                        break;

                    case 8:
                        sample = new RasterizerSample2();
                        break;

                    case 9:
                        sample = new AddWatermarkSample();
                        break;

                    case 10:
                        sample = new DeviceUsageSample();
                        break;

                    case 11:
                        sample = new PipedOutputSample();
                        break;

                    case 12:
                        sample = new SendToPrinterSample();
                        break;

                    case 13:
                        sample = new RasterizerCropSample();
                        break;

                    case 14:
                        sample = new ProcessorSample3();
                        break;

                    case 15:
                        sample = new ProcessorSample4();
                        break;

                    default:
                        sample = null;
                        break;
                }

                if (sample == null)
                {
                    break;
                }

                sample.Start();
            }

            Console.ReadLine();
        }
    }
}
