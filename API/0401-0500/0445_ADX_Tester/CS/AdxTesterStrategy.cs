namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// ADX Tester Strategy.
/// Combines momentum (EMA slope) and ADX for entry signals.
/// Buys when ADX is above key level and DI+ > DI- with rising momentum.
/// Sells when ADX is above key level and DI- > DI+ with falling momentum.
/// </summary>
public class AdxTesterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<int> _adxKeyLevel;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _cooldownBars;

	private AverageDirectionalIndex _adx;
	private ExponentialMovingAverage _ema;

	private decimal _prevEma;
	private decimal _prevPrevEma;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AdxLength
	{
		get => _adxLength.Value;
		set => _adxLength.Value = value;
	}

	public int AdxKeyLevel
	{
		get => _adxKeyLevel.Value;
		set => _adxKeyLevel.Value = value;
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

	public AdxTesterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_adxLength = Param(nameof(AdxLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Length", "ADX/DI period", "ADX");

		_adxKeyLevel = Param(nameof(AdxKeyLevel), 20)
			.SetDisplay("ADX Key Level", "Minimum ADX level for trending", "ADX");

		_emaLength = Param(nameof(EmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Momentum EMA period", "Momentum");

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

		_adx = null;
		_ema = null;
		_prevEma = 0;
		_prevPrevEma = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_adx = new AverageDirectionalIndex { Length = AdxLength };
		_ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_adx, _ema, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_adx.IsFormed || !_ema.IsFormed)
			return;

		if (adxValue.IsEmpty || emaValue.IsEmpty)
			return;

		var emaVal = emaValue.ToDecimal();

		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adxVal)
		{
			_prevPrevEma = _prevEma;
			_prevEma = emaVal;
			return;
		}

		// Get DI+/DI- from the Dx (DirectionalIndex) sub-indicator
		var dxValue = adxTyped.Dx;
		decimal? diPlus = dxValue?.Plus;
		decimal? diMinus = dxValue?.Minus;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevPrevEma = _prevEma;
			_prevEma = emaVal;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevPrevEma = _prevEma;
			_prevEma = emaVal;
			return;
		}

		if (_prevEma == 0 || _prevPrevEma == 0)
		{
			_prevPrevEma = _prevEma;
			_prevEma = emaVal;
			return;
		}

		// Momentum: EMA rising or falling
		var momentumRising = emaVal > _prevEma && _prevEma > _prevPrevEma;
		var momentumFalling = emaVal < _prevEma && _prevEma < _prevPrevEma;

		// Strong trend
		var strongTrend = adxVal > AdxKeyLevel;

		// Buy: ADX above key level + momentum rising + (optionally DI+ > DI-)
		var bullish = strongTrend && momentumRising;
		if (diPlus.HasValue && diMinus.HasValue)
			bullish = bullish && diPlus.Value > diMinus.Value;

		// Sell: ADX above key level + momentum falling + (optionally DI- > DI+)
		var bearish = strongTrend && momentumFalling;
		if (diPlus.HasValue && diMinus.HasValue)
			bearish = bearish && diMinus.Value > diPlus.Value;

		if (bullish && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		else if (bearish && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: momentum turns negative
		else if (Position > 0 && emaVal < _prevEma)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: momentum turns positive
		else if (Position < 0 && emaVal > _prevEma)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevPrevEma = _prevEma;
		_prevEma = emaVal;
	}
}
