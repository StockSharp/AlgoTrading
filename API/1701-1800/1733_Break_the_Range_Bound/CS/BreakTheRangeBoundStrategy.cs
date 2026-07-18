using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that waits for converging SMAs and trades on breakout.
/// </summary>
public class BreakTheRangeBoundStrategy : Strategy
{
	private readonly StrategyParam<int> _fastSma;
	private readonly StrategyParam<int> _slowSma;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevClose;
	private bool _hasPrev;

	public int FastSma { get => _fastSma.Value; set => _fastSma.Value = value; }
	public int SlowSma { get => _slowSma.Value; set => _slowSma.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BreakTheRangeBoundStrategy()
	{
		_fastSma = Param(nameof(FastSma), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Fast moving average period", "Parameters");

		_slowSma = Param(nameof(SlowSma), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Slow moving average period", "Parameters");

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
		_prevClose = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = FastSma };
		var slow = new ExponentialMovingAverage { Length = SlowSma };

		SubscribeCandles(CandleType).Bind(fast, slow, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			_prevClose = close;
			_hasPrev = true;
			return;
		}

		// Cross above slow SMA => buy breakout
		if (_prevClose <= _prevSlow && close > slowValue && fastValue > slowValue && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Cross below slow SMA => sell breakout
		else if (_prevClose >= _prevSlow && close < slowValue && fastValue < slowValue && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
		_prevClose = close;
	}
}
