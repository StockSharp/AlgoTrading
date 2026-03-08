using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with Fibonacci-inspired targets.
/// </summary>
public class MacfiboStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MacfiboStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowLength = Param(nameof(SlowLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0;
		_prevSlow = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = FastLength };
		var slow = new ExponentialMovingAverage { Length = SlowLength };

		SubscribeCandles(CandleType).Bind(fast, slow, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			_hasPrev = true;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fastValue > slowValue;
		var crossDown = _prevFast >= _prevSlow && fastValue < slowValue;

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

		_prevFast = fastValue;
		_prevSlow = slowValue;
	}
}
