using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class DonchianChannelsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;
	private decimal? _prevHigh, _prevLow;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }

	public DonchianChannelsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_period = Param(nameof(Period), 20).SetGreaterThanZero().SetDisplay("Channel Period", "Donchian channel lookback", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

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
		if (_prevHigh == null || _prevLow == null) { _prevHigh = high; _prevLow = low; return; }
		if (candle.ClosePrice > _prevHigh.Value && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (candle.ClosePrice < _prevLow.Value && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
		_prevHigh = high; _prevLow = low;
	}
}
