﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information
namespace DotNetNuke.Build.Tasks
{
    using System.Collections.Generic;

    using Cake.Common.Build;
    using Cake.Common.Build.AzurePipelines.Data;
    using Cake.Common.IO;
    using Cake.Common.Tools.MSBuild;
    using Cake.Core.IO;
    using Cake.Frosting;
    using Cake.Issues;
    using Cake.Issues.MsBuild;

    using DotNetNuke.Build;

    /// <summary>A cake task to compile the platform.</summary>
    [IsDependentOn(typeof(CleanWebsite))]
    [IsDependentOn(typeof(RestoreNuGetPackages))]
    public sealed class Build : FrostingTask<Context>
    {
        /// <inheritdoc/>
        public override void Run(Context context)
        {
            // TODO: when Cake.Issues.MsBuild is updated to support Binary Log version 9, can use .EnableBinaryLogger() instead of .WithLogger(…)
            // TODO: also can remove the .InstallTool(…) call for Cake.Issues.MsBuild in Program.cs at that point
            var cleanLog = context.ArtifactsDir.Path.CombineWithFilePath("clean.binlog");
            var buildLog = context.ArtifactsDir.Path.CombineWithFilePath("rebuild.binlog");
            try
            {
                var cleanSettings = CreateMsBuildSettings(context, cleanLog).WithTarget("Clean");
                context.MSBuild(context.DnnSolutionPath, cleanSettings);

                var buildSettings = CreateMsBuildSettings(context, buildLog)
                    .SetPlatformTarget(PlatformTarget.MSIL)
                    .WithTarget("Rebuild")
                    .WithProperty("SourceLinkCreate", "true");
                context.MSBuild(context.DnnSolutionPath, buildSettings);
            }
            finally
            {
                var issues = context.ReadIssues(
                    new List<IIssueProvider>
                    {
                        new MsBuildIssuesProvider(
                            context.Log,
                            new MsBuildIssuesSettings(cleanLog, context.MsBuildBinaryLogFileFormat())),
                        new MsBuildIssuesProvider(
                            context.Log,
                            new MsBuildIssuesSettings(buildLog, context.MsBuildBinaryLogFileFormat())),
                    },
                    context.Directory("."));

                foreach (var issue in issues)
                {
                    context.AzurePipelines()
                           .Commands.WriteWarning(
                               issue.MessageText,
                               new AzurePipelinesMessageData
                               {
                                   SourcePath = issue.AffectedFileRelativePath?.FullPath, LineNumber = issue.Line,
                               });
                }
            }
        }

        private static MSBuildSettings CreateMsBuildSettings(Context context, FilePath binLogPath)
        {
            return new MSBuildSettings().SetConfiguration(context.BuildConfiguration)
                .SetMaxCpuCount(0)
                .WithLogger(context.Tools.Resolve("Cake.Issues.MsBuild*/**/StructuredLogger.dll").FullPath, "BinaryLogger", binLogPath.FullPath)
                .SetNoConsoleLogger(context.IsRunningInCI);
        }
    }
}
