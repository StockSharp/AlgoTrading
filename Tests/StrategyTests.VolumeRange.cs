namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public abstract partial class StrategyTests
{
	private const string VolumeRange = "1178_Powertrend_Volume_Range_Filter";

	[TestMethod]
	[TestCategory("Shard02")]
	public async Task S1178_DefaultNeverOpensShortPositions()
	{
		var position = 0m;
		var minimum = 0m;
		await Replay(VolumeRange, (strategy, _) => strategy.OwnTradeReceived += (_, trade) =>
		{
			position += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
			minimum = Math.Min(minimum, position);
		}, TimeSpan.FromDays(31));
		AreEqual(0m, minimum, "The published volume-range strategy only buys and closes longs; EMA short entries violate its contract.");
	}

	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow(false, false, false, false)]
	[DataRow(false, true, false, false)]
	[DataRow(false, false, true, false)]
	[DataRow(false, false, false, true)]
	[DataRow(false, true, true, false)]
	[DataRow(false, true, false, true)]
	[DataRow(false, false, true, true)]
	[DataRow(false, true, true, true)]
	[DataRow(true, true, true, true)]
	public async Task S1178_VolumeWeightedRangeBandsAndOptionalFiltersDetermineEveryLongOrder(bool secondary,
		bool adxEnabled, bool highLowEnabled, bool vwmaEnabled)
	{
		var period = secondary ? 14 : 40;
		var multiplier = secondary ? 1.2m : 1.5m;
		var adxPeriod = adxEnabled ? secondary ? 7 : 14 : 200;
		var adxThreshold = secondary ? 40m : 20m;
		var vwmaPeriod = secondary ? 7 : 20;
		var adx = new AverageDirectionalIndex { Length = adxPeriod };
		var changes = new List<(decimal delta, decimal volume)>();
		var filtered = new List<(decimal price, decimal volume)>();
		var violations = new List<string>();
		decimal? previousClose = null;
		decimal? previousHigh = null;
		decimal? previousLow = null;
		decimal? filter = null;
		Order pending = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var position = 0m;
		var entries = 0;
		var exits = 0;
		var expectedOrders = 0;
		var volumeChangedWidths = 0;
		var adxRejected = 0;
		var highLowRejected = 0;
		var vwmaRejected = 0;
		var filteredExits = 0;
		var nearSignals = 0;
		DateTime? lastEntry = null;
		await Replay(VolumeRange, (strategy, otherSecurity) =>
		{
			if (secondary)
				strategy.Security = otherSecurity;
			foreach (var (name, value) in new (string, object)[]
			{
				("RangePeriod", 40), ("RangeMultiplier", 1.5m), ("AdxPeriod", 14), ("AdxThreshold", 20m),
				("VwmaPeriod", 20), ("IsAdxFilterEnabled", false), ("IsHighLowFilterEnabled", false), ("IsVwmaFilterEnabled", false),
			})
			{
				IsTrue(strategy.Parameters.ContainsKey(name), $"Volume-range filtering requires {name}; EMA is not a volume-adjusted range filter.");
				AreEqual(value, strategy.Parameters[name].Value);
			}
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			foreach (var (name, value) in new (string, object)[]
			{
				("RangePeriod", period), ("RangeMultiplier", multiplier), ("AdxPeriod", adxPeriod), ("AdxThreshold", adxThreshold),
				("VwmaPeriod", vwmaPeriod), ("IsAdxFilterEnabled", adxEnabled),
				("IsHighLowFilterEnabled", highLowEnabled), ("IsVwmaFilterEnabled", vwmaEnabled),
			})
				SetParam(strategy, name, value);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;
				if (expectedSide is not null)
					violations.Add("An eligible volume-range entry/exit was missed.");
				expectedSide = null;
				var value = (AverageDirectionalIndexValue)adx.Process(candle);
				var higherHighLow = previousHigh is decimal high && previousLow is decimal low && candle.HighPrice > high && candle.LowPrice > low;
				previousHigh = candle.HighPrice;
				previousLow = candle.LowPrice;
				if (previousClose is decimal previous)
					changes.Add((Math.Abs(candle.ClosePrice - previous), candle.TotalVolume));
				previousClose = candle.ClosePrice;
				if (changes.Count < period)
					return;
				var sample = changes.Skip(changes.Count - period).ToArray();
				var totalVolume = sample.Sum(item => item.volume);
				if (totalVolume <= 0m)
					return;
				var width = multiplier * sample.Sum(item => item.delta * item.volume) / totalVolume;
				if (Math.Abs(width - multiplier * sample.Average(item => item.delta)) > 0.0000001m)
					volumeChangedWidths++;
				if (filter is not decimal prior)
				{
					filter = candle.ClosePrice;
					filtered.Add((filter.Value, candle.TotalVolume));
					return;
				}
				var upper = prior + width;
				var lower = prior - width;
				filter = Math.Clamp(prior, candle.ClosePrice - width, candle.ClosePrice + width);
				filtered.Add((filter.Value, candle.TotalVolume));
				var vwmaSample = filtered.Skip(Math.Max(0, filtered.Count - vwmaPeriod)).ToArray();
				var vwmaVolume = vwmaSample.Sum(item => item.volume);
				var vwmaPass = !vwmaEnabled || filtered.Count >= vwmaPeriod && vwmaVolume > 0m
					&& candle.ClosePrice > vwmaSample.Sum(item => item.price * item.volume) / vwmaVolume;
				var adxPass = !adxEnabled || value.IsFormed && value.MovingAverage is decimal strength && strength > adxThreshold;
				var highLowPass = !highLowEnabled || higherHighLow;
				if (pending is not null && pending.State is not (OrderStates.Done or OrderStates.Failed))
					return;
				if (candle.ClosePrice < lower && position > 0m)
				{
					expectedSide = Sides.Sell;
					expectedVolume = position;
					if (!adxPass || !highLowPass || !vwmaPass)
						filteredExits++;
				}
				else if (width > 0m && candle.ClosePrice > upper && filter > prior && position == 0m)
				{
					if (!adxPass) adxRejected++;
					if (!highLowPass) highLowRejected++;
					if (!vwmaPass) vwmaRejected++;
					if (adxPass && highLowPass && vwmaPass)
					{
						expectedSide = Sides.Buy;
						expectedVolume = strategy.Volume;
					}
				}
				if (expectedSide is not null)
					expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				if (order.Type != OrderTypes.Market || order.Side != expectedSide || order.Volume != expectedVolume || strategy.Position != position)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}; expected {expectedSide} {expectedVolume} from volume-weighted bands and optional entry filters.");
				if (order.Side == Sides.Buy)
				{
					entries++;
					if (lastEntry is DateTime last && strategy.CurrentTime - last < TimeSpan.FromHours(6))
						nearSignals++;
					lastEntry = strategy.CurrentTime;
				}
				else
				{
					exits++;
					if (position <= 0m)
						violations.Add("A sell must close a real long; short entries are forbidden.");
				}
				expectedSide = null;
				pending = order;
			};
			strategy.OwnTradeReceived += (_, trade) => position += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		IsTrue(expectedSide is null && position >= 0m);
		AreEqual(expectedOrders, entries + exits);
		IsTrue(entries > 0 && exits > 0 && volumeChangedWidths > 0 && nearSignals > 0);
		if (adxEnabled)
			IsTrue(adxRejected > 0, $"The ADX {adxPeriod}/{adxThreshold} fixture must exercise actual rejected entries.");
		if (highLowEnabled)
			IsTrue(highLowRejected > 0, "High/low confirmation must reject actual upper-band entry opportunities.");
		if (vwmaEnabled)
			IsTrue(vwmaRejected > 0, "The filter-value VWMA must reject actual upper-band entry opportunities.");
		if (adxEnabled || highLowEnabled || vwmaEnabled)
			IsTrue(filteredExits > 0, "Entry filters must never prevent a real lower-band exit.");
		TestContext.WriteLine($"{VolumeRange}: {entries} entries / {exits} exits, {nearSignals} entries within six hours, {volumeChangedWidths} volume-sensitive widths, {adxRejected}/{highLowRejected}/{vwmaRejected} ADX/high-low/VWMA rejections, {filteredExits} exits with rejected entry filters.");
	}
}
