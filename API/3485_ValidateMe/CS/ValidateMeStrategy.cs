namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// ValidateMe strategy: Stochastic crossover.
/// Buys when %K crosses above %D below 20, sells when %K crosses below %D above 80.
/// </summary>
public class ValidateMeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;

	private decimal _prevK;
	private decimal _prevD;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }

	public ValidateMeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_period = Param(nameof(Period), 14)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Stochastic period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var stoch = new StochasticOscillator { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(stoch, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal kValue, decimal dValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			if (_prevK <= _prevD && kValue > dValue && kValue < 30 && Position <= 0)
				BuyMarket();
			else if (_prevK >= _prevD && kValue < dValue && kValue > 70 && Position >= 0)
				SellMarket();
		}

		_prevK = kValue;
		_prevD = dValue;
		_hasPrev = true;
	}
}
