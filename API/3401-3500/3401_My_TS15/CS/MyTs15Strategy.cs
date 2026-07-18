namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// My TS15 strategy: WMA trend following with trailing stop management.
/// Enters on price crossing WMA, exits with trailing stop logic.
/// </summary>
public class MyTs15Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _trailMultiplier;
	private readonly StrategyParam<int> _signalCooldownCandles;

	private decimal _entryPrice;
	private decimal _bestPrice;
	private bool _wasBullish;
	private bool _hasPrevSignal;
	private int _candlesSinceTrade;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal TrailMultiplier { get => _trailMultiplier.Value; set => _trailMultiplier.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public MyTs15Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(120).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_maPeriod = Param(nameof(MaPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "WMA period", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for trailing", "Indicators");
		_trailMultiplier = Param(nameof(TrailMultiplier), 3m)
			.SetDisplay("Trail Multiplier", "ATR multiplier for trailing stop", "Risk");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 12)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_bestPrice = 0m;
		_wasBullish = false;
		_hasPrevSignal = false;
		_candlesSinceTrade = SignalCooldownCandles;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_entryPrice = 0;
		_bestPrice = 0;
		_hasPrevSignal = false;
		_candlesSinceTrade = SignalCooldownCandles;
		var wma = new WeightedMovingAverage { Length = MaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(wma, atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal wmaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;
		var trailDist = atrValue * TrailMultiplier;
		var isBullish = close > wmaValue;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		// Trailing stop check
		if (Position > 0)
		{
			if (close > _bestPrice) _bestPrice = close;
			if (_bestPrice - close > trailDist)
			{
				SellMarket();
				_entryPrice = 0;
				_bestPrice = 0;
				_candlesSinceTrade = 0;
				return;
			}
		}
		else if (Position < 0)
		{
			if (close < _bestPrice) _bestPrice = close;
			if (close - _bestPrice > trailDist)
			{
				BuyMarket();
				_entryPrice = 0;
				_bestPrice = 0;
				_candlesSinceTrade = 0;
				return;
			}
		}

		// Entry signals
		if (_hasPrevSignal && isBullish != _wasBullish && _candlesSinceTrade >= SignalCooldownCandles)
		{
			if (isBullish && Position <= 0)
			{
				BuyMarket();
				_entryPrice = close;
				_bestPrice = close;
				_candlesSinceTrade = 0;
			}
			else if (!isBullish && Position >= 0)
			{
				SellMarket();
				_entryPrice = close;
				_bestPrice = close;
				_candlesSinceTrade = 0;
			}
		}

		_wasBullish = isBullish;
		_hasPrevSignal = true;
	}
}
