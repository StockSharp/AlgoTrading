using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Three moving averages crossover strategy.
/// </summary>
public class Up3x1KrohaborDStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _middlePeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevMiddle;
	private bool _isInitialized;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int MiddlePeriod { get => _middlePeriod.Value; set => _middlePeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Up3x1KrohaborDStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast EMA period", "MA Settings");

		_middlePeriod = Param(nameof(MiddlePeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Middle Period", "Middle EMA period", "MA Settings");

		_slowPeriod = Param(nameof(SlowPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow EMA period", "MA Settings");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0;
		_prevMiddle = 0;
		_isInitialized = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastMa = new ExponentialMovingAverage { Length = FastPeriod };
		var middleMa = new ExponentialMovingAverage { Length = MiddlePeriod };
		var slowMa = new ExponentialMovingAverage { Length = SlowPeriod };

		SubscribeCandles(CandleType)
			.Bind(fastMa, middleMa, slowMa, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal middle, decimal slow)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_isInitialized)
		{
			_prevFast = fast;
			_prevMiddle = middle;
			_isInitialized = true;
			return;
		}

		var crossUp = _prevFast <= _prevMiddle && fast > middle;
		var crossDown = _prevFast >= _prevMiddle && fast < middle;

		_prevFast = fast;
		_prevMiddle = middle;

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
