using System;
using System.Collections.Generic;

using Ecng.Common;

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
	private const int BufferSize = 256;

	private readonly StrategyParam<int> _zigzagDepth;
	private readonly StrategyParam<int> _safetyBuffer;
	private readonly StrategyParam<int> _trendPrecision;
	private readonly StrategyParam<int> _closeBarPause;
	private readonly StrategyParam<decimal> _takeProfitFactor;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _highBuffer = new decimal[BufferSize];
	private readonly decimal[] _lowBuffer = new decimal[BufferSize];

	private bool _longSetupArmed;
	private bool _shortSetupArmed;
	private decimal _prevClose;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private int _barsSinceExit;
	private int _bufferIndex;
	private int _bufferCount;

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

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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

		Array.Clear(_highBuffer);
		Array.Clear(_lowBuffer);
		_longSetupArmed = false;
		_shortSetupArmed = false;
		_prevClose = 0m;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_barsSinceExit = CloseBarPause;
		_bufferIndex = 0;
		_bufferCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		Array.Clear(_highBuffer);
		Array.Clear(_lowBuffer);
		_longSetupArmed = false;
		_shortSetupArmed = false;
		_prevClose = 0m;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_barsSinceExit = CloseBarPause;
		_bufferIndex = 0;
		_bufferCount = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		PushBar(candle.HighPrice, candle.LowPrice);
		_barsSinceExit++;

		if (_bufferCount < Math.Min(ZigzagDepth, BufferSize))
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var highest = GetHighest(Math.Min(ZigzagDepth, BufferSize));
		var lowest = GetLowest(Math.Min(ZigzagDepth, BufferSize));
		var range = highest - lowest;
		var precision = 0.01m * TrendPrecision;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket();
				_entryPrice = 0m;
				_longSetupArmed = false;
				_barsSinceExit = 0;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket();
				_entryPrice = 0m;
				_shortSetupArmed = false;
				_barsSinceExit = 0;
			}
		}

		if (range <= precision || !IsFormedAndOnlineAndAllowTrading())
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var midpoint = lowest + (range / 2m);
		var longTrigger = midpoint;
		var longRetracement = highest - 0.618m * range;
		var shortTrigger = midpoint;
		var shortRetracement = lowest + 0.618m * range;
		var buffer = 0.01m * SafetyBuffer;

		if (Position == 0 && _barsSinceExit >= CloseBarPause)
		{
			if (candle.ClosePrice > midpoint)
			{
				_shortSetupArmed = false;

				if (candle.LowPrice <= longRetracement + buffer)
					_longSetupArmed = true;

				if (_longSetupArmed && CrossAbove(_prevClose, candle.ClosePrice, longTrigger, buffer))
				{
					BuyMarket();
					_entryPrice = candle.ClosePrice;
					_stopPrice = _entryPrice - 0.01m * StopLossPoints;
					_takePrice = highest + TakeProfitFactor * range;
					_longSetupArmed = false;
					_barsSinceExit = 0;
				}
			}
			else if (candle.ClosePrice < midpoint)
			{
				_longSetupArmed = false;

				if (candle.HighPrice >= shortRetracement - buffer)
					_shortSetupArmed = true;

				if (_shortSetupArmed && CrossBelow(_prevClose, candle.ClosePrice, shortTrigger, buffer))
				{
					SellMarket();
					_entryPrice = candle.ClosePrice;
					_stopPrice = _entryPrice + 0.01m * StopLossPoints;
					_takePrice = lowest - TakeProfitFactor * range;
					_shortSetupArmed = false;
					_barsSinceExit = 0;
				}
			}
		}

		_prevClose = candle.ClosePrice;
	}

	private void PushBar(decimal high, decimal low)
	{
		_highBuffer[_bufferIndex] = high;
		_lowBuffer[_bufferIndex] = low;
		_bufferIndex = (_bufferIndex + 1) % BufferSize;

		if (_bufferCount < BufferSize)
			_bufferCount++;
	}

	private decimal GetHighest(int depth)
	{
		var highest = decimal.MinValue;
		var count = Math.Min(depth, _bufferCount);

		for (var i = 0; i < count; i++)
		{
			var idx = (_bufferIndex - 1 - i + BufferSize) % BufferSize;
			if (_highBuffer[idx] > highest)
				highest = _highBuffer[idx];
		}

		return highest;
	}

	private decimal GetLowest(int depth)
	{
		var lowest = decimal.MaxValue;
		var count = Math.Min(depth, _bufferCount);

		for (var i = 0; i < count; i++)
		{
			var idx = (_bufferIndex - 1 - i + BufferSize) % BufferSize;
			if (_lowBuffer[idx] < lowest)
				lowest = _lowBuffer[idx];
		}

		return lowest;
	}

	private static bool CrossBelow(decimal prev, decimal current, decimal level, decimal buffer)
	{
		return prev - level > buffer && level - current > buffer;
	}

	private static bool CrossAbove(decimal prev, decimal current, decimal level, decimal buffer)
	{
		return current - level > buffer && level - prev > buffer;
	}
}
