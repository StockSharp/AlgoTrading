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

	private decimal _entryPrice;
	private decimal _bestPrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	public NnfxAutoTradeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend filter period", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Indicators");
		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for stop", "Risk");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_entryPrice = 0;
		_bestPrice = 0;
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

		// Trailing stop check
		if (Position > 0)
		{
			if (close > _bestPrice) _bestPrice = close;
			if (_bestPrice - close > stopDist)
			{
				SellMarket();
				return;
			}
		}
		else if (Position < 0)
		{
			if (close < _bestPrice) _bestPrice = close;
			if (close - _bestPrice > stopDist)
			{
				BuyMarket();
				return;
			}
		}

		// Entry signals
		if (close > emaValue && Position <= 0)
		{
			BuyMarket();
			_entryPrice = close;
			_bestPrice = close;
		}
		else if (close < emaValue && Position >= 0)
		{
			SellMarket();
			_entryPrice = close;
			_bestPrice = close;
		}
	}
}
