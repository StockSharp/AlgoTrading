namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Bollinger Width Mean Reversion Strategy.
/// Trades based on mean reversion of Bollinger Bands width.
/// When width is compressed below average, expects expansion and enters long.
/// When width is expanded above average, expects contraction and enters short.
/// </summary>
public class BollingerWidthMeanReversionStrategy : Strategy
{
	private readonly List<decimal> _widthHistory = new();
	private int _candleCount;

	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _widthLookbackPeriod;
	private readonly StrategyParam<decimal> _widthDeviationMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	public int WidthLookbackPeriod
	{
		get => _widthLookbackPeriod.Value;
		set => _widthLookbackPeriod.Value = value;
	}

	public decimal WidthDeviationMultiplier
	{
		get => _widthDeviationMultiplier.Value;
		set => _widthDeviationMultiplier.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public BollingerWidthMeanReversionStrategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Period for Bollinger Bands calculation", "Indicators")
			.SetOptimize(10, 50, 5);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Deviation multiplier for Bollinger Bands", "Indicators")
			.SetOptimize(1.0m, 3.0m, 0.5m);

		_widthLookbackPeriod = Param(nameof(WidthLookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Width Lookback", "Lookback for width mean", "Strategy Parameters")
			.SetOptimize(10, 50, 5);

		_widthDeviationMultiplier = Param(nameof(WidthDeviationMultiplier), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Width Dev Mult", "Multiplier for width std dev threshold", "Strategy Parameters")
			.SetOptimize(0.5m, 3.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_widthHistory.Clear();
		_candleCount = 0;

		var bollinger = new BollingerBands
		{
			Length = BollingerLength,
			Width = BollingerDeviation
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, ProcessCandle)
			.Start();

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

		_candleCount++;

		var bb = (BollingerBandsValue)bollingerValue;

		if (bb.UpBand is not decimal upperBand ||
			bb.LowBand is not decimal lowerBand ||
			bb.MovingAverage is not decimal middleBand)
			return;

		if (middleBand <= 0)
			return;

		// Calculate normalized Bollinger width
		var lastWidth = (upperBand - lowerBand) / middleBand;

		// Track width history
		_widthHistory.Add(lastWidth);
		if (_widthHistory.Count > WidthLookbackPeriod)
			_widthHistory.RemoveAt(0);

		if (_widthHistory.Count < WidthLookbackPeriod)
			return;

		// Calculate mean and std dev of width
		var sum = 0m;
		foreach (var w in _widthHistory)
			sum += w;
		var avgWidth = sum / _widthHistory.Count;

		var sumSq = 0m;
		foreach (var w in _widthHistory)
			sumSq += (w - avgWidth) * (w - avgWidth);
		var stdWidth = (decimal)Math.Sqrt((double)(sumSq / _widthHistory.Count));

		if (avgWidth <= 0)
			return;

		var lowerThreshold = avgWidth - WidthDeviationMultiplier * stdWidth;
		var upperThreshold = avgWidth + WidthDeviationMultiplier * stdWidth;

		// Entry logic
		if (lastWidth < lowerThreshold && Position <= 0)
		{
			// Width compressed - expect expansion, go long
			BuyMarket();
		}
		else if (lastWidth > upperThreshold && Position >= 0)
		{
			// Width expanded - expect contraction, go short
			SellMarket();
		}
		// Exit logic - width returned to mean
		else if (Position > 0 && lastWidth >= avgWidth)
		{
			SellMarket();
		}
		else if (Position < 0 && lastWidth <= avgWidth)
		{
			BuyMarket();
		}
	}
}
