namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// FXF Safe Trend Scalp V1 strategy: SMA crossover with ATR volatility filter.
/// Enters long when fast SMA crosses above slow SMA and ATR confirms volatility.
/// Enters short when fast SMA crosses below slow SMA.
/// </summary>
public class FxfSafeTrendScalpV1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _signalCooldownCandles;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;
	private int _candlesSinceTrade;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public FxfSafeTrendScalpV1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast SMA period", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow SMA period", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Indicators");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 3)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between entries", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0m;
		_prevSlow = 0m;
		_hasPrev = false;
		_candlesSinceTrade = SignalCooldownCandles;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		_candlesSinceTrade = SignalCooldownCandles;
		var fast = new SimpleMovingAverage { Length = FastPeriod };
		var slow = new SimpleMovingAverage { Length = SlowPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, slow, atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		if (_hasPrev)
		{
			var close = candle.ClosePrice;
			var longSignal = _prevFast <= _prevSlow && fast > slow && close > slow + atr * 0.25m;
			var shortSignal = _prevFast >= _prevSlow && fast < slow && close < slow - atr * 0.25m;

			if (_candlesSinceTrade >= SignalCooldownCandles)
			{
				if (longSignal && Position <= 0)
				{
					BuyMarket();
					_candlesSinceTrade = 0;
				}
				else if (shortSignal && Position >= 0)
				{
					SellMarket();
					_candlesSinceTrade = 0;
				}
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
		_hasPrev = true;
	}
}
