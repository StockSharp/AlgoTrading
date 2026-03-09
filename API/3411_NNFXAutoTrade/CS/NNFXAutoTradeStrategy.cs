namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// NNFX Auto Trade strategy: ATR-based trend following with EMA filter.
/// Enters on EMA direction with ATR-based trailing stop management.
/// </summary>
public class NnfxAutoTradeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _signalCooldownCandles;

	private decimal _entryPrice;
	private decimal _bestPrice;
	private bool _wasBullish;
	private bool _hasPrevSignal;
	private int _candlesSinceTrade;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public NnfxAutoTradeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(120).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend filter period", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Indicators");
		_atrMultiplier = Param(nameof(AtrMultiplier), 2.5m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for stop", "Risk");
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
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;
		var stopDist = atrValue * AtrMultiplier;
		var isBullish = close > emaValue;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		// Trailing stop check
		if (Position > 0)
		{
			if (close > _bestPrice) _bestPrice = close;
			if (_bestPrice - close > stopDist)
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
			if (close - _bestPrice > stopDist)
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
