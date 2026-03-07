namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// MACD + Bollinger Bands + RSI Strategy.
/// Uses MACD for momentum, BB for volatility levels, RSI for confirmation.
/// Buys when MACD bullish + price near lower BB + RSI oversold.
/// Sells when MACD bearish + price near upper BB + RSI overbought.
/// </summary>
public class MacdBbRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbWidth;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _cooldownBars;

	private MovingAverageConvergenceDivergence _macd;
	private BollingerBands _bollinger;
	private RelativeStrengthIndex _rsi;
	private decimal _prevMacd;
	private int _cooldownRemaining;

	public MacdBbRsiStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_bbLength = Param(nameof(BBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Bollinger Bands");

		_bbWidth = Param(nameof(BBWidth), 1.5m)
			.SetDisplay("BB Width", "BB standard deviation multiplier", "Bollinger Bands");

		_rsiLength = Param(nameof(RSILength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_cooldownBars = Param(nameof(CooldownBars), 10)
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

	public decimal BBWidth
	{
		get => _bbWidth.Value;
		set => _bbWidth.Value = value;
	}

	public int RSILength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
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

		_macd = null;
		_bollinger = null;
		_rsi = null;
		_prevMacd = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_macd = new MovingAverageConvergenceDivergence();
		_bollinger = new BollingerBands { Length = BBLength, Width = BBWidth };
		_rsi = new RelativeStrengthIndex { Length = RSILength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, _bollinger, _rsi, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue bbValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_macd.IsFormed || !_bollinger.IsFormed || !_rsi.IsFormed)
			return;

		if (macdValue.IsEmpty || rsiValue.IsEmpty)
			return;

		var macdVal = macdValue.ToDecimal();

		var bb = (BollingerBandsValue)bbValue;
		if (bb.UpBand is not decimal upper ||
			bb.LowBand is not decimal lower ||
			bb.MovingAverage is not decimal middle)
			return;

		var rsi = rsiValue.ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevMacd = macdVal;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevMacd = macdVal;
			return;
		}

		var close = candle.ClosePrice;

		// MACD crossover zero
		var macdBullish = macdVal > 0 && _prevMacd <= 0 && _prevMacd != 0;
		var macdBearish = macdVal < 0 && _prevMacd >= 0 && _prevMacd != 0;

		// Buy: MACD crosses above zero + price below middle BB + RSI < 50
		if (macdBullish && close < middle && rsi < 50 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: MACD crosses below zero + price above middle BB + RSI > 50
		else if (macdBearish && close > middle && rsi > 50 && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: price at upper BB or RSI overbought
		else if (Position > 0 && (close >= upper || rsi > 70))
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: price at lower BB or RSI oversold
		else if (Position < 0 && (close <= lower || rsi < 30))
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevMacd = macdVal;
	}
}
