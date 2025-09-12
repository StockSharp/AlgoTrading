using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-Leg Doji breakout strategy.
/// Detects long-legged doji candles and trades breakouts above or below the pattern.
/// </summary>
public class LongLegDojiBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _dojiBodyThreshold;
	private readonly StrategyParam<decimal> _minWickRatio;
	private readonly StrategyParam<bool> _useAtrFilter;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;
	private SimpleMovingAverage _sma;

	private decimal _dojiHigh;
	private decimal _dojiLow;
	private bool _waitingForBreakout;
	private decimal _prevClose;
	private decimal _prevSma;

	/// <summary>
	/// Maximum body size as percentage of total range.
	/// </summary>
	public decimal DojiBodyThreshold
	{
		get => _dojiBodyThreshold.Value;
		set => _dojiBodyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum wick to body ratio.
	/// </summary>
	public decimal MinWickRatio
	{
		get => _minWickRatio.Value;
		set => _minWickRatio.Value = value;
	}

	/// <summary>
	/// Use ATR filter for wick length.
	/// </summary>
	public bool UseAtrFilter
	{
		get => _useAtrFilter.Value;
		set => _useAtrFilter.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for minimum wick length.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public LongLegDojiBreakoutStrategy()
	{
		_dojiBodyThreshold = Param(nameof(DojiBodyThreshold), 0.1m)
			.SetRange(0.01m, 1m)
			.SetDisplay("Doji Body Threshold %", "Body size as % of range", "Pattern");

		_minWickRatio = Param(nameof(MinWickRatio), 2m)
			.SetRange(1m, 10m)
			.SetDisplay("Minimum Wick Ratio", "Minimum wick to body ratio", "Pattern");

		_useAtrFilter = Param(nameof(UseAtrFilter), true)
			.SetDisplay("Use ATR Filter", "Require ATR-based wick size", "Filter");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR calculation period", "Filter");

		_atrMultiplier = Param(nameof(AtrMultiplier), 0.5m)
			.SetRange(0.1m, 2m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for wick length", "Filter");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_dojiHigh = default;
		_dojiLow = default;
		_waitingForBreakout = false;
		_prevClose = default;
		_prevSma = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_sma = new SimpleMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var candleRange = candle.HighPrice - candle.LowPrice;
		if (candleRange == 0)
		{
			_prevClose = candle.ClosePrice;
			_prevSma = smaValue;
			return;
		}

		var upperWick = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
		var lowerWick = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;

		var threshold = DojiBodyThreshold / 100m;
		var isSmallBody = bodySize <= candleRange * threshold;
		var hasLongWicks = upperWick >= bodySize * MinWickRatio && lowerWick >= bodySize * MinWickRatio;
		var atrCondition = !UseAtrFilter || (upperWick >= atrValue * AtrMultiplier && lowerWick >= atrValue * AtrMultiplier);
		var isDoji = isSmallBody && hasLongWicks && atrCondition;

		if (isDoji && !_waitingForBreakout)
		{
			_dojiHigh = candle.HighPrice;
			_dojiLow = candle.LowPrice;
			_waitingForBreakout = true;
		}

		var longSignal = _waitingForBreakout && candle.ClosePrice > _dojiHigh && _prevClose <= _dojiHigh;
		var shortSignal = _waitingForBreakout && candle.ClosePrice < _dojiLow && _prevClose >= _dojiLow;

		if (longSignal)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_waitingForBreakout = false;
		}
		else if (shortSignal)
		{
			SellMarket(Volume + Math.Abs(Position));
			_waitingForBreakout = false;
		}

		var crossedDown = Position > 0 && _prevClose >= _prevSma && candle.ClosePrice < smaValue;
		var crossedUp = Position < 0 && _prevClose <= _prevSma && candle.ClosePrice > smaValue;

		if (crossedDown)
			SellMarket(Math.Abs(Position));
		else if (crossedUp)
			BuyMarket(Math.Abs(Position));

		_prevClose = candle.ClosePrice;
		_prevSma = smaValue;
	}
}
