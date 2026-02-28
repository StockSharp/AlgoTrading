namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Alligator Candle Cross strategy: DEMA trend crossover.
/// Buys when close crosses above DEMA, sells when close crosses below.
/// </summary>
public class AlligatorCandleCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;

	private decimal _prevClose;
	private decimal _prevDema;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }

	public AlligatorCandleCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_period = Param(nameof(Period), 21)
			.SetGreaterThanZero()
			.SetDisplay("Period", "DEMA period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var dema = new DoubleExponentialMovingAverage { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(dema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal demaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			if (_prevClose <= _prevDema && candle.ClosePrice > demaValue && Position <= 0)
				BuyMarket();
			else if (_prevClose >= _prevDema && candle.ClosePrice < demaValue && Position >= 0)
				SellMarket();
		}

		_prevClose = candle.ClosePrice;
		_prevDema = demaValue;
		_hasPrev = true;
	}
}
