namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Stochastic RSI + Supertrend Strategy.
/// Uses RSI levels with SuperTrend direction and EMA trend filter.
/// Buys when RSI is oversold, SuperTrend is bullish, and price above EMA.
/// Sells when RSI is overbought, SuperTrend is bearish, and price below EMA.
/// </summary>
public class StochRsiSupertrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _supertrendLength;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<int> _cooldownBars;

	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _ema;
	private SuperTrend _supertrend;

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

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public int SupertrendLength
	{
		get => _supertrendLength.Value;
		set => _supertrendLength.Value = value;
	}

	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public StochRsiSupertrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_emaLength = Param(nameof(EmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Trend EMA period", "Moving Average");

		_supertrendLength = Param(nameof(SupertrendLength), 11)
			.SetGreaterThanZero()
			.SetDisplay("SuperTrend Length", "SuperTrend ATR period", "SuperTrend");

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 2.0m)
			.SetDisplay("SuperTrend Multiplier", "SuperTrend ATR multiplier", "SuperTrend");

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
		_ema = null;
		_supertrend = null;
		_prevRsi = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_supertrend = new SuperTrend { Length = SupertrendLength, Multiplier = SupertrendMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_rsi, _ema, _supertrend, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _supertrend);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue emaValue, IIndicatorValue stValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed || !_ema.IsFormed || !_supertrend.IsFormed)
			return;

		if (rsiValue.IsEmpty || emaValue.IsEmpty || stValue.IsEmpty)
			return;

		var rsiVal = rsiValue.ToDecimal();
		var emaVal = emaValue.ToDecimal();

		var stTyped = (SuperTrendIndicatorValue)stValue;
		var isUpTrend = stTyped.IsUpTrend;

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

		// RSI crossovers
		var rsiCrossUpOversold = rsiVal > 40 && _prevRsi <= 40;
		var rsiCrossDownOverbought = rsiVal < 60 && _prevRsi >= 60;

		// Buy: RSI crosses above oversold + SuperTrend bullish + price above EMA
		if (rsiCrossUpOversold && isUpTrend && candle.ClosePrice > emaVal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: RSI crosses below overbought + SuperTrend bearish + price below EMA
		else if (rsiCrossDownOverbought && !isUpTrend && candle.ClosePrice < emaVal && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: SuperTrend turns bearish
		else if (Position > 0 && !isUpTrend)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: SuperTrend turns bullish
		else if (Position < 0 && isUpTrend)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevRsi = rsiVal;
	}
}
