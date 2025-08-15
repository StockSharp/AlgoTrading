using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Pinbar (Pin Bar) candlestick pattern.
/// A pinbar is characterized by a small body with a long wick/tail, 
/// signaling a potential reversal in the market.
/// </summary>
public class PinbarReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tailToBodyRatio;
	private readonly StrategyParam<decimal> _oppositeTailRatio;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _useTrend;
	private readonly StrategyParam<int> _maPeriod;

	private SimpleMovingAverage _ma;

	/// <summary>
	/// Minimum ratio of the main tail to the body length to qualify as a pinbar.
	/// </summary>
	public decimal TailToBodyRatio
	{
		get => _tailToBodyRatio.Value;
		set => _tailToBodyRatio.Value = value;
	}

	/// <summary>
	/// Maximum ratio of the opposite tail to the body length.
	/// </summary>
	public decimal OppositeTailRatio
	{
		get => _oppositeTailRatio.Value;
		set => _oppositeTailRatio.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Whether to use trend filter for trade signals.
	/// </summary>
	public bool UseTrend
	{
		get => _useTrend.Value;
		set => _useTrend.Value = value;
	}

	/// <summary>
	/// Period for the moving average trend filter.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PinbarReversalStrategy"/>.
	/// </summary>
	public PinbarReversalStrategy()
	{
		_tailToBodyRatio = Param(nameof(TailToBodyRatio), 2.0m)
			.SetRange(1.5m, 5.0m)
			.SetDisplay("Tail/Body Ratio", "Minimum ratio of tail to body length", "Pattern Parameters")
			.SetCanOptimize(true);

		_oppositeTailRatio = Param(nameof(OppositeTailRatio), 0.5m)
			.SetRange(0.1m, 1.0m)
			.SetDisplay("Opposite Tail Ratio", "Maximum ratio of opposite tail to body", "Pattern Parameters")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
			.SetRange(0.5m, 3.0m)
			.SetDisplay("Stop Loss %", "Percentage-based stop loss from entry", "Risk Management")
			.SetCanOptimize(true);

		_useTrend = Param(nameof(UseTrend), true)
			.SetDisplay("Use Trend Filter", "Whether to use MA trend filter", "Signal Parameters");

		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("MA Period", "Period for the moving average trend filter", "Signal Parameters")
			.SetCanOptimize(true);
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

		// Create moving average for trend detection
		_ma = new SimpleMovingAverage { Length = MAPeriod };

		// Subscribe to candles
		var subscription = SubscribeCandles(CandleType);

		// Bind candle processing with the MA
		subscription
			.Bind(_ma, ProcessCandle)
			.Start();

		// Enable position protection
		StartProtection(
			new Unit(0, UnitTypes.Absolute), // No take profit (managed by exit conditions)
			new Unit(StopLossPercent, UnitTypes.Percent), // Stop loss at defined percentage
			false // No trailing
		);

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Calculate candle body and shadows
		var bodyLength = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var upperShadow = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
		var lowerShadow = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;
		
		// Check for bullish pinbar (long lower shadow)
		var isBullishPinbar = lowerShadow > bodyLength * TailToBodyRatio && 
							 upperShadow < bodyLength * OppositeTailRatio;
		
		// Check for bearish pinbar (long upper shadow)
		var isBearishPinbar = upperShadow > bodyLength * TailToBodyRatio && 
							 lowerShadow < bodyLength * OppositeTailRatio;
		
		// Determine trend if needed
		var isBullishTrend = !UseTrend || candle.ClosePrice > maValue;
		var isBearishTrend = !UseTrend || candle.ClosePrice < maValue;
		
		LogInfo($"Candle: {candle.OpenTime}, Close: {candle.ClosePrice}, MA: {maValue}, " +
			   $"Body: {bodyLength}, Upper: {upperShadow}, Lower: {lowerShadow}");

		// Process long signals
		if (isBullishPinbar && isBullishTrend && Position <= 0)
		{
			// Bullish pinbar in bullish trend or no trend filter
			if (Position < 0)
			{
				// Close any existing short position
				BuyMarket(Math.Abs(Position));
				LogInfo($"Closed short position on bullish pinbar");
			}

			// Open new long position
			BuyMarket(Volume);
			LogInfo($"Buy signal: Bullish Pinbar detected at {candle.OpenTime}, " +
				   $"Body: {bodyLength:F4}, Lower Shadow: {lowerShadow:F4}, Ratio: {lowerShadow/bodyLength:F2}");
		}
		// Process short signals
		else if (isBearishPinbar && isBearishTrend && Position >= 0)
		{
			// Bearish pinbar in bearish trend or no trend filter
			if (Position > 0)
			{
				// Close any existing long position
				SellMarket(Position);
				LogInfo($"Closed long position on bearish pinbar");
			}

			// Open new short position
			SellMarket(Volume);
			LogInfo($"Sell signal: Bearish Pinbar detected at {candle.OpenTime}, " +
				   $"Body: {bodyLength:F4}, Upper Shadow: {upperShadow:F4}, Ratio: {upperShadow/bodyLength:F2}");
		}
		// Exit signals based on opposite pinbars
		else if (Position > 0 && isBearishPinbar)
		{
			// Exit long position on bearish pinbar
			SellMarket(Position);
			LogInfo($"Exit long: Bearish pinbar appeared");
		}
		else if (Position < 0 && isBullishPinbar)
		{
			// Exit short position on bullish pinbar
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exit short: Bullish pinbar appeared");
		}
	}
}