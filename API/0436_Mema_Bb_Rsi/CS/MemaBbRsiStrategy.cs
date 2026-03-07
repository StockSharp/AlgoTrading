namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Multi EMA + Bollinger Bands + RSI Strategy.
/// Buys when price is above fast EMA and touches lower BB.
/// Sells when RSI becomes overbought or price touches upper BB.
/// </summary>
public class MemaBbRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _ma1Period;
	private readonly StrategyParam<int> _ma2Period;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _ma1;
	private ExponentialMovingAverage _ma2;
	private BollingerBands _bollinger;
	private RelativeStrengthIndex _rsi;

	private int _cooldownRemaining;

	public MemaBbRsiStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_ma1Period = Param(nameof(Ma1Period), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Period", "Fast EMA period", "Moving Average");

		_ma2Period = Param(nameof(Ma2Period), 55)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Period", "Slow EMA period", "Moving Average");

		_bbLength = Param(nameof(BBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Bollinger Bands");

		_bbMultiplier = Param(nameof(BBMultiplier), 2.0m)
			.SetDisplay("BB StdDev", "Standard deviation multiplier", "Bollinger Bands");

		_rsiLength = Param(nameof(RSILength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_rsiOverbought = Param(nameof(RsiOverbought), 70)
			.SetDisplay("RSI Overbought", "RSI overbought level", "RSI");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int Ma1Period
	{
		get => _ma1Period.Value;
		set => _ma1Period.Value = value;
	}

	public int Ma2Period
	{
		get => _ma2Period.Value;
		set => _ma2Period.Value = value;
	}

	public int BBLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	public decimal BBMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	public int RSILength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public int RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ma1 = null;
		_ma2 = null;
		_bollinger = null;
		_rsi = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ma1 = new ExponentialMovingAverage { Length = Ma1Period };
		_ma2 = new ExponentialMovingAverage { Length = Ma2Period };
		_bollinger = new BollingerBands
		{
			Length = BBLength,
			Width = BBMultiplier
		};
		_rsi = new RelativeStrengthIndex { Length = RSILength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_ma1, _ma2, _bollinger, _rsi, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma1);
			DrawIndicator(area, _ma2);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue ma1Value, IIndicatorValue ma2Value, IIndicatorValue bbValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ma1.IsFormed || !_ma2.IsFormed || !_bollinger.IsFormed || !_rsi.IsFormed)
			return;

		if (ma1Value.IsEmpty || ma2Value.IsEmpty || bbValue.IsEmpty || rsiValue.IsEmpty)
			return;

		var ma1Price = ma1Value.ToDecimal();
		var rsiVal = rsiValue.ToDecimal();

		var bb = (BollingerBandsValue)bbValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		// Buy: price above fast EMA and low touches lower BB (mean reversion from below)
		var entryLong = candle.ClosePrice > ma1Price && candle.LowPrice <= lower;
		// Sell: price below fast EMA and high touches upper BB
		var entryShort = candle.ClosePrice < ma1Price && candle.HighPrice >= upper;

		// Exit long: RSI overbought
		var exitLong = rsiVal > RsiOverbought;
		// Exit short: price drops below lower BB
		var exitShort = candle.ClosePrice < lower;

		// Exit positions first
		if (exitLong && Position > 0)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		else if (exitShort && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Enter new positions
		else if (entryLong && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		else if (entryShort && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
	}
}
