namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// AutoTrading Scheduler strategy: Momentum indicator crossover.
/// Buys when Momentum crosses above 100, sells when crosses below 100.
/// </summary>
public class AutoTradingSchedulerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _momentumPeriod;

	private decimal _prevMom;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MomentumPeriod { get => _momentumPeriod.Value; set => _momentumPeriod.Value = value; }

	public AutoTradingSchedulerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Momentum period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var momentum = new Momentum { Length = MomentumPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(momentum, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal momValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			if (_prevMom < 100 && momValue >= 100 && Position <= 0)
				BuyMarket();
			else if (_prevMom > 100 && momValue <= 100 && Position >= 0)
				SellMarket();
		}

		_prevMom = momValue;
		_hasPrev = true;
	}
}
