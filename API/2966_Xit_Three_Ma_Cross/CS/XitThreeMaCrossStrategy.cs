using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple MA cross strategy. Trades when fast crosses mid, confirmed by slow trend.
/// </summary>
public class XitThreeMaCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _midPeriod;
	private readonly StrategyParam<int> _slowPeriod;

	private decimal? _prevFast;
	private decimal? _prevMid;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int MidPeriod
	{
		get => _midPeriod.Value;
		set => _midPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public XitThreeMaCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast MA period", "Indicators");

		_midPeriod = Param(nameof(MidPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Mid Period", "Medium MA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow MA (trend filter)", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = null;
		_prevMid = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevMid = null;

		var fast = new SimpleMovingAverage { Length = FastPeriod };
		var mid = new SimpleMovingAverage { Length = MidPeriod };
		var slow = new SimpleMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, mid, slow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, mid);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal midVal, decimal slowVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == null || _prevMid == null)
		{
			_prevFast = fastVal;
			_prevMid = midVal;
			return;
		}

		// Fast crosses above mid → buy
		if (_prevFast.Value <= _prevMid.Value && fastVal > midVal)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		// Fast crosses below mid → sell
		else if (_prevFast.Value >= _prevMid.Value && fastVal < midVal)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}

		_prevFast = fastVal;
		_prevMid = midVal;
	}
}
