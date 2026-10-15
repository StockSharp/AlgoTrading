using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Heiken Ashi Supertrend ADX strategy.
/// A bullish Heiken Ashi candle without a lower wick buys and a bearish one without an upper wick sells. With UseSupertrend the
/// Supertrend (AtrPeriod, SupertrendMultiplier) must point the same way, and with UseAdxFilter the ADX must be above AdxThreshold.
/// An opposite Heiken Ashi candle closes the position, reversing it when the entry filters agree. Positions also exit on an ATR
/// trailing stop TrailAtrMultiplier ATRs behind the close.
/// </summary>
public class HeikenAshiSupertrendAdxStrategy : Strategy
{
	private readonly StrategyParam<bool> _useSupertrend;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<bool> _useAdxFilter;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _trailAtrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _haOpen;
	private decimal? _haClose;
	private decimal? _upperBand;
	private decimal? _lowerBand;
	private decimal? _prevClose;
	private int _trend;
	private decimal _trailStop;

	/// <summary>
	/// Require the Supertrend direction for entries.
	/// </summary>
	public bool UseSupertrend
	{
		get => _useSupertrend.Value;
		set => _useSupertrend.Value = value;
	}

	/// <summary>
	/// ATR period for Supertrend and the trailing stop.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Supertrend ATR multiplier.
	/// </summary>
	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
	}

	/// <summary>
	/// Require ADX above the threshold for entries.
	/// </summary>
	public bool UseAdxFilter
	{
		get => _useAdxFilter.Value;
		set => _useAdxFilter.Value = value;
	}

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Minimum ADX value for entries.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in ATRs.
	/// </summary>
	public decimal TrailAtrMultiplier
	{
		get => _trailAtrMultiplier.Value;
		set => _trailAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public HeikenAshiSupertrendAdxStrategy()
	{
		_useSupertrend = Param(nameof(UseSupertrend), true)
			.SetDisplay("Use Supertrend", "Require the Supertrend direction for entries", "Filters");

		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for Supertrend and the trailing stop", "Indicators");

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend Multiplier", "Supertrend ATR multiplier", "Indicators");

		_useAdxFilter = Param(nameof(UseAdxFilter), false)
			.SetDisplay("Use ADX Filter", "Require ADX above the threshold for entries", "Filters");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "ADX period", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetDisplay("ADX Threshold", "Minimum ADX value for entries", "Filters");

		_trailAtrMultiplier = Param(nameof(TrailAtrMultiplier), 2m)
			.SetNotNegative()
			.SetDisplay("Trail ATR Multiplier", "Trailing stop distance in ATRs", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_haOpen = null;
		_haClose = null;
		_upperBand = null;
		_lowerBand = null;
		_prevClose = null;
		_trend = 1;
		_trailStop = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(atr, adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, adx);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Heiken Ashi candle.
		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var haOpen = _haOpen is decimal prevHaOpen && _haClose is decimal prevHaClose
			? (prevHaOpen + prevHaClose) / 2m
			: (candle.OpenPrice + candle.ClosePrice) / 2m;
		var haHigh = Math.Max(candle.HighPrice, Math.Max(haOpen, haClose));
		var haLow = Math.Min(candle.LowPrice, Math.Min(haOpen, haClose));

		_haOpen = haOpen;
		_haClose = haClose;

		if (!atrValue.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		// Supertrend on the regular candles.
		var hl2 = (candle.HighPrice + candle.LowPrice) / 2m;
		var lower = hl2 - SupertrendMultiplier * atr;
		var upper = hl2 + SupertrendMultiplier * atr;

		if (_lowerBand is decimal prevLower && _prevClose is decimal pc1 && pc1 > prevLower)
			lower = Math.Max(lower, prevLower);

		if (_upperBand is decimal prevUpper && _prevClose is decimal pc2 && pc2 < prevUpper)
			upper = Math.Min(upper, prevUpper);

		if (_trend < 0 && _upperBand is decimal lastUpper && close > lastUpper)
			_trend = 1;
		else if (_trend > 0 && _lowerBand is decimal lastLower && close < lastLower)
			_trend = -1;

		_lowerBand = lower;
		_upperBand = upper;
		_prevClose = close;

		if (!adxValue.IsFormed || adxValue is not IAverageDirectionalIndexValue { MovingAverage: decimal adx })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bullishCandle = haClose > haOpen && haLow >= haOpen;
		var bearishCandle = haClose < haOpen && haHigh <= haOpen;
		var adxOk = !UseAdxFilter || adx > AdxThreshold;

		var longSignal = bullishCandle && (!UseSupertrend || _trend > 0) && adxOk;
		var shortSignal = bearishCandle && (!UseSupertrend || _trend < 0) && adxOk;
		var trailDistance = TrailAtrMultiplier * atr;

		if (Position > 0)
		{
			if (TrailAtrMultiplier > 0 && candle.LowPrice <= _trailStop)
			{
				SellMarket(Position);
				return;
			}

			if (bearishCandle)
			{
				if (shortSignal)
				{
					SellMarket(Volume + Math.Abs(Position));
					_trailStop = close + trailDistance;
				}
				else
				{
					SellMarket(Position);
				}

				return;
			}

			_trailStop = Math.Max(_trailStop, close - trailDistance);
		}
		else if (Position < 0)
		{
			if (TrailAtrMultiplier > 0 && candle.HighPrice >= _trailStop)
			{
				BuyMarket(-Position);
				return;
			}

			if (bullishCandle)
			{
				if (longSignal)
				{
					BuyMarket(Volume + Math.Abs(Position));
					_trailStop = close - trailDistance;
				}
				else
				{
					BuyMarket(-Position);
				}

				return;
			}

			_trailStop = Math.Min(_trailStop, close + trailDistance);
		}
		else if (longSignal)
		{
			BuyMarket(Volume);
			_trailStop = close - trailDistance;
		}
		else if (shortSignal)
		{
			SellMarket(Volume);
			_trailStop = close + trailDistance;
		}
	}
}
