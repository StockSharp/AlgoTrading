namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Stochastic Accelerator strategy: Rate of Change crossover.
/// Buys when ROC crosses above zero, sells when ROC crosses below zero.
/// </summary>
public class StochasticAcceleratorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;

	private decimal _prevRoc;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }

	public StochasticAcceleratorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_period = Param(nameof(Period), 12)
			.SetGreaterThanZero()
			.SetDisplay("Period", "ROC period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var roc = new RateOfChange { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(roc, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rocValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			if (_prevRoc <= 0 && rocValue > 0 && Position <= 0)
				BuyMarket();
			else if (_prevRoc >= 0 && rocValue < 0 && Position >= 0)
				SellMarket();
		}

		_prevRoc = rocValue;
		_hasPrev = true;
	}
}
