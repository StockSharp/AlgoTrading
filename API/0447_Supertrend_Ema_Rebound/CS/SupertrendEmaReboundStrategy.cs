namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Supertrend + EMA Rebound Strategy.
/// Trades SuperTrend direction changes and EMA rebounds.
/// Buys when SuperTrend turns bullish or price rebounds from EMA in uptrend.
/// Sells when SuperTrend turns bearish or price rebounds from EMA in downtrend.
/// </summary>
public class SupertrendEmaReboundStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrFactor;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _cooldownBars;

	private SuperTrend _supertrend;
	private ExponentialMovingAverage _ema;

	private bool _prevIsUpTrend;
	private bool _prevIsUpTrendSet;
	private decimal _prevClose;
	private decimal _prevEma;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal AtrFactor
	{
		get => _atrFactor.Value;
		set => _atrFactor.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public SupertrendEmaReboundStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for Supertrend", "Supertrend");

		_atrFactor = Param(nameof(AtrFactor), 3.0m)
			.SetDisplay("ATR Factor", "ATR factor for Supertrend", "Supertrend");

		_emaLength = Param(nameof(EmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Moving Average");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_supertrend = null;
		_ema = null;
		_prevIsUpTrend = false;
		_prevIsUpTrendSet = false;
		_prevClose = 0;
		_prevEma = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_supertrend = new SuperTrend { Length = AtrPeriod, Multiplier = AtrFactor };
		_ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_supertrend, _ema, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _supertrend);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue stValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_supertrend.IsFormed || !_ema.IsFormed)
			return;

		if (stValue.IsEmpty || emaValue.IsEmpty)
			return;

		var stTyped = (SuperTrendIndicatorValue)stValue;
		var isUpTrend = stTyped.IsUpTrend;
		var emaVal = emaValue.ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevIsUpTrend = isUpTrend;
			_prevIsUpTrendSet = true;
			_prevClose = candle.ClosePrice;
			_prevEma = emaVal;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevIsUpTrend = isUpTrend;
			_prevIsUpTrendSet = true;
			_prevClose = candle.ClosePrice;
			_prevEma = emaVal;
			return;
		}

		if (!_prevIsUpTrendSet || _prevClose == 0)
		{
			_prevIsUpTrend = isUpTrend;
			_prevIsUpTrendSet = true;
			_prevClose = candle.ClosePrice;
			_prevEma = emaVal;
			return;
		}

		// SuperTrend direction change
		var trendTurnedUp = isUpTrend && !_prevIsUpTrend;
		var trendTurnedDown = !isUpTrend && _prevIsUpTrend;

		// EMA rebound: price was below EMA and now crosses above
		var emaReboundUp = isUpTrend && _prevClose < _prevEma && candle.ClosePrice > emaVal;
		// EMA rebound: price was above EMA and now crosses below
		var emaReboundDown = !isUpTrend && _prevClose > _prevEma && candle.ClosePrice < emaVal;

		// Buy: SuperTrend turns bullish or EMA rebound up in uptrend
		if ((trendTurnedUp || emaReboundUp) && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: SuperTrend turns bearish or EMA rebound down in downtrend
		else if ((trendTurnedDown || emaReboundDown) && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long on SuperTrend bearish flip
		else if (Position > 0 && trendTurnedDown)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short on SuperTrend bullish flip
		else if (Position < 0 && trendTurnedUp)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevIsUpTrend = isUpTrend;
		_prevIsUpTrendSet = true;
		_prevClose = candle.ClosePrice;
		_prevEma = emaVal;
	}
}
