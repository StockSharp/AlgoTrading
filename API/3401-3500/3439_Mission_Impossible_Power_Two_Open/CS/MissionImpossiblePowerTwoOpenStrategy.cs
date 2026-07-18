namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Mission Impossible Power Two Open strategy: Candle direction with EMA trend filter.
/// Buys on bullish candle when above EMA, sells on bearish candle when below EMA.
/// </summary>
public class MissionImpossiblePowerTwoOpenStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private bool _wasBullishSignal;
	private bool _hasPrevSignal;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	public MissionImpossiblePowerTwoOpenStrategy()
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

		var close = candle.ClosePrice;
		var open = candle.OpenPrice;
		var bullish = close > open;
		var bearish = close < open;
		var bullishSignal = bullish && close > emaValue;
		var bearishSignal = bearish && close < emaValue;
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
