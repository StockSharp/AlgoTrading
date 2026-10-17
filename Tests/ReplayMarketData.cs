namespace StockSharp.Tests;

using System;

using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;

/// <summary>Storage, instruments and time boundaries for one isolated historical replay.</summary>
internal sealed record ReplayMarketData(
	IStorageRegistry Storage,
	MarketDataStorageCache Cache,
	Security Primary,
	Security Secondary,
	DateTime From,
	DateTime To);
