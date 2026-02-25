using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fractal AMA MBK crossover strategy.
/// Uses FRAMA and a signal EMA to generate trade signals on crossover.
/// </summary>
public class FractalAmaMbkStrategy : Strategy
{
	private readonly StrategyParam<int> _framaPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFrama;
	private decimal _prevSignal;
	private bool _isFirst = true;

	public int FramaPeriod { get => _framaPeriod.Value; set => _framaPeriod.Value = value; }
	public int SignalPeriod { get => _signalPeriod.Value; set => _signalPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FractalAmaMbkStrategy()
	{
		_framaPeriod = Param(nameof(FramaPeriod), 16)
			.SetGreaterThanZero()
			.SetDisplay("FRAMA Period", "Period for Fractal Adaptive Moving Average", "Indicator");

		_signalPeriod = Param(nameof(SignalPeriod), 16)
			.SetGreaterThanZero()
			.SetDisplay("Signal EMA Period", "Period for signal EMA", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_isFirst = true;

		var frama = new FractalAdaptiveMovingAverage { Length = FramaPeriod };
		var signal = new ExponentialMovingAverage { Length = SignalPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(frama, signal, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, frama);
			DrawIndicator(area, signal);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal framaValue, decimal signalValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_isFirst)
		{
			_prevFrama = framaValue;
			_prevSignal = signalValue;
			_isFirst = false;
			return;
		}

		// Detect crossover
		var wasAbove = _prevFrama > _prevSignal;
		var isAbove = framaValue > signalValue;

		if (!wasAbove && isAbove && Position <= 0)
		{
			// FRAMA crossed above signal -> buy
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (wasAbove && !isAbove && Position >= 0)
		{
			// FRAMA crossed below signal -> sell
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevFrama = framaValue;
		_prevSignal = signalValue;
	}
}
