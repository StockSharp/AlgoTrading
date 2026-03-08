using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR Alert Strategy.
/// Opens long or short positions when Parabolic SAR flips relative to price.
/// </summary>
public class ParabolicSarAlertStrategy : Strategy
{
	private readonly StrategyParam<decimal> _initialAcceleration;
	private readonly StrategyParam<decimal> _maxAcceleration;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSar;
	private decimal _prevClose;
	private bool _initialized;

	public decimal InitialAcceleration { get => _initialAcceleration.Value; set => _initialAcceleration.Value = value; }
	public decimal MaxAcceleration { get => _maxAcceleration.Value; set => _maxAcceleration.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ParabolicSarAlertStrategy()
	{
		_initialAcceleration = Param(nameof(InitialAcceleration), 0.02m)
			.SetDisplay("Initial Acceleration", "Initial acceleration factor for Parabolic SAR", "SAR Settings");
		_maxAcceleration = Param(nameof(MaxAcceleration), 0.2m)
			.SetDisplay("Max Acceleration", "Maximum acceleration factor for Parabolic SAR", "SAR Settings");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		var parabolicSar = new ParabolicSar
		{
			Acceleration = InitialAcceleration,
			AccelerationMax = MaxAcceleration
		};

		SubscribeCandles(CandleType)
			.Bind(parabolicSar, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_initialized)
		{
			_prevSar = sarValue;
			_prevClose = candle.ClosePrice;
			_initialized = true;
			return;
		}

		var crossUp = _prevSar > _prevClose && sarValue < candle.ClosePrice;
		var crossDown = _prevSar < _prevClose && sarValue > candle.ClosePrice;

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
		_prevClose = candle.ClosePrice;
	}
}
