using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// News-style volatility breakout strategy.
/// Enters on ATR expansion with momentum confirmation via EMA.
/// </summary>
public class NewsPendingOrdersStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMult;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevAtr;
	private decimal _entryPrice;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMult { get => _atrMult.Value; set => _atrMult.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public NewsPendingOrdersStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend period", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Indicators");
		_atrMult = Param(nameof(AtrMult), 1.5m)
			.SetDisplay("ATR Mult", "ATR expansion multiplier", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevAtr = 0;
		_entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		SubscribeCandles(CandleType).Bind(ema, atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal atr)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_prevAtr <= 0) { _prevAtr = atr; return; }

		var close = candle.ClosePrice;
		var bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);

		// Volatility expansion: current ATR > previous ATR * mult and big body candle
		var expansion = atr > _prevAtr * AtrMult && bodySize > atr * 0.5m;

		if (expansion && close > ema && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_entryPrice = close;
		}
		else if (expansion && close < ema && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_entryPrice = close;
		}
		// Exit long
		else if (Position > 0)
		{
			if (close < ema || (_entryPrice > 0 && close <= _entryPrice - atr * 2))
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		// Exit short
		else if (Position < 0)
		{
			if (close > ema || (_entryPrice > 0 && close >= _entryPrice + atr * 2))
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		_prevAtr = atr;
	}
}
