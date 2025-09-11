using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gauge strategy that trades based on price position within a predefined range.
/// </summary>
public class GuageStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _minValue;
	private readonly StrategyParam<decimal> _maxValue;
	private readonly StrategyParam<decimal> _upperThreshold;
	private readonly StrategyParam<decimal> _lowerThreshold;

	public GuageStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "Data");
		_minValue = Param(nameof(MinValue), 0m)
			.SetDisplay("Min Value", "Minimum gauge value", "General")
			.SetCanOptimize(true)
			.SetOptimize(-100m, 100m, 10m);
		_maxValue = Param(nameof(MaxValue), 100m)
			.SetDisplay("Max Value", "Maximum gauge value", "General")
			.SetCanOptimize(true)
			.SetOptimize(0m, 200m, 10m);
		_upperThreshold = Param(nameof(UpperThreshold), 0.75m)
			.SetDisplay("Upper Threshold", "Gauge percentage to enter long", "Entry Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.6m, 0.9m, 0.05m);
		_lowerThreshold = Param(nameof(LowerThreshold), 0.25m)
			.SetDisplay("Lower Threshold", "Gauge percentage to enter short", "Entry Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.4m, 0.05m);
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal MinValue
	{
		get => _minValue.Value;
		set => _minValue.Value = value;
	}

	public decimal MaxValue
	{
		get => _maxValue.Value;
		set => _maxValue.Value = value;
	}

	public decimal UpperThreshold
	{
		get => _upperThreshold.Value;
		set => _upperThreshold.Value = value;
	}

	public decimal LowerThreshold
	{
		get => _lowerThreshold.Value;
		set => _lowerThreshold.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (MaxValue <= MinValue)
			return;

		var value = candle.ClosePrice;
		var ratio = (value - MinValue) / (MaxValue - MinValue);

		if (ratio > UpperThreshold && Position <= 0)
			BuyMarket();
		else if (ratio < LowerThreshold && Position >= 0)
			SellMarket();

		AddInfoLog($"Gauge ratio: {ratio:P2}");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
}

