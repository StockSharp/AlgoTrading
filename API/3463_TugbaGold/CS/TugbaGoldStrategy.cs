namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// TugbaGold strategy: candle direction + EMA trend filter with martingale-style averaging.
/// Buys on bullish candle above EMA, sells on bearish candle below EMA.
/// </summary>
public class TugbaGoldStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private bool _wasBullishSignal;
	private bool _hasPrevSignal;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	public TugbaGoldStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend filter period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_wasBullishSignal = false;
		_hasPrevSignal = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_wasBullishSignal = false;
		_hasPrevSignal = false;
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var bullish = candle.ClosePrice > candle.OpenPrice;
		var bearish = candle.ClosePrice < candle.OpenPrice;
		var bullishSignal = bullish && candle.ClosePrice > emaValue;
		var bearishSignal = bearish && candle.ClosePrice < emaValue;
		var crossedUp = bullishSignal && (!_hasPrevSignal || !_wasBullishSignal);
		var crossedDown = bearishSignal && (!_hasPrevSignal || _wasBullishSignal);

		if (crossedUp && Position <= 0)
			BuyMarket();
		else if (crossedDown && Position >= 0)
			SellMarket();

		if (bullishSignal || bearishSignal)
		{
			_wasBullishSignal = bullishSignal;
			_hasPrevSignal = true;
		}
	}
}
