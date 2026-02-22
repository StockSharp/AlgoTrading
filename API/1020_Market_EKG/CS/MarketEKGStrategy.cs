using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MarketEKGStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _prev1;
	private ICandleMessage _prev2;
	private ICandleMessage _prev3;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MarketEKGStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prev1 = null;
		_prev2 = null;
		_prev3 = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prev1 != null && _prev2 != null && _prev3 != null)
		{
			var avgClose = (_prev3.ClosePrice + _prev2.ClosePrice) / 2m;
			var diffClose = avgClose - _prev1.ClosePrice;

			if (diffClose > 0 && Position <= 0)
				BuyMarket();
			else if (diffClose < 0 && Position >= 0)
				SellMarket();
		}

		_prev3 = _prev2;
		_prev2 = _prev1;
		_prev1 = candle;
	}
}
