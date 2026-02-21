using System;
using System.Linq;
using System.Threading;

using Ecng.Common;
using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Configuration;
using StockSharp.Samples.Strategies;

var strategy = new BtcOutperformStrategy();

// ---- common backtest harness ----

if (Paths.HistoryDataPath == null)
{
	Console.WriteLine("ERROR: History data path not found.");
	return 1;
}

var logManager = new LogManager();
logManager.Listeners.Add(new ConsoleLogListener());

var exchangeInfoProvider = new InMemoryExchangeInfoProvider();
var id = Paths.HistoryDefaultSecurity.ToSecurityId();
var board = exchangeInfoProvider.GetOrCreateBoard(id.BoardCode);

var security = new Security
{
	Id = Paths.HistoryDefaultSecurity,
	Code = id.SecurityCode,
	Board = board,
};

var storageRegistry = new StorageRegistry
{
	DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath)
};

var startTime = Paths.HistoryBeginDate.UtcKind();
var stopTime = Paths.HistoryEndDate.UtcKind();

var secId = security.ToSecurityId();

var level1Info = new Level1ChangeMessage
{
	SecurityId = secId,
	ServerTime = startTime,
}
.TryAdd(Level1Fields.MinPrice, 0.01m)
.TryAdd(Level1Fields.MaxPrice, 1000000m)
.TryAdd(Level1Fields.MarginBuy, 10000m)
.TryAdd(Level1Fields.MarginSell, 10000m);

var secProvider = (ISecurityProvider)new CollectionSecurityProvider(new[] { security });
var pf = Portfolio.CreateSimulator();
pf.CurrentValue = 1000;

var connector = new HistoryEmulationConnector(secProvider, new[] { pf })
{
	EmulationAdapter =
	{
		Settings =
		{
			MatchOnTouch = false,
		}
	},
	HistoryMessageAdapter =
	{
		StorageRegistry = storageRegistry,
	},
};

((ILogSource)connector).LogLevel = LogLevels.Info;
logManager.Sources.Add(connector);

strategy.Volume = 1;
strategy.Portfolio = connector.Portfolios.First();
strategy.Security = security;
strategy.Connector = connector;
strategy.LogLevel = LogLevels.Info;
logManager.Sources.Add(strategy);

connector.HistoryMessageAdapter.StartDate = startTime;
connector.HistoryMessageAdapter.StopDate = stopTime;

var finishedEvent = new ManualResetEvent(false);

connector.SecurityReceived += (subscr, s) =>
{
	if (s != security)
		return;
	_ = connector.EmulationAdapter.SendInMessageAsync(level1Info, default);
};

connector.StateChanged2 += state =>
{
	if (state == ChannelStates.Stopped)
	{
		strategy.Stop();
		finishedEvent.Set();
	}
};

strategy.Start();
connector.Connect();
await connector.StartAsync();

if (!finishedEvent.WaitOne(TimeSpan.FromSeconds(600)))
{
	Console.WriteLine("TIMEOUT");
	return 1;
}

logManager.Dispose();

var orders = strategy.Orders.Count();
var trades = strategy.MyTrades.Count();
var pnl = strategy.StatisticManager.Parameters.FirstOrDefault(p => p.Name == "NetProfit")?.Value;

Console.WriteLine($"Orders: {orders}");
Console.WriteLine($"Trades: {trades}");
Console.WriteLine($"PnL: {pnl}");

if (trades == 0)
{
	Console.WriteLine("FAIL: No trades generated.");
	return 1;
}

Console.WriteLine("OK");
return 0;
