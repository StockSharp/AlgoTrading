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
	private readonly StrategyParam<int> _signalCooldownCandles;

	private bool _prevAbove;
	private bool _hasPrev;
	private int _candlesSinceTrade;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public CrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaLength = Param(nameof(EmaLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period for cross detection", "Indicators");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 3)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between entries", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevAbove = false;
		_hasPrev = false;
		_candlesSinceTrade = SignalCooldownCandles;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		_candlesSinceTrade = SignalCooldownCandles;
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		var above = candle.OpenPrice > ema && candle.ClosePrice > ema;

		if (_hasPrev && _candlesSinceTrade >= SignalCooldownCandles)
		{
			if (above && !_prevAbove && Position <= 0)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
			else if (!above && _prevAbove && Position >= 0)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}
		}

		_prevAbove = above;
		_hasPrev = true;
	}
}
