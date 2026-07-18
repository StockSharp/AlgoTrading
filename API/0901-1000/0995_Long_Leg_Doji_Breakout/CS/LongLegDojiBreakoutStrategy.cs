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
/// Long-Leg Doji breakout strategy.
/// Detects long-legged doji candles and trades breakouts above or below the pattern.
/// Uses SMA trend filter for entries and exits.
/// </summary>
public class LongLegDojiBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _dojiBodyThreshold;
	private readonly StrategyParam<decimal> _minWickRatio;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;
	private SimpleMovingAverage _sma;

	private decimal _dojiHigh;
	private decimal _dojiLow;
	private bool _waitingForBreakout;
	private decimal _prevClose;
	private decimal _prevSma;
	private int _cooldown;
	private decimal _entryPrice;

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
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
		_dojiBodyThreshold = Param(nameof(DojiBodyThreshold), 15m)
			.SetRange(0.01m, 100m)
			.SetDisplay("Doji Body Threshold %", "Body size as % of range", "Pattern");

		_minWickRatio = Param(nameof(MinWickRatio), 1.2m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Minimum Wick Ratio", "Minimum wick to body ratio", "Pattern");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR calculation period", "Filter");

		_cooldownBars = Param(nameof(CooldownBars), 70)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_cooldown = default;
		_entryPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_sma = new SMA { Length = 10 };

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

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevClose = candle.ClosePrice;
			_prevSma = smaValue;
			return;
		}

		if (atrValue <= 0 || _prevSma == 0)
		{
			_prevClose = candle.ClosePrice;
			_prevSma = smaValue;
			return;
		}

		var bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var candleRange = candle.HighPrice - candle.LowPrice;

		// Detect doji
		if (candleRange > 0)
		{
			var upperWick = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
			var lowerWick = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;

			var threshold = DojiBodyThreshold / 100m;
			var isSmallBody = bodySize <= candleRange * threshold;
			var hasLongWicks = upperWick >= bodySize * MinWickRatio && lowerWick >= bodySize * MinWickRatio;

			if (isSmallBody && hasLongWicks)
			{
				_dojiHigh = candle.HighPrice;
				_dojiLow = candle.LowPrice;
				_waitingForBreakout = true;
			}
		}

		// Entry: doji breakout with SMA confirmation, or SMA cross fallback
		if (Position == 0 && _prevClose > 0)
		{
			var dojiLong = _waitingForBreakout && candle.ClosePrice > _dojiHigh && candle.ClosePrice > smaValue;
			var dojiShort = _waitingForBreakout && candle.ClosePrice < _dojiLow && candle.ClosePrice < smaValue;
			var smaCrossUp = _prevClose < _prevSma && candle.ClosePrice > smaValue;
			var smaCrossDown = _prevClose > _prevSma && candle.ClosePrice < smaValue;

			if (dojiLong || smaCrossUp)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_waitingForBreakout = false;
				_cooldown = CooldownBars;
			}
			else if (dojiShort || smaCrossDown)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_waitingForBreakout = false;
				_cooldown = CooldownBars;
			}
		}

		// Exit: SMA cross or ATR-based stop
		if (Position > 0)
		{
			var smaCross = _prevClose >= _prevSma && candle.ClosePrice < smaValue;
			var atrStop = _entryPrice > 0 && candle.ClosePrice < _entryPrice - atrValue * 2;

			if (smaCross || atrStop)
			{
				SellMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			var smaCross = _prevClose <= _prevSma && candle.ClosePrice > smaValue;
			var atrStop = _entryPrice > 0 && candle.ClosePrice > _entryPrice + atrValue * 2;

			if (smaCross || atrStop)
			{
				BuyMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}

		_prevClose = candle.ClosePrice;
		_prevSma = smaValue;
	}
}
