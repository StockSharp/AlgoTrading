using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands + Aroon Strategy.
/// Buys when price touches lower Bollinger Band with Aroon Up confirming uptrend.
/// Exits when price reaches upper Bollinger Band or Aroon signals weakness.
/// </summary>
public class BollingerAroonStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _aroonLength;
	private readonly StrategyParam<decimal> _aroonConfirmation;
	private readonly StrategyParam<decimal> _aroonStop;

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
	/// Aroon indicator period.
	/// </summary>
	public int AroonLength
	{
		get => _aroonLength.Value;
		set => _aroonLength.Value = value;
	}

	/// <summary>
	/// Aroon confirmation level.
	/// </summary>
	public decimal AroonConfirmation
	{
		get => _aroonConfirmation.Value;
		set => _aroonConfirmation.Value = value;
	}

	/// <summary>
	/// Aroon stop level.
	/// </summary>
	public decimal AroonStop
	{
		get => _aroonStop.Value;
		set => _aroonStop.Value = value;
	}

	private BollingerBands _bollinger;
	private Aroon _aroon;
	private int _cooldownRemaining;

	public BollingerAroonStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_bbLength = Param(nameof(BBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands");

		_bbMultiplier = Param(nameof(BBMultiplier), 2.0m)
			.SetDisplay("BB StdDev", "Bollinger Bands standard deviation multiplier", "Bollinger Bands");

		_aroonLength = Param(nameof(AroonLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Aroon Period", "Aroon indicator period", "Aroon");

		_aroonConfirmation = Param(nameof(AroonConfirmation), 60m)
			.SetDisplay("Aroon Confirmation", "Aroon confirmation level", "Aroon");

		_aroonStop = Param(nameof(AroonStop), 40m)
			.SetDisplay("Aroon Stop", "Aroon stop level", "Aroon");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bollinger = null;
		_aroon = null;
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

		_aroon = new Aroon { Length = AroonLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(_bollinger, _aroon, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle,
		IIndicatorValue bollingerValue, IIndicatorValue aroonValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bollinger.IsFormed || !_aroon.IsFormed)
			return;

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.LowBand is not decimal lowerBand ||
			bb.UpBand is not decimal upperBand ||
			bb.MovingAverage is not decimal middleBand)
			return;

		var aa = (AroonValue)aroonValue;
		if (aa.Up is not decimal aroonUp)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var close = candle.ClosePrice;

		// Long entry: price at/below lower BB + Aroon Up confirming uptrend
		if (close <= lowerBand && aroonUp > AroonConfirmation && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = 12;
		}
		// Short entry: price at/above upper BB + Aroon Up weak
		else if (close >= upperBand && aroonUp < AroonStop && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = 12;
		}
		// Exit long: price reaches upper band or Aroon signals weakness
		else if (Position > 0 && (close >= upperBand || aroonUp < AroonStop))
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = 12;
		}
		// Exit short: price reaches lower band or Aroon signals strength
		else if (Position < 0 && (close <= lowerBand || aroonUp > AroonConfirmation))
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = 12;
		}
	}
}
