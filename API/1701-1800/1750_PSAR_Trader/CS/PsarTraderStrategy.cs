using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR cross strategy.
/// Opens long when price moves above SAR and short when price moves below SAR.
/// </summary>
public class PsarTraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaxStep;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevPriceAboveSar;
	private bool _hasPrev;

	public decimal SarStep { get => _sarStep.Value; set => _sarStep.Value = value; }
	public decimal SarMaxStep { get => _sarMaxStep.Value; set => _sarMaxStep.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PsarTraderStrategy()
	{
		_sarStep = Param(nameof(SarStep), 0.001m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Step", "Acceleration factor for Parabolic SAR", "Parabolic SAR");

		_sarMaxStep = Param(nameof(SarMaxStep), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max Step", "Maximum acceleration factor", "Parabolic SAR");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevPriceAboveSar = false;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var parabolicSar = new ParabolicSar
		{
			AccelerationStep = SarStep,
			AccelerationMax = SarMaxStep
		};

		SubscribeCandles(CandleType).Bind(parabolicSar, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var isPriceAboveSar = candle.ClosePrice > sarValue;

		if (!_hasPrev)
		{
			_prevPriceAboveSar = isPriceAboveSar;
			_hasPrev = true;
			return;
		}

		if (_prevPriceAboveSar != isPriceAboveSar)
		{
			if (isPriceAboveSar && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			else if (!isPriceAboveSar && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prevPriceAboveSar = isPriceAboveSar;
	}
}
