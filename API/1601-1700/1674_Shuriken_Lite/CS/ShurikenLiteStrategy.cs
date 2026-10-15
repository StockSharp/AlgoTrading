using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Shuriken Lite - fast EMA/RSI scalping strategy.
/// </summary>
public class ShurikenLiteStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<DataType> _candleType;

	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ShurikenLiteStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("EMA", "EMA period", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("RSI", "RSI period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		// Buy when RSI oversold
		if (rsi < 30 && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Sell when RSI overbought
		else if (rsi > 70 && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
	}
}
