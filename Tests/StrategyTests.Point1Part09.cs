namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Independent reference checks for point 1, part 09. Inherited by both CSharpTests and PythonTests.
/// </summary>
public abstract partial class StrategyTests
{
	private sealed class Point1Part09FiveMinuteReference
	{
		private readonly List<decimal> _closes = [];
		private readonly List<decimal> _opens = [];
		private readonly List<decimal> _typicals = [];
		private readonly List<decimal> _gains = [];
		private readonly List<decimal> _losses = [];
		private decimal _smma;
		private decimal _ema;
		private decimal _avgGain;
		private decimal _avgLoss;
		private decimal? _prevRsi;
		private decimal? _lastRsi;
		private readonly int _rsiPeriod;
		private readonly int _fastSmma;
		private readonly int _slowEma;
		private readonly int _fastCci;
		private readonly int _slowCci;

		public Point1Part09FiveMinuteReference(int rsiPeriod, int fastSmma, int slowEma, int fastCci, int slowCci)
		{
			_rsiPeriod = rsiPeriod;
			_fastSmma = fastSmma;
			_slowEma = slowEma;
			_fastCci = fastCci;
			_slowCci = slowCci;
		}

		public Sides? Signal { get; private set; }
		public int ReadyBars { get; private set; }
		public int IgnoredBars { get; private set; }

		private decimal? Cci(int length)
		{
			if (_typicals.Count < length)
				return null;
			var values = _typicals.Skip(_typicals.Count - length).ToArray();
			var mean = values.Sum() / length;
			var deviation = values.Sum(v => Math.Abs(v - mean)) / length;
			return deviation == 0m ? null : (values[^1] - mean) / (0.015m * deviation);
		}

		public void Add(ICandleMessage candle, bool spreadAllowed, bool flat, decimal longLevel, decimal shortLevel)
		{
			Signal = null;
			var priorClose = _closes.Count > 0 ? _closes[^1] : (decimal?)null;
			_closes.Add(candle.ClosePrice);
			_opens.Add(candle.OpenPrice);
			_typicals.Add((candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m);

			if (_opens.Count <= _fastSmma)
				_smma = _opens.Sum() / _fastSmma;
			else
				_smma = (_smma * (_fastSmma - 1) + candle.OpenPrice) / _fastSmma;

			if (_opens.Count <= _slowEma)
				_ema = _opens.Sum() / _slowEma;
			else
				_ema += (candle.OpenPrice - _ema) * 2m / (_slowEma + 1);

			if (priorClose is null)
				return;

			var delta = candle.ClosePrice - priorClose.Value;
			var gain = Math.Max(delta, 0m);
			var loss = Math.Max(-delta, 0m);
			if (_gains.Count < _rsiPeriod)
			{
				_gains.Add(gain);
				_losses.Add(loss);
				_avgGain = _gains.Sum() / _rsiPeriod;
				_avgLoss = _losses.Sum() / _rsiPeriod;
			}
			else
			{
				_avgGain = (_avgGain * (_rsiPeriod - 1) + gain) / _rsiPeriod;
				_avgLoss = (_avgLoss * (_rsiPeriod - 1) + loss) / _rsiPeriod;
			}

			var total = _avgGain + _avgLoss;
			var rsi = delta == 0m && _lastRsi is decimal unchanged
				? unchanged
				: total == 0m ? _lastRsi ?? 50m : 100m * _avgGain / total;
			_lastRsi = rsi;

			var fast = Cci(_fastCci);
			var slow = Cci(_slowCci);
			if (_gains.Count < _rsiPeriod || _opens.Count < Math.Max(_fastSmma, _slowEma) || fast is null || slow is null)
				return;

			ReadyBars++;
			if (_prevRsi is decimal previous)
			{
				if (previous <= longLevel && rsi > longLevel && _smma > _ema && fast > 0 && slow > 0)
				{
					if (spreadAllowed && flat)
						Signal = Sides.Buy;
					else
						IgnoredBars++;
				}
				else if (previous >= shortLevel && rsi < shortLevel && _smma < _ema && fast < 0 && slow < 0)
				{
					if (spreadAllowed && flat)
						Signal = Sides.Sell;
					else
						IgnoredBars++;
				}
			}
			_prevRsi = rsi;
		}
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public async Task S3410_IndependentFiveIndicatorEntriesAndFilledVolume()
	{
		const string key = "3410_Five_Minute_Rsi_Cci";
		// Reachable profile only: do not describe this as a default-parameter trading result.
		const int rsiPeriod = 5, fastSmma = 2, slowEma = 6, fastCci = 7, slowCci = 15;
		var reference = new Point1Part09FiveMinuteReference(rsiPeriod, fastSmma, slowEma, fastCci, slowCci);
		var errors = new List<string>();
		decimal? bid = null, ask = null;
		decimal step = 0m;
		decimal min = 0m, max = 0m, volStep = 0m;
		int decimals = 0;
		var expectedOrders = 0;
		var actualOrders = 0;
		var fillCount = 0;
		var expectedSide = (Sides?)null;
		var expectedVolume = 0m;
		var realizedPosition = 0m;

		await Replay(key, (strategy, other) =>
		{
			foreach (var (name, expected) in new (string, decimal)[]
			{
				("RsiPeriod", 14m), ("FastSmmaPeriod", 2m), ("SlowEmaPeriod", 6m),
				("FastCciPeriod", 34m), ("SlowCciPeriod", 175m),
				("BullishRsiLevel", 55m), ("BearishRsiLevel", 45m),
				("StopLossPoints", 60m), ("TakeProfitPoints", 0m), ("TrailingStopPoints", 20m),
				("LotCoefficient", 0.01m), ("EquityDivisor", 10m), ("MaxSpreadPoints", 18m),
			})
			{
				IsTrue(strategy.Parameters.ContainsKey(name), $"{key} must expose {name}.");
				AreEqual(expected, Convert.ToDecimal(strategy.Parameters[name].Value), $"{name} default mismatch.");
			}
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "RsiPeriod", rsiPeriod);
			SetParam(strategy, "FastCciPeriod", fastCci);
			SetParam(strategy, "SlowCciPeriod", slowCci);
			SetParam(strategy, "MaxSpreadPoints", 1000000m);
			SetParam(strategy, "StopLossPoints", 0m);
			SetParam(strategy, "TakeProfitPoints", 0m);
			SetParam(strategy, "TrailingStopPoints", 0m);

			step = strategy.Security.PriceStep;
			decimals = strategy.Security.Decimals;
			min = strategy.Security.MinVolume ?? 0m;
			max = strategy.Security.MaxVolume ?? decimal.MaxValue;
			volStep = strategy.Security.VolumeStep ?? 0m;

			strategy.Level1Received += (_, message) =>
			{
				if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal currentBid && currentBid > 0m)
					bid = currentBid;
				if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal currentAsk && currentAsk > 0m)
					ask = currentAsk;
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || candle.SecurityId != strategy.Security.Id.ToSecurityId())
					return;
				if (expectedSide is not null)
					errors.Add("Previous independent RSI/CCI cross had no matching order.");
				expectedSide = null;
				var point = decimals is 3 or 5 ? step * 10m : step;
				var spreadAllowed = bid is decimal b && ask is decimal a && a >= b && a - b < 1000000m * point;
				reference.Add(candle, spreadAllowed, strategy.Position == 0m, 55m, 45m);
				expectedSide = reference.Signal;
				if (expectedSide is not null)
				{
					var equity = strategy.Portfolio.CurrentValue;
					var raw = 0.01m * (decimal)Math.Sqrt((double)(equity / 10m));
					expectedVolume = volStep > 0m ? Math.Clamp(Math.Floor(raw / volStep) * volStep,
						Math.Ceiling(min / volStep) * volStep, Math.Floor(max / volStep) * volStep)
						: Math.Clamp(raw, min, max);
					expectedOrders++;
				}
			};
			strategy.OrderRegistering += order =>
			{
				if (strategy.Position != 0m)
				{
					errors.Add("The no-protection profile must not submit a separate exit order.");
					return;
				}
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					errors.Add($"{strategy.CurrentTime:O}: {order.Side} x {order.Volume}, expected {expectedSide} x {expectedVolume} from independent RSI/SMMA/EMA/two CCI gates.");
				expectedSide = null;
			};
			strategy.OwnTradeReceived += (_, trade) =>
			{
				fillCount++;
				realizedPosition += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
				if (realizedPosition != strategy.Position)
					errors.Add($"Actual fills sum to {realizedPosition}, but strategy holds {strategy.Position}.");
			};
		}, TimeSpan.FromDays(31));
		IsTrue(errors.Count == 0, string.Join(Environment.NewLine, errors.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(expectedOrders > 0 && fillCount > 0 && reference.ReadyBars > 0,
			"Independent causal RSI/SMMA/EMA/CCI reference must see real entry and fill events.");
		TestContext.WriteLine($"3410 reachable profile ({Extension}): {reference.ReadyBars} fully formed bars, {expectedOrders} entries, {fillCount} fills, {reference.IgnoredBars} gated signals.");
	}
	[TestMethod]
	[TestCategory("Shard05")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S3413_IndependentMedianLwmaEnvelopeBidEntriesAndActualFills(bool useTon)
	{
		const string key = "3413_5Mins_Envelopes";
		var medians = new List<decimal>();
		decimal? previousLow = null, previousHigh = null, lowerBand = null, upperBand = null;
		decimal? bid = null, ask = null;
		var errors = new List<string>();
		var expectedCount = 0;
		var orderCount = 0;
		var fillCount = 0;
		var position = 0m;
		var readyQuotes = 0;
		var rejectedQuotes = 0;
		Sides? expectedSide = null;
		const int period = 3;
		const decimal deviation = 0.05m;
		const decimal distancePoints = 1m;
		decimal point = 0m;

		await Replay(key, (strategy, alternate) =>
		{
			if (useTon)
				strategy.Security = alternate;
			foreach (var (name, value) in new (string, decimal)[]
			{
				("TradeVolume", 1m), ("DistancePoints", 140m), ("EnvelopePeriod", 3m),
				("EnvelopeDeviationPercent", 0.05m), ("StopLossPoints", 250m),
				("TakeProfitPoints", 0m), ("TrailingStopPoints", 120m), ("MaxSpreadPoints", 25m),
			})
			{
				IsTrue(strategy.Parameters.ContainsKey(name), $"{key} is missing {name}.");
				AreEqual(value, Convert.ToDecimal(strategy.Parameters[name].Value));
			}
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "DistancePoints", distancePoints);
			SetParam(strategy, "StopLossPoints", 0m);
			SetParam(strategy, "TrailingStopPoints", 0m);
			SetParam(strategy, "MaxSpreadPoints", 1000000m);
			point = strategy.Security.PriceStep;

			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || candle.SecurityId != strategy.Security.Id.ToSecurityId())
					return;
				medians.Add((candle.HighPrice + candle.LowPrice) / 2m);
				if (medians.Count > period)
					medians.RemoveAt(0);
				if (medians.Count < period)
					return;
				// Standalone linear-weighted mean, oldest=1, newest=3.
				var middle = medians.Select((price, i) => price * (i + 1)).Sum() / 6m;
				previousLow = candle.LowPrice;
				previousHigh = candle.HighPrice;
				lowerBand = middle * (1m - deviation / 100m);
				upperBand = middle * (1m + deviation / 100m);
			};
			strategy.Level1Received += (_, message) =>
			{
				if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal currentBid && currentBid > 0m)
					bid = currentBid;
				if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal currentAsk && currentAsk > 0m)
					ask = currentAsk;

				expectedSide = null;
				if (previousLow is not decimal low || previousHigh is not decimal high ||
					lowerBand is not decimal lower || upperBand is not decimal upper ||
					bid is not decimal b || ask is not decimal a || a < b)
					return;
				readyQuotes++;
				if (a - b >= 1000000m * point || position != 0m)
				{
					rejectedQuotes++;
					return;
				}
				if (low < lower - distancePoints * point && b < lower - distancePoints * point)
					expectedSide = Sides.Buy;
				else if (high > upper + distancePoints * point && b > upper + distancePoints * point)
					expectedSide = Sides.Sell;
				if (expectedSide is not null)
					expectedCount++;
			};
			strategy.OrderRegistering += order =>
			{
				orderCount++;
				if (order.Side != expectedSide || order.Volume != 1m || order.Type != OrderTypes.Market)
					errors.Add($"{strategy.CurrentTime:O}: order {order.Side} x{order.Volume} must match the preceding finished candle's median LWMA envelope AND live bid.");
				if (position != 0m)
					errors.Add("A second entry was submitted before the first position closed.");
				expectedSide = null;
			};
			strategy.OwnTradeReceived += (_, trade) =>
			{
				fillCount++;
				position += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
				if (position != strategy.Position)
					errors.Add($"Trade-by-trade position {position} does not equal strategy position {strategy.Position}.");
			};
		}, TimeSpan.FromDays(31));

		IsTrue(errors.Count == 0, string.Join(Environment.NewLine, errors.Take(12)));
		IsTrue(orderCount > 0 && fillCount > 0 && readyQuotes > 0,
			"Both implementations need an actual archive-backed order/fill justified by the independent envelope reference.");
		// A quote can trigger the same signal many times until the exchange confirms the entry.
		IsTrue(expectedCount >= orderCount);
		TestContext.WriteLine($"3413 ({Extension}), TON={useTon}: {readyQuotes} ready quotes, {rejectedQuotes} ineligible, {expectedCount} matching quote signals, {orderCount} orders, {fillCount} fills.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S3433_IndependentMfiSmaPatternEntriesAndCrossingExits()
	{
		const string key = "3433_AH_HM_MFI";
		// Reachable profile to exercise mechanics; the documented defaults are checked separately below.
		const int mfiPeriod = 5, maPeriod = 2;
		var flow = new List<(decimal up, decimal down)>();
		var closes = new List<decimal>();
		ICandleMessage previousCandle = null;
		decimal? previousMa = null, previousMfi = null;
		decimal? previousTypical = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var errors = new List<string>();
		var orders = 0;
		var entries = 0;
		var exits = 0;
		var fills = 0;
		var position = 0m;
		var candidatePatterns = 0;

		await Replay(key, (strategy, alternate) =>
		{
			strategy.Security = alternate;
			AreEqual(TimeSpan.FromMinutes(30).TimeFrame(), strategy.Parameters["CandleType"].Value);
			foreach (var (name, expected) in new (string, decimal)[]
			{
				("MfiPeriod", 47m), ("MaPeriod", 5m), ("HammerEntryThreshold", 40m),
				("HangingEntryThreshold", 60m), ("MfiUpperExitLevel", 70m), ("MfiLowerExitLevel", 30m),
			})
			{
				IsTrue(strategy.Parameters.ContainsKey(name), $"{key} needs {name}.");
				AreEqual(expected, Convert.ToDecimal(strategy.Parameters[name].Value));
			}
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "MfiPeriod", mfiPeriod);
			SetParam(strategy, "MaPeriod", maPeriod);
			SetParam(strategy, "HammerEntryThreshold", 100m);
			SetParam(strategy, "HangingEntryThreshold", 0m);

			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || candle.SecurityId != strategy.Security.Id.ToSecurityId())
					return;
				if (expectedSide is not null)
					errors.Add("Previous reference candle predicted an order that was not sent.");
				expectedSide = null;

				var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
				var money = typical * candle.TotalVolume;
				flow.Add((previousTypical is null || typical > previousTypical ? money : 0m,
					previousTypical is not null && typical < previousTypical ? money : 0m));
				previousTypical = typical;
				closes.Add(candle.ClosePrice);
				var ma = closes.Count < maPeriod ? (decimal?)null : closes.Skip(closes.Count - maPeriod).Average();
				decimal? mfi = null;
				if (flow.Count >= mfiPeriod)
				{
					var window = flow.Skip(flow.Count - mfiPeriod).ToArray();
					var pos = window.Sum(f => f.up);
					var neg = window.Sum(f => f.down);
					mfi = neg == 0m ? 100m : pos + neg == 0m ? null : 100m * pos / (pos + neg);
				}
				if (mfi is decimal current && previousMfi is decimal previous)
				{
					if (position > 0m && (previous <= 70m && current > 70m || previous >= 30m && current < 30m))
					{
						expectedSide = Sides.Sell;
						expectedVolume = position;
						exits++;
					}
					else if (position < 0m && (previous <= 30m && current > 30m || previous <= 70m && current > 70m))
					{
						expectedSide = Sides.Buy;
						expectedVolume = -position;
						exits++;
					}
				}
				if (mfi is decimal value && previousCandle is not null && previousMa is decimal priorMa && position == 0m)
				{
					var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
					var range = candle.HighPrice - candle.LowPrice;
					var highBody = Math.Min(candle.OpenPrice, candle.ClosePrice);
					var upperShadow = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
					var lowerShadow = highBody - candle.LowPrice;
					var pattern = range > 0m && body > 0m && highBody >= candle.HighPrice - range / 3m
						&& lowerShadow >= body * 2.5m && upperShadow <= body * 0.5m;
					if (pattern)
					{
						candidatePatterns++;
						var midpoint = (previousCandle.OpenPrice + previousCandle.ClosePrice) / 2m;
						if (candle.OpenPrice < previousCandle.ClosePrice && midpoint < priorMa && value <= 100m)
							expectedSide = Sides.Buy;
						else if (candle.OpenPrice > previousCandle.ClosePrice && midpoint > priorMa && value >= 0m)
							expectedSide = Sides.Sell;
						if (expectedSide is not null)
						{
							expectedVolume = strategy.Volume;
							entries++;
						}
					}
				}
				previousCandle = candle;
				previousMa = ma;
				previousMfi = mfi;
			};
			strategy.OrderRegistering += order =>
			{
				orders++;
				if (order.Type != OrderTypes.Market || order.Side != expectedSide || order.Volume != expectedVolume)
					errors.Add($"{strategy.CurrentTime:O} submitted {order.Side} x{order.Volume}; independent MFI/SMA/gap/pattern reference expected {expectedSide} x{expectedVolume}.");
				expectedSide = null;
			};
			strategy.OwnTradeReceived += (_, trade) =>
			{
				fills++;
				position += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
				if (position != strategy.Position)
					errors.Add("Net actual execution position differs from strategy.Position.");
			};
		}, TimeSpan.FromDays(31));

		IsTrue(errors.Count == 0, string.Join(Environment.NewLine, errors.Take(12)));
		AreEqual(entries + exits, orders);
		IsTrue(candidatePatterns > 0 && entries > 0 && fills > 0,
			"Real archive must exercise the published hammer/hanging-man geometry and one real filled order.");
		TestContext.WriteLine($"3433 ({Extension}) reachable M5 profile: {candidatePatterns} candidate patterns, {entries} entries, {exits} MFI exits, {fills} fills.");
	}

}
