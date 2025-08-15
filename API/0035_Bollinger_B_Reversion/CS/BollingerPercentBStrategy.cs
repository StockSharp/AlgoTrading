using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on Bollinger %B indicator.
/// Bollinger %B shows where price is relative to the Bollinger Bands.
/// Values below 0 or above 1 indicate price outside the bands.
/// </summary>
public class BollingerPercentBStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<decimal> _exitValue;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Period for Bollinger Bands calculation (default: 20)
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Deviation for Bollinger Bands calculation (default: 2.0)
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Exit threshold for %B (default: 0.5)
	/// </summary>
	public decimal ExitValue
	{
		get => _exitValue.Value;
		set => _exitValue.Value = value;
	}

	/// <summary>
	/// Stop-loss as percentage from entry price (default: 2%)
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Type of candles used for strategy calculation
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the Bollinger %B Reversion strategy
	/// </summary>
	public BollingerPercentBStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Bollinger Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
			.SetDisplay("Bollinger Deviation", "Deviation for Bollinger Bands calculation", "Bollinger Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1.5m, 2.5m, 0.25m);

		_exitValue = Param(nameof(ExitValue), 0.5m)
			.SetDisplay("Exit %B Value", "Exit threshold for %B", "Exit Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.3m, 0.7m, 0.1m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
			.SetDisplay("Stop Loss %", "Stop loss as percentage from entry price", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 5.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create indicators
		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, ProcessCandle)
			.Start();

		// Configure chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}

		// Setup protection with stop-loss
		StartProtection(
			new Unit(0), // No take profit
			new Unit(StopLossPercent, UnitTypes.Percent) // Stop loss as percentage of entry price
		);
	}

	/// <summary>
	/// Process candle and calculate Bollinger %B
	/// </summary>
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bollingerTyped = (BollingerBandsValue)bollingerValue;
		
		if (bollingerTyped.UpBand is not decimal upperBand ||
			bollingerTyped.LowBand is not decimal lowerBand)
			return;

		// Calculate Bollinger %B: (Price - Lower Band) / (Upper Band - Lower Band)
		decimal percentB = 0;
		if (upperBand != lowerBand)
		{
			percentB = (candle.ClosePrice - lowerBand) / (upperBand - lowerBand);
		}

		if (Position == 0)
		{
			// No position - check for entry signals
			if (percentB < 0)
			{
				// Price below lower band (%B < 0) - buy (long)
				BuyMarket(Volume);
			}
			else if (percentB > 1)
			{
				// Price above upper band (%B > 1) - sell (short)
				SellMarket(Volume);
			}
		}
		else if (Position > 0)
		{
			// Long position - check for exit signal
			if (percentB > ExitValue)
			{
				// %B moved above exit threshold - exit long
				SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			// Short position - check for exit signal
			if (percentB < ExitValue)
			{
				// %B moved below exit threshold - exit short
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
