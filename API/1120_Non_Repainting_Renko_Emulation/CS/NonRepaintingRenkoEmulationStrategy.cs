using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
namespace StockSharp.Samples.Strategies;
public class NonRepaintingRenkoEmulationStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public NonRepaintingRenkoEmulationStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var fast = new ExponentialMovingAverage { Length = 10 };
		var slow = new ExponentialMovingAverage { Length = 30 };
		var rsi = new RelativeStrengthIndex { Length = 14 };
		var prevF = 0m; var prevS = 0m; var init = false;
		var sub = SubscribeCandles(CandleType);
		sub.Bind(fast, slow, rsi, (c, f, s, r) =>
		{
			if (c.State != CandleStates.Finished || !fast.IsFormed || !slow.IsFormed || !rsi.IsFormed) return;
			if (!init) { prevF = f; prevS = s; init = true; return; }
			if (prevF <= prevS && f > s && r > 45 && Position <= 0) BuyMarket();
			else if (prevF >= prevS && f < s && r < 55 && Position > 0) SellMarket();
			prevF = f; prevS = s;
		}).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, sub); DrawIndicator(area, fast); DrawIndicator(area, slow); DrawOwnTrades(area); }
	}
}
