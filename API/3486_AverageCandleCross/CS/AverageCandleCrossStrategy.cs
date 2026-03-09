namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Average Candle Cross strategy: price crossover above/below SMA.
/// Buys when close crosses above SMA, sells when close crosses below SMA.
/// </summary>
public class AverageCandleCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;

	private decimal _prevClose;
	private decimal _prevSma;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }

	public AverageCandleCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_period = Param(nameof(Period), 20)
			.SetGreaterThanZero()
			.SetDisplay("Period", "SMA period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0;
		_prevSma = 0;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var sma = new SimpleMovingAverage { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			if (_prevClose <= _prevSma && candle.ClosePrice > smaValue && Position <= 0)
				BuyMarket();
			else if (_prevClose >= _prevSma && candle.ClosePrice < smaValue && Position >= 0)
				SellMarket();
		}

		_prevClose = candle.ClosePrice;
		_prevSma = smaValue;
		_hasPrev = true;
	}
}
