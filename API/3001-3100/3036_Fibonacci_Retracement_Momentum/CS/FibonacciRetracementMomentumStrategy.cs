using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class FibonacciRetracementMomentumStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private decimal? _prevFast, _prevSlow;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }

	public FibonacciRetracementMomentumStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_fastPeriod = Param(nameof(FastPeriod), 6).SetGreaterThanZero().SetDisplay("Fast WMA", "Fast WMA period", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 21).SetGreaterThanZero().SetDisplay("Slow WMA", "Slow WMA period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = null;
		_prevSlow = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevFast = null; _prevSlow = null;
		var fast = new WeightedMovingAverage { Length = FastPeriod };
		var slow = new WeightedMovingAverage { Length = SlowPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, slow, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawIndicator(area, fast); DrawIndicator(area, slow); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished) return;
		if (!IsFormedAndOnlineAndAllowTrading()) { _prevFast = fast; _prevSlow = slow; return; }
		if (_prevFast == null || _prevSlow == null) { _prevFast = fast; _prevSlow = slow; return; }
		var prevAbove = _prevFast.Value > _prevSlow.Value;
		var currAbove = fast > slow;
		_prevFast = fast; _prevSlow = slow;
		if (!prevAbove && currAbove && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (prevAbove && !currAbove && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
	}
}
