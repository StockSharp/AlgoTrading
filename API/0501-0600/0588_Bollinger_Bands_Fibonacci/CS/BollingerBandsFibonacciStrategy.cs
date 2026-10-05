using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands and Fibonacci strategy.
/// The Fibonacci levels lie at FibonacciLevel0 and FibonacciLevel100 of the range between the lowest low and the highest high of
/// the last FibonacciLength candles. A close crossing above the upper Bollinger band with the low above the Fibonacci low goes long,
/// a close crossing below the lower band with the high below the Fibonacci high goes short, reversing an opposite position.
/// A long closes when the close crosses below the middle band and a short when it crosses above it.
/// </summary>
public class BollingerBandsFibonacciStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _fibonacciLength;
	private readonly StrategyParam<decimal> _fibonacciLevel0;
	private readonly StrategyParam<decimal> _fibonacciLevel100;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal? _prevUpper;
	private decimal? _prevLower;
	private decimal? _prevMiddle;

	/// <summary>
	/// Bollinger period.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	/// <summary>
	/// Bollinger standard deviation multiplier.
	/// </summary>
	public decimal BollingerMultiplier
	{
		get => _bollingerMultiplier.Value;
		set => _bollingerMultiplier.Value = value;
	}

	/// <summary>
	/// Candles of the Fibonacci range.
	/// </summary>
	public int FibonacciLength
	{
		get => _fibonacciLength.Value;
		set => _fibonacciLength.Value = value;
	}

	/// <summary>
	/// Fraction of the range that gives the Fibonacci low.
	/// </summary>
	public decimal FibonacciLevel0
	{
		get => _fibonacciLevel0.Value;
		set => _fibonacciLevel0.Value = value;
	}

	/// <summary>
	/// Fraction of the range that gives the Fibonacci high.
	/// </summary>
	public decimal FibonacciLevel100
	{
		get => _fibonacciLevel100.Value;
		set => _fibonacciLevel100.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BollingerBandsFibonacciStrategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Bollinger period", "Bollinger");

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Multiplier", "Bollinger standard deviation multiplier", "Bollinger");

		_fibonacciLength = Param(nameof(FibonacciLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Fibonacci Length", "Candles of the Fibonacci range", "Fibonacci");

		_fibonacciLevel0 = Param(nameof(FibonacciLevel0), 0m)
			.SetDisplay("Fibonacci Level 0", "Fraction of the range that gives the Fibonacci low", "Fibonacci");

		_fibonacciLevel100 = Param(nameof(FibonacciLevel100), 1m)
			.SetDisplay("Fibonacci Level 100", "Fraction of the range that gives the Fibonacci high", "Fibonacci");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_prevClose = null;
		_prevUpper = null;
		_prevLower = null;
		_prevMiddle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var bollinger = new BollingerBands { Length = BollingerLength, Width = BollingerMultiplier };
		var highest = new Highest { Length = FibonacciLength };
		var lowest = new Lowest { Length = FibonacciLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue highestValue, IIndicatorValue lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bollingerValue.IsFormed || !highestValue.IsFormed || !lowestValue.IsFormed)
			return;

		var bands = (BollingerBandsValue)bollingerValue;

		if (bands.UpBand is not decimal upper || bands.LowBand is not decimal lower || bands.MovingAverage is not decimal middle)
			return;

		var close = candle.ClosePrice;
		var prevClose = _prevClose;
		var prevUpper = _prevUpper;
		var prevLower = _prevLower;
		var prevMiddle = _prevMiddle;

		_prevClose = close;
		_prevUpper = upper;
		_prevLower = lower;
		_prevMiddle = middle;

		if (prevClose is not decimal pc || prevUpper is not decimal pu || prevLower is not decimal pl || prevMiddle is not decimal pm)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var highest = highestValue.GetValue<decimal>();
		var lowest = lowestValue.GetValue<decimal>();
		var range = highest - lowest;
		var fibLow = lowest + range * FibonacciLevel0;
		var fibHigh = lowest + range * FibonacciLevel100;

		var crossAboveUpper = pc <= pu && close > upper;
		var crossBelowLower = pc >= pl && close < lower;

		if (crossAboveUpper && candle.LowPrice > fibLow && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossBelowLower && candle.HighPrice < fibHigh && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && pc >= pm && close < middle)
			SellMarket(Position);
		else if (Position < 0 && pc <= pm && close > middle)
			BuyMarket(-Position);
	}
}
