namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Basic ATR Stop Take strategy: EMA trend with ATR-based stop/take levels.
/// Enters on EMA direction, manages position with ATR-distance stops.
/// </summary>
public class BasicAtrStopTakeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _stopFactor;
	private readonly StrategyParam<decimal> _takeFactor;
	private readonly StrategyParam<int> _signalCooldownCandles;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private bool _prevAboveEma;
	private bool _hasPrevSignal;
	private int _candlesSinceTrade;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal StopFactor { get => _stopFactor.Value; set => _stopFactor.Value = value; }
	public decimal TakeFactor { get => _takeFactor.Value; set => _takeFactor.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public BasicAtrStopTakeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend period", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Indicators");
		_stopFactor = Param(nameof(StopFactor), 1.5m)
			.SetDisplay("Stop Factor", "ATR multiplier for stop loss", "Risk");
		_takeFactor = Param(nameof(TakeFactor), 2.0m)
			.SetDisplay("Take Factor", "ATR multiplier for take profit", "Risk");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 6)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between entries", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_prevAboveEma = false;
		_hasPrevSignal = false;
		_candlesSinceTrade = SignalCooldownCandles;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_entryPrice = 0;
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
		var aboveEma = close > ema;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		if (Position > 0 && _entryPrice > 0)
		{
			if (close <= _stopPrice || close >= _takePrice)
			{
				SellMarket();
				_entryPrice = 0;
				_stopPrice = 0;
				_takePrice = 0;
				_candlesSinceTrade = 0;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (close >= _stopPrice || close <= _takePrice)
			{
				BuyMarket();
				_entryPrice = 0;
				_stopPrice = 0;
				_takePrice = 0;
				_candlesSinceTrade = 0;
			}
		}

		if (Position == 0 && atr > 0 && _hasPrevSignal && aboveEma != _prevAboveEma && _candlesSinceTrade >= SignalCooldownCandles)
		{
			if (aboveEma)
			{
				BuyMarket();
				_entryPrice = close;
				_stopPrice = close - atr * StopFactor;
				_takePrice = close + atr * TakeFactor;
				_candlesSinceTrade = 0;
			}
			else
			{
				SellMarket();
				_entryPrice = close;
				_stopPrice = close + atr * StopFactor;
				_takePrice = close - atr * TakeFactor;
				_candlesSinceTrade = 0;
			}
		}

		_prevAboveEma = aboveEma;
		_hasPrevSignal = true;
	}
}
