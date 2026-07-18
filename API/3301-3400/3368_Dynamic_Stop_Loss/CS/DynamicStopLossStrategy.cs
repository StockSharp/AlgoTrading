namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Dynamic Stop Loss strategy: EMA trend with ATR-based dynamic stop management.
/// Enters on EMA trend direction, exits when price moves against by ATR distance.
/// </summary>
public class DynamicStopLossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _signalCooldownCandles;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private bool _prevAboveEma;
	private bool _hasPrevSignal;
	private int _candlesSinceTrade;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public DynamicStopLossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend period", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for stop distance", "Indicators");
		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for stop distance", "Risk");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 12)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between entries", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_stopPrice = 0m;
		_prevAboveEma = false;
		_hasPrevSignal = false;
		_candlesSinceTrade = SignalCooldownCandles;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_entryPrice = 0;
		_stopPrice = 0;
		_hasPrevSignal = false;
		_candlesSinceTrade = SignalCooldownCandles;
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal atr)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;
		var stopDist = atr * AtrMultiplier;
		var aboveEma = close > ema;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		if (Position > 0)
		{
			var newStop = close - stopDist;
			if (newStop > _stopPrice) _stopPrice = newStop;
			if (close <= _stopPrice)
			{
				SellMarket();
				_entryPrice = 0;
				_stopPrice = 0;
				_candlesSinceTrade = 0;
				_prevAboveEma = aboveEma;
				_hasPrevSignal = true;
				return;
			}
		}
		else if (Position < 0)
		{
			var newStop = close + stopDist;
			if (newStop < _stopPrice || _stopPrice == 0) _stopPrice = newStop;
			if (close >= _stopPrice)
			{
				BuyMarket();
				_entryPrice = 0;
				_stopPrice = 0;
				_candlesSinceTrade = 0;
				_prevAboveEma = aboveEma;
				_hasPrevSignal = true;
				return;
			}
		}

		if (_hasPrevSignal && aboveEma != _prevAboveEma && _candlesSinceTrade >= SignalCooldownCandles)
		{
			if (aboveEma && Position <= 0)
			{
				BuyMarket();
				_entryPrice = close;
				_stopPrice = close - stopDist;
				_candlesSinceTrade = 0;
			}
			else if (!aboveEma && Position >= 0)
			{
				SellMarket();
				_entryPrice = close;
				_stopPrice = close + stopDist;
				_candlesSinceTrade = 0;
			}
		}

		_prevAboveEma = aboveEma;
		_hasPrevSignal = true;
	}
}
