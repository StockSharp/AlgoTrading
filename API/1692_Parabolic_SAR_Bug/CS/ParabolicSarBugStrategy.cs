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
/// Buys when price crosses above SAR, sells when price crosses below.
/// </summary>
public class ParabolicSarBugStrategy : Strategy
{
	private readonly StrategyParam<decimal> _step;
	private readonly StrategyParam<decimal> _maxStep;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSar;
	private decimal _prevClose;
	private bool _initialized;

	public decimal Step { get => _step.Value; set => _step.Value = value; }
	public decimal MaxStep { get => _maxStep.Value; set => _maxStep.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ParabolicSarBugStrategy()
	{
		_step = Param(nameof(Step), 0.02m)
			.SetDisplay("Step", "Acceleration factor", "Indicator");

		_maxStep = Param(nameof(MaxStep), 0.2m)
			.SetDisplay("Max Step", "Maximum acceleration", "Indicator");

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

		var sar = new ParabolicSar
		{
			Acceleration = Step,
			AccelerationMax = MaxStep
		};

		SubscribeCandles(CandleType).Bind(sar, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (!_initialized)
		{
			_prevSar = sarValue;
			_prevClose = close;
			_initialized = true;
			return;
		}

		var crossUp = close > sarValue && _prevClose <= _prevSar;
		var crossDown = close < sarValue && _prevClose >= _prevSar;

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

		_prevSar = sarValue;
		_prevClose = close;
	}
}
