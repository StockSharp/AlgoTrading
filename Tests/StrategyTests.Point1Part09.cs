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
}
