namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Logging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo.Storages;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;

[TestClass]
public class StrategyTests
{
	public static IEnumerable<object[]> StrategyTypes => typeof(StockSharp.Samples.Strategies.MaCrossoverStrategy)
		.Assembly
		.GetTypes()
		.Where(t => typeof(Strategy).IsAssignableFrom(t) && !t.IsAbstract && t.Namespace == "StockSharp.Samples.Strategies")
		.Select(t => new object[] { t });

	[DataTestMethod]
	[DynamicData(nameof(StrategyTypes))]
	public async Task RunStrategy(Type strategyType)
	{
		var strategy = (Strategy)Activator.CreateInstance(strategyType)!;
		await Run(strategy);
	}

	private static async Task Run(Strategy strategy)
	{
		var logManager = new LogManager();
		logManager.Listeners.Add(new ConsoleLogListener());

		var token = CancellationToken.None;

		var secId = Paths.HistoryDefaultSecurity;
		var security = new Security { Id = secId };

		var storageRegistry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive(Paths.HistoryDataPath) };

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

		var pf = Portfolio.CreateSimulator();
		pf.CurrentValue = 1000000m;

		var connector = new HistoryEmulationConnector([security], [pf], storageRegistry)
		{
			HistoryMessageAdapter =
			{
				StartDate = startTime,
				StopDate = stopTime,
			}
		};

		strategy.Portfolio = pf;
		strategy.Security = security;
		strategy.Connector = connector;
		strategy.Volume = 1;

		logManager.Sources.Add(connector);
		logManager.Sources.Add(strategy);

		await connector.ConnectAsync(token);

		var task = strategy.ExecAsync(null, token);
		connector.Start();
		await task.AsTask();

		Assert.IsTrue(strategy.Orders.Count() > 10, $"{strategy.GetType().Name} placed {strategy.Orders.Count()} orders");
		Assert.IsTrue(strategy.MyTrades.Count() > 5, $"{strategy.GetType().Name} executed {strategy.MyTrades.Count()} trades");
	}
}
