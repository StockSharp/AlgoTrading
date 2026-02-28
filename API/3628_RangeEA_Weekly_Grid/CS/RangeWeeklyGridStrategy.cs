using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Range based grid strategy that detects the trading range from recent price action
/// and places buy/sell limit orders at grid levels within the range.
/// Buys at lower grid levels, sells at upper grid levels.
/// </summary>
public class RangeWeeklyGridStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rangePeriod;
	private readonly StrategyParam<int> _gridLevels;

	private decimal _rangeHigh;
	private decimal _rangeLow;
	private bool _rangeSet;
	private decimal _entryPrice;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RangePeriod
	{
		get => _rangePeriod.Value;
		set => _rangePeriod.Value = value;
	}

	public int GridLevels
	{
		get => _gridLevels.Value;
		set => _gridLevels.Value = value;
	}

	public RangeWeeklyGridStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle type", "General");

		_rangePeriod = Param(nameof(RangePeriod), 100)
			.SetDisplay("Range Period", "Number of candles to determine range", "Logic");

		_gridLevels = Param(nameof(GridLevels), 5)
			.SetDisplay("Grid Levels", "Number of grid levels within the range", "Logic");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = RangePeriod };
		var lowest = new Lowest { Length = RangePeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (highestValue <= 0 || lowestValue <= 0 || highestValue <= lowestValue)
			return;

		_rangeHigh = highestValue;
		_rangeLow = lowestValue;
		_rangeSet = true;

		if (!_rangeSet)
			return;

		var range = _rangeHigh - _rangeLow;
		if (range <= 0)
			return;

		var gridStep = range / (GridLevels + 1);
		var close = candle.ClosePrice;
		var mid = (_rangeHigh + _rangeLow) / 2;

		// Buy when price is in lower portion of range
		if (close <= _rangeLow + gridStep && Position <= 0)
		{
			BuyMarket();
			_entryPrice = close;
		}
		// Sell when price is in upper portion of range
		else if (close >= _rangeHigh - gridStep && Position >= 0)
		{
			SellMarket();
			_entryPrice = close;
		}
		// Take profit at mid-range
		else if (Position > 0 && close >= mid)
		{
			SellMarket();
		}
		else if (Position < 0 && close <= mid)
		{
			BuyMarket();
		}
	}
}
