using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ForexLine: two weighted-moving-average stages for each fast and slow line.
/// Enter on a formed, finished-candle crossover, reversing the entire held side.
/// </summary>
public class ForexLineStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength1;
	private readonly StrategyParam<int> _fastLength2;
	private readonly StrategyParam<int> _slowLength1;
	private readonly StrategyParam<int> _slowLength2;
	private readonly StrategyParam<DataType> _candleType;
	private WeightedMovingAverage _fastFirst;
	private WeightedMovingAverage _fastSecond;
	private WeightedMovingAverage _slowFirst;
	private WeightedMovingAverage _slowSecond;
	private decimal? _previousFast;
	private decimal? _previousSlow;
	private Order _pendingOrder;

	public int FastLength1 { get => _fastLength1.Value; set => _fastLength1.Value = value; }
	public int FastLength2 { get => _fastLength2.Value; set => _fastLength2.Value = value; }
	public int SlowLength1 { get => _slowLength1.Value; set => _slowLength1.Value = value; }
	public int SlowLength2 { get => _slowLength2.Value; set => _slowLength2.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ForexLineStrategy()
	{
		_fastLength1 = Param(nameof(FastLength1), 5).SetGreaterThanZero()
			.SetDisplay("Fast WMA 1", "First fast smoothing stage", "Indicator");
		_fastLength2 = Param(nameof(FastLength2), 10).SetGreaterThanZero()
			.SetDisplay("Fast WMA 2", "Second fast smoothing stage", "Indicator");
		_slowLength1 = Param(nameof(SlowLength1), 20).SetGreaterThanZero()
			.SetDisplay("Slow WMA 1", "First slow smoothing stage", "Indicator");
		_slowLength2 = Param(nameof(SlowLength2), 20).SetGreaterThanZero()
			.SetDisplay("Slow WMA 2", "Second slow smoothing stage", "Indicator");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candle closes", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousFast = _previousSlow = null;
		_pendingOrder = null;
		_fastFirst = _fastSecond = _slowFirst = _slowSecond = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_previousFast = _previousSlow = null;
		_pendingOrder = null;
		_fastFirst = new WeightedMovingAverage { Length = FastLength1 };
		_fastSecond = new WeightedMovingAverage { Length = FastLength2 };
		_slowFirst = new WeightedMovingAverage { Length = SlowLength1 };
		_slowSecond = new WeightedMovingAverage { Length = SlowLength2 };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = new DecimalIndicatorValue(_fastFirst, candle.ClosePrice, candle.OpenTime) { IsFinal = true };
		var fastOne = _fastFirst.Process(price);
		var slowOne = _slowFirst.Process(new DecimalIndicatorValue(_slowFirst, candle.ClosePrice, candle.OpenTime) { IsFinal = true });
		if (!fastOne.IsFormed || !slowOne.IsFormed)
			return;
		var fastTwo = _fastSecond.Process(fastOne);
		var slowTwo = _slowSecond.Process(slowOne);
		if (!fastTwo.IsFormed || !slowTwo.IsFormed)
			return;

		var fast = fastTwo.GetValue<decimal>();
		var slow = slowTwo.GetValue<decimal>();
		if (_previousFast is decimal pf && _previousSlow is decimal ps && IsFormedAndOnlineAndAllowTrading()
			&& (_pendingOrder is null || _pendingOrder.State is OrderStates.Done or OrderStates.Failed))
		{
			if (pf <= ps && fast > slow && Position <= 0m)
				_pendingOrder = BuyMarket(Volume + Math.Abs(Position));
			else if (pf >= ps && fast < slow && Position >= 0m)
				_pendingOrder = SellMarket(Volume + Math.Abs(Position));
		}
		_previousFast = fast;
		_previousSlow = slow;
	}
}
