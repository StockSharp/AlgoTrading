using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on price momentum percentage change.
/// </summary>
public class MomentumPercentageStrategy : Strategy
{
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _thresholdPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Momentum period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Threshold percentage for entry.
	/// </summary>
	public decimal ThresholdPercent
	{
		get => _thresholdPercent.Value;
		set => _thresholdPercent.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	/// Initializes a new instance of the <see cref="MomentumPercentageStrategy"/>.
	/// </summary>
	public MomentumPercentageStrategy()
	{
		_momentumPeriod = Param(nameof(MomentumPeriod), 10)
			.SetRange(5, 30)
			.SetDisplay("Momentum Period", "Period for momentum calculation", "Indicators")
			.SetCanOptimize(true);

		_thresholdPercent = Param(nameof(ThresholdPercent), 5m)
			.SetRange(1m, 10m)
			.SetDisplay("Threshold %", "Momentum percentage threshold for entry", "Strategy")
			.SetCanOptimize(true);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 5m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		var momentum = new Momentum { Length = MomentumPeriod };
		var sma = new SimpleMovingAverage { Length = 20 };

		// Subscribe to candles and bind the indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(momentum, sma, ProcessCandle)
			.Start();

		// Enable stop loss protection
		StartProtection(
			takeProfit: null,
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			useMarketOrders: true
		);

		// Setup chart visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, momentum);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal momentumValue, decimal smaValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Calculate the percentage change
		// Momentum indicator returns the difference between current price and price N periods ago
		// To get percentage change: (momentum / (close - momentum)) * 100
		decimal closePrice = candle.ClosePrice;
		decimal previousPrice = closePrice - momentumValue;
		
		if (previousPrice == 0)
			return;  // Avoid division by zero
			
		decimal percentChange = (momentumValue / previousPrice) * 100;

		// Entry logic
		if (percentChange > ThresholdPercent && candle.ClosePrice > smaValue && Position <= 0)
		{
			// Price increased by more than threshold percentage and is above MA - buy signal
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			LogInfo($"Buy signal: Momentum {percentChange:F2}% increase over {MomentumPeriod} periods");
		}
		else if (percentChange < -ThresholdPercent && candle.ClosePrice < smaValue && Position >= 0)
		{
			// Price decreased by more than threshold percentage and is below MA - sell signal
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			LogInfo($"Sell signal: Momentum {Math.Abs(percentChange):F2}% decrease over {MomentumPeriod} periods");
		}

		// Exit logic
		if (Position > 0 && candle.ClosePrice < smaValue)
		{
			// Exit long position when price falls below MA
			SellMarket(Math.Abs(Position));
			LogInfo($"Exiting long position: Price fell below MA. Price: {candle.ClosePrice}, MA: {smaValue}");
		}
		else if (Position < 0 && candle.ClosePrice > smaValue)
		{
			// Exit short position when price rises above MA
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exiting short position: Price rose above MA. Price: {candle.ClosePrice}, MA: {smaValue}");
		}
	}
}