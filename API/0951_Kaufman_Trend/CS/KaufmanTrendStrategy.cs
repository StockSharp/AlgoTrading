using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Kaufman Trend strategy using Kalman filter for trend detection.
/// </summary>
public class KaufmanTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _trendStrengthEntry;
	private readonly StrategyParam<int> _trendStrengthExit;
	private readonly StrategyParam<decimal> _processNoise;
	private readonly StrategyParam<decimal> _measurementNoise;
	private readonly StrategyParam<int> _oscBufferLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _filteredSrc;
	private decimal _oscillator;
	private decimal _p00 = 1m;
	private decimal _p01;
	private decimal _p10;
	private decimal _p11 = 1m;
	private readonly Queue<decimal> _oscBuffer = new();
	private decimal _prevTrendStrength;
	private decimal _entryPrice;

	public int TrendStrengthEntry
	{
		get => _trendStrengthEntry.Value;
		set => _trendStrengthEntry.Value = value;
	}

	public int TrendStrengthExit
	{
		get => _trendStrengthExit.Value;
		set => _trendStrengthExit.Value = value;
	}

	public decimal ProcessNoise
	{
		get => _processNoise.Value;
		set => _processNoise.Value = value;
	}

	public decimal MeasurementNoise
	{
		get => _measurementNoise.Value;
		set => _measurementNoise.Value = value;
	}

	public int OscBufferLength
	{
		get => _oscBufferLength.Value;
		set => _oscBufferLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public KaufmanTrendStrategy()
	{
		_trendStrengthEntry = Param(nameof(TrendStrengthEntry), 60)
			.SetDisplay("Trend Strength Entry", "Entry threshold.", "Trend");

		_trendStrengthExit = Param(nameof(TrendStrengthExit), 40)
			.SetDisplay("Trend Strength Exit", "Exit threshold.", "Trend");

		_processNoise = Param(nameof(ProcessNoise), 0.01m)
			.SetDisplay("Process Noise", "Kalman process noise.", "Kalman");

		_measurementNoise = Param(nameof(MeasurementNoise), 500m)
			.SetDisplay("Measurement Noise", "Observation noise.", "Kalman");

		_oscBufferLength = Param(nameof(OscBufferLength), 10)
			.SetDisplay("Oscillator Buffer", "Bars for normalization.", "Trend");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles.", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_filteredSrc = 0m;
		_oscillator = 0m;
		_p00 = 1m;
		_p01 = 0m;
		_p10 = 0m;
		_p11 = 1m;
		_oscBuffer.Clear();
		_prevTrendStrength = 0m;
		_entryPrice = 0m;

		var atr = new AverageTrueRange { Length = 14 };

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

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateKalman(candle.ClosePrice);

		_oscBuffer.Enqueue(_oscillator);
		if (_oscBuffer.Count > OscBufferLength)
			_oscBuffer.Dequeue();

		decimal maxAbs = 0m;
		foreach (var v in _oscBuffer)
		{
			var abs = Math.Abs(v);
			if (abs > maxAbs)
				maxAbs = abs;
		}

		var trendStrength = maxAbs > 0m ? _oscillator / maxAbs * 100m : 0m;

		if (_oscBuffer.Count < OscBufferLength)
		{
			_prevTrendStrength = trendStrength;
			return;
		}

		var priceAboveMa = candle.ClosePrice > _filteredSrc;
		var priceBelowMa = candle.ClosePrice < _filteredSrc;

		var trendStrongLong = trendStrength >= TrendStrengthEntry;
		var trendStrongShort = trendStrength <= -TrendStrengthEntry;
		var trendWeakLong = trendStrength < TrendStrengthExit;
		var trendWeakShort = trendStrength > -TrendStrengthExit;

		// Exit logic
		if (Position > 0 && trendWeakLong)
		{
			SellMarket();
		}
		else if (Position < 0 && trendWeakShort)
		{
			BuyMarket();
		}

		// Entry logic
		if (Position == 0)
		{
			if (trendStrongLong && priceAboveMa)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (trendStrongShort && priceBelowMa)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}

		_prevTrendStrength = trendStrength;
	}

	private void UpdateKalman(decimal price)
	{
		if (_filteredSrc == 0m)
		{
			_filteredSrc = price;
			return;
		}

		_filteredSrc += _oscillator;

		var p00p = _p00 + _p01 + _p10 + _p11 + ProcessNoise;
		var p01p = _p01 + _p11;
		var p10p = _p10 + _p11;
		var p11p = _p11 + ProcessNoise;

		var s = p00p + MeasurementNoise;
		if (s == 0m) return;

		var k0 = p00p / s;
		var k1 = p10p / s;
		var innovation = price - _filteredSrc;

		_filteredSrc += k0 * innovation;
		_oscillator += k1 * innovation;

		_p00 = (1 - k0) * p00p;
		_p01 = (1 - k0) * p01p;
		_p10 = p10p - k1 * p00p;
		_p11 = p11p - k1 * p01p;
	}
}
