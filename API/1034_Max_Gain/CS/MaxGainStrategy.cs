using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MaxGainStrategy : Strategy
{
	private readonly StrategyParam<int> _periodLength;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;

	public int PeriodLength { get => _periodLength.Value; set => _periodLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MaxGainStrategy()
	{
		_periodLength = Param(nameof(PeriodLength), 30);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highest = new Highest { Length = PeriodLength };
		_lowest = new Lowest { Length = PeriodLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maxHigh, decimal minLow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		if (minLow == 0 || maxHigh == 0)
			return;

		var maxGain = (candle.HighPrice - minLow) / minLow * 100m;
		var maxLoss = (candle.LowPrice - maxHigh) / maxHigh * -100m;

		if (maxLoss >= 100m) return;

		var adjustedMaxLoss = maxLoss / (100m - maxLoss) * 100m;

		if (maxGain > adjustedMaxLoss)
		{
			if (Position <= 0)
				BuyMarket();
		}
		else if (Position >= 0)
		{
			SellMarket();
		}
	}
}
