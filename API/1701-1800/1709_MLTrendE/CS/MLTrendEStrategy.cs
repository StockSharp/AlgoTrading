using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Weighted moving average trend strategy.
/// Buys when close crosses above WMA, sells when below.
/// </summary>
public class MLTrendEStrategy : Strategy
{
	private readonly StrategyParam<int> _wmaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevWma;
	private bool _hasPrev;

	public int WmaPeriod { get => _wmaPeriod.Value; set => _wmaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MLTrendEStrategy()
	{
		_wmaPeriod = Param(nameof(WmaPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("WMA Length", "Weighted moving average period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0;
		_prevWma = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var wma = new WeightedMovingAverage { Length = WmaPeriod };
		SubscribeCandles(CandleType).Bind(wma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal wmaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevClose = close;
			_prevWma = wmaValue;
			_hasPrev = true;
			return;
		}

		// Cross above WMA
		if (_prevClose <= _prevWma && close > wmaValue && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Cross below WMA
		else if (_prevClose >= _prevWma && close < wmaValue && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevClose = close;
		_prevWma = wmaValue;
	}
}
