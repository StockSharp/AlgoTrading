namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public abstract partial class StrategyTests
{
	private const string GoldenEdge = "1180_Precision_Trading_Golden_Edge";

	private sealed class GoldenEdgeReference(int fastLength, int slowLength, int hmaLength, int rsiLength,
		int atrLength, int rangeLength, decimal minAtrPercent, decimal rangeAtrMultiplier, decimal longThreshold, decimal shortThreshold)
	{
		private readonly ImpulseEma _fast = new(fastLength);
		private readonly ImpulseEma _slow = new(slowLength);
		private readonly List<decimal> _closes = [];
		private readonly List<decimal> _hullDifferences = [];
		private readonly List<(decimal high, decimal low)> _ranges = [];
		private decimal _gainSum;
		private decimal _lossSum;
		private decimal _gain;
		private decimal _loss;
		private decimal? _rsi;
		private decimal _atr;
		private decimal? _previousHma;
		private decimal _previousFast;
		private decimal _previousSlow;
		private bool _isInitialized;

		public Sides? Entry { get; private set; }
		public int UpCrosses { get; private set; }
		public int DownCrosses { get; private set; }
		public int RsiRejected { get; private set; }
		public int HmaRejected { get; private set; }
		public int VolatilityRejected { get; private set; }

		private static decimal WeightedAverage(List<decimal> prices, int length)
		{
			var sum = 0m;
			for (var i = 0; i < length; i++)
				sum += prices[prices.Count - length + i] * (i + 1);
			return sum / (length * (length + 1m) / 2m);
		}

		public void Add(ICandleMessage candle)
		{
			Entry = null;
			var close = candle.ClosePrice;
			var priorClose = _closes.Count == 0 ? (decimal?)null : _closes[^1];
			_closes.Add(close);
			_ranges.Add((candle.HighPrice, candle.LowPrice));
			_fast.Add(close);
			_slow.Add(close);
			if (priorClose is decimal previous)
			{
				var delta = close - previous;
				var count = _closes.Count - 1;
				if (count <= rsiLength)
				{
					_gainSum += Math.Max(delta, 0m);
					_lossSum += Math.Max(-delta, 0m);
					_gain = _gainSum / rsiLength;
					_loss = _lossSum / rsiLength;
				}
				else
				{
					_gain = (_gain * (rsiLength - 1) + Math.Max(delta, 0m)) / rsiLength;
					_loss = (_loss * (rsiLength - 1) + Math.Max(-delta, 0m)) / rsiLength;
				}
				if (delta != 0m || _rsi is null)
					_rsi = _gain + _loss == 0m ? _rsi ?? 50m : 100m * _gain / (_gain + _loss);
			}
			var trueRange = priorClose is decimal old
				? Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - old), Math.Abs(candle.LowPrice - old)))
				: candle.HighPrice - candle.LowPrice;
			var atrCount = Math.Min(_closes.Count, atrLength);
			_atr = (_atr * (atrCount - 1) + trueRange) / atrCount;
			if (_closes.Count >= hmaLength)
				_hullDifferences.Add(2m * WeightedAverage(_closes, hmaLength / 2) - WeightedAverage(_closes, hmaLength));
			var rootLength = (int)Math.Sqrt(hmaLength);
			if (!_fast.Formed || !_slow.Formed || _closes.Count <= rsiLength || _closes.Count < atrLength
				|| _ranges.Count < rangeLength || _hullDifferences.Count < rootLength)
				return;
			var hma = WeightedAverage(_hullDifferences, rootLength);
			if (_isInitialized)
			{
				var up = _previousFast <= _previousSlow && _fast.Value > _slow.Value;
				var down = _previousFast >= _previousSlow && _fast.Value < _slow.Value;
				if (up)
					UpCrosses++;
				if (down)
					DownCrosses++;
				if (up || down)
				{
					var rsiPass = up ? _rsi > longThreshold : _rsi < shortThreshold;
					var hmaPass = up ? hma > _previousHma : hma < _previousHma;
					var ranges = _ranges.Skip(_ranges.Count - rangeLength).ToArray();
					var volatilityPass = close > 0m && _atr > 0m && 100m * _atr / close >= minAtrPercent
						&& ranges.Max(bar => bar.high) - ranges.Min(bar => bar.low) >= _atr * rangeAtrMultiplier;
					if (!rsiPass)
						RsiRejected++;
					if (!hmaPass)
						HmaRejected++;
					if (!volatilityPass)
						VolatilityRejected++;
					if (rsiPass && hmaPass && volatilityPass)
						Entry = up ? Sides.Buy : Sides.Sell;
				}
			}
			_isInitialized = true;
			_previousFast = _fast.Value;
			_previousSlow = _slow.Value;
			_previousHma = hma;
		}
	}

	[TestMethod]
	[TestCategory("Shard04")]
	public async Task S1180_DefaultEntriesNeedHmaRsiAndVolatilityConfirmation()
	{
		var reference = new GoldenEdgeReference(14, 40, 55, 14, 14, 20, 0.05m, 3m, 55m, 45m);
		var entries = 0;
		var invalid = 0;
		await Replay(GoldenEdge, (strategy, _) =>
		{
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State == CandleStates.Finished)
					reference.Add(candle);
			};
			strategy.OrderRegistering += order =>
			{
				entries++;
				if (order.Side != reference.Entry || order.Volume != strategy.Volume + Math.Abs(strategy.Position))
					invalid++;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(entries > 0);
		AreEqual(0, invalid, "An actual entry must confirm its EMA cross with HMA slope, RSI 55/45 and real ATR/range volatility, and fully reverse the held side.");
	}

	[TestMethod]
	[TestCategory("Shard04")]
	[DataRow(false, 14, 40, 55, 14, 14, 20, 0.05, 3.0)]
	[DataRow(true, 5, 20, 21, 7, 7, 14, 0.1, 2.0)]
	[DataRow(false, 14, 40, 55, 14, 14, 20, 0.2, 3.0)]
	[DataRow(false, 8, 21, 34, 7, 10, 40, 0.05, 6.0)]
	public async Task S1180_ConfirmedEmaCrossesAndFullReversalsDetermineEveryOrder(bool secondary, int fastLength,
		int slowLength, int hmaLength, int rsiLength, int atrLength, int rangeLength, double minAtrPercent, double rangeAtrMultiplier)
	{
		var longThreshold = secondary ? 60m : 55m;
		var shortThreshold = secondary ? 40m : 45m;
		var reference = new GoldenEdgeReference(fastLength, slowLength, hmaLength, rsiLength,
			atrLength, rangeLength, (decimal)minAtrPercent, (decimal)rangeAtrMultiplier, longThreshold, shortThreshold);
		var violations = new List<string>();
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var orders = 0;
		var buys = 0;
		var sells = 0;
		var reversals = 0;
		var nearSignals = 0;
		var position = 0m;
		Order pending = null;
		DateTime? lastOrder = null;
		await Replay(GoldenEdge, (strategy, otherSecurity) =>
		{
			if (secondary)
				strategy.Security = otherSecurity;
			foreach (var (name, value) in new (string, object)[]
			{
				("FastLength", 14), ("SlowLength", 40), ("HmaLength", 55), ("RsiLength", 14),
				("RsiLongThreshold", 55m), ("RsiShortThreshold", 45m), ("AtrLength", 14),
				("RangeLength", 20), ("MinAtrPercent", 0.05m), ("RangeAtrMultiplier", 3m),
			})
			{
				IsTrue(strategy.Parameters.ContainsKey(name), $"Golden Edge needs {name}; an unfiltered EMA crossover does not fulfill the README.");
				AreEqual(value, strategy.Parameters[name].Value);
			}
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			foreach (var (name, value) in new (string, object)[]
			{
				("FastLength", fastLength), ("SlowLength", slowLength), ("HmaLength", hmaLength),
				("RsiLength", rsiLength), ("AtrLength", atrLength), ("RangeLength", rangeLength),
				("MinAtrPercent", (decimal)minAtrPercent), ("RangeAtrMultiplier", (decimal)rangeAtrMultiplier),
				("RsiLongThreshold", longThreshold), ("RsiShortThreshold", shortThreshold),
			})
				SetParam(strategy, name, value);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;
				if (expectedSide is not null)
					violations.Add("An eligible confirmed EMA cross was missed.");
				expectedSide = null;
				reference.Add(candle);
				if (pending is not null && pending.State is not (OrderStates.Done or OrderStates.Failed))
					return;
				if (reference.Entry is not Sides side || side == Sides.Buy && position > 0m || side == Sides.Sell && position < 0m)
					return;
				expectedSide = side;
				expectedVolume = strategy.Volume + Math.Abs(position);
				expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				orders++;
				if (order.Type != OrderTypes.Market || order.Side != expectedSide || order.Volume != expectedVolume || strategy.Position != position)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}; expected {expectedSide} {expectedVolume} from the full EMA/HMA/RSI/ATR/highest-lowest contract.");
				if (order.Side == Sides.Buy)
					buys++;
				else
					sells++;
				if (position != 0m)
					reversals++;
				if (lastOrder is DateTime last && strategy.CurrentTime - last < TimeSpan.FromHours(6))
					nearSignals++;
				lastOrder = strategy.CurrentTime;
				pending = order;
				expectedSide = null;
			};
			strategy.OwnTradeReceived += (_, trade) => position += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		IsTrue(expectedSide is null);
		AreEqual(expectedOrders, orders);
		IsTrue(buys > 0 && sells > 0 && reversals > 0 && nearSignals > 0,
			$"Coverage: {buys}/{sells} buys/sells, {reversals} reversals, {nearSignals} signals within six hours.");
		IsTrue(reference.RsiRejected > 0 && reference.HmaRejected > 0,
			$"Coverage: {reference.RsiRejected}/{reference.HmaRejected} RSI/HMA rejections.");
		if (minAtrPercent >= 0.2 || rangeAtrMultiplier >= 6.0)
			IsTrue(reference.VolatilityRejected > 0, "Tight volatility fixtures must reject actual EMA crosses without suppressing every trade.");
		TestContext.WriteLine($"{GoldenEdge}: {orders} orders ({buys}/{sells}), {reversals} full reversals, {nearSignals} signals within six hours, {reference.UpCrosses}/{reference.DownCrosses} crosses, {reference.RsiRejected}/{reference.HmaRejected}/{reference.VolatilityRejected} RSI/HMA/volatility rejections.");
	}
}
