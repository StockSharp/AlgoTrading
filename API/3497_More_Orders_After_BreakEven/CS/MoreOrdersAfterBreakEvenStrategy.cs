namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// More Orders After BreakEven strategy: TEMA crossover.
/// Buys when price crosses above TEMA, sells when price crosses below TEMA.
/// </summary>
public class MoreOrdersAfterBreakEvenStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;

	private decimal _prevClose;
	private decimal _prevTema;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }

	public MoreOrdersAfterBreakEvenStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_period = Param(nameof(Period), 14)
			.SetGreaterThanZero()
			.SetDisplay("Period", "TEMA period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var tema = new TripleExponentialMovingAverage { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(tema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal temaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			if (_prevClose <= _prevTema && candle.ClosePrice > temaValue && Position <= 0)
				BuyMarket();
			else if (_prevClose >= _prevTema && candle.ClosePrice < temaValue && Position >= 0)
				SellMarket();
		}

		_prevClose = candle.ClosePrice;
		_prevTema = temaValue;
		_hasPrev = true;
	}
}
