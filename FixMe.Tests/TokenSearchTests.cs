// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace FixMe.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Xunit;

    public class TokenSearchTests
    {
        public static readonly string DefaultTokens = "BUG;FIXME;HACK;UNDONE;NOTE;OPTIMIZE;TODO;WORKAROUND;XXX;UnresolvedMergeConflict";

        [Fact]
        public void Execute_WhenFileIsBinary_CreatesMessageForSkippedFile()
        {
            var assemblyPath = Path.GetDirectoryName(typeof(TokenSearchTests).Assembly.Location);

            var subject = new TokenSearch();
            var filePath = Path.Combine(assemblyPath, "TestCases", "test.gif");
            subject.Files = new[] { new TaskItem(filePath) };
            subject.Tokens = Array.ConvertAll(DefaultTokens.Split(';'), token => new TaskItem(token));
            var engine = new BuildEngine();
            subject.BuildEngine = engine;

            var result = subject.Execute();

            Assert.True(result);
            Assert.Collection(engine.Logs,
                x => Assert.Equal($"Skipping '{filePath}' because it appears to be a binary file.", ((BuildMessageEventArgs)x).Message));
        }

        [Fact]
        public void Execute_WhenFileIsNotFound_CreatesMessageForMissingFile()
        {
            var subject = new TokenSearch();
            subject.Files = new[] { new TaskItem("notfound.txt") };
            subject.Tokens = Array.ConvertAll(DefaultTokens.Split(';'), token => new TaskItem(token));
            var engine = new BuildEngine();
            subject.BuildEngine = engine;

            var result = subject.Execute();

            Assert.True(result);
            Assert.Collection(engine.Logs,
                x => Assert.Equal("Skipping 'notfound.txt' because it was not found.", ((BuildMessageEventArgs)x).Message));
        }

        [Theory]
        [InlineData("test.cmd", "Found 'TODO': Change to the path of your project")]
        [InlineData("test.cshtml", "Found 'TODO': Add a \"description\" here.", "Found 'TODO': Add some \"remarks\" here.", "Found 'WORKAROUND': Include this CSS inline.", "Found 'NOTE': This should be uncommented after the year 1,000,000½.")]
        [InlineData("test.js", "Found 'UNDONE': Test", "Found 'BUG': This is a bug.")]
        [InlineData("test.ps1", "Found 'HACK': Ignore inconclusive result.")]
        [InlineData("test.sql", "Found 'FIXME': This breaks if you don't have DBO permissions")]
        public void Execute_WhenFilesWithTokensAreIncluded_CreatesWarningsForTheTokens(string file, params string[] warnings)
        {
            var assemblyPath = Path.GetDirectoryName(typeof(TokenSearchTests).Assembly.Location);

            var subject = new TokenSearch();
            var filePath = Path.Combine(assemblyPath, "TestCases", file);
            subject.Files = new[] { new TaskItem(filePath) };
            subject.Tokens = Array.ConvertAll(DefaultTokens.Split(';'), token => new TaskItem(token));
            var engine = new BuildEngine();
            subject.BuildEngine = engine;

            var result = subject.Execute();

            Assert.True(result);
            Assert.Collection(engine.Logs,
                warnings.Select(w => new Action<object>(x => Assert.Equal(w, ((BuildWarningEventArgs)x).Message))).ToArray());
        }

        private class BuildEngine : IBuildEngine
        {
            private List<object> logs = new List<object>();

            public int ColumnNumberOfTaskNode => throw new NotImplementedException();

            public bool ContinueOnError => throw new NotImplementedException();

            public int LineNumberOfTaskNode => throw new NotImplementedException();

            public IList<object> Logs => this.logs.AsReadOnly();

            public string ProjectFileOfTaskNode => throw new NotImplementedException();

            public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => throw new NotImplementedException();

            public void LogCustomEvent(CustomBuildEventArgs e) => this.logs.Add(e);

            public void LogErrorEvent(BuildErrorEventArgs e) => this.logs.Add(e);

            public void LogMessageEvent(BuildMessageEventArgs e) => this.logs.Add(e);

            public void LogWarningEvent(BuildWarningEventArgs e) => this.logs.Add(e);
        }
    }
}
