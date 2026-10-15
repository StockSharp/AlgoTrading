using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triangle Breakout for BTC MARK804 strategy using EMA crossover.
/// </summary>
public class TriangleBreakoutBtcMark804Strategy : Strategy
{
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DataType> _candleType;

	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TriangleBreakoutBtcMark804Strategy()
	{
		_slowLength = Param(nameof(SlowLength), 40)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow EMA period", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var fast = new ExponentialMovingAverage { Length = 14 };
		var slow = new ExponentialMovingAverage { Length = SlowLength };
		var prevF = 0m; var prevS = 0m; var init = false;
		var lastSignal = DateTimeOffset.MinValue;
		var cooldown = TimeSpan.FromMinutes(360);
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, slow, (candle, f, s) =>
		{
			if (candle.State != CandleStates.Finished) return;
			if (!fast.IsFormed || !slow.IsFormed) return;
			if (!init) { prevF = f; prevS = s; init = true; return; }
			if (candle.OpenTime - lastSignal >= cooldown)
			{
				if (prevF <= prevS && f > s && Position <= 0) { BuyMarket(); lastSignal = candle.OpenTime; }
				else if (prevF >= prevS && f < s && Position >= 0) { SellMarket(); lastSignal = candle.OpenTime; }
			}
			prevF = f; prevS = s;
		}).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawIndicator(area, fast); DrawIndicator(area, slow); DrawOwnTrades(area); }
	}
}
