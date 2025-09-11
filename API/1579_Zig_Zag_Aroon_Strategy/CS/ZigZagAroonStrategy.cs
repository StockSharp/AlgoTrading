using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ZigZag + Aroon strategy.
/// </summary>
public class ZigZagAroonStrategy : Strategy
{
	private readonly StrategyParam<int> _zigZagDepth;
	private readonly StrategyParam<int> _aroonLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastZigzag;
	private decimal _lastZigzagHigh;
	private decimal _lastZigzagLow;
	private int _direction;
	private decimal _prevAroonUp;
	private decimal _prevAroonDown;

	public int ZigZagDepth
	{
		get => _zigZagDepth.Value;
		set => _zigZagDepth.Value = value;
	}

	public int AroonLength
	{
		get => _aroonLength.Value;
		set => _aroonLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ZigZagAroonStrategy()
	{
		_zigZagDepth = Param(nameof(ZigZagDepth), 5)
			.SetDisplay("ZigZag Depth", "Pivot search depth", "ZigZag");
		_aroonLength = Param(nameof(AroonLength), 14)
			.SetDisplay("Aroon Period", "Aroon indicator period", "Aroon");
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
		_prevAroonUp = 0m;
		_prevAroonDown = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var highest = new Highest { Length = ZigZagDepth };
		var lowest = new Lowest { Length = ZigZagDepth };
		var aroon = new Aroon { Length = AroonLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, aroon, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, aroon);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest, IIndicatorValue aroonValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var aroon = (AroonValue)aroonValue;
		var up = aroon.Up;
		var down = aroon.Down;

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

		var crossUp = _prevAroonUp <= _prevAroonDown && up > down;
		var crossDown = _prevAroonDown <= _prevAroonUp && down > up;

		if (crossUp && _lastZigzag == _lastZigzagHigh && Position <= 0)
			BuyMarket();
		else if (crossDown && _lastZigzag == _lastZigzagLow && Position >= 0)
			SellMarket();

		_prevAroonUp = up;
		_prevAroonDown = down;
	}
}
