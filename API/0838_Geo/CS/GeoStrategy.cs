using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy trading when candle high/low ratio is near the golden ratio.
/// </summary>
public class GeoStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tolerance;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Golden ratio tolerance in percent.
	/// </summary>
	public decimal Tolerance
	{
	get => _tolerance.Value;
	set => _tolerance.Value = value;
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
	/// Initializes strategy parameters.
	/// </summary>
	public GeoStrategy()
	{
	_tolerance = Param(nameof(Tolerance), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Tolerance", "Tolerance percent for phi ratio", "General")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 5m, 0.5m);

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

	StartProtection();

	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
	if (candle.State != CandleStates.Finished)
		return;

	if (!IsFormedAndOnlineAndAllowTrading())
		return;

	if (candle.LowPrice <= 0)
		return;

	var phiMatch = IsPhiRatio(candle.LowPrice, candle.HighPrice, Tolerance);

	if (phiMatch)
	{
		if (Position <= 0)
		BuyMarket();
	}
	else
	{
		if (Position >= 0)
		SellMarket();
	}
	}

	private static bool IsPhiRatio(decimal a, decimal b, decimal tolerance)
	{
	var loPhi = Phi * (1 - tolerance / 100m);
	var hiPhi = Phi * (1 + tolerance / 100m);
	var r = b / a;
	return r > loPhi && r < hiPhi;
	}

	private static decimal Gap(decimal value1, decimal value2)
	{
	return Math.Abs(value1 - value2);
	}

	private const decimal Phi = 1.61803398875m;
}
