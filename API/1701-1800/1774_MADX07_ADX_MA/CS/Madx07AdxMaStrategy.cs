using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining EMA crossover for trend following.
/// </summary>
public class Madx07AdxMaStrategy : Strategy
{
	private readonly StrategyParam<int> _bigMaPeriod;
	private readonly StrategyParam<int> _smallMaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSmall;
	private decimal _prevBig;
	private bool _hasPrev;

	public int BigMaPeriod { get => _bigMaPeriod.Value; set => _bigMaPeriod.Value = value; }
	public int SmallMaPeriod { get => _smallMaPeriod.Value; set => _smallMaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Madx07AdxMaStrategy()
	{
		_bigMaPeriod = Param(nameof(BigMaPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("Big MA Period", "Period of the slower MA", "MA");

		_smallMaPeriod = Param(nameof(SmallMaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Small MA Period", "Period of the faster MA", "MA");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevSmall = 0;
		_prevBig = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bigMa = new ExponentialMovingAverage { Length = BigMaPeriod };
		var smallMa = new ExponentialMovingAverage { Length = SmallMaPeriod };

		SubscribeCandles(CandleType)
			.Bind(bigMa, smallMa, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal bigMaVal, decimal smallMaVal)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev)
		{
			_prevSmall = smallMaVal;
			_prevBig = bigMaVal;
			_hasPrev = true;
			return;
		}

		var crossUp = _prevSmall <= _prevBig && smallMaVal > bigMaVal;
		var crossDown = _prevSmall >= _prevBig && smallMaVal < bigMaVal;

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

		_prevSmall = smallMaVal;
		_prevBig = bigMaVal;
	}
}
