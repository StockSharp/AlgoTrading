namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Sample Trailing Stop strategy: Smoothed MA crossover.
/// Buys when close crosses above Smoothed MA, sells when crosses below.
/// </summary>
public class SampleTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;

	private decimal _prevClose;
	private decimal _prevSma;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }

	public SampleTrailingStopStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_period = Param(nameof(Period), 25)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Smoothed MA period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var smma = new SmoothedMovingAverage { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(smma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smmaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			if (_prevClose <= _prevSma && candle.ClosePrice > smmaValue && Position <= 0)
				BuyMarket();
			else if (_prevClose >= _prevSma && candle.ClosePrice < smmaValue && Position >= 0)
				SellMarket();
		}

		_prevClose = candle.ClosePrice;
		_prevSma = smmaValue;
		_hasPrev = true;
	}
}
