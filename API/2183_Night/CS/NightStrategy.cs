using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Night trading strategy based on the Stochastic Oscillator.
/// Trades only between 21:00 and 06:00 when the market is quiet.
/// Enters long when %K falls below oversold level and short when it rises above overbought level.
/// Uses fixed stop loss and take profit.
/// </summary>
public class NightStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Stop loss in price points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit in price points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stochastic oversold threshold.
	/// </summary>
	public decimal StochOversold
	{
		get => _stochOversold.Value;
		set => _stochOversold.Value = value;
	}

	/// <summary>
	/// Stochastic overbought threshold.
	/// </summary>
	public decimal StochOverbought
	{
		get => _stochOverbought.Value;
		set => _stochOverbought.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public NightStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 40)
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 10);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 20)
			.SetDisplay("Take Profit", "Take profit in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 10);

		_stochOversold = Param(nameof(StochOversold), 30m)
			.SetDisplay("Stochastic Oversold", "Oversold level for %K", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10m, 50m, 5m);

		_stochOverbought = Param(nameof(StochOverbought), 70m)
			.SetDisplay("Stochastic Overbought", "Overbought level for %K", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(50m, 90m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");
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

		// Create Stochastic Oscillator with fixed periods
		var stochastic = new StochasticOscillator { KPeriod = 5, DPeriod = 3, Smooth = 3 };

		// Subscribe to candles and bind indicator
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		// Visualize if chart area is available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}

		// Setup stop loss and take profit protection
		var step = Security.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfitPoints * step, UnitTypes.Point),
			stopLoss: new Unit(StopLossPoints * step, UnitTypes.Point));
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		// Ignore unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Wait until strategy is ready and trading allowed
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var kValue = ((StochasticOscillatorValue)stochValue).K;

		// Trade only during night hours 21:00-06:00
		var hour = candle.OpenTime.Hour;
		var isNight = hour >= 21 || hour < 6;

		if (!isNight)
			return;

		if (kValue < StochOversold && Position <= 0)
		{
			// Oversold condition - buy
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (kValue > StochOverbought && Position >= 0)
		{
			// Overbought condition - sell
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
