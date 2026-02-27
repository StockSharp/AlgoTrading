using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AMA Trader strategy. Uses Kaufman Adaptive MA with price crossover.
/// </summary>
public class AmaTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _amaPeriod;
	private decimal? _prevClose;
	private decimal? _prevAma;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int AmaPeriod { get => _amaPeriod.Value; set => _amaPeriod.Value = value; }

	public AmaTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_amaPeriod = Param(nameof(AmaPeriod), 10).SetGreaterThanZero().SetDisplay("AMA Period", "Kaufman AMA period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevClose = null; _prevAma = null;
		var ama = new KaufmanAdaptiveMovingAverage { Length = AmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ama, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawIndicator(area, ama); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal amaVal)
	{
		if (candle.State != CandleStates.Finished) return;
		var close = candle.ClosePrice;
		if (_prevClose == null || _prevAma == null) { _prevClose = close; _prevAma = amaVal; return; }
		if (_prevClose.Value <= _prevAma.Value && close > amaVal && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (_prevClose.Value >= _prevAma.Value && close < amaVal && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
		_prevClose = close; _prevAma = amaVal;
	}
}
