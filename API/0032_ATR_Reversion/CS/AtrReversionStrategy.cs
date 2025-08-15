using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on sudden price movements measured in ATR units.
/// It enters positions when price makes a significant move in one direction (N * ATR)
/// and expects a reversion to the mean.
/// </summary>
public class AtrReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;

	/// <summary>
	/// Period for ATR calculation (default: 14)
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for entry signal (default: 2.0)
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Period for Moving Average calculation for exit (default: 20)
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
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
	/// Initialize the ATR Reversion strategy
	/// </summary>
	public AtrReversionStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Period for ATR calculation", "Technical Parameters")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 7);

		_atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for entry signal", "Entry Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1.5m, 3.0m, 0.5m);

		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetDisplay("MA Period", "Period for Moving Average calculation for exit", "Exit Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

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
	protected override void OnReseted()
	{
		base.OnReseted();
		// Reset state variables
		_prevClose = 0;

	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create indicators
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var sma = new SimpleMovingAverage { Length = MAPeriod };

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, sma, ProcessCandle)
			.Start();

		// Configure chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}

		// Setup protection with stop-loss
		StartProtection(
			new Unit(0), // No take profit
			new Unit(StopLossPercent, UnitTypes.Percent) // Stop loss as percentage of entry price
		);
	}

	/// <summary>
	/// Process candle and check for ATR-based signals
	/// </summary>
	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal smaValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Initialize _prevClose on first formed candle
		if (_prevClose == 0)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		// Calculate price change from previous candle
		decimal priceChange = candle.ClosePrice - _prevClose;
		
		// Normalize price change by ATR
		decimal normalizedChange = 0;
		if (atrValue > 0)
		{
			normalizedChange = priceChange / atrValue;
		}

		if (Position == 0)
		{
			// No position - check for entry signals
			if (normalizedChange < -AtrMultiplier)
			{
				// Price dropped significantly (N*ATR) - buy (long) expecting reversion
				BuyMarket(Volume);
			}
			else if (normalizedChange > AtrMultiplier)
			{
				// Price jumped significantly (N*ATR) - sell (short) expecting reversion
				SellMarket(Volume);
			}
		}
		else if (Position > 0)
		{
			// Long position - check for exit signal
			if (candle.ClosePrice > smaValue)
			{
				// Price has reverted to above MA - exit long
				SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			// Short position - check for exit signal
			if (candle.ClosePrice < smaValue)
			{
				// Price has reverted to below MA - exit short
				BuyMarket(Math.Abs(Position));
			}
		}

		// Update previous close price
		_prevClose = candle.ClosePrice;
	}
}
