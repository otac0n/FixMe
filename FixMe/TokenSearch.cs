// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace FixMe
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Searches for tokens in comments.
    /// </summary>
    public class TokenSearch : Task
    {
        /// <summary>
        /// Gets or sets the files to search.
        /// </summary>
        [Required]
        public ITaskItem[] Files { get; set; }

        /// <summary>
        /// Gets or sets the tokens to search for.
        /// </summary>
        [Required]
        public ITaskItem[] Tokens { get; set; }

        /// <inheritdoc />
        public override bool Execute()
        {
            var regexBuilder = new StringBuilder();
            foreach (var token in this.Tokens)
            {
                if (regexBuilder.Length > 0)
                {
                    regexBuilder.Append("|");
                }

                regexBuilder.Append(Regex.Escape(token.ItemSpec));
            }

            var tokensRegex = new Regex(regexBuilder.ToString(), RegexOptions.Compiled);
            var singleLineCommentRegex = new Regex(@"(?://+|--+|#|'|REM|::)\s*$", RegexOptions.Compiled);
            var multiLineCommentStartRegex = new Regex(@"(/\*+|<#|<!--+|@\*+)\s*$", RegexOptions.Compiled);
            var stringStartRegex = new Regex(@"(@?"")\s*$", RegexOptions.Compiled);
            var extractCharsVerbatimRegex = new Regex(@"((?<char>[^""])|""(?<char>""))*", RegexOptions.Compiled);
            var extractCharsEscapedRegex = new Regex(@"((?<char>[^\\""])|\\(?<char>[""'])|(?<char>\\.))*", RegexOptions.Compiled);

            foreach (var file in this.Files)
            {
                var fileName = file.ItemSpec;

                if (!File.Exists(fileName))
                {
                    this.Log.LogMessage(MessageImportance.Normal, "Skipping '{0}' because it was not found.", fileName);
                    continue;
                }

                using (var fileStream = File.OpenRead(fileName))
                {
                    var isBinary = false;
                    var header = new byte[1024];
                    var read = fileStream.Read(header, 0, header.Length);
                    for (var i = 0; i < read; i++)
                    {
                        if (header[i] == 0)
                        {
                            isBinary = true;
                            break;
                        }
                    }

                    if (isBinary)
                    {
                        this.Log.LogMessage(MessageImportance.Normal, "Skipping '{0}' because it appears to be a binary file.", fileName);
                        continue;
                    }

                    fileStream.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(fileStream))
                    {
                        var lineNumber = 0;
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            lineNumber++;
                            foreach (Match match in tokensRegex.Matches(line))
                            {
                                var startColumn = match.Index + 1;
                                var endColumn = startColumn + match.Length;
                                var tokenEndIndex = match.Index + match.Length;

                                var token = match.Value;
                                var found = false;
                                string message = null;

                                var prefix = line.Substring(0, match.Index);
                                foreach (Match singleLineMatch in singleLineCommentRegex.Matches(prefix))
                                {
                                    if (string.IsNullOrWhiteSpace(prefix.Substring(singleLineMatch.Index + singleLineMatch.Length)))
                                    {
                                        found = true;
                                        message = line.Substring(tokenEndIndex);
                                        break;
                                    }
                                }

                                if (string.IsNullOrEmpty(message))
                                {
                                    foreach (Match multiLineMatch in multiLineCommentStartRegex.Matches(prefix))
                                    {
                                        if (string.IsNullOrWhiteSpace(prefix.Substring(multiLineMatch.Index + multiLineMatch.Length)))
                                        {
                                            string terminator = null;
                                            switch (multiLineMatch.Groups[1].Value)
                                            {
                                                case "/*":
                                                    terminator = "*/";
                                                    break;

                                                case "@*":
                                                    terminator = "*@";
                                                    break;

                                                case "<#":
                                                    terminator = "#>";
                                                    break;

                                                case "<!--":
                                                    terminator = "-->";
                                                    break;
                                            }

                                            var terminatorIndex = line.IndexOf(terminator, tokenEndIndex, StringComparison.Ordinal);
                                            if (terminatorIndex == -1)
                                            {
                                                found = true;
                                                message = line.Substring(tokenEndIndex);
                                                break;
                                            }
                                            else
                                            {
                                                found = true;
                                                message = line.Substring(tokenEndIndex, terminatorIndex - tokenEndIndex);
                                                break;
                                            }
                                        }
                                    }

                                    if (string.IsNullOrEmpty(message))
                                    {
                                        foreach (Match stringMatch in stringStartRegex.Matches(prefix))
                                        {
                                            if (string.IsNullOrWhiteSpace(prefix.Substring(stringMatch.Index + stringMatch.Length)))
                                            {
                                                Regex extractRegex = null;
                                                switch (stringMatch.Groups[1].Value)
                                                {
                                                    case @"@""":
                                                        extractRegex = extractCharsVerbatimRegex;
                                                        break;

                                                    case @"""":
                                                        extractRegex = extractCharsEscapedRegex;
                                                        break;
                                                }

                                                found = true;
                                                message = string.Concat(extractRegex.Match(line.Substring(tokenEndIndex)).Groups["char"].Captures.Cast<Capture>());
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (found)
                                {
                                    if (string.IsNullOrWhiteSpace(message))
                                    {
                                        this.Log.LogWarning(null, null, null, fileName, lineNumber, startColumn, lineNumber, endColumn, "Found '{0}'", token);
                                    }
                                    else
                                    {
                                        message = message.TrimStart(": ".ToCharArray()).TrimEnd();
                                        this.Log.LogWarning(null, null, null, fileName, lineNumber, startColumn, lineNumber, endColumn, "Found '{0}': {1}", token, message);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }
    }
}
