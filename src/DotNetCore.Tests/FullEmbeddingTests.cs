﻿using DotNetCore.Tests.Helper;
using FluentAssertions;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetCore.Tests
{
    public class FullEmbeddingTests
    {
        [Test]
        public async Task NetCoreWebProjectShouldWorkWithEmbeddedResources()
        {
            const string rel = "../../../../testmodules/WebDotNetCore";
            var configuration = new DirectoryInfo(".").Parent.Name;
            var srcDir = Path.Combine(rel, $"bin/{configuration}/netcoreapp3.0");
            // ensure we have the right dir
            File.Exists(Path.Combine(srcDir, "WebDotNetCore.dll")).Should().BeTrue();
            // some dirs are ok to exist in output
            var nonLanguageDirs = new[]
            {
                "publish",
                "Properties"
            };
            var translationFolders = Directory
                .GetDirectories(srcDir)
                .Where(d => nonLanguageDirs.All(_ => !d.EndsWith(_)))
                .ToArray();
            translationFolders.Should().HaveCount(0, "because they where embedded into the web project");

            using (var factory = WebTestHelper.SetupTestFactory(rel))
            using (var client = factory.CreateClient())
            {
                async Task<string> SendAsync(string iso)
                {
                    var response = await client.GetAsync("?language=" + iso);
                    return await response.Content.ReadAsStringAsync();
                }
                var content = await SendAsync("en");
                content.Should().StartWith("Current culture: English");

                content = await SendAsync("de");
                content.Should().StartWith("Current culture: German");

                // fallback
                content = await SendAsync("en");
                content.Should().StartWith("Current culture: English");
            }
        }

        [Test]
        public void CliShouldLocalize()
        {
            const string rel = "../../../../testmodules/DotNetCoreCli";
            var configuration = new DirectoryInfo(".").Parent.Name;
            var srcDir = Path.Combine(rel, $"bin/{configuration}/netcoreapp3.0");
            // ensure we have the right dir
            var run = Path.Combine(srcDir, "DotNetCoreCli.dll");
            File.Exists(run).Should().BeTrue();

            Directory.Exists(Path.Combine(srcDir, "de")).Should().BeFalse();
            Directory.Exists(Path.Combine(srcDir, "fr")).Should().BeFalse();

            var p = Process.Start("dotnet", run);
            p.WaitForExit(2000).Should().BeTrue();
            p.ExitCode.Should().Be(0);
        }
    }
}
