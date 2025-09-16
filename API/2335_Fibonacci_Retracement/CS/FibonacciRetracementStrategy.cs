using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Fibonacci retracement levels calculated from ZigZag pivots.
/// Buys when price breaks above a Fibonacci level in an uptrend.
/// Sells when price breaks below a Fibonacci level in a downtrend.
/// </summary>
public class FibonacciRetracementStrategy : Strategy
{
	private readonly StrategyParam<int> _zigzagDepth;
	private readonly StrategyParam<int> _safetyBuffer;
	private readonly StrategyParam<int> _trendPrecision;
	private readonly StrategyParam<int> _closeBarPause;
	private readonly StrategyParam<decimal> _takeProfitFactor;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _hl = new decimal[4];
	private int _direction;
	private int _trendDirection;
	private decimal _fibo00;
	private decimal _fibo23;
	private decimal _fibo38;
	private decimal _fibo61;
	private decimal _fibo76;
	private decimal _fibo100;
	private decimal _fiboBase;
	private decimal _prevClose;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private int _barsSinceExit;

	/// <summary>
	/// Depth parameter for ZigZag pivot detection.
	/// </summary>
	public int ZigzagDepth
	{
		get => _zigzagDepth.Value;
		set => _zigzagDepth.Value = value;
	}

	/// <summary>
	/// Number of points used as a safety buffer around Fibonacci levels.
	/// </summary>
	public int SafetyBuffer
	{
		get => _safetyBuffer.Value;
		set => _safetyBuffer.Value = value;
	}

	/// <summary>
	/// Minimal pivot distance in points to determine the trend.
	/// </summary>
	public int TrendPrecision
	{
		get => _trendPrecision.Value;
		set => _trendPrecision.Value = value;
	}

	/// <summary>
	/// Number of bars to wait after closing a position before trading again.
	/// </summary>
	public int CloseBarPause
	{
		get => _closeBarPause.Value;
		set => _closeBarPause.Value = value;
	}

	/// <summary>
	/// Take profit multiplier of the last swing range.
	/// </summary>
	public decimal TakeProfitFactor
	{
		get => _takeProfitFactor.Value;
		set => _takeProfitFactor.Value = value;
	}

	/// <summary>
	/// Stop loss in points from the entry price.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public FibonacciRetracementStrategy()
	{
		_zigzagDepth = Param(nameof(ZigzagDepth), 12)
			.SetDisplay("ZigZag Depth", "Pivot search depth", "ZigZag");

		_safetyBuffer = Param(nameof(SafetyBuffer), 1)
			.SetDisplay("Safety Buffer", "Minimum distance from level in points", "General");

		_trendPrecision = Param(nameof(TrendPrecision), 5)
			.SetDisplay("Trend Precision", "Minimum pivot difference in points", "General");

		_closeBarPause = Param(nameof(CloseBarPause), 5)
			.SetDisplay("Pause Bars", "Bars to wait after close", "Risk");

		_takeProfitFactor = Param(nameof(TakeProfitFactor), 0.2m)
			.SetDisplay("Take Profit Factor", "Extension from last extreme", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 15)
			.SetDisplay("Stop Loss Points", "Distance to stop from entry", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles timeframe", "General");
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
		Array.Clear(_hl);
		_direction = 0;
		_trendDirection = 0;
		_fibo00 = _fibo23 = _fibo38 = _fibo61 = _fibo76 = _fibo100 = _fiboBase = 0m;
		_prevClose = 0m;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_barsSinceExit = CloseBarPause;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var highest = new Highest { Length = ZigzagDepth };
		var lowest = new Lowest { Length = ZigzagDepth };

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

		// detect new pivot using zigzag logic
		if (candle.HighPrice >= highest && _direction != 1)
		{
		ShiftPivots(candle.HighPrice);
		_direction = 1;
		}
		else if (candle.LowPrice <= lowest && _direction != -1)
		{
		ShiftPivots(candle.LowPrice);
		_direction = -1;
		}

		// update trend and fibonacci levels when we have enough pivots
		if (_hl[3] != 0m)
		{
		_trendDirection = CheckTrend(_hl[0], _hl[1], _hl[2], _hl[3]);

		_fibo00 = _hl[0];
		_fibo100 = _hl[1];
		_fiboBase = Math.Abs(_fibo00 - _fibo100);

		if (_trendDirection == 1)
		{
		_fibo23 = _fibo00 - 0.236m * _fiboBase;
		_fibo38 = _fibo00 - 0.382m * _fiboBase;
		_fibo61 = _fibo00 - 0.618m * _fiboBase;
		_fibo76 = _fibo00 - 0.764m * _fiboBase;
		}
		else if (_trendDirection == -1)
		{
		_fibo23 = _fibo00 + 0.236m * _fiboBase;
		_fibo38 = _fibo00 + 0.382m * _fiboBase;
		_fibo61 = _fibo00 + 0.618m * _fiboBase;
		_fibo76 = _fibo00 + 0.764m * _fiboBase;
		}
		}

		// manage open positions
		if (Position > 0)
		{
		if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
		{
		SellMarket(Math.Abs(Position));
		_entryPrice = 0m;
		_barsSinceExit = 0;
		}
		}
		else if (Position < 0)
		{
		if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
		{
		BuyMarket(Math.Abs(Position));
		_entryPrice = 0m;
		_barsSinceExit = 0;
		}
		}
		else
		{
		if (_barsSinceExit >= CloseBarPause)
		{
		var buffer = Security.MinStep * SafetyBuffer;

		if (_trendDirection == 1 &&
		(CrossAbove(_prevClose, candle.ClosePrice, _fibo76, buffer) ||
		CrossAbove(_prevClose, candle.ClosePrice, _fibo61, buffer) ||
		CrossAbove(_prevClose, candle.ClosePrice, _fibo38, buffer) ||
		CrossAbove(_prevClose, candle.ClosePrice, _fibo23, buffer)))
		{
		BuyMarket();
		_entryPrice = candle.ClosePrice;
		_stopPrice = _entryPrice - Security.MinStep * StopLossPoints;
		_takePrice = _fibo00 + TakeProfitFactor * _fiboBase;
		}
		else if (_trendDirection == -1 &&
		(CrossBelow(_prevClose, candle.ClosePrice, _fibo76, buffer) ||
		CrossBelow(_prevClose, candle.ClosePrice, _fibo61, buffer) ||
		CrossBelow(_prevClose, candle.ClosePrice, _fibo38, buffer) ||
		CrossBelow(_prevClose, candle.ClosePrice, _fibo23, buffer)))
		{
		SellMarket();
		_entryPrice = candle.ClosePrice;
		_stopPrice = _entryPrice + Security.MinStep * StopLossPoints;
		_takePrice = _fibo00 - TakeProfitFactor * _fiboBase;
		}
		}
		}

		_prevClose = candle.ClosePrice;
		_barsSinceExit++;
	}

	private void ShiftPivots(decimal newValue)
	{
		_hl[3] = _hl[2];
		_hl[2] = _hl[1];
		_hl[1] = _hl[0];
		_hl[0] = newValue;
	}

	private int CheckTrend(decimal hl0, decimal hl1, decimal hl2, decimal hl3)
	{
	var precision = Security.MinStep * TrendPrecision;

	if ((hl2 - hl0) > precision && (hl3 - hl1) > precision)
	return -1;
	if ((hl0 - hl2) > precision && (hl1 - hl3) > precision)
	return 1;
	return 0;
	}

	private static bool CrossAbove(decimal prev, decimal current, decimal level, decimal buffer)
	{
	return current - level > buffer && level - prev > buffer;
	}

	private static bool CrossBelow(decimal prev, decimal current, decimal level, decimal buffer)
	{
	return prev - level > buffer && level - current > buffer;
	}
}
