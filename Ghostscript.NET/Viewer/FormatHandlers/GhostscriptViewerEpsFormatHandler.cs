﻿//
// GhostscriptViewerEpsFormatHandler.cs
// This file is part of Ghostscript.NET library
//
// Author: Josip Habjan (habjan@gmail.com, http://www.linkedin.com/in/habjan) 
// Copyright (c) 2013-2016 by Josip Habjan. All rights reserved.
//
// Author ported some parts of this code from GSView. 
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
using System.IO;
using Ghostscript.NET.Viewer.DSC;

namespace Ghostscript.NET.Viewer
{
    internal class GhostscriptViewerEpsFormatHandler : GhostscriptViewerFormatHandler
    {
        private string _content;

        public GhostscriptViewerEpsFormatHandler(GhostscriptViewer viewer) : base(viewer) { }

        public override void Initialize()
        {

        }

        public override void Open(string filePath)
        {
            _content = File.ReadAllText(filePath);

            int i = _content.IndexOf("%!");

            if (i > 0)
            {
                _content = _content.Substring(i, _content.Length - i - 1);
            }

            i = _content.IndexOf("%%EOF");

            if (i > -1)
            {
                _content = _content.Substring(0, i + 5);
            }

            if (this.Viewer.EPSClip)
            {
                unsafe
                {
                    fixed (char* p = _content)
                    {
                        UnmanagedMemoryStream ums = new UnmanagedMemoryStream((byte*)p, _content.Length);
                        DSCTokenizer tokenizer = new DSCTokenizer(ums, true, BitConverter.IsLittleEndian);

                        DSCToken token = null;

                        while ((token = tokenizer.GetNextDSCKeywordToken()) != null)
                        {
                            if (token.Text == "%%BoundingBox:")
                            {
                                try
                                {
                                    DSCToken v1 = tokenizer.GetNextDSCValueToken(DSCTokenEnding.Whitespace | DSCTokenEnding.LineEnd);
                                    DSCToken v2 = tokenizer.GetNextDSCValueToken(DSCTokenEnding.Whitespace | DSCTokenEnding.LineEnd);
                                    DSCToken v3 = tokenizer.GetNextDSCValueToken(DSCTokenEnding.Whitespace | DSCTokenEnding.LineEnd);
                                    DSCToken v4 = tokenizer.GetNextDSCValueToken(DSCTokenEnding.Whitespace | DSCTokenEnding.LineEnd);

                                    this.BoundingBox = new GhostscriptRectangle(
                                            float.Parse(v1.Text, System.Globalization.CultureInfo.InvariantCulture),
                                            float.Parse(v2.Text, System.Globalization.CultureInfo.InvariantCulture),
                                            float.Parse(v3.Text, System.Globalization.CultureInfo.InvariantCulture),
                                            float.Parse(v4.Text, System.Globalization.CultureInfo.InvariantCulture));
                                }
                                catch { }

                                break;
                            }
                        }

                        tokenizer.Dispose(); tokenizer = null;
                        ums.Close(); ums.Dispose(); ums = null;
                    }
                }
            }

            this.FirstPageNumber = 1;
            this.LastPageNumber = 1;
        }

        public override void InitPage(int pageNumber)
        {

        }

        public override void ShowPage(int pageNumber)
        {
            this.ShowPagePostScriptCommandInvoked = false;

            this.Execute(_content);

            if (!this.ShowPagePostScriptCommandInvoked)
            {
                this.Execute("showpage");
            }
        }
    }
}
