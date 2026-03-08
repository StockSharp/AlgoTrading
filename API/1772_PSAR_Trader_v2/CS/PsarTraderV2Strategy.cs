using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR trader v2 with reversal logic.
/// </summary>
public class PsarTraderV2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _step;
	private readonly StrategyParam<decimal> _maximum;

	private decimal _prevSar;
	private bool _prevPriceAboveSar;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal Step { get => _step.Value; set => _step.Value = value; }
	public decimal Maximum { get => _maximum.Value; set => _maximum.Value = value; }

	public PsarTraderV2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Trading timeframe", "General");

		_step = Param(nameof(Step), 0.001m)
			.SetDisplay("PSAR Step", "Acceleration step for PSAR", "Indicators");

		_maximum = Param(nameof(Maximum), 0.2m)
			.SetDisplay("PSAR Maximum", "Maximum acceleration for PSAR", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevSar = 0;
		_prevPriceAboveSar = false;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var psar = new ParabolicSar
		{
			AccelerationStep = Step,
			AccelerationMax = Maximum
		};

		SubscribeCandles(CandleType).Bind(psar, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sar)
	{
		if (candle.State != CandleStates.Finished) return;

		var priceAboveSar = candle.ClosePrice > sar;

		if (!_hasPrev)
		{
			_prevSar = sar;
			_prevPriceAboveSar = priceAboveSar;
			_hasPrev = true;
			return;
		}

		if (priceAboveSar != _prevPriceAboveSar)
		{
			if (priceAboveSar && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			else if (!priceAboveSar && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prevSar = sar;
		_prevPriceAboveSar = priceAboveSar;
	}
}
