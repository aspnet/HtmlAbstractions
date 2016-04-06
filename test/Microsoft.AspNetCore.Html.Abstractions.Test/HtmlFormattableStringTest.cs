﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.WebEncoders.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Html
{
    public class HtmlFormattableStringTest
    {
        [Fact]
        public void HtmlFormattableString_EmptyArgs()
        {
            // Arrange
            var formattableString = new HtmlFormattableString("Hello, World!");

            // Act
            var result = HtmlContentToString(formattableString);

            // Assert
            Assert.Equal("Hello, World!", result);
        }

        [Fact]
        public void HtmlFormattableString_EmptyArgsAndCulture()
        {
            // Arrange
            var formattableString = new HtmlFormattableString(CultureInfo.CurrentCulture, "Hello, World!");

            // Act
            var result = HtmlContentToString(formattableString);

            // Assert
            Assert.Equal("Hello, World!", result);
        }

        [Fact]
        public void HtmlFormattableString_MultipleArguments()
        {
            // Arrange
            var formattableString = new HtmlFormattableString("{0} {1} {2} {3}!", "First", "Second", "Third", "Fourth");

            // Act
            var result = HtmlContentToString(formattableString);

            // Assert
            Assert.Equal(
                "HtmlEncode[[First]] HtmlEncode[[Second]] HtmlEncode[[Third]] HtmlEncode[[Fourth]]!",
                result);
        }

        [Fact]
        public void HtmlFormattableString_WithHtmlEncodedString()
        {
            // Arrange
            var formattableString = new HtmlFormattableString("{0}!", new HtmlEncodedString("First"));

            // Act
            var result = HtmlContentToString(formattableString);

            // Assert
            Assert.Equal("First!", result);
        }

        [Fact]
        public void HtmlFormattableString_With3Arguments()
        {
            // Arrange
            var formattableString = new HtmlFormattableString("0x{0:X} - {1} equivalent for {2}.", 50, "hex", 50);

            // Act
            var result = HtmlContentToString(formattableString);

            // Assert
            Assert.Equal(
                "0xHtmlEncode[[32]] - HtmlEncode[[hex]] equivalent for HtmlEncode[[50]].",
                result);
        }

        [Fact]
        public void HtmlFormattableString_WithAlignmentComponent()
        {
            // Arrange
            var formattableString = new HtmlFormattableString("{0, -25} World!", "Hello");

            // Act
            var result = HtmlContentToString(formattableString);

            // Assert
            Assert.Equal(
                "HtmlEncode[[Hello]]       World!", result);
        }

        [Fact]
        public void HtmlFormattableString_WithFormatStringComponent()
        {
            // Arrange
            var formattableString = new HtmlFormattableString("0x{0:X}", 50);

            // Act
            var result = HtmlContentToString(formattableString);

            // Assert
            Assert.Equal("0xHtmlEncode[[32]]", result);
        }

        [Fact]
        public void HtmlFormattableString_WithCulture()
        {
            // Arrange
            var formattableString = new HtmlFormattableString(
                CultureInfo.InvariantCulture,
                "Numbers in InvariantCulture - {0, -5:N} {1} {2} {3}!",
                1.1,
                2.98,
                145.82,
                32.86);

            // Act
            var result = HtmlContentToString(formattableString);

            // Assert
            Assert.Equal(
                "Numbers in InvariantCulture - HtmlEncode[[1.10]] HtmlEncode[[2.98]] " +
                    "HtmlEncode[[145.82]] HtmlEncode[[32.86]]!",
                result);
        }

        [Fact]
        [ReplaceCulture("en-US", "en-US")]
        public void HtmlFormattableString_UsesPassedInCulture()
        {
            // Arrange
            var culture = new CultureInfo("fr-FR");
            var formattableString = new HtmlFormattableString(culture, "{0} in french!", 1.21);

            // Act
            var result = HtmlContentToString(formattableString);

            // Assert
            Assert.Equal("HtmlEncode[[1,21]] in french!", result);
        }

        [Fact]
        [ReplaceCulture("de-DE", "de-DE")]
        public void HtmlFormattableString_UsesCurrentCulture()
        {
            // Arrange
            var formattableString = new HtmlFormattableString("{0:D}", DateTime.Parse("01/02/2015"));

            // Act
            var result = HtmlContentToString(formattableString);

            // Assert
            Assert.Equal("HtmlEncode[[Sonntag, 1. Februar 2015]]", result);
        }

        private static string HtmlContentToString(IHtmlContent content)
        {
            using (var writer = new StringWriter())
            {
                content.WriteTo(writer, new HtmlTestEncoder());
                return writer.ToString();
            }
        }
    }
}
