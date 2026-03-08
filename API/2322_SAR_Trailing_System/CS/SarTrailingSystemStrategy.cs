using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that uses Parabolic SAR for trend-following entries and exits.
/// Buy when price crosses above SAR, sell when below.
/// </summary>
public class SarTrailingSystemStrategy : Strategy
{
	private readonly StrategyParam<decimal> _accelerationStep;
	private readonly StrategyParam<decimal> _accelerationMax;
	private readonly StrategyParam<DataType> _candleType;

	public decimal AccelerationStep { get => _accelerationStep.Value; set => _accelerationStep.Value = value; }
	public decimal AccelerationMax { get => _accelerationMax.Value; set => _accelerationMax.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SarTrailingSystemStrategy()
	{
		_accelerationStep = Param(nameof(AccelerationStep), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Step", "Parabolic SAR acceleration step", "Indicators");

		_accelerationMax = Param(nameof(AccelerationMax), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max", "Parabolic SAR maximum acceleration", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sar = new ParabolicSar
		{
			Acceleration = AccelerationStep,
			AccelerationStep = AccelerationStep,
			AccelerationMax = AccelerationMax
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sar, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Price above SAR = uptrend, buy
		if (candle.ClosePrice > sarValue && Position <= 0)
			BuyMarket();
		// Price below SAR = downtrend, sell
		else if (candle.ClosePrice < sarValue && Position >= 0)
			SellMarket();
	}
}
