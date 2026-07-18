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
/// Statistical Mean Reversion strategy.
/// Enters long when price falls below the mean by a specified number of standard deviations.
/// Enters short when price rises above the mean by a specified number of standard deviations.
/// Exits positions when price returns to the mean.
/// </summary>
public class MeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _movingAveragePeriod;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _ma;
	private StandardDeviation _stdDev;
	private bool _wasBelowLower;
	private bool _wasAboveUpper;
	private int _cooldown;

	/// <summary>
	/// Moving average period parameter.
	/// </summary>
	public int MovingAveragePeriod
	{
		get => _movingAveragePeriod.Value;
		set => _movingAveragePeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier parameter.
	/// </summary>
	public decimal DeviationMultiplier
	{
		get => _deviationMultiplier.Value;
		set => _deviationMultiplier.Value = value;
	}

	/// <summary>
	/// Bars to wait between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage parameter.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MeanReversionStrategy()
	{
		_movingAveragePeriod = Param(nameof(MovingAveragePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for moving average calculation", "Indicators")
			
			.SetOptimize(10, 50, 5);

		_deviationMultiplier = Param(nameof(DeviationMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation Multiplier", "Standard deviation multiplier for entry signals", "Indicators")
			
			.SetOptimize(1.5m, 3.0m, 0.5m);

		_cooldownBars = Param(nameof(CooldownBars), 50)
			.SetRange(1, 200)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop-loss %", "Stop-loss as percentage of entry price", "Risk Management")
			
			.SetOptimize(1m, 3m, 0.5m);

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

		_ma = null;
		_stdDev = null;
		_wasBelowLower = false;
		_wasAboveUpper = false;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

// Initialize indicators
		_ma = new() { Length = MovingAveragePeriod };
		_stdDev = new() { Length = MovingAveragePeriod };

		// Create candles subscription
		var subscription = SubscribeCandles(CandleType);

		// Bind indicators to subscription
		subscription
			.Bind(_ma, _stdDev, ProcessCandle)
			.Start();

		// Setup chart if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal stdDevValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Skip if strategy is not ready to trade
		// Calculate upper and lower bands based on mean and standard deviation
		decimal upperBand = maValue + (stdDevValue * DeviationMultiplier);
		decimal lowerBand = maValue - (stdDevValue * DeviationMultiplier);
		var isBelowLower = candle.ClosePrice < lowerBand;
		var isAboveUpper = candle.ClosePrice > upperBand;
		var crossedBelowLower = !_wasBelowLower && isBelowLower;
		var crossedAboveUpper = !_wasAboveUpper && isAboveUpper;
		_wasBelowLower = isBelowLower;
		_wasAboveUpper = isAboveUpper;
		if (_cooldown > 0)
			_cooldown--;

		// Trading logic
		if (_cooldown == 0 && isBelowLower)
		{
			// Long signal: Price below lower band (mean - k*stdDev)
			if (Position <= 0)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (_cooldown == 0 && isAboveUpper)
		{
			// Short signal: Price above upper band (mean + k*stdDev)
			if (Position >= 0)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if ((Position > 0 && candle.ClosePrice > maValue) ||
		(Position < 0 && candle.ClosePrice < maValue))
		{
			// Exit signals: Price returned to the mean
			if (Position > 0)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
			else if (Position < 0)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}
	}
}
	
