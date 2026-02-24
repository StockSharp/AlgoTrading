using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SwingCyborg strategy using RSI overbought/oversold levels.
/// Buys on oversold and sells on overbought.
/// </summary>
public class SwingCyborgStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SwingCyborgStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position > 0 && rsiValue >= 70m)
		{
			SellMarket();
		}
		else if (Position < 0 && rsiValue <= 30m)
		{
			BuyMarket();
		}
		else if (Position == 0)
		{
			if (rsiValue <= 30m)
				BuyMarket();
			else if (rsiValue >= 70m)
				SellMarket();
		}
	}
}
