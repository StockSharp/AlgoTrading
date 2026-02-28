namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Time Bomb strategy: Momentum zero-line crossover.
/// Buys when momentum crosses above zero, sells when momentum crosses below zero.
/// </summary>
public class TimeBombStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;

	private decimal _prevMom;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }

	public TimeBombStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_period = Param(nameof(Period), 10)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Momentum period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var mom = new Momentum { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(mom, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal momValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			if (_prevMom <= 0 && momValue > 0 && Position <= 0)
				BuyMarket();
			else if (_prevMom >= 0 && momValue < 0 && Position >= 0)
				SellMarket();
		}

		_prevMom = momValue;
		_hasPrev = true;
	}
}
