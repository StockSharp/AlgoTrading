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
/// Strategy that trades on Williams %R breakouts.
/// When Williams %R crosses above the overbought level or below the oversold level,
/// it enters position in the corresponding direction. Exits when Williams %R
/// crosses back through its moving average.
/// </summary>
public class WilliamsRBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _williamsRPeriod;
	private readonly StrategyParam<int> _avgPeriod;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLoss;

	private WilliamsR _williamsR;
	private SimpleMovingAverage _williamsRAverage;
	private bool _prevInitialized;
	private decimal _prevWilliamsRValue;
	private decimal _prevWilliamsRAvgValue;
	private int _cooldown;

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int WilliamsRPeriod
	{
		get => _williamsRPeriod.Value;
		set => _williamsRPeriod.Value = value;
	}

	/// <summary>
	/// Period for Williams %R average calculation.
	/// </summary>
	public int AvgPeriod
	{
		get => _avgPeriod.Value;
		set => _avgPeriod.Value = value;
	}

	/// <summary>
	/// Overbought level for Williams %R (e.g. -10).
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Oversold level for Williams %R (e.g. -90).
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="WilliamsRBreakoutStrategy"/>.
	/// </summary>
	public WilliamsRBreakoutStrategy()
	{
		_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators")
			.SetOptimize(10, 30, 2);

		_avgPeriod = Param(nameof(AvgPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Average Period", "Period for Williams %R average calculation", "Indicators")
			.SetOptimize(10, 50, 5);

		_overboughtLevel = Param(nameof(OverboughtLevel), -10m)
			.SetDisplay("Overbought Level", "Williams %R overbought threshold", "Indicators");

		_oversoldLevel = Param(nameof(OversoldLevel), -90m)
			.SetDisplay("Oversold Level", "Williams %R oversold threshold", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_stopLoss = Param(nameof(StopLoss), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop Loss percentage", "Risk Management")
			.SetOptimize(1.0m, 5.0m, 0.5m);
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

		_prevInitialized = false;
		_prevWilliamsRValue = 0;
		_prevWilliamsRAvgValue = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create indicators
		_williamsR = new WilliamsR { Length = WilliamsRPeriod };
		_williamsRAverage = new SimpleMovingAverage { Length = AvgPeriod };

		// Create subscription and bind Williams %R
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_williamsR, ProcessCandle)
			.Start();

		// Enable stop loss protection
		StartProtection(
			takeProfit: new Unit(0, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Percent)
		);

		// Create chart area for visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _williamsR);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Feed WR value through SMA to get the average (must set IsFinal for buffer to accumulate)
		var input = new DecimalIndicatorValue(_williamsRAverage, wrValue, candle.ServerTime) { IsFinal = true };
		var avgResult = _williamsRAverage.Process(input);

		if (!_williamsRAverage.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var currentWilliamsRAvg = avgResult.ToDecimal();

		if (!_prevInitialized)
		{
			_prevWilliamsRValue = wrValue;
			_prevWilliamsRAvgValue = currentWilliamsRAvg;
			_prevInitialized = true;
			return;
		}

		// Cooldown between trades (minimum bars between signals)
		if (_cooldown > 0)
		{
			_cooldown--;
			_prevWilliamsRValue = wrValue;
			_prevWilliamsRAvgValue = currentWilliamsRAvg;
			return;
		}

		const int cooldownBars = 100;

		// Williams %R breakout detection using crossover of extreme levels
		// Williams %R crossing above overbought level from below = bullish breakout
		if (_prevWilliamsRValue <= OverboughtLevel && wrValue > OverboughtLevel && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_cooldown = cooldownBars;
		}
		// Williams %R crossing below oversold level from above = bearish breakout
		else if (_prevWilliamsRValue >= OversoldLevel && wrValue < OversoldLevel && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_cooldown = cooldownBars;
		}
		// Exit long when Williams %R drops below the midpoint (-50)
		else if (Position > 0 && _prevWilliamsRValue >= -50m && wrValue < -50m)
		{
			SellMarket(Math.Abs(Position));
			_cooldown = cooldownBars;
		}
		// Exit short when Williams %R rises above the midpoint (-50)
		else if (Position < 0 && _prevWilliamsRValue <= -50m && wrValue > -50m)
		{
			BuyMarket(Math.Abs(Position));
			_cooldown = cooldownBars;
		}

		// Update previous values
		_prevWilliamsRValue = wrValue;
		_prevWilliamsRAvgValue = currentWilliamsRAvg;
	}
}
