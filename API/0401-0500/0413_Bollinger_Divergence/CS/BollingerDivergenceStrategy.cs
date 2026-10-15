using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Divergence strategy.
/// Goes long when a candle closes below the lower band while the upper band contracts, and short when a candle closes
/// above the upper band while the lower band contracts. The contraction of the opposite band over the bar must reach
/// CandlePercent percent of the signal candle's range. Positions exit on a return to the middle band or at the
/// TakeProfit percentage.
/// </summary>
public class BollingerDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<decimal> _candlePercent;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevUpperBand;
	private decimal? _prevLowerBand;

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
	/// Required contraction of the opposite band, in percent of the signal candle's range.
	/// </summary>
	public decimal CandlePercent
	{
		get => _candlePercent.Value;
		set => _candlePercent.Value = value;
	}

	/// <summary>
	/// Take profit percentage. 0 disables it.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BollingerDivergenceStrategy()
	{
		_bbLength = Param(nameof(BBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands");

		_bbMultiplier = Param(nameof(BBMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("BB StdDev", "Bollinger Bands standard deviation multiplier", "Bollinger Bands");

		_candlePercent = Param(nameof(CandlePercent), 30m)
			.SetNotNegative()
			.SetDisplay("Candle Percent", "Required contraction of the opposite band in percent of the candle range", "Signals");

		_takeProfit = Param(nameof(TakeProfit), 5m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevUpperBand = null;
		_prevLowerBand = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevUpperBand = null;
		_prevLowerBand = null;

		var bollinger = new BollingerBands
		{
			Length = BBLength,
			Width = BBMultiplier
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, ProcessCandle)
			.Start();

		StartProtection(TakeProfit > 0 ? new Unit(TakeProfit, UnitTypes.Percent) : new Unit(), new Unit(), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bollingerValue.IsFormed)
			return;

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upperBand ||
			bb.LowBand is not decimal lowerBand ||
			bb.MovingAverage is not decimal middleBand)
			return;

		var prevUpper = _prevUpperBand;
		var prevLower = _prevLowerBand;

		_prevUpperBand = upperBand;
		_prevLowerBand = lowerBand;

		if (prevUpper is not decimal lastUpper || prevLower is not decimal lastLower)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var minContraction = (candle.HighPrice - candle.LowPrice) * CandlePercent / 100m;

		var upperContraction = lastUpper - upperBand;
		var lowerContraction = lowerBand - lastLower;

		var longSignal = close < lowerBand && upperContraction > 0 && upperContraction >= minContraction;
		var shortSignal = close > upperBand && lowerContraction > 0 && lowerContraction >= minContraction;

		if (longSignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && close >= middleBand)
			SellMarket(Position);
		else if (Position < 0 && close <= middleBand)
			BuyMarket(-Position);
	}
}
