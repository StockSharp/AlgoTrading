using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with trailing stop management.
/// </summary>
public class CharlesSmaTrailingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isInitialized;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }

	public CharlesSmaTrailingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");

		_fastPeriod = Param(nameof(FastPeriod), 18)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Length of fast EMA", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 60)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Length of slow EMA", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0;
		_prevSlow = 0;
		_isInitialized = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowPeriod };

		SubscribeCandles(CandleType)
			.Bind(fastEma, slowEma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_isInitialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isInitialized = true;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;

		_prevFast = fast;
		_prevSlow = slow;

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
	}
}
