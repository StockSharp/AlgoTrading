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
/// Stochastic Z-Score Oscillator Strategy.
/// Combines rescaled Stochastic %K and price Z-Score with cooldown filters.
/// </summary>
public class StochasticZScoreOscillatorStrategy : Strategy
	{
	private readonly StrategyParam<int> _rollingWindow;
	private readonly StrategyParam<decimal> _zThreshold;
	private readonly StrategyParam<int> _coolDown;
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<int> _stochSmooth;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _rollingMean;
	private StandardDeviation _rollingStdDev;
	private int _buyCooldownCounter;
	private int _sellCooldownCounter;

/// <summary>
/// Rolling window length.
/// </summary>
	public int RollingWindow
	{
	get => _rollingWindow.Value;
	set => _rollingWindow.Value = value;
}

/// <summary>
/// Z-Score threshold.
/// </summary>
	public decimal ZThreshold
	{
	get => _zThreshold.Value;
	set => _zThreshold.Value = value;
}

/// <summary>
/// Signal cool down period.
/// </summary>
	public int CoolDown
	{
	get => _coolDown.Value;
	set => _coolDown.Value = value;
}

/// <summary>
/// Stochastic length.
/// </summary>
	public int StochLength
	{
	get => _stochLength.Value;
	set => _stochLength.Value = value;
}

/// <summary>
/// Stochastic smoothing period.
/// </summary>
	public int StochSmooth
	{
	get => _stochSmooth.Value;
	set => _stochSmooth.Value = value;
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
/// Initialize <see cref="StochasticZScoreOscillatorStrategy"/>.
/// </summary>
	public StochasticZScoreOscillatorStrategy()
	{
	_rollingWindow = Param(nameof(RollingWindow), 80)
	.SetGreaterThanZero()
	.SetDisplay("Rolling Window", "Length of rolling window", "Parameters")
	.SetCanOptimize(true)
	.SetOptimize(40, 120, 20);

	_zThreshold = Param(nameof(ZThreshold), 2.8m)
	.SetGreaterThanZero()
	.SetDisplay("Z Threshold", "Z-score threshold", "Parameters")
	.SetCanOptimize(true)
	.SetOptimize(1m, 4m, 0.5m);

	_coolDown = Param(nameof(CoolDown), 5)
	.SetGreaterThanZero()
	.SetDisplay("Cool Down", "Signal cool down period", "Parameters")
	.SetCanOptimize(true)
	.SetOptimize(1, 10, 1);

	_stochLength = Param(nameof(StochLength), 14)
	.SetGreaterThanZero()
	.SetDisplay("Stochastic Length", "Length for Stochastic", "Stochastic Settings");

	_stochSmooth = Param(nameof(StochSmooth), 7)
	.SetGreaterThanZero()
	.SetDisplay("Stochastic Smooth", "Smoothing for Stochastic", "Stochastic Settings");

	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
	.SetDisplay("Candle Type", "Type of candles", "Parameters");
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

	_buyCooldownCounter = CoolDown;
	_sellCooldownCounter = CoolDown;
}

/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_rollingMean = new SimpleMovingAverage { Length = RollingWindow };
	_rollingStdDev = new StandardDeviation { Length = RollingWindow };
	var stochastic = new StochasticOscillator
	{
	Length = StochLength,
	KPeriod = StochSmooth,
	DPeriod = 1
};

	var subscription = SubscribeCandles(CandleType);
	subscription
	.BindEx(stochastic, ProcessCandle)
	.Start();

	StartProtection();

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
	if (candle.State != CandleStates.Finished)
	return;

	var stochTyped = (StochasticOscillatorValue)stochValue;

	if (stochTyped.K is not decimal stochK)
	return;

	var stochRescaled = (stochK / 100m) * 8m - 4m;
	var meanValue = _rollingMean.Process(candle.ClosePrice, candle.ServerTime, true);
	var stdValue = _rollingStdDev.Process(candle.ClosePrice, candle.ServerTime, true);

	if (!IsFormedAndOnlineAndAllowTrading() || !_rollingMean.IsFormed || !_rollingStdDev.IsFormed)
	return;

	var zScore = (candle.ClosePrice - meanValue.ToDecimal()) / stdValue.ToDecimal();
	var combined = (zScore + stochRescaled) / 2m;

	if (combined > ZThreshold)
	{
	if (_sellCooldownCounter >= CoolDown)
	{
	if (Position >= 0)
	SellMarket(Volume);

	_sellCooldownCounter = 0;
	_buyCooldownCounter = CoolDown;
}
	else
	{
	_sellCooldownCounter++;
}
}

	if (zScore > 0 && Position > 0)
	SellMarket(Position);

	else if (combined < -ZThreshold)
	{
	if (_buyCooldownCounter >= CoolDown)
	{
	if (Position <= 0)
	BuyMarket(Volume);

	_buyCooldownCounter = 0;
	_sellCooldownCounter = CoolDown;
}
	else
	{
	_buyCooldownCounter++;
}
}

	if (zScore < 0 && Position < 0)
	BuyMarket(-Position);
}
}
