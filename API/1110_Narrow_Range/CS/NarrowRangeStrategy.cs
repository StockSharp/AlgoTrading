using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
namespace StockSharp.Samples.Strategies;
public class NarrowRangeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public NarrowRangeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var fast = new ExponentialMovingAverage { Length = 14 };
		var slow = new ExponentialMovingAverage { Length = 40 };
		var rsi = new RelativeStrengthIndex { Length = 14 };
		var prevF = 0m; var prevS = 0m; var init = false;
		var lastSignal = DateTimeOffset.MinValue;
		var cooldown = TimeSpan.FromMinutes(120);
		var sub = SubscribeCandles(CandleType);
		sub.Bind(fast, slow, rsi, (c, f, s, r) =>
		{
			if (c.State != CandleStates.Finished || !fast.IsFormed || !slow.IsFormed || !rsi.IsFormed) return;
			if (!init) { prevF = f; prevS = s; init = true; return; }
			if (c.OpenTime - lastSignal >= cooldown)
			{
				if (prevF <= prevS && f > s && r > 50 && Position <= 0) { BuyMarket(); lastSignal = c.OpenTime; }
				else if (prevF >= prevS && f < s && r < 50 && Position > 0) { SellMarket(); lastSignal = c.OpenTime; }
			}
			prevF = f; prevS = s;
		}).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, sub); DrawIndicator(area, fast); DrawIndicator(area, slow); DrawOwnTrades(area); }
	}
}
