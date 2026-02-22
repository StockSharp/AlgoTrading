using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Reflected EMA Difference (RED) strategy.
/// Compares two EMAs and trades based on the reflected difference crossing zero.
/// </summary>
public class ReflectedEmaDifferenceRedStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevDiff;
	private bool _hasPrev;

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ReflectedEmaDifferenceRedStrategy()
	{
		_fastLength = Param(nameof(FastLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "Parameters");

		_slowLength = Param(nameof(SlowLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastEma, slowEma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var diff = fastValue - slowValue;

		if (_hasPrev)
		{
			if (diff > 0 && _prevDiff <= 0 && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (diff < 0 && _prevDiff >= 0 && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
		}

		_prevDiff = diff;
		_hasPrev = true;
	}
}
