using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on MACD and Williams %R indicators.
/// Enters long when MACD > Signal and Williams %R is oversold (< -80)
/// Enters short when MACD < Signal and Williams %R is overbought (> -20)
/// </summary>
public class MacdWilliamsRStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _williamsRPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// MACD fast period
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD slow period
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal period
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Williams %R period
	/// </summary>
	public int WilliamsRPeriod
	{
		get => _williamsRPeriod.Value;
		set => _williamsRPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor
	/// </summary>
	public MacdWilliamsRStrategy()
	{
		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast Period", "Fast EMA period for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(8, 16, 2);

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow Period", "Slow EMA period for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 30, 2);

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal Period", "Signal line period for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 12, 1);

		_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 20, 2);

		_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 3.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");
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
		protected override void OnStarted(DateTimeOffset time)
		{
		base.OnStarted(time);

		// Create indicators

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};
		var williamsR = new WilliamsR { Length = WilliamsRPeriod };

		// Enable position protection with stop-loss
		StartProtection(
			takeProfit: new Unit(0), // No take profit
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
		);

		// Subscribe to candles and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, williamsR, ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			
			// Create a separate area for Williams %R
			var williamsArea = CreateChartArea();
			if (williamsArea != null)
			{
				DrawIndicator(williamsArea, williamsR);
			}
			
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue williamsRValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;
		
		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Get additional values from MACD (signal line)
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macd = macdTyped.Macd;
		var signal = macdTyped.Signal;
		var williamsR = williamsRValue.ToDecimal();

		// Trading logic
		if (macd > signal) // MACD above signal line - bullish
		{
			if (williamsR < -80 && Position <= 0) // Oversold condition
			{
				// Buy signal
				BuyMarket(Volume + Math.Abs(Position));
			}
		}
		else if (macd < signal) // MACD below signal line - bearish
		{
			if (williamsR > -20 && Position >= 0) // Overbought condition
			{
				// Sell signal
				SellMarket(Volume + Math.Abs(Position));
			}
			else if (Position > 0) // Already long, exit on MACD crossing down
			{
				// Exit long position
				SellMarket(Position);
			}
		}
		else if (macd > signal && Position < 0) // Already short, exit on MACD crossing up
		{
			// Exit short position
			BuyMarket(Math.Abs(Position));
		}
	}
}