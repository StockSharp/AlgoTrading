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

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal StopFactor { get => _stopFactor.Value; set => _stopFactor.Value = value; }
	public decimal TakeFactor { get => _takeFactor.Value; set => _takeFactor.Value = value; }

	public BasicAtrStopTakeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend period", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Indicators");
		_stopFactor = Param(nameof(StopFactor), 1.5m)
			.SetDisplay("Stop Factor", "ATR multiplier for stop loss", "Risk");
		_takeFactor = Param(nameof(TakeFactor), 2.0m)
			.SetDisplay("Take Factor", "ATR multiplier for take profit", "Risk");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_entryPrice = 0;
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal atr)
	{
		if (candle.State != CandleStates.Finished) return;
		var close = candle.ClosePrice;

		if (Position > 0 && _entryPrice > 0)
		{
			if (close <= _stopPrice || close >= _takePrice)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (close >= _stopPrice || close <= _takePrice)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		if (Position == 0 && atr > 0)
		{
			if (close > ema)
			{
				BuyMarket();
				_entryPrice = close;
				_stopPrice = close - atr * StopFactor;
				_takePrice = close + atr * TakeFactor;
			}
			else if (close < ema)
			{
				SellMarket();
				_entryPrice = close;
				_stopPrice = close + atr * StopFactor;
				_takePrice = close - atr * TakeFactor;
			}
		}
	}
}
