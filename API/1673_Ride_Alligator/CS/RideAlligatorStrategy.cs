using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ride Alligator strategy using SMMA jaw/teeth/lips crossover.
/// </summary>
public class RideAlligatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevJaw;
	private decimal _prevLips;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RideAlligatorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevJaw = 0;
		_prevLips = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var jaw = new SmoothedMovingAverage { Length = 13 };
		var teeth = new SmoothedMovingAverage { Length = 8 };
		var lips = new SmoothedMovingAverage { Length = 5 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(jaw, teeth, lips, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal jaw, decimal teeth, decimal lips)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevJaw = jaw;
			_prevLips = lips;
			_hasPrev = true;
			return;
		}

		// Lips crosses above jaw -> buy
		if (_prevLips <= _prevJaw && lips > jaw && teeth < jaw)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		// Lips crosses below jaw -> sell
		else if (_prevLips >= _prevJaw && lips < jaw && teeth > jaw)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}

		// Exit on price crossing jaw
		if (Position > 0 && candle.ClosePrice < jaw)
			SellMarket();
		else if (Position < 0 && candle.ClosePrice > jaw)
			BuyMarket();

		_prevJaw = jaw;
		_prevLips = lips;
	}
}
