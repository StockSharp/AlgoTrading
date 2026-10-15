using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on crossover of two smoothed moving averages with Fibonacci offset.
/// </summary>
public class FiboAvg001aStrategy : Strategy
{
	private readonly StrategyParam<int> _fiboNumPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	public int FiboNumPeriod { get => _fiboNumPeriod.Value; set => _fiboNumPeriod.Value = value; }
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FiboAvg001aStrategy()
	{
		_fiboNumPeriod = Param(nameof(FiboNumPeriod), 11)
			.SetGreaterThanZero()
			.SetDisplay("Fibo Period", "Additional length for slow MA", "Indicators");

		_maPeriod = Param(nameof(MaPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Base moving average period", "Indicators");

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

		var fastMa = new SmoothedMovingAverage { Length = MaPeriod };
		var slowMa = new SmoothedMovingAverage { Length = MaPeriod + FiboNumPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_hasPrev = true;
			return;
		}

		// Fast crosses above slow -> buy
		if (_prevFast <= _prevSlow && fast > slow)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		// Fast crosses below slow -> sell
		else if (_prevFast >= _prevSlow && fast < slow)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
