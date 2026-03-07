using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy based on X_trail_2.
/// Enters long when fast MA crosses above slow MA,
/// enters short on the reverse crossover.
/// </summary>
public class XTrail2Strategy : Strategy
{
	private readonly StrategyParam<int> _ma1Length;
	private readonly StrategyParam<int> _ma2Length;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	public int Ma1Length { get => _ma1Length.Value; set => _ma1Length.Value = value; }
	public int Ma2Length { get => _ma2Length.Value; set => _ma2Length.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public XTrail2Strategy()
	{
		_ma1Length = Param(nameof(Ma1Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Length", "Length of the fast MA", "Moving Averages");

		_ma2Length = Param(nameof(Ma2Length), 30)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Length", "Length of the slow MA", "Moving Averages");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0;
		_prevSlow = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new SimpleMovingAverage { Length = Ma1Length };
		var slow = new SimpleMovingAverage { Length = Ma2Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_hasPrev)
		{
			if (fast > slow && _prevFast <= _prevSlow && Position <= 0)
				BuyMarket();
			else if (fast < slow && _prevFast >= _prevSlow && Position >= 0)
				SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
		_hasPrev = true;
	}
}
