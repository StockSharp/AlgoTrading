using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Outside Bar Reversal strategy.
/// The strategy looks for outside bar patterns (a bar with higher high and lower low than the previous bar)
/// and takes positions based on the direction (bullish or bearish) of the outside bar.
/// </summary>
public class OutsideBarReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	
	private ICandleMessage _previousCandle;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public OutsideBarReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2.0m, 0.5m);
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

		_previousCandle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create subscription
		var subscription = SubscribeCandles(CandleType);
		
		subscription
			.Bind(ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: new(), // No take profit, rely on exit logic
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// First candle - just store it
		if (_previousCandle == null)
		{
			_previousCandle = candle;
			return;
		}

		// Check if current candle is an outside bar compared to previous candle
		bool isOutsideBar = IsOutsideBar(_previousCandle, candle);
		
		if (isOutsideBar)
		{
			LogInfo($"Outside bar detected: High {candle.HighPrice} > Previous High {_previousCandle.HighPrice}, " +
						   $"Low {candle.LowPrice} < Previous Low {_previousCandle.LowPrice}");

			// Determine if the outside bar is bullish or bearish
			bool isBullish = candle.ClosePrice > candle.OpenPrice;
			bool isBearish = candle.ClosePrice < candle.OpenPrice;

			// Trading logic based on outside bar direction
			if (isBullish && Position <= 0)
			{
				// Bullish outside bar - go long
				CancelActiveOrders();
				BuyMarket(Volume + Math.Abs(Position));
				LogInfo($"Long entry at {candle.ClosePrice} on bullish outside bar");					
			}
			else if (isBearish && Position >= 0)
			{
				// Bearish outside bar - go short
				CancelActiveOrders();
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Short entry at {candle.ClosePrice} on bearish outside bar");
			}
		}

		// Exit logic
		if (Position > 0)
		{
			// Exit long position if price breaks above the outside bar's high
			if (candle.HighPrice > _previousCandle.HighPrice)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Long exit at {candle.ClosePrice} (price above outside bar high {_previousCandle.HighPrice})");
			}
		}
		else if (Position < 0)
		{
			// Exit short position if price breaks below the outside bar's low
			if (candle.LowPrice < _previousCandle.LowPrice)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short exit at {candle.ClosePrice} (price below outside bar low {_previousCandle.LowPrice})");
			}
		}

		// Update previous candle for next iteration
		_previousCandle = candle;
	}

	private bool IsOutsideBar(ICandleMessage previous, ICandleMessage current)
	{
		// An outside bar has its high higher than the previous candle's high
		// and its low lower than the previous candle's low
		return current.HighPrice > previous.HighPrice && current.LowPrice < previous.LowPrice;
	}
}