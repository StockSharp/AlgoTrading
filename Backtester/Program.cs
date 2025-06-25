namespace StockSharp.Backtester;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Compilation;
using Ecng.Logging;
using Ecng.Reflection;

using StockSharp.Algo.Compilation;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;

static class Program
{
	public static async Task Main(string[] args)
	{
		if (args.Length == 0)
		{
			Console.WriteLine("Usage: Backtester <strategy.cs>");
			return;
		}

		var strategyPath = args[0];
		if (!File.Exists(strategyPath))
		{
			Console.WriteLine($"File not found: {strategyPath}");
			return;
		}

		var logManager = new LogManager();
		logManager.Listeners.Add(new FileLogListener("backtest.log"));
		logManager.Listeners.Add(new ConsoleLogListener());

		var token = CancellationToken.None;

		Console.WriteLine("Initializing compilation environment...");

		await CompilationExtensions.Init(logManager.Application, [], token);

		var code = new CodeInfo
		{
			Name = Path.GetFileNameWithoutExtension(strategyPath),
			Text = File.ReadAllText(strategyPath),
		};

		Console.WriteLine($"Compiling strategy from {strategyPath}...");

		var errors = await code.CompileAsync(t => t.IsRequiredType<Strategy>(), code.Name, token);

		if (errors.HasErrors())
		{
			foreach (var err in errors)
				Console.WriteLine(err);

			return;
		}

		Console.WriteLine("Compilation successful.");

		var secId = Paths.HistoryDefaultSecurity;
		var security = new Security { Id = secId };

		var storageRegistry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive(Paths.HistoryDataPath) };

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

		var pf = Portfolio.CreateSimulator();
		pf.CurrentValue = 1000000;

		var connector = new HistoryEmulationConnector([security], [pf], storageRegistry)
		{
			HistoryMessageAdapter =
			{
				StartDate = startTime,
				StopDate = stopTime,
			}
		};

		var strategy = code.ObjectType.CreateInstance<Strategy>();

		strategy.Portfolio = pf;
		strategy.Security = security;
		strategy.Connector = connector;
		strategy.Volume = 1;

		logManager.Sources.Add(connector);
		logManager.Sources.Add(strategy);

		await connector.ConnectAsync(token);

		var task1 = strategy.ExecAsync(null, token);

		connector.Start();

		var task2 = Task.Run(Console.ReadLine, token);
		Task.WaitAny(task1.AsTask(), task2);

		Console.WriteLine($"Backtest finished. PnL: {strategy.PnL}");
	}
}
