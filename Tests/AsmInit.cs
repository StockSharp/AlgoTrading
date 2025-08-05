namespace StockSharp.Tests;

using System.Collections;
using System.Reflection;

using Ecng.Configuration;
using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Compilation;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;

[TestClass]
public static class AsmInit
{
	private readonly static MarketDataStorageCache _cache = new();
	private static LogManager _logManager;

	public static Security Security1 { get; private set; }
	public static Security Security2 { get; private set; }

	[AssemblyInitialize]
	public static async Task Init(TestContext _)
	{
		_logManager = new();
		_logManager.Listeners.Add(new ConsoleLogListener());

		await CompilationExtensions.Init(_logManager.Application, [], default);

		var drive = new LocalMarketDataDrive(Paths.HistoryDataPath);
		var storageRegistry = new StorageRegistry { DefaultDrive = drive };

		SecurityId[] secIds = [Paths.HistoryDefaultSecurity.ToSecurityId(), Paths.HistoryDefaultSecurity2.ToSecurityId()];
		var dts = secIds.SelectMany(id => drive.GetAvailableDataTypes(id, StorageFormats.Binary)).Where(dt => dt.IsTFCandles).ToArray();
		var days = Paths.HistoryBeginDate.Range(Paths.HistoryEndDate, TimeSpan.FromDays(1)).ToArray();

		foreach (var day in days)
		{
			foreach (var secId in secIds)
			{
				foreach (var dt in dts)
				{
					_cache.GetMessages(secId, dt, day, date => [.. storageRegistry.GetStorage(secId, dt).Load(date)]);
				}
			}
		}

		var secId1 = Paths.HistoryDefaultSecurity;
		Security1 = new Security { Id = secId1 };

		var secId2 = Paths.HistoryDefaultSecurity2;
		Security2 = new Security { Id = secId2 };

		var pf = Portfolio.CreateSimulator();
		pf.CurrentValue = 1000000m;

		ConfigManager.RegisterService<ISecurityProvider>(new CollectionSecurityProvider([Security1, Security2]));
		ConfigManager.RegisterService<IPortfolioProvider>(new CollectionPortfolioProvider([pf]));
	}

	public static async Task RunStrategy<T>(T strategy, Action<T, Security> extra = null)
		where T : Strategy
	{
		var token = CancellationToken.None;

		var storageRegistry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive(Paths.HistoryDataPath) };

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

		var connector = new HistoryEmulationConnector(ServicesRegistry.SecurityProvider, ServicesRegistry.PortfolioProvider, storageRegistry)
		{
			HistoryMessageAdapter =
			{
				StartDate = startTime,
				StopDate = stopTime,
				StorageCache = _cache,
			}
		};

		strategy.Portfolio = ServicesRegistry.PortfolioProvider.Portfolios.First();
		strategy.Security = Security1;
		strategy.Connector = connector;
		strategy.Volume = 1;
		strategy.WaitRulesOnStop = false;
		extra?.Invoke(strategy, Security2);

		var clone = strategy.TypedClone();
		clone.Connector = connector;

		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				strategy.Stop();
		};

		Exception error = null;
		strategy.Error += (s, e) =>
		{
			error = e;
			s.Stop();
		};

		//logManager.Sources.Add(connector);
		//logManager.Sources.Add(strategy);

		await connector.ConnectAsync(token);

		var (_, timeout) = token.CreateChildToken(TimeSpan.FromSeconds(30));

		var orders = new HashSet<long>();
		strategy.OrderReceived += (s, o) => orders.Add(o.TransactionId);

		var tradesCount = 0;
		strategy.OwnTradeReceived += (s, t) => tradesCount++;

		var task = strategy.ExecAsync(null, timeout);
		connector.Start();
		await task.AsTask();

		if (error is not null)
			throw error;

		//var ordersCount = orders.Count;
		//ordersCount.AssertGreater(0, "No orders were created by the strategy.");
		//ordersCount.AssertLess(100, "Too many orders were created by the strategy.");

		//tradesCount.AssertGreater(0, "No trades were created by the strategy.");
		//tradesCount.AssertLess(300, "Too many trades were created by the strategy.");

		//// Check the distribution of trades over the entire period
		//var firstTradeTime = strategy.MyTrades.Min(t => t.Trade.ServerTime);
		//var lastTradeTime = strategy.MyTrades.Max(t => t.Trade.ServerTime);

		//// The time of the first and last trade should not be too close to the start/end of the period
		//var totalPeriod = (stopTime - startTime).TotalSeconds;
		//var firstOffset = (firstTradeTime - startTime).TotalSeconds / totalPeriod;
		//var lastOffset = (stopTime - lastTradeTime).TotalSeconds / totalPeriod;

		//// The first trade should not be later than 15% from the start, the last not earlier than 15% before the end
		//(firstOffset < 0.85).AssertTrue($"First trade too late: {firstTradeTime}");
		//(lastOffset < 0.85).AssertTrue($"Last trade too early: {lastTradeTime}");

		//// Trades should be distributed over at least 70% of the period
		//var tradesSpan = (lastTradeTime - firstTradeTime).TotalSeconds / totalPeriod;
		//(tradesSpan > 0.7).AssertTrue($"Trades are not distributed enough: {tradesSpan:P0}");

		strategy.Reset();
		clone.Reset();

		static void validateSettingsStorage(SettingsStorage s1, SettingsStorage s2, string name)
		{
			s1.Count.AreEqual(s2.Count, name);

			foreach (var (k, v) in s1)
			{
				if (v is SettingsStorage v1)
					validateSettingsStorage(v1, (SettingsStorage)s2[k], k);
				else if (k != nameof(IIndicator.Id))
					v.AreEqual(s2[k], k);
			}
		}

		foreach (var field in strategy.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
		{
			var fv = field.GetValue(strategy);
			var fv2 = field.GetValue(clone);

			if (fv is IStrategyParam sp)
			{
				var cloneParam = (IStrategyParam)fv2;
				sp.Value.AreEqual(cloneParam.Value, field.Name);
			}
			else if (fv is IIndicator i)
			{
				validateSettingsStorage(i.Save(), ((IIndicator)fv2).Save(), field.Name);
			}
			else if (fv is IEnumerable e)
				e.Cast<object>().ToArray().AssertEqual([.. ((IEnumerable)fv2).Cast<object>()], field.Name);
			else
				fv.AreEqual(fv2, field.Name);
		}
	}
}
