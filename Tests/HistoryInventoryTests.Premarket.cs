namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo.Storages;
using StockSharp.Configuration;
using StockSharp.Messages;

public partial class HistoryInventoryTests
{
	[TestMethod]
	[TestCategory("Archive")]
	[TestCategory("Shard05")]
	public async Task S1181_PublishedDefaultsHaveARealTonPremarketSignal()
	{
		using var registry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath) };
		var frameTicks = TimeSpan.FromMinutes(5).Ticks;
		foreach (var id in new[] { Paths.HistoryDefaultSecurity, Paths.HistoryDefaultSecurity2 })
		{
			var storage = registry.GetStorage(id.ToSecurityId(), TimeSpan.FromMinutes(1).TimeFrame());
			var dates = await storage.GetDatesAsync().ToArrayAsync(CancellationToken);
			var bars = new SortedDictionary<long, (decimal open, decimal high, decimal low, decimal close, decimal volume)>();
			foreach (var date in dates.Order())
				await foreach (var message in storage.LoadAsync(date).WithCancellation(CancellationToken))
				{
					var candle = (ICandleMessage)message;
					var key = candle.OpenTime.Ticks / frameTicks;
					bars[key] = bars.TryGetValue(key, out var bar)
						? (bar.open, Math.Max(bar.high, candle.HighPrice), Math.Min(bar.low, candle.LowPrice), candle.ClosePrice, bar.volume + candle.TotalVolume)
						: (candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, candle.TotalVolume);
				}
			decimal? previousClose = null;
			var candidates = 0;
			var maximumGain = decimal.MinValue;
			foreach (var (key, bar) in bars)
			{
				if (previousClose is decimal previous && previous > 0m)
				{
					var gain = 100m * (bar.close - previous) / previous;
					maximumGain = Math.Max(maximumGain, gain);
					var finish = new DateTime((key + 1) * frameTicks, DateTimeKind.Utc);
					var local = TimeZoneInfo.ConvertTimeFromUtc(finish, TimeHelper.Est).TimeOfDay;
					if (local >= TimeSpan.FromHours(4) && local < TimeSpan.FromMinutes(570) && gain >= 5m
						&& bar.volume > 15000m && bar.close > bar.open && bar.high > bar.low
						&& 100m * (bar.close - bar.open) >= 75m * (bar.high - bar.low))
						candidates++;
				}
				previousClose = bar.close;
			}
			IsTrue(bars.Count > 0);
			if (id == Paths.HistoryDefaultSecurity)
			{
				IsTrue(maximumGain < 5m, "BTC has no five-minute 5% gain; lowering the published defaults is not a default test.");
				AreEqual(0, candidates);
			}
			else
				IsTrue(candidates > 0, "The TON default replay must have a real candle satisfying gain, body, volume and New York session filters.");
			TestContext.WriteLine($"{id}: independently aggregated 5m bars={bars.Count}, maximum gain={maximumGain:F8}%, default-qualified premarket candles={candidates}.");
		}
	}
}
