using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on a simplified ColorBullsGap indicator.
/// Enters long when a bullish color turns neutral or bearish.
/// Enters short when a bearish color turns neutral or bullish.
/// </summary>
public class ColorBullsGapStrategy : Strategy
{
	private readonly StrategyParam<int> _length1;
	private readonly StrategyParam<int> _length2;
	private readonly StrategyParam<DataType> _candleType;
	private SimpleMovingAverage _smaClose;
	private SimpleMovingAverage _smaOpen;
	private SimpleMovingAverage _smaBullsC;
	private SimpleMovingAverage _smaBullsO;
	private readonly Queue<int> _colorHistory = new();
	private decimal _prevXbullsC;
	private bool _isFirst = true;

	public int Length1 { get => _length1.Value; set => _length1.Value = value; }
	public int Length2 { get => _length2.Value; set => _length2.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorBullsGapStrategy()
	{
		_length1 = Param(nameof(Length1), 12)
			.SetGreaterThanZero()
			.SetDisplay("First Length", "Length for initial smoothing", "Indicator");
		_length2 = Param(nameof(Length2), 5)
			.SetGreaterThanZero()
			.SetDisplay("Second Length", "Length for secondary smoothing", "Indicator");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_smaClose = new SimpleMovingAverage { Length = Length1 };
		_smaOpen = new SimpleMovingAverage { Length = Length1 };
		_smaBullsC = new SimpleMovingAverage { Length = Length2 };
		_smaBullsO = new SimpleMovingAverage { Length = Length2 };
		_isFirst = true;
		_colorHistory.Clear();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var t = candle.OpenTime;
		var smaClose = _smaClose.Process(candle.ClosePrice, t, true).GetValue<decimal>();
		var smaOpen = _smaOpen.Process(candle.OpenPrice, t, true).GetValue<decimal>();
		var bullsC = candle.HighPrice - smaClose;
		var bullsO = candle.HighPrice - smaOpen;
		var xbullsC = _smaBullsC.Process(bullsC, t, true).GetValue<decimal>();
		var xbullsO = _smaBullsO.Process(bullsO, t, true).GetValue<decimal>();

		if (_isFirst)
		{
			_prevXbullsC = xbullsC;
			_isFirst = false;
			return;
		}

		var diff = xbullsO - _prevXbullsC;
		var color = diff > 0 ? 0 : diff < 0 ? 2 : 1;
		_prevXbullsC = xbullsC;
		_colorHistory.Enqueue(color);
		if (_colorHistory.Count > 2)
			_colorHistory.Dequeue();
		if (_colorHistory.Count < 2)
			return;

		var prevColor = _colorHistory.ElementAt(0);
		var lastColor = _colorHistory.ElementAt(1);

		if (prevColor == 0)
		{
			if (lastColor > 0)
				BuyMarket();
			else if (Position < 0)
				BuyMarket();
		}
		else if (prevColor == 2)
		{
			if (lastColor < 2)
				SellMarket();
			else if (Position > 0)
				SellMarket();
		}
	}
}
