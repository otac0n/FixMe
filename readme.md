FixMe
=======

A utility to emit MSBuild warnings for FIXME-style comments.

[![MIT Licensed](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](license.md)
[![Get it on NuGet](https://img.shields.io/nuget/v/FixMe.svg?style=flat-square)](http://nuget.org/packages/FixMe)

[![Appveyor Build](https://img.shields.io/appveyor/ci/otac0n/FixMe.svg?style=flat-square)](https://ci.appveyor.com/project/otac0n/FixMe)
[![Test Coverage](https://img.shields.io/codecov/c/github/otac0n/FixMe.svg?style=flat-square)](https://codecov.io/gh/otac0n/FixMe)
[![Pre-release packages available](https://img.shields.io/nuget/vpre/FixMe.svg?style=flat-square)](http://nuget.org/packages/FixMe)

Getting Started
---------------

    PM> Install-Package FixMe

When the FixMe package is installed, elements in your project will be searched for tokens specified by the MSBuild `FixMeTokens` variable.

You can override the default list by specifying your own value.  The default list of tokens is:

    <FixMeTokens Include="BUG;FIXME;HACK;UNDONE;NOTE;OPTIMIZE;TODO;WORKAROUND;XXX;UnresolvedMergeConflict" />
