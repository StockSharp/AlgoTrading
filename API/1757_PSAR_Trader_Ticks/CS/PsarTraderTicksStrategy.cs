using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// PSAR Trader strategy - opens long when price crosses above SAR
/// and short when price crosses below SAR.
/// </summary>
public class PsarTraderTicksStrategy : Strategy
{
	private readonly StrategyParam<decimal> _step;
	private readonly StrategyParam<decimal> _maximum;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSar;
	private decimal _prevPrice;
	private bool _hasPrev;

	public decimal Step { get => _step.Value; set => _step.Value = value; }
	public decimal Maximum { get => _maximum.Value; set => _maximum.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PsarTraderTicksStrategy()
	{
		_step = Param(nameof(Step), 0.001m)
			.SetDisplay("SAR Step", "Acceleration factor step", "Indicators");
		_maximum = Param(nameof(Maximum), 0.2m)
			.SetDisplay("SAR Maximum", "Maximum acceleration factor", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevSar = 0;
		_prevPrice = 0;
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

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev)
		{
			_prevSar = sarValue;
			_prevPrice = candle.ClosePrice;
			_hasPrev = true;
			return;
		}

		var prevAbove = _prevPrice > _prevSar;
		var currAbove = candle.ClosePrice > sarValue;

		if (currAbove && !prevAbove && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (!currAbove && prevAbove && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevSar = sarValue;
		_prevPrice = candle.ClosePrice;
	}
}
