using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Compares the close of the last finished candle with the open of the prior candle.
/// Buys when the latest close is significantly above the previous open, sells when below.
/// </summary>
public class CloseVsPreviousOpenStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevOpen;
	private decimal _prevPrevOpen;
	private decimal _prevClose;
	private int _barCount;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public CloseVsPreviousOpenStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevOpen = 0; _prevPrevOpen = 0; _prevClose = 0; _barCount = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stdev = new StandardDeviation { Length = 20 };

		SubscribeCandles(CandleType).Bind(stdev, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal stdevVal)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;
		var open = candle.OpenPrice;
		_barCount++;

		if (_barCount >= 3 && stdevVal > 0)
		{
			var diff = _prevClose - _prevPrevOpen;

			// Only trade on significant moves (> 1 stdev)
			if (diff > stdevVal && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			else if (diff < -stdevVal && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prevPrevOpen = _prevOpen;
		_prevOpen = open;
		_prevClose = close;
	}
}
