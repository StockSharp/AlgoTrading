using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fractal ZigZag: Confirms Bill Williams fractals then trades
/// in the direction of the last confirmed extremum.
/// Bullish after low fractal, bearish after high fractal.
/// </summary>
public class FractalZigZagStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _level;
	private readonly StrategyParam<int> _atrLength;

	private readonly List<(decimal high, decimal low, DateTimeOffset time)> _window = new();
	private int _trend; // 1=bearish (last was high), 2=bullish (last was low)
	private int _prevTrend;
	private decimal _entryPrice;

	public FractalZigZagStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_level = Param(nameof(Level), 2)
			.SetDisplay("Fractal Depth", "Candles on each side to confirm fractal.", "Signals");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period for stops.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Level
	{
		get => _level.Value;
		set => _level.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_window.Clear();
		_trend = 0;
		_prevTrend = 0;
		_entryPrice = 0;

		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update fractal window
		var depth = Math.Max(1, Level);
		var windowSize = depth * 2 + 1;

		_window.Add((candle.HighPrice, candle.LowPrice, candle.OpenTime));
		while (_window.Count > windowSize)
			_window.RemoveAt(0);

		// Evaluate fractals
		if (_window.Count >= windowSize)
		{
			var centerIndex = _window.Count - 1 - depth;
			var center = _window[centerIndex];
			var isHigh = true;
			var isLow = true;

			for (var i = 0; i < _window.Count; i++)
			{
				if (i == centerIndex)
					continue;

				if (_window[i].high >= center.high)
					isHigh = false;
				if (_window[i].low <= center.low)
					isLow = false;

				if (!isHigh && !isLow)
					break;
			}

			if (isHigh)
				_trend = 1; // bearish: last fractal was a high
			if (isLow)
				_trend = 2; // bullish: last fractal was a low
		}

		if (atrVal <= 0 || _trend == 0)
		{
			_prevTrend = _trend;
			return;
		}

		var close = candle.ClosePrice;

		// Exit management
		if (Position > 0)
		{
			if (close <= _entryPrice - atrVal * 2m || close >= _entryPrice + atrVal * 3m || _trend == 1)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (close >= _entryPrice + atrVal * 2m || close <= _entryPrice - atrVal * 3m || _trend == 2)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Entry on trend change
		if (Position == 0 && _prevTrend != 0 && _trend != _prevTrend)
		{
			if (_trend == 2)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (_trend == 1)
			{
				_entryPrice = close;
				SellMarket();
			}
		}

		_prevTrend = _trend;
	}
}
