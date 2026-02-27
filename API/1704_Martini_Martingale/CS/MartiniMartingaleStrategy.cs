using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean reversion strategy inspired by martingale.
/// Enters on RSI extremes, exits when price returns to SMA.
/// </summary>
public class MartiniMartingaleStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MartiniMartingaleStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA for mean reversion target", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		SubscribeCandles(CandleType).Bind(rsi, sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal sma)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;

		// RSI oversold => buy
		if (rsi < 30 && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// RSI overbought => sell
		else if (rsi > 70 && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
		// Exit long at SMA
		else if (Position > 0 && close >= sma && rsi > 50)
		{
			SellMarket();
		}
		// Exit short at SMA
		else if (Position < 0 && close <= sma && rsi < 50)
		{
			BuyMarket();
		}
	}
}
