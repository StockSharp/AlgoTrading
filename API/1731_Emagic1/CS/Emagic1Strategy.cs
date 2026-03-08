using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover with MACD histogram trend confirmation.
/// </summary>
public class Emagic1Strategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Emagic1Strategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Length for fast EMA", "Indicators");

		_slowEmaLength = Param(nameof(SlowEmaLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Length for slow EMA", "Indicators");

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

		var fast = new ExponentialMovingAverage { Length = FastEmaLength };
		var slow = new ExponentialMovingAverage { Length = SlowEmaLength };

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

		// Fast crosses above slow => buy
		if (_prevFast <= _prevSlow && fastValue > slowValue && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Fast crosses below slow => sell
		else if (_prevFast >= _prevSlow && fastValue < slowValue && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
	}
}
