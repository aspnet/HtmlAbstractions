﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Encodings.Web;

namespace Microsoft.AspNetCore.Html
{
    /// <summary>
    /// An <see cref="IHtmlContent"/> implementation of composite string formatting
    /// (see https://msdn.microsoft.com/en-us/library/txafckwd(v=vs.110).aspx) which HTML encodes
    /// formatted arguments.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString()}")]
    public class HtmlFormattableString : IHtmlContent
    {
        private readonly IFormatProvider _formatProvider;
        private readonly string _format;
        private readonly object[] _args;

        /// <summary>
        /// Creates a new <see cref="HtmlFormattableString"/> with the given <paramref name="format"/> and
        /// <paramref name="args"/>.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An array that contains objects to format.</param>
        public HtmlFormattableString(string format, params object[] args)
            : this(formatProvider: null, format: format, args: args)
        {
        }

        /// <summary>
        /// Creates a new <see cref="HtmlFormattableString"/> with the given <paramref name="formatProvider"/>,
        /// <paramref name="format"/> and <paramref name="args"/>.
        /// </summary>
        /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An array that contains objects to format.</param>
        public HtmlFormattableString(IFormatProvider formatProvider, string format, params object[] args)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            _formatProvider = formatProvider ?? CultureInfo.CurrentCulture;
            _format = format;
            _args = args;
        }

        /// <inheritdoc />
        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            var formatProvider = new EncodingFormatProvider(_formatProvider, encoder);
            writer.Write(string.Format(formatProvider, _format, _args));
        }

        private string DebuggerToString()
        {
            using (var writer = new StringWriter())
            {
                WriteTo(writer, HtmlEncoder.Default);
                return writer.ToString();
            }
        }

        // This class implements Html encoding via an ICustomFormatter. Passing an instance of this
        // class into a string.Format method or anything similar will evaluate arguments implementing
        // IHtmlContent without HTML encoding them, and will give other arguments the standard
        // composite format string treatment, and then HTML encode the result.
        //
        // Plenty of examples of ICustomFormatter and the interactions with string.Format here:
        // https://msdn.microsoft.com/en-us/library/system.string.format(v=vs.110).aspx#Format6_Example
        private class EncodingFormatProvider : IFormatProvider, ICustomFormatter
        {
            private readonly HtmlEncoder _encoder;
            private readonly IFormatProvider _formatProvider;

            private StringWriter _writer;

            public EncodingFormatProvider(IFormatProvider formatProvider, HtmlEncoder encoder)
            {
                Debug.Assert(formatProvider != null);
                Debug.Assert(encoder != null);

                _formatProvider = formatProvider;
                _encoder = encoder;
            }

            public string Format(string format, object arg, IFormatProvider formatProvider)
            {
                // These are the cases we need to special case. We trust the HtmlString or IHtmlContent instance
                // to do the right thing with encoding.
                var htmlString = arg as HtmlString;
                if (htmlString != null)
                {
                    return htmlString.ToString();
                }

                var htmlContent = arg as IHtmlContent;
                if (htmlContent != null)
                {
                    _writer = _writer ?? new StringWriter();

                    htmlContent.WriteTo(_writer, _encoder);

                    var result = _writer.ToString();
                    _writer.GetStringBuilder().Clear();

                    return result;
                }

                // If we get here then 'arg' is not an IHtmlContent, and we want to handle it the way a normal
                // string.Format would work, but then HTML encode the result.
                //
                // First check for an ICustomFormatter - if the IFormatProvider is a CultureInfo, then it's likely
                // that ICustomFormatter will be null.
                var customFormatter = (ICustomFormatter)_formatProvider.GetFormat(typeof(ICustomFormatter));
                if (customFormatter != null)
                {
                    var result = customFormatter.Format(format, arg, _formatProvider);
                    if (result != null)
                    {
                        return _encoder.Encode(result);
                    }
                }

                // Next check if 'arg' is an IFormattable (DateTime is an example).
                //
                // An IFormattable will likely call back into the IFormatterProvider and ask for more information
                // about how to format itself. This is the typical case when IFormatterProvider is a CultureInfo.
                var formattable = arg as IFormattable;
                if (formattable != null)
                {
                    var result = formattable.ToString(format, _formatProvider);
                    if (result != null)
                    {
                        return _encoder.Encode(result);
                    }
                }

                // If we get here then there's nothing really smart left to try.
                if (arg != null)
                {
                    var result = arg.ToString();
                    if (result != null)
                    {
                        return _encoder.Encode(result);
                    }
                }

                return string.Empty;
            }

            public object GetFormat(Type formatType)
            {
                if (formatType == typeof(ICustomFormatter))
                {
                    return this;
                }

                return null;
            }
        }
    }
}
