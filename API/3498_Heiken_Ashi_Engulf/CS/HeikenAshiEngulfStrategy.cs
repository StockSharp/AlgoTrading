namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Heiken Ashi Engulf strategy: WMA crossover with candle direction filter.
/// Buys when price crosses above WMA with bullish candle, sells on bearish cross below.
/// </summary>
public class HeikenAshiEngulfStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;

	private decimal _prevClose;
	private decimal _prevWma;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }

	public HeikenAshiEngulfStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_period = Param(nameof(Period), 20)
			.SetGreaterThanZero()
			.SetDisplay("Period", "WMA period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var wma = new WeightedMovingAverage { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(wma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal wmaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			var bullish = candle.ClosePrice > candle.OpenPrice;
			var bearish = candle.ClosePrice < candle.OpenPrice;

			if (_prevClose <= _prevWma && candle.ClosePrice > wmaValue && bullish && Position <= 0)
				BuyMarket();
			else if (_prevClose >= _prevWma && candle.ClosePrice < wmaValue && bearish && Position >= 0)
				SellMarket();
		}

		_prevClose = candle.ClosePrice;
		_prevWma = wmaValue;
		_hasPrev = true;
	}
}
