namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.BusinessEntities;
using StockSharp.Messages;

public abstract partial class StrategyTests
{
	private const string PresentTrend = "1182_PresentTrend";

	private sealed class PresentTrendReference(int length, decimal multiplier, bool useMfi)
	{
		private readonly List<(decimal close, decimal typical, decimal volume)> _prices = [];
		private readonly List<(decimal up, decimal down)> _flows = [];
		private readonly List<decimal> _lines = [];
		private decimal _atr;
		private decimal _gain;
		private decimal _loss;
		private decimal? _rsi;
		private int? _lastLong;
		private int? _lastShort;

		public Sides? Signal { get; private set; }
		public int UpCrosses { get; private set; }
		public int DownCrosses { get; private set; }
		public int UnconfirmedCrosses { get; private set; }
		public int RisingRailBars { get; private set; }
		public int FallingRailBars { get; private set; }

		public void Add(ICandleMessage candle)
		{
			Signal = null;
			var prior = _prices.Count == 0 ? ((decimal close, decimal typical, decimal volume)?)null : _prices[^1];
			var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
			_prices.Add((candle.ClosePrice, typical, candle.TotalVolume));
			var money = typical * candle.TotalVolume;
			_flows.Add((typical > (prior?.typical ?? 0m) ? money : 0m, typical < (prior?.typical ?? 0m) ? money : 0m));
			var trueRange = candle.HighPrice - candle.LowPrice;
			if (prior is { } previous)
			{
				trueRange = Math.Max(trueRange, Math.Max(Math.Abs(candle.HighPrice - previous.close), Math.Abs(candle.LowPrice - previous.close)));
				var delta = candle.ClosePrice - previous.close;
				var changes = _prices.Count - 1;
				var gain = Math.Max(delta, 0m);
				var loss = Math.Max(-delta, 0m);
				if (changes <= length)
				{
					_gain += gain / length;
					_loss += loss / length;
				}
				else
				{
					_gain = (_gain * (length - 1) + gain) / length;
					_loss = (_loss * (length - 1) + loss) / length;
				}
				if (delta != 0m || _rsi is null)
					_rsi = _gain + _loss == 0m ? _rsi ?? 50m : 100m * _gain / (_gain + _loss);
			}
			var count = Math.Min(_prices.Count, length);
			_atr = (_atr * (count - 1) + trueRange) / count;
			if (_prices.Count < length || !useMfi && _prices.Count <= length)
				return;
			var window = _flows.Skip(_flows.Count - length).ToArray();
			var upFlow = window.Sum(flow => flow.up);
			var downFlow = window.Sum(flow => flow.down);
			var oscillator = useMfi ? downFlow == 0m ? 100m : 100m * upFlow / (upFlow + downFlow) : _rsi.Value;
			var bullish = oscillator >= 50m;
			var candidate = bullish ? candle.LowPrice - multiplier * _atr : candle.HighPrice + multiplier * _atr;
			var line = _lines.Count == 0 ? candidate : bullish ? Math.Max(_lines[^1], candidate) : Math.Min(_lines[^1], candidate);
			if (bullish)
				RisingRailBars++;
			else
				FallingRailBars++;
			if (_lines.Count >= 3)
			{
				var up = _lines[^1] <= _lines[^3] && line > _lines[^2];
				var down = _lines[^1] >= _lines[^3] && line < _lines[^2];
				if (up)
				{
					UpCrosses++;
					if (_lastShort is int lastShort && (_lastLong is null || lastShort > _lastLong))
						Signal = Sides.Buy;
					else
						UnconfirmedCrosses++;
					_lastLong = _lines.Count;
				}
				else if (down)
				{
					DownCrosses++;
					if (_lastLong is int lastLong && (_lastShort is null || lastLong > _lastShort))
						Signal = Sides.Sell;
					else
						UnconfirmedCrosses++;
					_lastShort = _lines.Count;
				}
			}
			_lines.Add(line);
		}
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S1182_ActualOrdersNeedAtrOscillatorAndConfirmedTwoBarCross()
	{
		var reference = new PresentTrendReference(14, 1m, false);
		var orders = 0;
		var invalid = 0;
		await Replay(PresentTrend, (strategy, _) =>
		{
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State == CandleStates.Finished)
					reference.Add(candle);
			};
			strategy.OrderRegistering += order =>
			{
				orders++;
				if (order.Side != reference.Signal || order.Volume != strategy.Volume + Math.Abs(strategy.Position))
					invalid++;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(orders > 0);
		AreEqual(0, invalid, "An EMA cross cannot replace a causal ATR/RSI PresentTrend two-bar cross confirmed by the latest opposite raw signal.");
	}

	[TestMethod]
	[TestCategory("Shard06")]
	[DataRow(false, false, 14, 1.0, true, true)]
	[DataRow(true, true, 7, 1.5, true, true)]
	[DataRow(false, true, 14, 1.0, true, true)]
	[DataRow(false, false, 14, 1.0, true, false)]
	[DataRow(true, true, 7, 1.5, false, true)]
	[DataRow(false, false, 7, 0.5, true, true)]
	public async Task S1182_EveryRsiMfiRailSignalReversalAndOneSidedExit(bool secondary, bool useMfi, int length,
		double multiplier, bool longEnabled, bool shortEnabled)
	{
		var reference = new PresentTrendReference(length, (decimal)multiplier, useMfi);
		var violations = new List<string>();
		var orders = 0;
		var entries = 0;
		var exits = 0;
		var reversals = 0;
		var nearSignals = 0;
		var position = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		DateTime? lastOrder = null;
		Order pending = null;
		await Replay(PresentTrend, (strategy, other) =>
		{
			if (secondary)
				strategy.Security = other;
			foreach (var (name, value) in new (string, object)[]
			{
				("Length", 14), ("Multiplier", 1m), ("IsUseMfi", false),
				("IsLongEnabled", true), ("IsShortEnabled", true),
			})
			{
				IsTrue(strategy.Parameters.ContainsKey(name), $"PresentTrend needs its real {name} parameter.");
				AreEqual(value, strategy.Parameters[name].Value);
			}
			SetParam(strategy, "Length", length);
			SetParam(strategy, "Multiplier", (decimal)multiplier);
			SetParam(strategy, "IsUseMfi", useMfi);
			SetParam(strategy, "IsLongEnabled", longEnabled);
			SetParam(strategy, "IsShortEnabled", shortEnabled);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;
				if (expectedSide is not null)
					violations.Add("A confirmed rail cross or one-sided exit was missed.");
				expectedSide = null;
				reference.Add(candle);
				if (reference.Signal is not Sides side || pending is not null && pending.State is not (OrderStates.Done or OrderStates.Failed))
					return;
				if (side == Sides.Buy && position > 0m || side == Sides.Sell && position < 0m)
					return;
				var enabled = side == Sides.Buy ? longEnabled : shortEnabled;
				if (!enabled && position == 0m)
					return;
				expectedSide = side;
				expectedVolume = Math.Abs(position) + (enabled ? strategy.Volume : 0m);
			};
			strategy.OrderRegistering += order =>
			{
				orders++;
				if (order.Type != OrderTypes.Market || order.Side != expectedSide || order.Volume != expectedVolume || strategy.Position != position)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume} from a confirmed rail cross.");
				var enabled = order.Side == Sides.Buy ? longEnabled : shortEnabled;
				if (enabled)
				{
					entries++;
					if (position != 0m)
						reversals++;
				}
				else
					exits++;
				if (lastOrder is DateTime last && strategy.CurrentTime - last < TimeSpan.FromHours(6))
					nearSignals++;
				lastOrder = strategy.CurrentTime;
				pending = order;
				expectedSide = null;
			};
			strategy.OwnTradeReceived += (_, trade) =>
			{
				position += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
				if (!longEnabled && position > 0m || !shortEnabled && position < 0m)
					violations.Add("A fill opened a disabled direction.");
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"{PresentTrend}: MFI={useMfi}, long/short={longEnabled}/{shortEnabled}, {orders} orders, {entries} entries, {exits} one-sided exits, {reversals} full reversals, {nearSignals} signals <6h, raw crosses {reference.UpCrosses}/{reference.DownCrosses}, unconfirmed {reference.UnconfirmedCrosses}, oscillator >=50/<50 bars {reference.RisingRailBars}/{reference.FallingRailBars}.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		IsTrue(expectedSide is null);
		IsTrue(entries > 0 && nearSignals > 0 && reference.UpCrosses > 0 && reference.DownCrosses > 0
			&& reference.UnconfirmedCrosses > 0 && reference.RisingRailBars > 0 && reference.FallingRailBars > 0);
		if (longEnabled && shortEnabled)
			IsTrue(reversals > 0 && exits == 0);
		else
			IsTrue(exits > 0 && reversals == 0);
	}
}
