using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands Divergence Strategy.
/// Detects divergence between price and Bollinger Bands expansion.
/// </summary>
public class BollingerDivergenceStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _cooldownBars;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BBLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BBMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	private BollingerBands _bollinger;
	private decimal _prevUpperBand;
	private decimal _prevLowerBand;
	private int _cooldownRemaining;

	public BollingerDivergenceStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_bbLength = Param(nameof(BBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands");

		_bbMultiplier = Param(nameof(BBMultiplier), 2.0m)
			.SetDisplay("BB StdDev", "Bollinger Bands standard deviation multiplier", "Bollinger Bands");

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

		_bollinger = null;
		_prevUpperBand = 0;
		_prevLowerBand = 0;
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
			return;

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upperBand ||
			bb.LowBand is not decimal lowerBand ||
			bb.MovingAverage is not decimal middleBand)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevUpperBand = upperBand;
			_prevLowerBand = lowerBand;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevUpperBand = upperBand;
			_prevLowerBand = lowerBand;
			return;
		}

		var close = candle.ClosePrice;

		if (_prevUpperBand > 0 && _prevLowerBand > 0)
		{
			// Bands expanding: upper rising, lower dropping
			var bandsExpanding = upperBand > _prevUpperBand && lowerBand < _prevLowerBand;
			var bullishCandle = close > candle.OpenPrice;
			var bearishCandle = close < candle.OpenPrice;

			// Buy: close above upper band + bands expanding + bullish candle
			if (close > upperBand && bandsExpanding && bullishCandle && Position <= 0)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));
				BuyMarket(Volume);
				_cooldownRemaining = CooldownBars;
			}
			// Sell: close below lower band + bands expanding + bearish candle
			else if (close < lowerBand && bandsExpanding && bearishCandle && Position >= 0)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				SellMarket(Volume);
				_cooldownRemaining = CooldownBars;
			}
			// Exit long: price returns below middle
			else if (Position > 0 && close < middleBand)
			{
				SellMarket(Math.Abs(Position));
				_cooldownRemaining = CooldownBars;
			}
			// Exit short: price returns above middle
			else if (Position < 0 && close > middleBand)
			{
				BuyMarket(Math.Abs(Position));
				_cooldownRemaining = CooldownBars;
			}
		}

		_prevUpperBand = upperBand;
		_prevLowerBand = lowerBand;
	}
}
