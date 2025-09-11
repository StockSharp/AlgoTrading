using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy trading on ZigZag pivots.
/// </summary>
public class ZigzagCandlesStrategy : Strategy
{
	private readonly StrategyParam<int> _zigzagLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastZigzag;
	private decimal _lastZigzagHigh;
	private decimal _lastZigzagLow;
	private int _direction;

	public int ZigzagLength
	{
		get => _zigzagLength.Value;
		set => _zigzagLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ZigzagCandlesStrategy()
	{
		_zigzagLength = Param(nameof(ZigzagLength), 5)
			.SetDisplay("ZigZag Length", "Lookback for pivot search", "ZigZag");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_lastZigzag = 0m;
		_lastZigzagHigh = 0m;
		_lastZigzagLow = 0m;
		_direction = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var highest = new Highest { Length = ZigzagLength };
		var lowest = new Lowest { Length = ZigzagLength };

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

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.HighPrice >= highest && _direction != 1)
		{
			_lastZigzag = candle.HighPrice;
			_lastZigzagHigh = candle.HighPrice;
			_direction = 1;
		}
		else if (candle.LowPrice <= lowest && _direction != -1)
		{
			_lastZigzag = candle.LowPrice;
			_lastZigzagLow = candle.LowPrice;
			_direction = -1;
		}

		if (_lastZigzag == _lastZigzagLow && Position <= 0)
			BuyMarket();
		else if (_lastZigzag == _lastZigzagHigh && Position >= 0)
			SellMarket();
	}
}
