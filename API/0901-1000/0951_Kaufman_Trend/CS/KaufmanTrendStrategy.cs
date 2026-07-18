using System;
using System.Collections.Generic;

using Ecng.Common;
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
	private readonly StrategyParam<int> _maxEntries;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _filteredSrc;
	private decimal _oscillator;
	private decimal _p00 = 1m;
	private decimal _p01;
	private decimal _p10;
	private decimal _p11 = 1m;
	private decimal _oscAbsAverage;
	private int _warmupCount;
	private int _entriesExecuted;
	private int _barsSinceSignal;

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

	public int MaxEntries
	{
		get => _maxEntries.Value;
		set => _maxEntries.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public KaufmanTrendStrategy()
	{
		_trendStrengthEntry = Param(nameof(TrendStrengthEntry), 80)
			.SetDisplay("Trend Strength Entry", "Entry threshold.", "Trend");

		_trendStrengthExit = Param(nameof(TrendStrengthExit), 20)
			.SetDisplay("Trend Strength Exit", "Exit threshold.", "Trend");

		_processNoise = Param(nameof(ProcessNoise), 0.01m)
			.SetDisplay("Process Noise", "Kalman process noise.", "Kalman");

		_measurementNoise = Param(nameof(MeasurementNoise), 500m)
			.SetDisplay("Measurement Noise", "Observation noise.", "Kalman");

		_oscBufferLength = Param(nameof(OscBufferLength), 10)
			.SetDisplay("Oscillator Buffer", "Bars for normalization.", "Trend");

		_maxEntries = Param(nameof(MaxEntries), 45)
			.SetDisplay("Max Entries", "Maximum entries per run.", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 300)
			.SetDisplay("Cooldown Bars", "Minimum bars between entries.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles.", "General");
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
		_filteredSrc = 0m;
		_oscillator = 0m;
		_p00 = 1m;
		_p01 = 0m;
		_p10 = 0m;
		_p11 = 1m;
		_oscAbsAverage = 0m;
		_warmupCount = 0;
		_entriesExecuted = 0;
		_barsSinceSignal = 0;
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
		_oscAbsAverage = 0m;
		_warmupCount = 0;
		_entriesExecuted = 0;
		_barsSinceSignal = CooldownBars;

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

		_barsSinceSignal++;
		UpdateKalman(candle.ClosePrice);

		var absOsc = Math.Abs(_oscillator);
		if (_warmupCount == 0)
		{
			_oscAbsAverage = absOsc;
		}
		else
		{
			var alpha = 2m / (OscBufferLength + 1m);
			_oscAbsAverage += (absOsc - _oscAbsAverage) * alpha;
		}

		_warmupCount++;

		var trendStrength = _oscAbsAverage > 0m ? _oscillator / _oscAbsAverage * 100m : 0m;

		if (_warmupCount < OscBufferLength)
			return;

		var priceAboveMa = candle.ClosePrice > _filteredSrc;
		var priceBelowMa = candle.ClosePrice < _filteredSrc;

		var trendStrongLong = trendStrength >= TrendStrengthEntry;
		var trendStrongShort = trendStrength <= -TrendStrengthEntry;
		var trendWeakLong = trendStrength < TrendStrengthExit;
		var trendWeakShort = trendStrength > -TrendStrengthExit;

		// Exit logic
		if (Position > 0 && trendWeakLong)
		{
			SellMarket(Math.Abs(Position));
			_barsSinceSignal = 0;
		}
		else if (Position < 0 && trendWeakShort)
		{
			BuyMarket(Math.Abs(Position));
			_barsSinceSignal = 0;
		}

		// Entry logic
		if (Position == 0 && _entriesExecuted < MaxEntries && _barsSinceSignal >= CooldownBars)
		{
			if (trendStrongLong && priceAboveMa)
			{
				BuyMarket();
				_entriesExecuted++;
				_barsSinceSignal = 0;
			}
			else if (trendStrongShort && priceBelowMa)
			{
				SellMarket();
				_entriesExecuted++;
				_barsSinceSignal = 0;
			}
		}
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
