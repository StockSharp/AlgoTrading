namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Bollinger Breakout Strategy.
/// Buys when candle body extends below lower BB with RSI oversold filter.
/// Sells when candle body extends above upper BB with RSI overbought filter.
/// </summary>
public class BollingerBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _candlePercent;
	private readonly StrategyParam<int> _cooldownBars;

	private BollingerBands _bollinger;
	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _ma;

	private decimal? _entryPrice;
	private int _cooldownRemaining;

	public BollingerBreakoutStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_bbLength = Param(nameof(BBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Bollinger Bands");
		_bbMultiplier = Param(nameof(BBMultiplier), 1.5m)
			.SetDisplay("BB StdDev", "Standard deviation multiplier", "Bollinger Bands");

		_rsiLength = Param(nameof(RSILength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI Filter");
		_rsiOversold = Param(nameof(RSIOversold), 45)
			.SetDisplay("RSI Oversold", "RSI oversold level", "RSI Filter");
		_rsiOverbought = Param(nameof(RSIOverbought), 55)
			.SetDisplay("RSI Overbought", "RSI overbought level", "RSI Filter");

		_maLength = Param(nameof(MALength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving Average period", "Moving Average");

		_candlePercent = Param(nameof(CandlePercent), 0.3m)
			.SetDisplay("Candle %", "Candle body penetration percentage", "Strategy");

		_cooldownBars = Param(nameof(CooldownBars), 15)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
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

	public int RSIOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	public int RSIOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	public int MALength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	public decimal CandlePercent
	{
		get => _candlePercent.Value;
		set => _candlePercent.Value = value;
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

		_bollinger = null;
		_rsi = null;
		_ma = null;
		_entryPrice = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bollinger = new BollingerBands
		{
			Length = BBLength,
			Width = BBMultiplier
		};

		_rsi = new RelativeStrengthIndex { Length = RSILength };
		_ma = new ExponentialMovingAverage { Length = MALength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(_bollinger, _rsi, _ma, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue rsiValue, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bollinger.IsFormed || !_rsi.IsFormed || !_ma.IsFormed)
			return;

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upper ||
			bb.LowBand is not decimal lower ||
			bb.MovingAverage is not decimal middle)
			return;

		if (rsiValue.IsEmpty || maValue.IsEmpty)
			return;

		var rsi = rsiValue.ToDecimal();
		var maVal = maValue.ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var close = candle.ClosePrice;
		var open = candle.OpenPrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		var candleSize = high - low;
		if (candleSize <= 0)
			return;

		var buyZone = candleSize * CandlePercent + low;
		var sellZone = high - candleSize * CandlePercent;

		// Buy: candle buy zone below lower BB, bearish candle, RSI oversold, price above MA
		var buySignal = buyZone < lower && close < open && rsi < RSIOversold && close > maVal;

		// Sell: candle sell zone above upper BB, bullish candle, RSI overbought, price below MA
		var sellSignal = sellZone > upper && close > open && rsi > RSIOverbought && close < maVal;

		// Early exit at middle band
		if (Position > 0 && close >= middle)
		{
			SellMarket(Math.Abs(Position));
			_entryPrice = null;
			_cooldownRemaining = CooldownBars;
			return;
		}
		else if (Position < 0 && close <= middle)
		{
			BuyMarket(Math.Abs(Position));
			_entryPrice = null;
			_cooldownRemaining = CooldownBars;
			return;
		}

		// Entry signals
		if (buySignal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_entryPrice = close;
			_cooldownRemaining = CooldownBars;
		}
		else if (sellSignal && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_entryPrice = close;
			_cooldownRemaining = CooldownBars;
		}
	}
}
