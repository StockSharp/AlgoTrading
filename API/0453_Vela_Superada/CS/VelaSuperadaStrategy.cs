namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Vela Superada Strategy.
/// Trades on candle pattern reversals with EMA, RSI and MACD filters.
/// Buys on bullish reversal pattern above EMA with rising MACD.
/// Sells on bearish reversal pattern below EMA with falling MACD.
/// </summary>
public class VelaSuperadaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _ema;
	private RelativeStrengthIndex _rsi;
	private MovingAverageConvergenceDivergence _macd;

	private decimal _prevClose;
	private decimal _prevOpen;
	private decimal _prevMacd;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public VelaSuperadaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_emaLength = Param(nameof(EmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Moving Averages");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

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

		_ema = null;
		_rsi = null;
		_macd = null;
		_prevClose = 0;
		_prevOpen = 0;
		_prevMacd = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_macd = new MovingAverageConvergenceDivergence();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, _rsi, _macd, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal emaVal, decimal rsiVal, decimal macdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed || !_rsi.IsFormed || !_macd.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			_prevOpen = candle.OpenPrice;
			_prevMacd = macdVal;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevClose = candle.ClosePrice;
			_prevOpen = candle.OpenPrice;
			_prevMacd = macdVal;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevClose = candle.ClosePrice;
			_prevOpen = candle.OpenPrice;
			_prevMacd = macdVal;
			return;
		}

		if (_prevClose == 0)
		{
			_prevClose = candle.ClosePrice;
			_prevOpen = candle.OpenPrice;
			_prevMacd = macdVal;
			return;
		}

		// Candle pattern detection
		var bullishReversal = _prevClose < _prevOpen && candle.ClosePrice > candle.OpenPrice; // Red->Green
		var bearishReversal = _prevClose > _prevOpen && candle.ClosePrice < candle.OpenPrice; // Green->Red

		// MACD momentum
		var macdRising = macdVal > _prevMacd;
		var macdFalling = macdVal < _prevMacd;

		// Buy: bullish reversal + above EMA + RSI not overbought + MACD rising
		if (bullishReversal && candle.ClosePrice > emaVal && rsiVal < 65 && macdRising && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: bearish reversal + below EMA + RSI not oversold + MACD falling
		else if (bearishReversal && candle.ClosePrice < emaVal && rsiVal > 35 && macdFalling && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: bearish reversal below EMA
		else if (Position > 0 && bearishReversal && candle.ClosePrice < emaVal)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: bullish reversal above EMA
		else if (Position < 0 && bullishReversal && candle.ClosePrice > emaVal)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevClose = candle.ClosePrice;
		_prevOpen = candle.OpenPrice;
		_prevMacd = macdVal;
	}
}
