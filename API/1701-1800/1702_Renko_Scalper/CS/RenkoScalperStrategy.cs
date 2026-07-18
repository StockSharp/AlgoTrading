using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Renko scalper strategy.
/// Opens long when close moves significantly above previous close, short when below.
/// </summary>
public class RenkoScalperStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousClose;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RenkoScalperStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousClose = 0;
		_hasPrev = false;
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

		if (!_hasPrev)
		{
			_previousClose = close;
			_hasPrev = true;
			return;
		}

		if (stdevVal <= 0) { _previousClose = close; return; }

		var diff = close - _previousClose;

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

		_previousClose = close;
	}
}
