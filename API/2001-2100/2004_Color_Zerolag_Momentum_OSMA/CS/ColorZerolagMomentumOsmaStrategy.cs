using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the zero-lag momentum OSMA indicator.
/// Uses weighted momentum composite with EMA smoothing for trend detection.
/// </summary>
public class ColorZerolagMomentumOsmaStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevOsma;
	private decimal _prevPrevOsma;
	private int _count;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorZerolagMomentumOsmaStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast EMA period", "General");

		_slowPeriod = Param(nameof(SlowPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow EMA period", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle Type", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevOsma = 0;
		_prevPrevOsma = 0;
		_count = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = FastPeriod };
		var slow = new ExponentialMovingAverage { Length = SlowPeriod };

		SubscribeCandles(CandleType)
			.Bind(fast, slow, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var osma = fastValue - slowValue;
		_count++;

		if (_count < 3)
		{
			_prevPrevOsma = _prevOsma;
			_prevOsma = osma;
			return;
		}

		// Buy when OSMA turns up (was decreasing, now increasing)
		var turnUp = _prevOsma < _prevPrevOsma && osma > _prevOsma;
		// Sell when OSMA turns down (was increasing, now decreasing)
		var turnDown = _prevOsma > _prevPrevOsma && osma < _prevOsma;

		if (turnUp && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (turnDown && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevPrevOsma = _prevOsma;
		_prevOsma = osma;
	}
}
