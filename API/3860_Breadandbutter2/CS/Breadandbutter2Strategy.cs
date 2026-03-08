using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bread and Butter strategy - triple WMA crossover.
/// Buys when WMA(5) crosses above WMA(10) and WMA(10) is above WMA(15).
/// Sells when WMA(5) crosses below WMA(10) and WMA(10) is below WMA(15).
/// </summary>
public class Breadandbutter2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevWma5;
	private decimal _prevWma10;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Breadandbutter2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];
	protected override void OnReseted() { base.OnReseted(); _prevWma5 = 0m; _prevWma10 = 0m; _hasPrev = false; }

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var wma5 = new WeightedMovingAverage { Length = 5 };
		var wma10 = new WeightedMovingAverage { Length = 10 };
		var wma15 = new WeightedMovingAverage { Length = 15 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(wma5, wma10, wma15, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal wma5, decimal wma10, decimal wma15)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevWma5 = wma5;
			_prevWma10 = wma10;
			_hasPrev = true;
			return;
		}

		if (_prevWma5 <= _prevWma10 && wma5 > wma10 && wma10 > wma15 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (_prevWma5 >= _prevWma10 && wma5 < wma10 && wma10 < wma15 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevWma5 = wma5;
		_prevWma10 = wma10;
	}
}
