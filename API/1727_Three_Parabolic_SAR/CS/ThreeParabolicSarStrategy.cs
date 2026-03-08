using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR strategy using fast and slow SAR parameters.
/// Buys when price is above both SAR levels, sells when below both.
/// </summary>
public class ThreeParabolicSarStrategy : Strategy
{
	private readonly StrategyParam<decimal> _fastAcceleration;
	private readonly StrategyParam<decimal> _slowAcceleration;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevFastAbove;
	private bool _prevSlowAbove;
	private bool _hasPrev;

	public decimal FastAcceleration { get => _fastAcceleration.Value; set => _fastAcceleration.Value = value; }
	public decimal SlowAcceleration { get => _slowAcceleration.Value; set => _slowAcceleration.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ThreeParabolicSarStrategy()
	{
		_fastAcceleration = Param(nameof(FastAcceleration), 0.04m)
			.SetDisplay("Fast Acceleration", "Fast SAR acceleration", "SAR");

		_slowAcceleration = Param(nameof(SlowAcceleration), 0.01m)
			.SetDisplay("Slow Acceleration", "Slow SAR acceleration", "SAR");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFastAbove = false;
		_prevSlowAbove = false;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastSar = new ParabolicSar { Acceleration = FastAcceleration, AccelerationMax = 0.2m };
		var slowSar = new ParabolicSar { Acceleration = SlowAcceleration, AccelerationMax = 0.1m };

		SubscribeCandles(CandleType).Bind(fastSar, slowSar, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastSar, decimal slowSar)
	{
		if (candle.State != CandleStates.Finished) return;

		var fastAbove = candle.ClosePrice > fastSar;
		var slowAbove = candle.ClosePrice > slowSar;

		if (!_hasPrev)
		{
			_prevFastAbove = fastAbove;
			_prevSlowAbove = slowAbove;
			_hasPrev = true;
			return;
		}

		// Buy when both SAR levels flip bullish
		if (fastAbove && slowAbove && (!_prevFastAbove || !_prevSlowAbove) && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Sell when both SAR levels flip bearish
		else if (!fastAbove && !slowAbove && (_prevFastAbove || _prevSlowAbove) && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
		// Exit long if slow SAR turns bearish
		else if (Position > 0 && !slowAbove)
		{
			SellMarket();
		}
		// Exit short if slow SAR turns bullish
		else if (Position < 0 && slowAbove)
		{
			BuyMarket();
		}

		_prevFastAbove = fastAbove;
		_prevSlowAbove = slowAbove;
	}
}
