using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Stochastic Oscillator's overbought/oversold conditions.
/// </summary>
public class StochasticOverboughtOversoldStrategy : Strategy
{
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Stochastic oscillator period.
	/// </summary>
	public int StochPeriod
	{
		get => _stochPeriod.Value;
		set => _stochPeriod.Value = value;
	}

	/// <summary>
	/// K period for Stochastic oscillator.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// D period for Stochastic oscillator.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Candle type and timeframe used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public StochasticOverboughtOversoldStrategy()
	{
		_stochPeriod = Param(nameof(StochPeriod), 14)
					  .SetGreaterThanZero()
					  .SetDisplay("Stochastic Period", "Period for Stochastic oscillator calculation", "Indicators")
					  .SetCanOptimize(true)
					  .SetOptimize(5, 30, 5);

		_kPeriod = Param(nameof(KPeriod), 3)
				  .SetGreaterThanZero()
				  .SetDisplay("K Period", "Smoothing period for Stochastic %K line", "Indicators")
				  .SetCanOptimize(true)
				  .SetOptimize(1, 10, 1);

		_dPeriod = Param(nameof(DPeriod), 3)
				  .SetGreaterThanZero()
				  .SetDisplay("D Period", "Smoothing period for Stochastic %D line", "Indicators")
				  .SetCanOptimize(true)
				  .SetOptimize(1, 10, 1);

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

		// Create Stochastic oscillator
		var stochastic = new StochasticOscillator
		{
			K = { Length = KPeriod },
			D = { Length = DPeriod },
		};

		// Subscribe to candles and bind Stochastic indicator
		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		// Setup stop loss/take profit protection
		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(2, UnitTypes.Percent)
		);

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stochTyped = (StochasticOscillatorValue)stochValue;
		var kValue = stochTyped.K;
		var dValue = stochTyped.D;

		LogInfo($"Stochastic %K: {kValue}, %D: {dValue}");

		if (kValue < 20 && Position <= 0)
		{
			// Oversold condition - Buy
			LogInfo($"Oversold condition detected. K: {kValue}, D: {dValue}");
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (kValue > 80 && Position >= 0)
		{
			// Overbought condition - Sell
			LogInfo($"Overbought condition detected. K: {kValue}, D: {dValue}");
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (kValue > 50 && Position > 0)
		{
			// Exit long position when Stochastic moves back above 50
			LogInfo($"Exiting long position. K: {kValue}");
			SellMarket(Position);
		}
		else if (kValue < 50 && Position < 0)
		{
			// Exit short position when Stochastic moves back below 50
			LogInfo($"Exiting short position. K: {kValue}");
			BuyMarket(Math.Abs(Position));
		}
	}
}