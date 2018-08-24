//#define PARALLEL

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LuckParser;
using LuckParser.Controllers;
using LuckParser.Models.DataModels;

namespace EILogBenchmarks
{
	internal class Program
	{
		public static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [directory with html files]");
				return;
			}

			var directory = args[0];
			if (!Directory.Exists(directory))
			{
				Console.WriteLine("Directory doesn't exist.");
				return;
			}

			var firstFilename = Directory.EnumerateFiles(directory)
				.FirstOrDefault(x => x.EndsWith(".evtc") || x.EndsWith(".evtc.zip"));
			if (firstFilename == null)
			{
				Console.WriteLine("No logs found.");
				return;
			}

			// Dry run, there is initialization being done on first parse we don't want to include in our measurements.
			MeasureTimes(firstFilename, TextWriter.Null);

#if !PARALLEL
			foreach (string filename in Directory.EnumerateFiles(directory))
#else
			Parallel.ForEach(Directory.EnumerateFiles(directory), filename =>
#endif
            {
                if (!filename.EndsWith(".evtc", StringComparison.InvariantCultureIgnoreCase) &&
                    !filename.EndsWith(".evtc.zip", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine($"Ignoring file: {filename}");
	                continue;
                }

                MeasureTimes(filename, Console.Out);
            }
#if PARALLEL
			);
#endif
		}

		private static void MeasureTimes(string filename, TextWriter outputWriter)
		{
			var row = new GridRow(filename, "");
			var worker = new BackgroundWorker {WorkerReportsProgress = true};
			// Requires a BackgroundWorker to report progress (ugh), but we don't actually use it for anything
			row.BgWorker = worker;

			var stopwatch = Stopwatch.StartNew();
			Parser parser = new Parser();
			parser.ParseLog(row, filename);
			ParsedLog log = parser.GetParsedLog();

			var parsedTime = stopwatch.Elapsed;

			SettingsContainer settings = new SettingsContainer(LuckParser.Properties.Settings.Default);
			StatisticsCalculator statisticsCalculator = new StatisticsCalculator(settings);
			StatisticsCalculator.Switches switches = new StatisticsCalculator.Switches
			{
				CalculateCombatReplay = true,
				CalculateBoons = true,
				CalculateConditions = true,
				CalculateDefense = true,
				CalculateMechanics = true,
				CalculateStats = true,
				CalculateSupport = true,
				CalculateDPS = true
			};

			HTMLBuilder.UpdateStatisticSwitches(switches);
			var statistics = statisticsCalculator.CalculateStatistics(log, switches);

			var statisticsTime = stopwatch.Elapsed - parsedTime;

			var nullWriter = StreamWriter.Null;

			var htmlBuilder = new HTMLBuilder(log, settings, statistics);
			htmlBuilder.CreateHTML(nullWriter);

			var htmlTime = stopwatch.Elapsed - statisticsTime - parsedTime;

            var csvBuilder = new CSVBuilder(nullWriter, ",", log, settings, statistics);
			csvBuilder.CreateCSV();

			var csvTime = stopwatch.Elapsed - htmlTime - statisticsTime - parsedTime;

			var totalTime = stopwatch.Elapsed;
			outputWriter.WriteLine(
				$"{filename},{parsedTime.TotalMilliseconds},{statisticsTime.TotalMilliseconds},{htmlTime.TotalMilliseconds},{csvTime.TotalMilliseconds},{totalTime.TotalMilliseconds}");
			outputWriter.Flush();
		}
	}
}