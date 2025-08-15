using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Spring Reversal pattern, which occurs when price makes a new low below support 
/// but immediately reverses and closes above the support level, indicating a bullish reversal.
/// </summary>
public class SpringReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private SimpleMovingAverage _ma;
	private Lowest _lowest;
	
	private decimal _lastLowestValue;

	/// <summary>
	/// Candle type and timeframe for the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for low range detection.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Period for moving average calculation.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
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
	/// Initializes a new instance of the <see cref="SpringReversalStrategy"/>.
	/// </summary>
	public SpringReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
					 .SetDisplay("Candle Type", "Type of candles to use for analysis", "General");
		
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
						 .SetDisplay("Lookback Period", "Period for support level detection", "Range")
						 .SetRange(5, 50);
		
		_maPeriod = Param(nameof(MaPeriod), 20)
				   .SetDisplay("MA Period", "Period for moving average calculation", "Trend")
				   .SetRange(5, 50);
		
		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
						  .SetDisplay("Stop Loss %", "Stop-loss percentage from entry price", "Protection")
						  .SetRange(0.5m, 3m);
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

		_lastLowestValue = 0;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		// Initialize indicators
		_ma = new SimpleMovingAverage { Length = MaPeriod };
		_lowest = new Lowest { Length = LookbackPeriod };
		
		// Create and setup subscription for candles
		var subscription = SubscribeCandles(CandleType);
		
		// Bind indicators and processor
		subscription
			.Bind(_ma, _lowest, ProcessCandle)
			.Start();
		
		// Enable stop-loss protection
		StartProtection(new Unit(0), new Unit(StopLossPercent, UnitTypes.Percent));
		
		// Setup chart if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma, decimal lowest)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		
		// Store the last lowest value
		_lastLowestValue = lowest;
		
		// Determine candle characteristics
		bool isBullish = candle.ClosePrice > candle.OpenPrice;
		bool piercesBelowSupport = candle.LowPrice < _lastLowestValue;
		bool closeAboveSupport = candle.ClosePrice > _lastLowestValue;
		
		// Spring pattern:
		// 1. Price dips below recent low (support level)
		// 2. But closes above the support level (bullish rejection)
		if (piercesBelowSupport && closeAboveSupport && isBullish)
		{
			// Enter long position only if we're not already long
			if (Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				
				LogInfo($"Spring Reversal detected. Support level: {_lastLowestValue}, Low: {candle.LowPrice}. Long entry at {candle.ClosePrice}");
			}
		}
		
		// Exit conditions
		if (Position > 0)
		{
			// Exit when price rises above the moving average (take profit)
			if (candle.ClosePrice > ma)
			{
				SellMarket(Math.Abs(Position));
				
				LogInfo($"Exit signal: Price above MA. Closed long position at {candle.ClosePrice}");
			}
		}
	}
}