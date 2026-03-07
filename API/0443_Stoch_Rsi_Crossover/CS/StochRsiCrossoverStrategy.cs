namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Stochastic RSI Crossover Strategy with EMA trend filter.
/// Uses RSI crossovers with triple EMA alignment for trend confirmation.
/// Buys when RSI crosses above oversold in bullish EMA alignment.
/// Sells when RSI crosses below overbought in bearish EMA alignment.
/// </summary>
public class StochRsiCrossoverStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _ema1Length;
	private readonly StrategyParam<int> _ema2Length;
	private readonly StrategyParam<int> _ema3Length;
	private readonly StrategyParam<int> _cooldownBars;

	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _ema1;
	private ExponentialMovingAverage _ema2;
	private ExponentialMovingAverage _ema3;

	private decimal _prevRsi;
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

	public int RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	public int RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	public int Ema1Length
	{
		get => _ema1Length.Value;
		set => _ema1Length.Value = value;
	}

	public int Ema2Length
	{
		get => _ema2Length.Value;
		set => _ema2Length.Value = value;
	}

	public int Ema3Length
	{
		get => _ema3Length.Value;
		set => _ema3Length.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public StochRsiCrossoverStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_rsiOversold = Param(nameof(RsiOversold), 40)
			.SetDisplay("RSI Oversold", "RSI oversold level", "RSI");

		_rsiOverbought = Param(nameof(RsiOverbought), 60)
			.SetDisplay("RSI Overbought", "RSI overbought level", "RSI");

		_ema1Length = Param(nameof(Ema1Length), 8)
			.SetGreaterThanZero()
			.SetDisplay("EMA 1 Length", "Fast EMA length", "Moving Averages");

		_ema2Length = Param(nameof(Ema2Length), 14)
			.SetGreaterThanZero()
			.SetDisplay("EMA 2 Length", "Medium EMA length", "Moving Averages");

		_ema3Length = Param(nameof(Ema3Length), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA 3 Length", "Slow EMA length", "Moving Averages");

		_cooldownBars = Param(nameof(CooldownBars), 15)
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
		_ema1 = null;
		_ema2 = null;
		_ema3 = null;
		_prevRsi = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_ema1 = new ExponentialMovingAverage { Length = Ema1Length };
		_ema2 = new ExponentialMovingAverage { Length = Ema2Length };
		_ema3 = new ExponentialMovingAverage { Length = Ema3Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _ema1, _ema2, _ema3, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema1);
			DrawIndicator(area, _ema2);
			DrawIndicator(area, _ema3);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal rsiVal, decimal ema1Val, decimal ema2Val, decimal ema3Val)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed || !_ema1.IsFormed || !_ema2.IsFormed || !_ema3.IsFormed)
		{
			_prevRsi = rsiVal;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevRsi = rsiVal;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevRsi = rsiVal;
			return;
		}

		if (_prevRsi == 0)
		{
			_prevRsi = rsiVal;
			return;
		}

		// EMA alignment (relaxed - only fast vs slow)
		var bullishEma = ema1Val > ema3Val;
		var bearishEma = ema1Val < ema3Val;

		// RSI crossovers
		var rsiCrossUpOversold = rsiVal > RsiOversold && _prevRsi <= RsiOversold;
		var rsiCrossDownOverbought = rsiVal < RsiOverbought && _prevRsi >= RsiOverbought;

		// Buy: RSI crosses above oversold + bullish EMA
		if (rsiCrossUpOversold && bullishEma && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: RSI crosses below overbought + bearish EMA
		else if (rsiCrossDownOverbought && bearishEma && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: RSI overbought or EMA bearish cross
		else if (Position > 0 && (rsiVal > RsiOverbought || ema1Val < ema2Val))
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: RSI oversold or EMA bullish cross
		else if (Position < 0 && (rsiVal < RsiOversold || ema1Val > ema2Val))
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevRsi = rsiVal;
	}
}
