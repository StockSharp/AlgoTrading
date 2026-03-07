namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Candle 245 Breakout Strategy.
/// Captures reference candle high/low, then trades breakout
/// in the next N bars. Uses EMA as trend filter.
/// </summary>
public class Candle245BreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _refPeriod;
	private readonly StrategyParam<int> _lookForwardBars;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _ema;

	private decimal _refHigh;
	private decimal _refLow;
	private int _barsLeft;
	private int _barCount;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RefPeriod
	{
		get => _refPeriod.Value;
		set => _refPeriod.Value = value;
	}

	public int LookForwardBars
	{
		get => _lookForwardBars.Value;
		set => _lookForwardBars.Value = value;
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

	public Candle245BreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_refPeriod = Param(nameof(RefPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Ref Period", "Every N bars capture reference candle", "Trading");

		_lookForwardBars = Param(nameof(LookForwardBars), 3)
			.SetGreaterThanZero()
			.SetDisplay("Look Forward Bars", "Bars to watch for breakout", "Trading");

		_emaLength = Param(nameof(EmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period for trend filter", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ema = null;
		_refHigh = 0;
		_refLow = 0;
		_barsLeft = 0;
		_barCount = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_barCount++;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			if (_barsLeft > 0)
				_barsLeft--;
			return;
		}

		// Every RefPeriod bars, capture reference candle
		if (_barCount % RefPeriod == 0)
		{
			_refHigh = candle.HighPrice;
			_refLow = candle.LowPrice;
			_barsLeft = LookForwardBars;
			return;
		}

		if (_barsLeft <= 0)
			return;

		_barsLeft--;

		var price = candle.ClosePrice;

		// Breakout above reference high + EMA bullish
		if (price > _refHigh && price > emaVal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Breakout below reference low + EMA bearish
		else if (price < _refLow && price < emaVal && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}

		// Close position at end of breakout window
		if (_barsLeft == 0 && Position != 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			else
				BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
	}
}
