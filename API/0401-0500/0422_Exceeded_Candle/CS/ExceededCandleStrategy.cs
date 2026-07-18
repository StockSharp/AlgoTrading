using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Exceeded Candle Strategy - trades on candle engulfing patterns with BB filter.
/// Buys when a bullish candle surpasses the previous bearish candle and price is below BB middle.
/// Exits when price reaches the upper BB.
/// </summary>
public class ExceededCandleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _cooldownBars;

	private BollingerBands _bollinger;
	private ICandleMessage _prevCandle;
	private ICandleMessage _prevPrevCandle;
	private ICandleMessage _prevPrevPrevCandle;
	private int _cooldownRemaining;

	public ExceededCandleStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_bbLength = Param(nameof(BBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands");

		_bbMultiplier = Param(nameof(BBMultiplier), 1.5m)
			.SetDisplay("BB StdDev", "Bollinger Bands standard deviation multiplier", "Bollinger Bands");

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

	public decimal BBMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
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
		_prevCandle = null;
		_prevPrevCandle = null;
		_prevPrevPrevCandle = null;
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

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_bollinger, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bollinger.IsFormed)
		{
			UpdateCandleHistory(candle);
			return;
		}

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upperBand ||
			bb.LowBand is not decimal lowerBand ||
			bb.MovingAverage is not decimal middleBand)
		{
			UpdateCandleHistory(candle);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateCandleHistory(candle);
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			UpdateCandleHistory(candle);
			return;
		}

		var close = candle.ClosePrice;
		var open = candle.OpenPrice;

		// Check for engulfing patterns
		var greenExceeded = false;
		var redExceeded = false;

		if (_prevCandle != null)
		{
			// Bullish engulfing: previous was bearish, current is bullish and closes above previous open
			greenExceeded = _prevCandle.ClosePrice < _prevCandle.OpenPrice &&
							close > open &&
							close > _prevCandle.OpenPrice;

			// Bearish engulfing: previous was bullish, current is bearish and closes below previous open
			redExceeded = _prevCandle.ClosePrice > _prevCandle.OpenPrice &&
						  close < open &&
						  close < _prevCandle.OpenPrice;
		}

		// Check for 3 consecutive bearish candles (avoid buying into strong downtrend)
		var last3Red = false;
		if (_prevCandle != null && _prevPrevCandle != null && _prevPrevPrevCandle != null)
		{
			last3Red = _prevCandle.ClosePrice < _prevCandle.OpenPrice &&
					   _prevPrevCandle.ClosePrice < _prevPrevCandle.OpenPrice &&
					   _prevPrevPrevCandle.ClosePrice < _prevPrevPrevCandle.OpenPrice;
		}

		// Buy: bullish engulfing below middle band, not in strong downtrend
		if (greenExceeded && close < middleBand && !last3Red && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: bearish engulfing above middle band
		else if (redExceeded && close > middleBand && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long at upper band
		else if (Position > 0 && close >= upperBand)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short at lower band
		else if (Position < 0 && close <= lowerBand)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		UpdateCandleHistory(candle);
	}

	private void UpdateCandleHistory(ICandleMessage candle)
	{
		_prevPrevPrevCandle = _prevPrevCandle;
		_prevPrevCandle = _prevCandle;
		_prevCandle = candle;
	}
}
