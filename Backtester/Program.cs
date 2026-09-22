namespace StockSharp.Backtester;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Compilation;
using Ecng.ComponentModel;
using Ecng.Logging;
using Ecng.Reflection;

using StockSharp.Algo;
using StockSharp.Algo.Compilation;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;

static class Program
{
	private const int _success = 0;
	private const int _executionFailure = 1;
	private const int _usageFailure = 2;
	private const int _compilationFailure = 3;
	private const int _cancelled = 130;

	public static async Task<int> Main(string[] args)
	{
		if (args.Length != 1)
		{
			Console.Error.WriteLine("Usage: Backtester <strategy.cs>");
			return _usageFailure;
		}

		var strategyPath = args[0];

		if (!Path.GetExtension(strategyPath).EqualsIgnoreCase(FileExts.CSharp))
		{
			Console.Error.WriteLine($"Expected a C# strategy file: {strategyPath}");
			return _usageFailure;
		}

		if (!File.Exists(strategyPath))
		{
			Console.Error.WriteLine($"File not found: {strategyPath}");
			return _usageFailure;
		}

		strategyPath = Path.GetFullPath(strategyPath);

		using var cancellation = new CancellationTokenSource();

		void cancel(object _, ConsoleCancelEventArgs eventArgs)
		{
			eventArgs.Cancel = true;
			cancellation.Cancel();
		}

		Console.CancelKeyPress += cancel;

		try
		{
			return await RunAsync(strategyPath, cancellation.Token);
		}
		catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
		{
			Console.Error.WriteLine("Backtest cancelled.");
			return _cancelled;
		}
		catch (Exception error)
		{
			Console.Error.WriteLine($"Backtest failed: {error}");
			return _executionFailure;
		}
		finally
		{
			Console.CancelKeyPress -= cancel;
		}
	}

	private static async Task<int> RunAsync(string strategyPath, CancellationToken cancellationToken)
	{
		using var logManager = new LogManager();
		logManager.Listeners.Add(new FileLogListener("backtest.log"));
		logManager.Listeners.Add(new ConsoleLogListener());

		Console.WriteLine("Initializing compilation environment...");

		await CompilationExtensions.Init(Paths.FileSystem, logManager.Application, [], cancellationToken);

		using var code = new CodeInfo
		{
			Name = Path.GetFileNameWithoutExtension(strategyPath),
			Text = await File.ReadAllTextAsync(strategyPath, cancellationToken),
		};

		ResolveLocalReferences(code);

		Console.WriteLine($"Compiling strategy from {strategyPath}...");

		var errors = (await code.CompileAsync(t => t.IsRequiredType<Strategy>(), code.Name, cancellationToken)).ToArray();

		if (errors.HasErrors())
		{
			foreach (var error in errors)
				Console.Error.WriteLine(error);

			return _compilationFailure;
		}

		Console.WriteLine("Compilation successful.");

		var security = new Security
		{
			Id = Paths.HistoryDefaultSecurity,
			PriceStep = 0.01m,
			VolumeStep = 0.001m,
			MinVolume = 0.001m,
			MaxVolume = 1000m,
		};

		using var storageRegistry = new StorageRegistry
		{
			DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath),
		};

		var portfolio = Portfolio.CreateSimulator();
		portfolio.CurrentValue = 1000000m;

		using var connector = new HistoryEmulationConnector([security], [portfolio], storageRegistry)
		{
			IsAutoUnSubscribeOnDisconnect = false,
			StopOnSubscriptionError = false,
			HistoryMessageAdapter =
			{
				StartDate = Paths.HistoryBeginDate,
				StopDate = Paths.HistoryEndDate.Date.AddDays(1).AddTicks(-1),
			}
		};

		using var strategy = code.ObjectType.CreateInstance<Strategy>();

		strategy.Portfolio = portfolio;
		strategy.Security = security;
		strategy.Connector = connector;
		strategy.Volume = 1;
		strategy.WaitRulesOnStop = false;

		logManager.Sources.Add(connector);
		logManager.Sources.Add(strategy);

		var connectorTerminal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		Exception connectorError = null;

		connector.StateChanged2 += state =>
		{
			if (state is ChannelStates.Stopping or ChannelStates.Stopped)
				connectorTerminal.TrySetResult(true);
		};
		connector.Error += error =>
		{
			Interlocked.CompareExchange(ref connectorError, error, null);
			connectorTerminal.TrySetResult(true);
		};
		connector.ConnectionError += error =>
		{
			Interlocked.CompareExchange(ref connectorError, error, null);
			connectorTerminal.TrySetResult(true);
		};

		Exception cleanupError = null;
		(bool completed, Exception error) result;

		try
		{
			await connector.ConnectAsync(cancellationToken);

			var execution = strategy.ExecAsync(token => connector.StartAsync(token), cancellationToken).AsTask();
			var first = await Task.WhenAny(execution, connectorTerminal.Task);

			if (first == connectorTerminal.Task && strategy.ProcessState == ProcessStates.Started)
			{
				// The replay boundary is not a trading signal. Do not submit cancellation
				// requests after the historical connector has already begun shutting down.
				strategy.CancelOrdersWhenStopping = false;
				await strategy.StopAsync(CancellationToken.None);
			}

			result = await execution;
		}
		finally
		{
			cleanupError = await CleanupAsync(strategy, connector);
			strategy.Connector = null;
		}

		if (cancellationToken.IsCancellationRequested)
			throw new OperationCanceledException(cancellationToken);

		// What failed is reported before the fact that the run did not finish: a strategy that
		// throws leaves both, and the exception is the half worth printing.
		if (connectorError is not null)
			throw new InvalidOperationException("The historical connector failed.", connectorError);

		if (result.error is not null)
			throw new InvalidOperationException("The strategy failed.", result.error);

		if (!result.completed)
			throw new InvalidOperationException("The strategy did not run to completion.");

		if (!connector.IsFinished)
			throw new InvalidOperationException("The strategy stopped before the historical replay completed.");

		if (cleanupError is not null)
			throw new InvalidOperationException("Backtest cleanup failed.", cleanupError);

		Console.WriteLine($"Backtest finished. PnL: {strategy.PnL}");
		return _success;
	}

	private static void ResolveLocalReferences(CodeInfo code)
	{
		foreach (var reference in code.AssemblyReferences)
		{
			if (Path.IsPathRooted(reference.FileName))
				continue;

			var localPath = Path.Combine(AppContext.BaseDirectory, reference.FileName);
			if (File.Exists(localPath))
				reference.FileName = localPath;
		}
	}

	private static async Task<Exception> CleanupAsync(Strategy strategy, HistoryEmulationConnector connector)
	{
		Exception failure = null;

		try
		{
			if (strategy.ProcessState == ProcessStates.Started)
			{
				strategy.CancelOrdersWhenStopping = false;
				await strategy.StopAsync(CancellationToken.None);
			}
		}
		catch (Exception error)
		{
			failure = error;
		}

		try
		{
			if (connector.ConnectionState == ConnectionStates.Connected)
				await connector.DisconnectAsync(CancellationToken.None);
		}
		catch (Exception error)
		{
			failure = failure is null ? error : new AggregateException(failure, error);
		}

		return failure;
	}
}
