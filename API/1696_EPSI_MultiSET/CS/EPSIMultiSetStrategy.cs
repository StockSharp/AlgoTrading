using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy. Opens a position when price moves significantly from candle open.
/// </summary>
public class EPSIMultiSetStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _breakoutMult;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal BreakoutMult { get => _breakoutMult.Value; set => _breakoutMult.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EPSIMultiSetStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Indicators");
		_breakoutMult = Param(nameof(BreakoutMult), 0.5m)
			.SetDisplay("Breakout Mult", "ATR multiplier for breakout", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
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

		var atr = new StandardDeviation { Length = AtrPeriod };

		SubscribeCandles(CandleType).Bind(atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished) return;
		if (atrValue <= 0) return;

		var minDist = atrValue * BreakoutMult;

		if (Position == 0)
		{
			if (candle.HighPrice - candle.OpenPrice >= minDist)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (candle.OpenPrice - candle.LowPrice >= minDist)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}
		else if (Position > 0)
		{
			if (candle.ClosePrice <= _entryPrice - atrValue * 2 || candle.ClosePrice >= _entryPrice + atrValue * 1.5m)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice >= _entryPrice + atrValue * 2 || candle.ClosePrice <= _entryPrice - atrValue * 1.5m)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}
	}
}
