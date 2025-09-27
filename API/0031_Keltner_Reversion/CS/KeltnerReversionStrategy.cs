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
/// Strategy that trades on mean reversion using Keltner Channels.
/// It opens positions when price touches or breaks through the upper or lower Keltner Channel bands
/// and exits when price reverts to the middle band (EMA).
/// </summary>
public class KeltnerReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Period for EMA calculation (middle band) (default: 20)
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Period for ATR calculation (default: 14)
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for Keltner Channel width (default: 2.0)
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop-loss calculation (default: 2.0)
	/// </summary>
	public decimal StopLossAtrMultiplier
	{
		get => _stopLossAtrMultiplier.Value;
		set => _stopLossAtrMultiplier.Value = value;
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
	/// Initialize the Keltner Reversion strategy
	/// </summary>
	public KeltnerReversionStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetDisplay("EMA Period", "Period for EMA calculation (middle band)", "Technical Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Period for ATR calculation (middle band)", "Technical Parameters")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 7);

		_atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for Keltner Channel width", "Technical Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 3.0m, 0.5m);

		_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 2.0m)
			.SetDisplay("ATR Multiplier (Stop Loss)", "ATR multiplier for stop-loss calculation", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 3.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "Technical Parameters");
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
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, atr, ProcessCandle)
			.Start();

		// Configure chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	/// <summary>
	/// Process candle and check for Keltner Channel signals
	/// </summary>
	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Calculate Keltner Channel bands
		decimal upperBand = emaValue + (atrValue * AtrMultiplier);
		decimal lowerBand = emaValue - (atrValue * AtrMultiplier);
		
		// Calculate stop-loss amount based on ATR
		decimal stopLossAmount = atrValue * StopLossAtrMultiplier;

		if (Position == 0)
		{
			// No position - check for entry signals
			if (candle.ClosePrice < lowerBand)
			{
				// Price is below lower band - buy (long)
				BuyMarket(Volume);
			}
			else if (candle.ClosePrice > upperBand)
			{
				// Price is above upper band - sell (short)
				SellMarket(Volume);
			}
		}
		else if (Position > 0)
		{
			// Long position - check for exit signal
			if (candle.ClosePrice > emaValue)
			{
				// Price has returned to or above EMA - exit long
				SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			// Short position - check for exit signal
			if (candle.ClosePrice < emaValue)
			{
				// Price has returned to or below EMA - exit short
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
