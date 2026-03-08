using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR strategy - opens long when price crosses above SAR, short when below.
/// </summary>
public class PsarBug6Strategy : Strategy
{
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSar;
	private decimal _prevClose;
	private bool _initialized;

	public decimal SarStep { get => _sarStep.Value; set => _sarStep.Value = value; }
	public decimal SarMax { get => _sarMax.Value; set => _sarMax.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PsarBug6Strategy()
	{
		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetDisplay("SAR Step", "Acceleration factor step", "Indicator");

		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetDisplay("SAR Max", "Maximum acceleration factor", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevSar = 0;
		_prevClose = 0;
		_initialized = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var psar = new ParabolicSar
		{
			AccelerationStep = SarStep,
			AccelerationMax = SarMax
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(psar, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sar)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_initialized)
		{
			_prevSar = sar;
			_prevClose = candle.ClosePrice;
			_initialized = true;
			return;
		}

		var close = candle.ClosePrice;
		var crossUp = close > sar && _prevClose <= _prevSar;
		var crossDown = close < sar && _prevClose >= _prevSar;

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

		_prevSar = sar;
		_prevClose = close;
	}
}
