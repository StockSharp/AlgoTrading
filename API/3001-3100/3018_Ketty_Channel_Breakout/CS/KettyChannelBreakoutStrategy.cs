using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ketty Channel Breakout strategy. Uses Highest/Lowest channel breakout (period 15).
/// </summary>
public class KettyChannelBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;
	private decimal? _prevHigh;
	private decimal? _prevLow;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }

	public KettyChannelBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_period = Param(nameof(Period), 15).SetGreaterThanZero().SetDisplay("Channel Period", "Highest/Lowest lookback", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHigh = null;
		_prevLow = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevHigh = null; _prevLow = null;
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
		if (!IsFormedAndOnlineAndAllowTrading()) { _prevHigh = high; _prevLow = low; return; }
		if (_prevHigh == null || _prevLow == null) { _prevHigh = high; _prevLow = low; return; }
		var close = candle.ClosePrice;
		if (close > _prevHigh.Value && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (close < _prevLow.Value && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
		_prevHigh = high; _prevLow = low;
	}
}
