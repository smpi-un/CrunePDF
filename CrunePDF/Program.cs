// Copyright (c) Shimpei Uenoi. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using System;
using System.CommandLine;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace CrunePDF
{
    class AppSettings
    {
        public string Language { get; set; } = "";
        public double RaitoThreshold { get; set; } = 1.25;
        public double ScoreThreshold { get; set; } = 10;
        public string OutputDirectory { get; set; } = "";
        public bool OutputAlways { get; set; } = true;
    }
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var settingFileName = "appsettings.json";
            var currentDirSettingFilePath = Path.Combine(Directory.GetCurrentDirectory(), settingFileName);
            var appBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            AppSettings? appSettings = null;
            if (appBase != null)
            {
                var appDirSettingFilePath = Path.Combine(appBase, settingFileName);
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile(path: currentDirSettingFilePath, true)
                    .AddJsonFile(path: appDirSettingFilePath, true)
                    .Build();

                appSettings = configuration.Get<AppSettings>();
                return -1;
            }
            appSettings ??= new AppSettings();

            var inputArgument = new Argument<FileInfo[]>(name: "Input Files", description: "");
            var languageOption = new Option<string>(name: "--language", description: "", getDefaultValue: () => appSettings.Language);
            var scoreThresholdOption = new Option<double>(name: "--score-threshold", description: "", getDefaultValue: () => appSettings.ScoreThreshold);
            var raitoThresholdOption = new Option<double>(name: "--raito-threshold", description: "", getDefaultValue: () => appSettings.RaitoThreshold);
            var outputDirectoryOption = new Option<string>(name: "--output-directory", description: "", getDefaultValue: () => appSettings.OutputDirectory);
            var outputAlwaysOption = new Option<bool>(name: "--output-always", description: "", getDefaultValue: () => appSettings.OutputAlways);
            var rootCommand = new RootCommand("CrunePDF");

            rootCommand.AddArgument(inputArgument);
            rootCommand.AddOption(languageOption);
            rootCommand.AddOption(scoreThresholdOption);
            rootCommand.AddOption(raitoThresholdOption);
            rootCommand.AddOption(outputDirectoryOption);
            rootCommand.AddOption(outputAlwaysOption);

            rootCommand.SetHandler((files, outputDirectory, language, scoreThreshold, raitoThreshold, outputAlways) =>
                {
                    if (language.Trim() == "")
                    {
                        // Use system locale
                        language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                    }
                    var outputDirectoryExpandedEnv = Environment.ExpandEnvironmentVariables(outputDirectory);
                    AutoRotatePDFFiles(files, outputDirectoryExpandedEnv, language, scoreThreshold, raitoThreshold, outputAlways);
                },
                inputArgument, outputDirectoryOption, languageOption, scoreThresholdOption, raitoThresholdOption, outputAlwaysOption);

            return await rootCommand.InvokeAsync(args);

        }
        static void AutoRotatePDFFiles(IEnumerable<FileInfo> files, string outputDirectory, string language, double scoreThreshold, double raitoThreshold, bool outputAlways)
        {
            var ocr = new OcrImage(language, scoreThreshold, raitoThreshold);
            foreach (var file in files)
            {
                if (File.Exists(file.FullName))
                {
                    AutoRotatePDFFile(ocr, outputDirectory, file.FullName, outputAlways);
                }
                else if (Directory.Exists(file.FullName))
                {
                    foreach (var f in Directory.GetFiles(file.FullName))
                    {
                        AutoRotatePDFFile(ocr, outputDirectory, f, outputAlways);
                    }
                }
            }

        }
        static void AutoRotatePDFFile(OcrImage ocr, string outputDirectory, string srcPath, bool outputAlways)
        {
            var rots = ocr.GetRotations(srcPath).Result;
            if (rots.Any(x => x != 0) || outputAlways)
            {
                // Rotate and output pdf if need
                var stdErr = new StreamWriter(Console.OpenStandardError());
                if (!File.Exists(srcPath))
                {
                    return;
                }
                if (Path.GetExtension(srcPath).ToLower() != ".pdf")
                {
                    return;
                }
                if (outputDirectory.Trim() != "" && !Directory.Exists(outputDirectory))
                {
                    stdErr.WriteLine("Output directory is not found.");
                    return;
                }
                var srcDir = Path.GetDirectoryName(srcPath);
                if (srcDir is null)
                {
                    return;
                }
                var srcName = Path.GetFileNameWithoutExtension(srcPath);
                var dstDir = outputDirectory.Trim() != "" ? outputDirectory.Trim() : srcDir;
                var i = 1;
                var dstPath = Path.Combine(dstDir, srcName + $".pdf");
                while (File.Exists(dstPath))
                {
                    dstPath = Path.Combine(dstDir, srcName + $"_{i}.pdf");
                    i++;
                }
                PdfEditor.RotatePdfPages(srcPath, rots, dstPath);
            }
            Console.WriteLine(string.Join("\n", rots.Select((i, v) => $"{srcPath},{i},{v}")));
        }
    }
}
