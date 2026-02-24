using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Boring EA2 strategy: Triple SMA crossover.
/// Buys when fast > medium > slow. Sells when fast < medium < slow.
/// </summary>
public class BoringEa2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public BoringEa2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new SimpleMovingAverage { Length = 5 };
		var med = new SimpleMovingAverage { Length = 13 };
		var slow = new SimpleMovingAverage { Length = 21 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, med, slow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, med);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal med, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (fast > med && med > slow && Position <= 0)
			BuyMarket();
		else if (fast < med && med < slow && Position >= 0)
			SellMarket();
	}
}
