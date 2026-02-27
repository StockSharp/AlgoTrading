namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Cross strategy: opens long when candle open crosses above EMA,
/// short when candle open crosses below EMA.
/// </summary>
public class CrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;

	private bool _prevAbove;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

	public CrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaLength = Param(nameof(EmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period for cross detection", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished) return;

		var above = candle.OpenPrice > ema;

		if (_hasPrev)
		{
			if (above && !_prevAbove && Position <= 0)
				BuyMarket();
			else if (!above && _prevAbove && Position >= 0)
				SellMarket();
		}

		_prevAbove = above;
		_hasPrev = true;
	}
}
