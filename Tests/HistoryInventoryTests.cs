namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.UnitTesting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo.Storages;
using StockSharp.Configuration;
using StockSharp.Messages;

[TestClass]
public partial class HistoryInventoryTests : BaseTestClass
{
	[TestMethod]
	[TestCategory("Archive")]
	[TestCategory("Shard00")]
	public async Task PackagedHistoryHasNoImpliedVolatilityStream()
	{
		var drive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath);
		using var registry = new StorageRegistry { DefaultDrive = drive };
		var securities = await drive.GetAvailableSecuritiesAsync().ToArrayAsync(CancellationToken);
		CollectionAssert.AreEquivalent(new[] { Paths.HistoryDefaultSecurity, Paths.HistoryDefaultSecurity2 },
			securities.Select(id => id.ToString()).ToArray(), "Audit every actual security in the bundled archive, not only the two configured test defaults.");
		foreach (var security in securities)
		{
			var storage = registry.GetStorage(security, DataType.Level1);
			var dates = await storage.GetDatesAsync().ToArrayAsync(CancellationToken);
			var messages = 0;
			var impliedVolatility = 0;
			var fields = new HashSet<Level1Fields>();
			foreach (var date in dates)
				await foreach (var message in storage.LoadAsync(date).WithCancellation(CancellationToken))
				{
					var level1 = (Level1ChangeMessage)message;
					messages++;
					fields.UnionWith(level1.Changes.Keys);
					if (level1.Changes.ContainsKey(Level1Fields.ImpliedVolatility)) impliedVolatility++;
				}
			IsTrue(dates.Length > 0 && messages > 0 && fields.Contains(Level1Fields.BestBidPrice) && fields.Contains(Level1Fields.BestAskPrice));
			AreEqual(0, impliedVolatility, "Price standard deviation cannot supply missing implied volatility. If the packaged archive gains real IV, explicitly revise this data-availability contract and restore the real strategy instead of a price proxy.");
			TestContext.WriteLine($"{security}: all {dates.Length} Level1 dates, {messages} messages, IV={impliedVolatility}, fields={string.Join(", ", fields.OrderBy(field => field))}");
		}
	}

	[TestMethod]
	[TestCategory("Archive")]
	[TestCategory("Shard00")]
	public async Task PackagedHistoryStreamShapes()
	{
		var drive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath);
		using var registry = new StorageRegistry { DefaultDrive = drive };
		foreach (var id in new[] { Paths.HistoryDefaultSecurity, Paths.HistoryDefaultSecurity2 })
		{
			var security = id.ToSecurityId();
			var types = await drive.GetAvailableDataTypesAsync(security, StorageFormats.Binary).ToArrayAsync(CancellationToken);
			IsTrue(types.Any(type => type.IsTFCandles), "The packaged archive must contain candle data.");
			TestContext.WriteLine($"{id}: {string.Join(", ", types.Select(type => type.ToString()))}");
			foreach (var type in types.Where(type => type.IsTFCandles || type == DataType.Level1))
			{
				var storage = registry.GetStorage(security, type);
				var dates = await storage.GetDatesAsync().ToArrayAsync(CancellationToken);
				IsTrue(dates.Length > 0);
				var lastDate = dates.Max();
				DateTime? lastArchivedTime = null;
				await foreach (var message in storage.LoadAsync(lastDate).WithCancellation(CancellationToken))
					if (message is IServerTimeMessage timed)
						lastArchivedTime = timed.ServerTime;
				TestContext.WriteLine($"  {type}: dates={dates.Length}, begin={dates.Min():yyyy-MM-dd}, end={lastDate:yyyy-MM-dd}, lastArchivedTime={lastArchivedTime:O}, historyPath={Paths.HistoryDataPath}");
				var meta = await storage.GetMetaInfoAsync(Paths.HistoryBeginDate, CancellationToken);
				IsTrue(meta != null && meta.PriceStep > 0m, "Archive metadata must supply the instrument price step.");
				var fields = new HashSet<Level1Fields>();
				var count = 0;
				DateTime? first = null;
				DateTime? last = null;
				await foreach (var message in storage.LoadAsync(Paths.HistoryBeginDate).WithCancellation(CancellationToken))
				{
					count++;
					if (message is IServerTimeMessage timed)
					{
						first ??= timed.ServerTime;
						last = timed.ServerTime;
					}
					if (message is Level1ChangeMessage level1)
						fields.UnionWith(level1.Changes.Keys);
				}
				IsTrue(count > 0);
				if (type == DataType.Level1)
					IsTrue(fields.Contains(Level1Fields.BestBidPrice) && fields.Contains(Level1Fields.BestAskPrice), "The archive has real bid/ask updates; missing LastTradePrice is not missing Level1.");
				TestContext.WriteLine($"  {type}: priceStep={meta.PriceStep}, volumeStep={meta.VolumeStep}, first-day messages={count}, first={first:O}, last={last:O}, fields={string.Join(", ", fields.OrderBy(field => field))}");
			}
		}
	}

	[TestMethod]
	[TestCategory("Archive")]
	[TestCategory("Shard00")]
	public async Task PackagedHistoryAggressorSideCoverage()
	{
		var drive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath);
		using var registry = new StorageRegistry { DefaultDrive = drive };
		foreach (var id in new[] { Paths.HistoryDefaultSecurity, Paths.HistoryDefaultSecurity2 })
		{
			var security = id.ToSecurityId();
			var tickStorage = registry.GetStorage(security, DataType.Ticks);
			var tickDates = await tickStorage.GetDatesAsync().ToArrayAsync(CancellationToken);
			var ticks = 0;
			var knownBuy = 0;
			var knownSell = 0;
			var unknown = 0;
			var missingTradeVolume = 0;
			// Only recorded trade initiators contribute to genuine signed volume.
			var signedFiveMinuteVolume = new SortedDictionary<long, decimal>();
			foreach (var date in tickDates)
				await foreach (var message in tickStorage.LoadAsync(date).WithCancellation(CancellationToken))
				{
					var tick = (ExecutionMessage)message;
					ticks++;
					if (tick.OriginSide == Sides.Buy) knownBuy++;
					else if (tick.OriginSide == Sides.Sell) knownSell++;
					else unknown++;
					if (tick.TradeVolume is not decimal volume || volume <= 0m)
					{
						missingTradeVolume++;
						continue;
					}
					var slot = tick.ServerTime.ToUniversalTime().Ticks / TimeSpan.FromMinutes(5).Ticks;
					signedFiveMinuteVolume.TryGetValue(slot, out var signed);
					signedFiveMinuteVolume[slot] = signed + (tick.OriginSide == Sides.Buy ? volume : -volume);
				}
			var priorDeltas = new Queue<decimal>();
			var cumulativeDelta = 0m;
			decimal? previousCumulative = null;
			var upBreaks = 0;
			var downBreaks = 0;
			var zeroUp = 0;
			var zeroDown = 0;
			var sessionUpBreaks = 0;
			var sessionDownBreaks = 0;
			var sessionZeroUp = 0;
			var sessionZeroDown = 0;
			var sessionPriorDeltas = new Queue<decimal>();
			var sessionDelta = 0m;
			decimal? previousSessionDelta = null;
			long? previousUtcDay = null;
			foreach (var (slot, signed) in signedFiveMinuteVolume)
			{
				var utcDay = slot / (TimeSpan.FromDays(1).Ticks / TimeSpan.FromMinutes(5).Ticks);
				if (previousUtcDay != utcDay)
				{
					previousUtcDay = utcDay;
					sessionDelta = 0m;
					previousSessionDelta = null;
					sessionPriorDeltas.Clear();
				}
				sessionDelta += signed;
				if (sessionPriorDeltas.Count == 20)
				{
					if (sessionDelta > sessionPriorDeltas.Max()) sessionUpBreaks++;
					if (sessionDelta < sessionPriorDeltas.Min()) sessionDownBreaks++;
					sessionPriorDeltas.Dequeue();
				}
				sessionPriorDeltas.Enqueue(sessionDelta);
				if (previousSessionDelta is decimal priorSession)
				{
					if (priorSession <= 0m && sessionDelta > 0m) sessionZeroUp++;
					if (priorSession >= 0m && sessionDelta < 0m) sessionZeroDown++;
				}
				previousSessionDelta = sessionDelta;
				cumulativeDelta += signed;
				if (priorDeltas.Count == 20)
				{
					if (cumulativeDelta > priorDeltas.Max()) upBreaks++;
					if (cumulativeDelta < priorDeltas.Min()) downBreaks++;
					priorDeltas.Dequeue();
				}
				priorDeltas.Enqueue(cumulativeDelta);
				if (previousCumulative is decimal previous)
				{
					if (previous <= 0m && cumulativeDelta > 0m) zeroUp++;
					if (previous >= 0m && cumulativeDelta < 0m) zeroDown++;
				}
				previousCumulative = cumulativeDelta;
			}
			var candleStorage = registry.GetStorage(security, TimeSpan.FromMinutes(1).TimeFrame());
			var candleDates = await candleStorage.GetDatesAsync().ToArrayAsync(CancellationToken);
			var candles = 0;
			var buyVolume = 0;
			var sellVolume = 0;
			var volumeLevels = 0;
			foreach (var date in candleDates)
				await foreach (var message in candleStorage.LoadAsync(date).WithCancellation(CancellationToken))
				{
					var candle = (ICandleMessage)message;
					candles++;
					if (candle.BuyVolume.HasValue) buyVolume++;
					if (candle.SellVolume.HasValue) sellVolume++;
					if (candle.PriceLevels?.Any(level => level.BuyVolume != 0m || level.SellVolume != 0m) == true) volumeLevels++;
				}
			IsTrue(tickDates.Length > 0 && ticks > 0 && candleDates.Length > 0 && candles > 0);
			AreEqual(0, unknown, "A true cumulative delta requires a real aggressor side for every archived tick; candle direction is not an acceptable substitute.");
			AreEqual(0, missingTradeVolume, "Signed archived ticks require actual positive trade volumes.");
			IsTrue(sessionUpBreaks > 0 && sessionDownBreaks > 0 && sessionZeroUp > 0 && sessionZeroDown > 0,
				"The genuine signed-tick archive must exercise both same-day breakout directions and both zero-cross directions.");
			TestContext.WriteLine($"{id}: tickDates={tickDates.Length}, ticks={ticks}, sideBuy={knownBuy}, sideSell={knownSell}, sideUnknown={unknown}, missingTradeVolume={missingTradeVolume}; tick5mSlots={signedFiveMinuteVolume.Count}, fullMonthCumulativeEnd={cumulativeDelta}, fullMonthPrior20UpBreaks={upBreaks}, fullMonthPrior20DownBreaks={downBreaks}, fullMonthZeroUp={zeroUp}, fullMonthZeroDown={zeroDown}; UTCdailyPrior20UpBreaks={sessionUpBreaks}, UTCdailyPrior20DownBreaks={sessionDownBreaks}, UTCdailyZeroUp={sessionZeroUp}, UTCdailyZeroDown={sessionZeroDown}; candleDates={candleDates.Length}, candles={candles}, candleBuyVolume={buyVolume}, candleSellVolume={sellVolume}, sidedPriceLevels={volumeLevels}");
		}
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public async Task S0034_ArchiveRollingAtrRatios()
	{
		var fixture = Convert.ToDecimal(StrategyTests.LowVolFixtureThreshold);
		using var registry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath) };
		foreach (var id in new[] { Paths.HistoryDefaultSecurity, Paths.HistoryDefaultSecurity2 })
		{
			var storage = registry.GetStorage(id.ToSecurityId(), TimeSpan.FromMinutes(1).TimeFrame());
			var dates = await storage.GetDatesAsync().ToArrayAsync(CancellationToken);
			var bars = new SortedDictionary<long, (decimal high, decimal low, decimal close)>();
			foreach (var date in dates.Order())
				await foreach (var message in storage.LoadAsync(date).WithCancellation(CancellationToken))
				{
					var candle = (ICandleMessage)message;
					var key = candle.OpenTime.Ticks / TimeSpan.FromMinutes(5).Ticks;
					bars[key] = bars.TryGetValue(key, out var bar)
						? (Math.Max(bar.high, candle.HighPrice), Math.Min(bar.low, candle.LowPrice), candle.ClosePrice)
						: (candle.HighPrice, candle.LowPrice, candle.ClosePrice);
				}
			var atrWindow = new Queue<decimal>();
			var count = 0;
			var atr = 0m;
			var minimumRatio = decimal.MaxValue;
			var belowPublished = 0;
			var belowFixture = 0;
			decimal? previousClose = null;
			foreach (var bar in bars.Values)
			{
				var tr = previousClose is decimal close
					? Math.Max(bar.high - bar.low, Math.Max(Math.Abs(bar.high - close), Math.Abs(bar.low - close)))
					: bar.high - bar.low;
				count++;
				var length = Math.Min(count, 14);
				atr = (atr * (length - 1) + tr) / length;
				previousClose = bar.close;
				if (count < 14) continue;
				atrWindow.Enqueue(atr);
				if (atrWindow.Count > 20) atrWindow.Dequeue();
				if (atrWindow.Count < 20 || atrWindow.Average() == 0m) continue;
				var ratio = 100m * atr / atrWindow.Average();
				minimumRatio = Math.Min(minimumRatio, ratio);
				if (ratio < 50m) belowPublished++;
				if (ratio < fixture) belowFixture++;
			}
			IsTrue(minimumRatio < decimal.MaxValue);
			AreEqual(0, belowPublished, "Neither packaged instrument reaches the README's 50% ATR14/rolling-mean20 ratio on 5m candles.");
			IsTrue(belowFixture > 0, "The fixture threshold of the S0034 replays must actually be reached on both packaged instruments.");
			TestContext.WriteLine($"{id}: 5m bars={bars.Count}, minimum ATR14/rolling ATR mean20={minimumRatio:F8}%, below 50%={belowPublished}, below {StrategyTests.LowVolFixtureThreshold}%={belowFixture}.");
		}
	}

	[TestMethod]
	[TestCategory("Shard04")]
	public async Task S0020_ArchiveFiveMinuteReturnRanges()
	{
		using var registry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath) };
		foreach (var id in new[] { Paths.HistoryDefaultSecurity, Paths.HistoryDefaultSecurity2 })
		{
			var storage = registry.GetStorage(id.ToSecurityId(), TimeSpan.FromMinutes(1).TimeFrame());
			var dates = await storage.GetDatesAsync().ToArrayAsync(CancellationToken);
			var closes = new SortedDictionary<long, decimal>();
			foreach (var date in dates.Order())
				await foreach (var message in storage.LoadAsync(date).WithCancellation(CancellationToken))
				{
					var candle = (ICandleMessage)message;
					closes[candle.OpenTime.Ticks / TimeSpan.FromMinutes(5).Ticks] = candle.ClosePrice;
				}
			var window = new Queue<decimal>();
			var minimum = decimal.MaxValue;
			var maximum = decimal.MinValue;
			var aboveFive = 0;
			var belowMinusFive = 0;
			foreach (var close in closes.Values)
			{
				window.Enqueue(close);
				if (window.Count > 11) window.Dequeue();
				if (window.Count < 11 || window.Peek() == 0m) continue;
				var value = 100m * (close - window.Peek()) / window.Peek();
				minimum = Math.Min(minimum, value);
				maximum = Math.Max(maximum, value);
				if (value > 5m) aboveFive++;
				if (value < -5m) belowMinusFive++;
			}
			IsTrue(closes.Count > 11);
			if (id == Paths.HistoryDefaultSecurity)
				AreEqual(0, aboveFive + belowMinusFive, "The BTC archive cannot produce a strict signed 5% ROC10 signal on 5m closes.");
			else
				IsTrue(aboveFive > 0 && belowMinusFive > 0, "The other packaged instrument must actually exercise the original 5% threshold in both directions.");
			TestContext.WriteLine($"{id}: observed 5m closes={closes.Count}, ROC10 range={minimum:F8}%..{maximum:F8}%, above+5={aboveFive}, below-5={belowMinusFive}; actual archive only, no filled-in candles.");
		}
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S0029_ArchiveFiveMinuteDeviationRanges()
	{
		using var registry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath) };
		foreach (var id in new[] { Paths.HistoryDefaultSecurity, Paths.HistoryDefaultSecurity2 })
		{
			var storage = registry.GetStorage(id.ToSecurityId(), TimeSpan.FromMinutes(1).TimeFrame());
			var dates = await storage.GetDatesAsync().ToArrayAsync(CancellationToken);
			var closes = new SortedDictionary<long, decimal>();
			foreach (var date in dates.Order())
				await foreach (var message in storage.LoadAsync(date).WithCancellation(CancellationToken))
				{
					var candle = (ICandleMessage)message;
					closes[candle.OpenTime.Ticks / TimeSpan.FromMinutes(5).Ticks] = candle.ClosePrice;
				}
			var window = new Queue<decimal>();
			var minimum = decimal.MaxValue;
			var maximum = decimal.MinValue;
			var aboveFive = 0;
			var belowMinusFive = 0;
			foreach (var close in closes.Values)
			{
				window.Enqueue(close);
				if (window.Count > 20) window.Dequeue();
				if (window.Count < 20) continue;
				var mean = window.Average();
				if (mean <= 0m) continue;
				var value = 100m * (close - mean) / mean;
				minimum = Math.Min(minimum, value);
				maximum = Math.Max(maximum, value);
				if (value > 5m) aboveFive++;
				if (value < -5m) belowMinusFive++;
			}
			IsTrue(closes.Count > 20);
			if (id == Paths.HistoryDefaultSecurity)
				AreEqual(0, aboveFive + belowMinusFive, "Published 5% SMA20 deviation is not reachable on packaged BTC; do not silently lower it.");
			else
				IsTrue(aboveFive > 0 && belowMinusFive > 0, "Packaged TON must genuinely exercise both published 5% deviation directions.");
			TestContext.WriteLine($"{id}: observed 5m closes={closes.Count}, SMA20 deviation range={minimum:F8}%..{maximum:F8}%, above+5={aboveFive}, below-5={belowMinusFive}; actual archive only, no filled-in candles.");
		}
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S0425_PackagedBtcNeverTouchesThePublishedGrid()
	{
		// The README grid (45000-48000) and the fixture grid the S0425 runs trade instead, both split into ten levels.
		const decimal publishedUpper = 48000m;
		const decimal fixtureLower = 60000m;
		const decimal fixtureUpper = 74000m;
		const int count = 10;

		using var registry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath) };
		var storage = registry.GetStorage(Paths.HistoryDefaultSecurity.ToSecurityId(), TimeSpan.FromMinutes(1).TimeFrame());
		var dates = await storage.GetDatesAsync().ToArrayAsync(CancellationToken);
		var step = (fixtureUpper - fixtureLower) / count;
		var candles = 0;
		var low = decimal.MaxValue;
		var high = decimal.MinValue;
		var lowerHalfTouches = 0;
		var upperHalfTouches = 0;

		foreach (var date in dates.Order())
		{
			await foreach (var message in storage.LoadAsync(date).WithCancellation(CancellationToken))
			{
				var candle = (ICandleMessage)message;
				candles++;
				low = Math.Min(low, candle.LowPrice);
				high = Math.Max(high, candle.HighPrice);

				for (var index = 0; index <= count; index++)
				{
					var line = fixtureLower + index * step;

					// The middle line is neutral, so only lines of either half count.
					if (line < candle.LowPrice || line > candle.HighPrice || index * 2 == count)
						continue;

					if (index * 2 < count)
						lowerHalfTouches++;
					else
						upperHalfTouches++;
				}
			}
		}

		IsTrue(candles > 0, "The packaged BTC history must hold 1m candles.");

		// A minute low above the top published line keeps every candle of any longer timeframe off the grid too.
		IsTrue(low > publishedUpper, $"Packaged BTC fell to {low}, within reach of the published 45000-48000 grid.");
		IsTrue(lowerHalfTouches > 0 && upperHalfTouches > 0, $"Packaged BTC ({low}..{high}) must touch lines in both halves of the 60000-74000 fixture grid.");

		TestContext.WriteLine($"{Paths.HistoryDefaultSecurity}: 1m candles={candles}, range={low}..{high}, fixture touches lower/upper half={lowerHalfTouches}/{upperHalfTouches}.");
	}
}
