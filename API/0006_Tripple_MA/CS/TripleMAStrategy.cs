using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Triple Moving Average crossover.
/// It enters long position when short MA > middle MA > long MA and short position when short MA < middle MA < long MA.
/// </summary>
public class TripleMAStrategy : Strategy
{
	private readonly StrategyParam<int> _shortMaPeriod;
	private readonly StrategyParam<int> _middleMaPeriod;
	private readonly StrategyParam<int> _longMaPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	// Current state
	private bool _prevIsShortAboveMiddle;

	/// <summary>
	/// Period for short moving average.
	/// </summary>
	public int ShortMaPeriod
	{
		get => _shortMaPeriod.Value;
		set => _shortMaPeriod.Value = value;
	}

	/// <summary>
	/// Period for middle moving average.
	/// </summary>
	public int MiddleMaPeriod
	{
		get => _middleMaPeriod.Value;
		set => _middleMaPeriod.Value = value;
	}

	/// <summary>
	/// Period for long moving average.
	/// </summary>
	public int LongMaPeriod
	{
		get => _longMaPeriod.Value;
		set => _longMaPeriod.Value = value;
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
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the Triple MA strategy.
	/// </summary>
	public TripleMAStrategy()
	{
		_shortMaPeriod = Param(nameof(ShortMaPeriod), 5)
			.SetDisplay("Short MA Period", "Period for short moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_middleMaPeriod = Param(nameof(MiddleMaPeriod), 20)
			.SetDisplay("Middle MA Period", "Period for middle moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(15, 30, 5);

		_longMaPeriod = Param(nameof(LongMaPeriod), 50)
			.SetDisplay("Long MA Period", "Period for long moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(40, 100, 10);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetDisplay("Stop Loss (%)", "Stop loss as a percentage of entry price", "Risk parameters")
			.SetCanOptimize(true)
			.SetOptimize(1, 3, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevIsShortAboveMiddle = default;

	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create indicators
		var shortMa = new SimpleMovingAverage { Length = ShortMaPeriod };
		var middleMa = new SimpleMovingAverage { Length = MiddleMaPeriod };
		var longMa = new SimpleMovingAverage { Length = LongMaPeriod };

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(shortMa, middleMa, longMa, ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortMa);
			DrawIndicator(area, middleMa);
			DrawIndicator(area, longMa);
			DrawOwnTrades(area);
		}

		// Start protection with stop loss
		StartProtection(
			takeProfit: null,
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortMaValue, decimal middleMaValue, decimal longMaValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Check the MA alignments
		var isShortAboveMiddle = shortMaValue > middleMaValue;
		var isMiddleAboveLong = middleMaValue > longMaValue;

		// Check for MA crossover
		var isShortCrossedMiddle = isShortAboveMiddle != _prevIsShortAboveMiddle;

		// Check for alignment conditions
		var isBullishAlignment = isShortAboveMiddle && isMiddleAboveLong;
		var isBearishAlignment = !isShortAboveMiddle && !isMiddleAboveLong;

		// Entry logic based on three MA alignment
		if (isBullishAlignment && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			LogInfo($"Buy signal: Short MA={shortMaValue} > Middle MA={middleMaValue} > Long MA={longMaValue}");
		}
		else if (isBearishAlignment && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			LogInfo($"Sell signal: Short MA={shortMaValue} < Middle MA={middleMaValue} < Long MA={longMaValue}");
		}
		// Exit logic based on short MA crossing middle MA
		else if (isShortCrossedMiddle)
		{
			if (!isShortAboveMiddle && Position > 0)
			{
				SellMarket(Position);
				LogInfo($"Exit long: Short MA crossed below Middle MA (Short={shortMaValue}, Middle={middleMaValue})");
			}
			else if (isShortAboveMiddle && Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit short: Short MA crossed above Middle MA (Short={shortMaValue}, Middle={middleMaValue})");
			}
		}

		// Update previous state
		_prevIsShortAboveMiddle = isShortAboveMiddle;
	}
}