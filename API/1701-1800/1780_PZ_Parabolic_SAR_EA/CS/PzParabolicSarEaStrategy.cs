using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR crossover strategy.
/// </summary>
public class PzParabolicSarEaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevSar;
	private bool _hasPrev;

	public decimal SarStep { get => _sarStep.Value; set => _sarStep.Value = value; }
	public decimal SarMax { get => _sarMax.Value; set => _sarMax.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PzParabolicSarEaStrategy()
	{
		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetDisplay("SAR Step", "Acceleration step", "Indicators");
		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetDisplay("SAR Max", "Maximum acceleration", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0;
		_prevSar = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sar = new ParabolicSar { AccelerationStep = SarStep, AccelerationMax = SarMax };

		SubscribeCandles(CandleType)
			.Bind(sar, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarVal)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevClose = close;
			_prevSar = sarVal;
			_hasPrev = true;
			return;
		}

		var crossUp = _prevClose <= _prevSar && close > sarVal;
		var crossDown = _prevClose >= _prevSar && close < sarVal;

		if (crossUp && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (crossDown && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevClose = close;
		_prevSar = sarVal;
	}
}
