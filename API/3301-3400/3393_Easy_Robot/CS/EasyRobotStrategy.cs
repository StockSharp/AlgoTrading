namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Easy Robot strategy: simple EMA + RSI trend following.
/// Buys when close above EMA and RSI above 50.
/// Sells when close below EMA and RSI below 50.
/// </summary>
public class EasyRobotStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private bool _wasBullish;
	private bool _hasPrevSignal;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	public EasyRobotStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period", "Indicators");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_wasBullish = false;
		_hasPrevSignal = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrevSignal = false;
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal rsi)
	{
		if (candle.State != CandleStates.Finished) return;
		var close = candle.ClosePrice;
		var isBullish = close > ema && rsi > 50;

		if (_hasPrevSignal && isBullish != _wasBullish)
		{
			if (isBullish && Position <= 0)
				BuyMarket();
			else if (!isBullish && close < ema && rsi < 50 && Position >= 0)
				SellMarket();
		}

		_wasBullish = isBullish;
		_hasPrevSignal = true;
	}
}
