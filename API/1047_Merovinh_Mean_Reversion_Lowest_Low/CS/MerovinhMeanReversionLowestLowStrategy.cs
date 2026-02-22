using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MerovinhMeanReversionLowestLowStrategy : Strategy
{
	private readonly StrategyParam<int> _bars;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private decimal _prevLow;
	private decimal _prevHigh;

	public int Bars { get => _bars.Value; set => _bars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MerovinhMeanReversionLowestLowStrategy()
	{
		_bars = Param(nameof(Bars), 9);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highest = new Highest { Length = Bars };
		_lowest = new Lowest { Length = Bars };
		_prevLow = 0;
		_prevHigh = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_highest, _lowest, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestHigh, decimal lowestLow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
		{
			_prevLow = lowestLow;
			_prevHigh = highestHigh;
			return;
		}

		if (_prevLow > 0 && lowestLow < _prevLow && Position <= 0)
			BuyMarket();

		if (_prevHigh > 0 && highestHigh > _prevHigh && Position > 0)
			SellMarket();

		_prevLow = lowestLow;
		_prevHigh = highestHigh;
	}
}
