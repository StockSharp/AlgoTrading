using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual-level breakout strategy using SMA and ATR for dynamic support/resistance levels.
/// Buys when price breaks above SMA + ATR, sells when breaks below SMA - ATR.
/// Exits on mean reversion to SMA.
/// </summary>
public class LineOrderDualLevelStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMult;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMult { get => _atrMult.Value; set => _atrMult.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LineOrderDualLevelStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA period", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Indicators");
		_atrMult = Param(nameof(AtrMult), 1.0m)
			.SetDisplay("ATR Mult", "ATR multiplier for levels", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = SmaPeriod };
		var atr = new StandardDeviation { Length = AtrPeriod };

		SubscribeCandles(CandleType)
			.Bind(sma, atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished) return;
		if (atrVal <= 0) return;

		var close = candle.ClosePrice;
		var upperLevel = smaVal + atrVal * AtrMult;
		var lowerLevel = smaVal - atrVal * AtrMult;

		// Breakout above upper level => long
		if (close > upperLevel && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_entryPrice = close;
		}
		// Breakout below lower level => short
		else if (close < lowerLevel && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_entryPrice = close;
		}
		// Exit long at SMA or if loss > 2*ATR
		else if (Position > 0)
		{
			if (close <= smaVal || (_entryPrice > 0 && close <= _entryPrice - atrVal * 2))
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		// Exit short at SMA or if loss > 2*ATR
		else if (Position < 0)
		{
			if (close >= smaVal || (_entryPrice > 0 && close >= _entryPrice + atrVal * 2))
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}
	}
}
