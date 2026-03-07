namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// RSI + EMA Strategy.
/// Uses RSI oversold/overbought levels with dual EMA trend filter.
/// Buys when RSI is oversold and fast EMA > slow EMA.
/// Sells when RSI is overbought and fast EMA > slow EMA.
/// </summary>
public class RsiEmaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _ma1Length;
	private readonly StrategyParam<int> _ma2Length;
	private readonly StrategyParam<int> _cooldownBars;

	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _ma1;
	private ExponentialMovingAverage _ma2;

	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public int RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	public int RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	public int Ma1Length
	{
		get => _ma1Length.Value;
		set => _ma1Length.Value = value;
	}

	public int Ma2Length
	{
		get => _ma2Length.Value;
		set => _ma2Length.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public RsiEmaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation length", "RSI");

		_rsiOverbought = Param(nameof(RsiOverbought), 70)
			.SetDisplay("RSI Overbought", "RSI overbought level", "RSI");

		_rsiOversold = Param(nameof(RsiOversold), 30)
			.SetDisplay("RSI Oversold", "RSI oversold level", "RSI");

		_ma1Length = Param(nameof(Ma1Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Length", "Fast EMA length", "Moving Averages");

		_ma2Length = Param(nameof(Ma2Length), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Length", "Slow EMA length", "Moving Averages");

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

		_rsi = null;
		_ma1 = null;
		_ma2 = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_ma1 = new ExponentialMovingAverage { Length = Ma1Length };
		_ma2 = new ExponentialMovingAverage { Length = Ma2Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _ma1, _ma2, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma1);
			DrawIndicator(area, _ma2);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal rsiVal, decimal ma1Val, decimal ma2Val)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed || !_ma1.IsFormed || !_ma2.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var uptrend = ma1Val > ma2Val;
		var downtrend = ma1Val < ma2Val;

		// Buy: RSI oversold in uptrend
		if (rsiVal < RsiOversold && uptrend && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: RSI overbought in downtrend
		else if (rsiVal > RsiOverbought && downtrend && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: RSI overbought
		else if (Position > 0 && rsiVal > RsiOverbought)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: RSI oversold
		else if (Position < 0 && rsiVal < RsiOversold)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
	}
}
