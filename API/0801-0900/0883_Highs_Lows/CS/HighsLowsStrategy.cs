using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Highs Lows strategy.
/// Tracks the highest high and lowest low of the last Range candles. The candle midpoint is compared with the average of
/// these extremes, and their distance is normalized by half the range (0 at the average, 100 at an extreme). A long opens when the midpoint is
/// below the average and the normalized distance is below LowThreshold, and closes when the midpoint is above the average
/// and the normalized distance is above HighThreshold.
/// </summary>
public class HighsLowsStrategy : Strategy
{
	private readonly StrategyParam<int> _range;
	private readonly StrategyParam<decimal> _lowThreshold;
	private readonly StrategyParam<decimal> _highThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;

	/// <summary>
	/// Number of candles for the highest and lowest values.
	/// </summary>
	public int Range
	{
		get => _range.Value;
		set => _range.Value = value;
	}

	/// <summary>
	/// Normalized distance below which a long opens.
	/// </summary>
	public decimal LowThreshold
	{
		get => _lowThreshold.Value;
		set => _lowThreshold.Value = value;
	}

	/// <summary>
	/// Normalized distance above which the long closes.
	/// </summary>
	public decimal HighThreshold
	{
		get => _highThreshold.Value;
		set => _highThreshold.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public HighsLowsStrategy()
	{
		_range = Param(nameof(Range), 100)
			.SetGreaterThanZero()
			.SetDisplay("Range", "Number of candles for highest and lowest values", "Indicators");

		_lowThreshold = Param(nameof(LowThreshold), 15m)
			.SetDisplay("Low Threshold", "Normalized distance below which a long opens", "Signals");

		_highThreshold = Param(nameof(HighThreshold), 85m)
			.SetDisplay("High Threshold", "Normalized distance above which the long closes", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(240).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_highest = null;
		_lowest = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highest = new Highest { Length = Range };
		_lowest = new Lowest { Length = Range };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highest = _highest.Process(candle.HighPrice, candle.OpenTime, true).ToDecimal();
		var lowest = _lowest.Process(candle.LowPrice, candle.OpenTime, true).ToDecimal();

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var width = highest - lowest;
		if (width <= 0)
			return;

		var midpoint = (candle.HighPrice + candle.LowPrice) / 2;
		var average = (highest + lowest) / 2;
		// Distance from the average as a percentage of the half range: 0 at the average, 100 at an extreme.
		var distance = Math.Abs(midpoint - average) / (width / 2) * 100m;

		if (Position <= 0 && midpoint < average && distance < LowThreshold)
			BuyMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && midpoint > average && distance > HighThreshold)
			SellMarket(Position);
	}
}
