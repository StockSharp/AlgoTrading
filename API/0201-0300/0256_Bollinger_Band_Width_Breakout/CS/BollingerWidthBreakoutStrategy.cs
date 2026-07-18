using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on Bollinger Band width breakouts.
/// When Bollinger Band width increases significantly above its average, 
/// it enters position in the direction determined by price movement.
/// </summary>
public class BollingerWidthBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _avgPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stopMultiplier;
	
	private BollingerBands _bollinger;
	private SimpleMovingAverage _widthAverage;
	private AverageTrueRange _atr;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}
	
	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}
	
	/// <summary>
	/// Period for width average calculation.
	/// </summary>
	public int AvgPeriod
	{
		get => _avgPeriod.Value;
		set => _avgPeriod.Value = value;
	}
	
	/// <summary>
	/// Standard deviation multiplier for breakout detection.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}
	
	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Stop-loss ATR multiplier.
	/// </summary>
	public int StopMultiplier
	{
		get => _stopMultiplier.Value;
		set => _stopMultiplier.Value = value;
	}
	
	/// <summary>
	/// Initialize <see cref="BollingerWidthBreakoutStrategy"/>.
	/// </summary>
	public BollingerWidthBreakoutStrategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Period of the Bollinger Bands indicator", "Indicators")
			
			.SetOptimize(10, 50, 5);
			
		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
			
			.SetOptimize(1.0m, 3.0m, 0.5m);
		
		_avgPeriod = Param(nameof(AvgPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Average Period", "Period for Bollinger width average calculation", "Indicators")
			
			.SetOptimize(10, 50, 5);
		
		_multiplier = Param(nameof(Multiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Standard deviation multiplier for breakout detection", "Indicators")

			.SetOptimize(1.0m, 3.0m, 0.5m);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		
		_stopMultiplier = Param(nameof(StopMultiplier), 2)
			.SetGreaterThanZero()
			.SetDisplay("Stop Multiplier", "ATR multiplier for stop-loss", "Risk Management")
			
			.SetOptimize(1, 5, 1);
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
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);


		// Create indicators
		_bollinger = new BollingerBands
		{
			Length = BollingerLength,
			Width = BollingerDeviation
		};
		
		_widthAverage = new SMA { Length = AvgPeriod };
		_atr = new AverageTrueRange { Length = BollingerLength };
		
		// Create subscription
		var subscription = SubscribeCandles(CandleType);
		
		// Bind Bollinger Bands
		subscription
			.BindEx(_bollinger, _atr, ProcessBollinger)
			.Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);

		// Create chart area for visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessBollinger(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bollingerValue.IsFinal || !atrValue.IsFinal || bollingerValue.IsEmpty || atrValue.IsEmpty)
			return;
		
		// Calculate Bollinger Band width
		if (bollingerValue is not BollingerBandsValue bollingerTyped)
			return;

		if (bollingerTyped.UpBand is not decimal upperBand)
			return;

		if (bollingerTyped.LowBand is not decimal lowerBand)
			return;

		var lastWidth = upperBand - lowerBand;

		// Process width through average
		var widthAvgValue = _widthAverage.Process(new DecimalIndicatorValue(_widthAverage, lastWidth, candle.ServerTime) { IsFinal = true });
		var avgWidth = widthAvgValue.ToDecimal();
		
		// Skip if indicators are not formed yet
		if (!_widthAverage.IsFormed)
		{
			return;
		}

		// Bollinger width breakout detection
		if (lastWidth > avgWidth * (1m + Multiplier / 10m))
		{
			// Determine direction based on price and bands
			var upperDistance = (candle.ClosePrice - upperBand).Abs();
			var lowerDistance = (candle.ClosePrice - lowerBand).Abs();
			var priceDirection = upperDistance < lowerDistance;

			if (priceDirection && Position == 0)
			{
				BuyMarket();
			}
			else if (!priceDirection && Position == 0)
			{
				SellMarket();
			}
		}
	}
}
