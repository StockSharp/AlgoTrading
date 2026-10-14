namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Messages;

public abstract partial class StrategyTests
{
	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow(false, 2, 3.0, 10)]
	[DataRow(true, 4, 2.0, 7)]
	public async Task S1170_ConfirmedPivotBandsDetermineEveryFullReversal(bool secondary, int pivotPeriod, double factor, int atrPeriod)
	{
		const string key = "1170_Pivot_Point_Supertrend";
		var reference = new PivotBandsReference(pivotPeriod, atrPeriod, (decimal)factor);
		var violations = new List<string>();
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var filledPosition = 0m;
		var orders = 0;
		var signals = 0;
		var buys = 0;
		var sells = 0;
		var reversals = 0;
		var nearbySignals = 0;
		DateTime? previousSignalTime = null;
		await Replay(key, (strategy, alternateSecurity) =>
		{
			if (secondary)
				strategy.Security = alternateSecurity;
			foreach (var (name, value) in new (string, object)[] { ("PivotPeriod", 2), ("AtrFactor", 3m), ("AtrPeriod", 10) })
			{
				IsTrue(strategy.Parameters.ContainsKey(name), $"The pivot Supertrend needs {name}, not an EMA crossover proxy.");
				AreEqual(value, strategy.Parameters[name].Value);
			}
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "PivotPeriod", pivotPeriod);
			SetParam(strategy, "AtrFactor", (decimal)factor);
			SetParam(strategy, "AtrPeriod", atrPeriod);
			strategy.OwnTradeReceived += (_, trade) => filledPosition += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;
				if (expectedSide is not null)
					violations.Add($"{candle.OpenTime:O}: a confirmed flip was missed.");
				expectedSide = reference.Add(candle);
				if (expectedSide is null)
					return;
				if (previousSignalTime is DateTime previous && candle.OpenTime - previous < TimeSpan.FromHours(6))
					nearbySignals++;
				previousSignalTime = candle.OpenTime;
				signals++;
				expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
				if (expectedSide == Sides.Buy)
					buys++;
				else
					sells++;
				if (strategy.Position != 0m)
					reversals++;
			};
			strategy.OrderRegistering += order =>
			{
				orders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market || strategy.Position != filledPosition)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}; expected {expectedSide} {expectedVolume} from confirmed pivot bands, filled position {filledPosition}.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		IsTrue(expectedSide is null);
		AreEqual(signals, orders);
		IsTrue(reference.Highs > 0 && reference.Lows > 0 && buys > 0 && sells > 0 && reversals > 0);
		IsTrue(nearbySignals > 0, "The archive must exercise genuine flips less than six hours apart.");
		TestContext.WriteLine($"{key}: {orders} orders ({buys}/{sells}), {reversals} full reversals, {nearbySignals} signals less than six hours apart; {reference.Highs}/{reference.Lows} confirmed pivots.");
	}

	private sealed class PivotBandsReference(int pivotPeriod, int atrPeriod, decimal factor)
	{
		private readonly List<(decimal High, decimal Low, decimal Close)> _bars = [];
		private readonly List<decimal> _ranges = [];
		private decimal _atr;
		private decimal? _center;
		private decimal? _support;
		private decimal? _resistance;

		public bool? IsUp { get; private set; }
		public int Highs { get; private set; }
		public int Lows { get; private set; }

		public Sides? Add(ICandleMessage candle)
		{
			var previousClose = _bars.Count == 0 ? candle.ClosePrice : _bars[^1].Close;
			_bars.Add((candle.HighPrice, candle.LowPrice, candle.ClosePrice));
			_ranges.Add(Math.Max(candle.HighPrice - candle.LowPrice,
				Math.Max(Math.Abs(candle.HighPrice - previousClose), Math.Abs(candle.LowPrice - previousClose))));
			var count = _bars.Count;
			_atr = count <= atrPeriod ? _ranges.Average() : _atr + (_ranges[^1] - _atr) / atrPeriod;
			if (count >= 2 * pivotPeriod + 1)
			{
				var candidate = _bars[count - pivotPeriod - 1];
				var neighbors = _bars.Skip(count - 2 * pivotPeriod - 1).Take(2 * pivotPeriod + 1)
					.Where((_, index) => index != pivotPeriod).ToArray();
				var high = neighbors.All(bar => candidate.High > bar.High);
				var low = neighbors.All(bar => candidate.Low < bar.Low);
				if (high)
					Highs++;
				if (low)
					Lows++;
				if (high || low)
				{
					var pivot = high ? candidate.High : candidate.Low;
					_center = _center is decimal oldCenter ? oldCenter + (pivot - oldCenter) / 3m : pivot;
				}
			}
			if (_center is not decimal value || count < atrPeriod)
				return null;
			var oldSupport = _support;
			var oldResistance = _resistance;
			var basicSupport = value - factor * _atr;
			var basicResistance = value + factor * _atr;
			_support = oldSupport is decimal floor && previousClose > floor ? Math.Max(floor, basicSupport) : basicSupport;
			_resistance = oldResistance is decimal ceiling && previousClose < ceiling ? Math.Min(ceiling, basicResistance) : basicResistance;
			var previous = IsUp;
			IsUp = oldResistance is decimal upper && candle.ClosePrice > upper ? true
				: oldSupport is decimal lower && candle.ClosePrice < lower ? false : IsUp ?? true;
			return previous == false && IsUp == true ? Sides.Buy : previous == true && IsUp == false ? Sides.Sell : null;
		}
	}
}
