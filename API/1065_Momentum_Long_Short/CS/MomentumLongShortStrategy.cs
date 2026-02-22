using System;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum strategy trading long and short with trend and RSI filters.
/// </summary>
public class MomentumLongShortStrategy : Strategy
{
	private readonly StrategyParam<decimal> _slPercent;
	private readonly StrategyParam<DataType> _candleType;

	public decimal SlPercent { get => _slPercent.Value; set => _slPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MomentumLongShortStrategy()
	{
		_slPercent = Param(nameof(SlPercent), 3m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var maFast = new EMA { Length = 20 };
		var maSlow = new EMA { Length = 50 };
		var rsi = new RelativeStrengthIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(maFast, maSlow, rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maFast, decimal maSlow, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		// Long: price above MAs, trend up, RSI not overbought
		if (close > maFast && maFast > maSlow && rsi < 70 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket();
		}

		// Short: price below MAs, trend down, RSI not oversold
		if (close < maFast && maFast < maSlow && rsi > 30 && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Position);
			SellMarket();
		}

		// Stop loss
		if (Position > 0 && close < maFast)
			SellMarket();
		else if (Position < 0 && close > maFast)
			BuyMarket();
	}
}
