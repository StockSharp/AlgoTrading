using System;
using System.Linq;
using System.Collections.Generic;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MultiTimeframeRsiGridWithArrowsStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<DataType> _candleType;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MultiTimeframeRsiGridWithArrowsStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10).SetGreaterThanZero();
		_slowLength = Param(nameof(SlowLength), 30).SetGreaterThanZero();
		_rsiLength = Param(nameof(RsiLength), 14).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var fast = new ExponentialMovingAverage { Length = FastLength };
		var slow = new ExponentialMovingAverage { Length = SlowLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var prevFast = 0m; var prevSlow = 0m; var init = false;
		var sub = SubscribeCandles(CandleType);
		sub.Bind(fast, slow, rsi, (c, f, s, r) =>
		{
			if (c.State != CandleStates.Finished || !fast.IsFormed || !slow.IsFormed || !rsi.IsFormed) return;
			if (!init) { prevFast = f; prevSlow = s; init = true; return; }
			if (prevFast <= prevSlow && f > s && r > 45 && Position <= 0) BuyMarket();
			else if (prevFast >= prevSlow && f < s && r < 55 && Position > 0) SellMarket();
			prevFast = f; prevSlow = s;
		}).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, sub); DrawIndicator(area, fast); DrawIndicator(area, slow); DrawOwnTrades(area); }
	}
}
