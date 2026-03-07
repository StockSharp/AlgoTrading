namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// TEMA crossover with manual Supertrend filter strategy.
/// Calculates 2 TEMA lines (fast/slow) from close price.
/// Enters long on fast TEMA crossing above slow TEMA with Supertrend uptrend.
/// Enters short on fast TEMA crossing below slow TEMA with Supertrend downtrend.
/// </summary>
public class ThreeKilosBtc15mStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _ema1Short;
	private ExponentialMovingAverage _ema2Short;
	private ExponentialMovingAverage _ema3Short;

	private ExponentialMovingAverage _ema1Long;
	private ExponentialMovingAverage _ema2Long;
	private ExponentialMovingAverage _ema3Long;

	private AverageTrueRange _atr;

	private bool _isSupertrendInit;
	private decimal _up;
	private decimal _dn;
	private bool _uptrend;
	private decimal _prevClose;

	private decimal? _prevTemaFast;
	private decimal? _prevTemaSlow;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	public int ShortPeriod
	{
		get => _shortPeriod.Value;
		set => _shortPeriod.Value = value;
	}
	public int LongPeriod
	{
		get => _longPeriod.Value;
		set => _longPeriod.Value = value;
	}
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public ThreeKilosBtc15mStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy calculation", "General");

		_shortPeriod = Param(nameof(ShortPeriod), 8)
			.SetDisplay("Short TEMA Period", "Period for short TEMA", "Indicators");

		_longPeriod = Param(nameof(LongPeriod), 20)
			.SetDisplay("Long TEMA Period", "Period for long TEMA", "Indicators");

		_atrLength = Param(nameof(AtrLength), 10)
			.SetDisplay("ATR Length", "ATR length for Supertrend", "Supertrend");

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetDisplay("Multiplier", "ATR multiplier for Supertrend", "Supertrend");

		_cooldownBars = Param(nameof(CooldownBars), 12)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ema1Short = null;
		_ema2Short = null;
		_ema3Short = null;

		_ema1Long = null;
		_ema2Long = null;
		_ema3Long = null;

		_atr = null;

		_isSupertrendInit = false;
		_up = _dn = 0m;
		_uptrend = true;
		_prevClose = 0m;

		_prevTemaFast = null;
		_prevTemaSlow = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema1Short = new() { Length = ShortPeriod };
		_ema2Short = new() { Length = ShortPeriod };
		_ema3Short = new() { Length = ShortPeriod };

		_ema1Long = new() { Length = LongPeriod };
		_ema2Long = new() { Length = LongPeriod };
		_ema3Long = new() { Length = LongPeriod };

		_atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var time = candle.ServerTime;

		// Calculate TEMA fast and slow from close price
		var temaFast = CalcTema(_ema1Short, _ema2Short, _ema3Short, close, time);
		var temaSlow = CalcTema(_ema1Long, _ema2Long, _ema3Long, close, time);

		if (!_ema3Short.IsFormed || !_ema3Long.IsFormed)
		{
			_prevTemaFast = temaFast;
			_prevTemaSlow = temaSlow;
			_prevClose = close;
			return;
		}

		// Manual Supertrend
		var hl2 = (candle.HighPrice + candle.LowPrice) / 2m;

		if (!_isSupertrendInit)
		{
			_up = hl2 - Multiplier * atr;
			_dn = hl2 + Multiplier * atr;
			_uptrend = true;
			_isSupertrendInit = true;
		}
		else
		{
			var prevUp = _up;
			var prevDn = _dn;
			var prevTrend = _uptrend;

			_up = prevTrend ? Math.Max(hl2 - Multiplier * atr, prevUp)
				: hl2 - Multiplier * atr;
			_dn = prevTrend ? hl2 + Multiplier * atr
				: Math.Min(hl2 + Multiplier * atr, prevDn);
			_uptrend = _prevClose > prevDn ? true
				: _prevClose < prevUp ? false
				: prevTrend;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevTemaFast = temaFast;
			_prevTemaSlow = temaSlow;
			_prevClose = close;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevTemaFast = temaFast;
			_prevTemaSlow = temaSlow;
			_prevClose = close;
			return;
		}

		var bullCross = _prevTemaFast.HasValue && _prevTemaSlow.HasValue &&
			_prevTemaFast <= _prevTemaSlow && temaFast > temaSlow;
		var bearCross = _prevTemaFast.HasValue && _prevTemaSlow.HasValue &&
			_prevTemaFast >= _prevTemaSlow && temaFast < temaSlow;

		// Buy: TEMA bullish cross + Supertrend uptrend
		if (bullCross && _uptrend && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: TEMA bearish cross + Supertrend downtrend
		else if (bearCross && !_uptrend && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: Supertrend flips to downtrend
		else if (Position > 0 && !_uptrend)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: Supertrend flips to uptrend
		else if (Position < 0 && _uptrend)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevTemaFast = temaFast;
		_prevTemaSlow = temaSlow;
		_prevClose = close;
	}

	private static decimal CalcTema(ExponentialMovingAverage ema1,
		ExponentialMovingAverage ema2,
		ExponentialMovingAverage ema3,
		decimal price, DateTimeOffset time)
	{
		var e1 = ema1.Process(new DecimalIndicatorValue(ema1, price, time.UtcDateTime)).ToDecimal();
		var e2 = ema2.Process(new DecimalIndicatorValue(ema2, e1, time.UtcDateTime)).ToDecimal();
		var e3 = ema3.Process(new DecimalIndicatorValue(ema3, e2, time.UtcDateTime)).ToDecimal();
		return 3m * (e1 - e2) + e3;
	}
}
