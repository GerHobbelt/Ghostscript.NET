﻿//
// GhostscriptProcessor.cs
// This file is part of Ghostscript.NET library
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
using System.Reflection;
using System.Text;

namespace Ghostscript.NET.Processor
{
    public class GhostscriptProcessor : IDisposable
    {
        private readonly char[] EMPTY_SPACE_SPLIT = new char[] { ' ', '-' };

        private bool _disposed = false;
        private bool _processorOwnsLibrary = true;
        private GhostscriptLibrary _gs;
        private GhostscriptStdIO _stdIO_Callback;
        private GhostscriptProcessorInternalStdIOHandler _internalStdIO_Callback;
        private gsapi_pool_callback _poolCallBack;
        private StringBuilder _outputMessages = new StringBuilder();
        private StringBuilder _errorMessages = new StringBuilder();
        private int _totalPages;
        private bool _isRunning = false;
        private bool _stopProcessing = false;

        // Public events

        public event GhostscriptProcessorEventHandler Started;
        public event GhostscriptProcessorProcessingEventHandler Processing;
        public event GhostscriptProcessorErrorEventHandler Error;
        public event GhostscriptProcessorEventHandler Completed;

        protected void OnStarted(GhostscriptProcessorEventArgs e)
        {
            if (this.Started != null)
            {
                this.Started(this, e);
            }
        }

        protected void OnProcessing(GhostscriptProcessorProcessingEventArgs e)
        {
            if (this.Processing != null)
            {
                this.Processing(this, e);
            }
        }

        protected void OnError(GhostscriptProcessorErrorEventArgs e)
        {
            if (this.Error != null)
            {
                this.Error(this, e);
            }
        }

        protected void OnCompleted(GhostscriptProcessorEventArgs e)
        {
            if (this.Completed != null)
            {
                this.Completed(this, e);
            }
        }

        public GhostscriptProcessor()
            : this(GhostscriptVersionInfo.GetLastInstalledVersion(GhostscriptLicense.GPL | GhostscriptLicense.AFPL, GhostscriptLicense.GPL), false)
        { }

        public GhostscriptProcessor(GhostscriptLibrary library, bool processorOwnsLibrary = false)
        {
            if (library == null)
            {
                throw new ArgumentNullException("library");
            }
            _processorOwnsLibrary = processorOwnsLibrary;
            _gs = library;
        }

        public GhostscriptProcessor(byte[] library)
        {
            if (library == null)
            {
                throw new ArgumentNullException("library");
            }

            _gs = new GhostscriptLibrary(library);
        }

        public GhostscriptProcessor(GhostscriptVersionInfo version) : this(version, false)
        { }

        public GhostscriptProcessor(GhostscriptVersionInfo version, bool fromMemory)
        {
            if (version == null)
            {
                throw new ArgumentNullException("version");
            }

            _gs = new GhostscriptLibrary(version, fromMemory);
        }

        ~GhostscriptProcessor()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_processorOwnsLibrary)
                    {
                        _gs.Dispose();
                    }
                }

                _disposed = true;
            }
        }

        public void Process(GhostscriptDevice device, GhostscriptStdIO stdIO_callback = null)
        {
            this.StartProcessing(device, stdIO_callback);
        }

        public void Process(string[] args, GhostscriptStdIO stdIO_callback = null)
        {
            this.StartProcessing(args, stdIO_callback);
        }

        public void StartProcessing(GhostscriptDevice device, GhostscriptStdIO stdIO_callback = null)
        {
            if (device == null)
            {
                throw new ArgumentNullException("device");
            }

            this.StartProcessing(device.GetSwitches(), stdIO_callback);
        }

        /// <summary>
        /// Run Ghostscript.
        /// </summary>
        /// <param name="args">Command arguments</param>
        /// <param name="stdIO_callback">StdIO callback, can be set to null if you dont want to handle it.</param>
        public void StartProcessing(string[] args, GhostscriptStdIO stdIO_callback = null)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            if (args.Length < 3)
            {
                throw new ArgumentOutOfRangeException("args");
            }

            for (int i = 0; i < args.Length; i++)
            {
                args[i] = System.Text.Encoding.Default.GetString(System.Text.Encoding.UTF8.GetBytes(args[i]));
            }

            _isRunning = true;

            IntPtr instance = IntPtr.Zero;

            int rc_ins = _gs.gsapi_new_instance(out instance, IntPtr.Zero);

            if (ierrors.IsError(rc_ins))
            {
                throw new GhostscriptAPICallException("gsapi_new_instance", rc_ins, _internalStdIO_Callback);
            }

            try
            {
                _stdIO_Callback = stdIO_callback;

                _internalStdIO_Callback = new GhostscriptProcessorInternalStdIOHandler(
                                                new StdInputEventHandler(OnStdIoInput),
                                                new StdOutputEventHandler(OnStdIoOutput),
                                                new StdErrorEventHandler(OnStdIoError));

                int rc_stdio = _gs.gsapi_set_stdio(instance,
                                        _internalStdIO_Callback._std_in,
                                        _internalStdIO_Callback._std_out,
                                        _internalStdIO_Callback._std_err);

                _poolCallBack = new gsapi_pool_callback(Pool);

                int rc_pool = _gs.gsapi_set_poll(instance, _poolCallBack);

                if (ierrors.IsError(rc_pool))
                {
                    throw new GhostscriptAPICallException("gsapi_set_poll", rc_pool, _internalStdIO_Callback);
                }

                if (ierrors.IsError(rc_stdio))
                {
                    throw new GhostscriptAPICallException("gsapi_set_stdio", rc_stdio, _internalStdIO_Callback);
                }

                this.OnStarted(new GhostscriptProcessorEventArgs());

                _stopProcessing = false;

                if (_gs.is_gsapi_set_arg_encoding_supported)
                {
                    int rc_enc = _gs.gsapi_set_arg_encoding(instance, GS_ARG_ENCODING.UTF8);
                }

                int rc_init = _gs.gsapi_init_with_args(instance, args.Length, args);

                if (ierrors.IsErrorIgnoreQuit(rc_init))
                {
                    if (!ierrors.IsInterrupt(rc_init))
                    {
                        throw new GhostscriptAPICallException("gsapi_init_with_args", rc_init, _internalStdIO_Callback);
                    }
                }
            }
            finally
            {
                // gsapi_exit() :
                // 
                // Exit the interpreter. This MUST be called on shutdown if gsapi_init_with_args() has been called, and just before gsapi_delete_instance().
                //
                // ^^^ that's from the docs at https://ghostscript.com/doc/9.52/API.htm#exit (emphasis mine): it's placed in the `finally` clause
                // section here to ensure it is called when rc_init == e_Fatal (or other error) occurs and throws an exception in the code chunk above.
                try
                {
                    int rc_exit = _gs.gsapi_exit(instance);

                    if (ierrors.IsErrorIgnoreQuit(rc_exit))
                    {
                        throw new GhostscriptAPICallException("gsapi_exit", rc_exit, _internalStdIO_Callback);
                    }
                }
                finally
                {
                    _gs.gsapi_delete_instance(instance);

                    GC.Collect();

                    _isRunning = false;

                    this.OnCompleted(new GhostscriptProcessorEventArgs());
                }
            }
        }

        public void StopProcessing()
        {
            _stopProcessing = true;
        }

        private int Pool(IntPtr handle)
        {
            if (_stopProcessing)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        private void OnStdIoInput(out string input, int count)
        {
            if (_stdIO_Callback != null)
            {
                _stdIO_Callback.StdIn(out input, count);
            }
            else
            {
                input = string.Empty;
            }
        }

        private void OnStdIoOutput(string output)
        {
            lock (_outputMessages)
            {
                _outputMessages.Append(output);

                int rIndex = _outputMessages.ToString().IndexOf("\r\n");

                while (rIndex > -1)
                {
                    string line = _outputMessages.ToString().Substring(0, rIndex);
                    _outputMessages = _outputMessages.Remove(0, rIndex + 2);

                    this.ProcessOutputLine(line);

                    rIndex = _outputMessages.ToString().IndexOf("\r\n");
                }

                if (_stdIO_Callback != null)
                {
                    _stdIO_Callback.StdOut(output);
                }
            }
        }

        private void OnStdIoError(string error)
        {
            lock (_errorMessages)
            {
                _outputMessages.Append(error);

                int rIndex = _errorMessages.ToString().IndexOf("\r\n");

                while (rIndex > -1)
                {
                    string line = _errorMessages.ToString().Substring(0, rIndex);
                    _errorMessages = _errorMessages.Remove(0, rIndex + 2);

                    this.ProcessErrorLine(line);

                    rIndex = _errorMessages.ToString().IndexOf("\r\n");
                }

                if (_stdIO_Callback != null)
                {
                    _stdIO_Callback.StdError(error);
                }
            }
        }

        private void ProcessOutputLine(string line)
        {
            // e.g. "Processing pages 1-50."
            // e.g. "Processing pages 1-."    (when having specified a PageList like "1-")
            if (line.StartsWith("Processing pages"))
            {
                string[] chunks = line.Split(EMPTY_SPACE_SPLIT);
                string lastPage = chunks[chunks.Length - 1].TrimEnd('.');
                int.TryParse(lastPage, out _totalPages);
            }
            else if (line.StartsWith("Page"))
            {
                string[] chunks = line.Split(EMPTY_SPACE_SPLIT);
                int currentPage = int.Parse(chunks[1]);

                this.OnProcessing(new GhostscriptProcessorProcessingEventArgs(currentPage, _totalPages));
            }
        }

        private void ProcessErrorLine(string line)
        {
            this.OnError(new GhostscriptProcessorErrorEventArgs(line));
        }

        public bool IsRunning
        {
            get { return _isRunning; }
        }

        public bool IsStopping
        {
            get { return _isRunning && _stopProcessing; }
        }
    }
}
