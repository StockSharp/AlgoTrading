using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Sprut Pending Order Grid strategy. Uses Highest/Lowest midline crossover (period 14).
/// </summary>
public class SprutPendingOrderGridStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;
	private decimal? _prevMid;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }

	public SprutPendingOrderGridStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_period = Param(nameof(Period), 14).SetGreaterThanZero().SetDisplay("Channel Period", "Highest/Lowest lookback", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMid = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevMid = null;
		var highest = new Highest { Length = Period };
		var lowest = new Lowest { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(highest, lowest, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal high, decimal low)
	{
		if (candle.State != CandleStates.Finished) return;
		var mid = (high + low) / 2m;
		var close = candle.ClosePrice;
		if (!IsFormedAndOnlineAndAllowTrading()) { _prevMid = mid; return; }
		if (_prevMid == null) { _prevMid = mid; return; }
		if (close > mid && close > _prevMid.Value && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (close < mid && close < _prevMid.Value && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
		_prevMid = mid;
	}
}
