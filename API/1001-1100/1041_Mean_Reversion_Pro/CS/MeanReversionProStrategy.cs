using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MeanReversionProStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastSma;
	private SimpleMovingAverage _slowSma;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MeanReversionProStrategy()
	{
		_fastLength = Param(nameof(FastLength), 5);
		_slowLength = Param(nameof(SlowLength), 50);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastSma = new SimpleMovingAverage { Length = FastLength };
		_slowSma = new SimpleMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastSma, _slowSma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastSma.IsFormed || !_slowSma.IsFormed)
			return;

		var range = candle.HighPrice - candle.LowPrice;
		var longThreshold = candle.LowPrice + 0.2m * range;
		var shortThreshold = candle.HighPrice - 0.2m * range;

		var longSignal = candle.ClosePrice < fast &&
			candle.ClosePrice < longThreshold &&
			candle.ClosePrice > slow;

		var shortSignal = candle.ClosePrice > fast &&
			candle.ClosePrice > shortThreshold &&
			candle.ClosePrice < slow;

		var exitLong = candle.ClosePrice > fast && Position > 0;
		var exitShort = candle.ClosePrice < fast && Position < 0;

		if (longSignal && Position <= 0)
			BuyMarket();
		else if (shortSignal && Position >= 0)
			SellMarket();
		else if (exitLong)
			SellMarket();
		else if (exitShort)
			BuyMarket();
	}
}
