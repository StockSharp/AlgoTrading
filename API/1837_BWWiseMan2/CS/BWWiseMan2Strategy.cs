using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bill Williams Wise Man 2 strategy using Awesome Oscillator.
/// </summary>
public class BWWiseMan2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _ao0;
	private decimal _ao1;
	private decimal _ao2;
	private int _aoCount;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BWWiseMan2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_ao0 = 0;
		_ao1 = 0;
		_ao2 = 0;
		_aoCount = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ao = new AwesomeOscillator();

		SubscribeCandles(CandleType)
			.Bind(ao, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal aoValue)
	{
		if (candle.State != CandleStates.Finished) return;

		_ao2 = _ao1;
		_ao1 = _ao0;
		_ao0 = aoValue;

		if (_aoCount < 3)
		{
			_aoCount++;
			return;
		}

		var buySignal = (_ao2 < 0 && _ao1 < 0 && _ao0 > 0) || (_ao2 < _ao1 && _ao1 < _ao0 && _ao0 > 0);
		var sellSignal = (_ao2 > 0 && _ao1 > 0 && _ao0 < 0) || (_ao2 > _ao1 && _ao1 > _ao0 && _ao0 < 0);

		if (buySignal && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (sellSignal && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
	}
}
