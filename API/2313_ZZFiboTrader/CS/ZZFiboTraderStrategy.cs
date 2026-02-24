using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades breakouts of Fibonacci 50% retracement levels from ZigZag pivots.
/// Uses Highest/Lowest to detect pivot points and enters on 50% level crossover.
/// </summary>
public class ZZFiboTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _zigZagDepth;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevPivot;
	private decimal _currPivot;
	private int _direction;
	private decimal _level50;

	public int ZigZagDepth
	{
		get => _zigZagDepth.Value;
		set => _zigZagDepth.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ZZFiboTraderStrategy()
	{
		_zigZagDepth = Param(nameof(ZigZagDepth), 12)
			.SetDisplay("ZigZag Depth", "Number of bars to search for pivots", "ZigZag")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevPivot = 0m;
		_currPivot = 0m;
		_direction = 0;
		_level50 = 0m;

		var highest = new Highest { Length = ZigZagDepth };
		var lowest = new Lowest { Length = ZigZagDepth };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Update pivots when new extremes appear
		if (candle.HighPrice >= highest && candle.HighPrice != _currPivot)
		{
			_prevPivot = _currPivot;
			_currPivot = candle.HighPrice;
			UpdateLevels();
		}
		else if (candle.LowPrice <= lowest && candle.LowPrice != _currPivot)
		{
			_prevPivot = _currPivot;
			_currPivot = candle.LowPrice;
			UpdateLevels();
		}

		if (_direction == 0 || _level50 == 0m)
			return;

		// Enter when price crosses 50% level in direction of trend
		if (_direction == 1 && Position <= 0 && candle.ClosePrice > _level50)
			BuyMarket();
		else if (_direction == -1 && Position >= 0 && candle.ClosePrice < _level50)
			SellMarket();
	}

	private void UpdateLevels()
	{
		if (_prevPivot == 0m || _currPivot == 0m)
			return;

		_direction = _currPivot > _prevPivot ? 1 : -1;

		var high = _direction == 1 ? _currPivot : _prevPivot;
		var low = _direction == 1 ? _prevPivot : _currPivot;

		_level50 = high - (high - low) * 0.5m;
	}
}
