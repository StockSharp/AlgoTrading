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

	private decimal _entryPrice;
	private decimal _stopPrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	public DynamicStopLossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend period", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for stop distance", "Indicators");
		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for stop distance", "Risk");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_entryPrice = 0;
		_stopPrice = 0;
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

		if (Position > 0)
		{
			var newStop = close - stopDist;
			if (newStop > _stopPrice) _stopPrice = newStop;
			if (close <= _stopPrice)
			{
				SellMarket();
				_entryPrice = 0;
				_stopPrice = 0;
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
				return;
			}
		}

		if (close > ema && Position <= 0)
		{
			BuyMarket();
			_entryPrice = close;
			_stopPrice = close - stopDist;
		}
		else if (close < ema && Position >= 0)
		{
			SellMarket();
			_entryPrice = close;
			_stopPrice = close + stopDist;
		}
	}
}
