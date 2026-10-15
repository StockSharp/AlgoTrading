using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// XDeMarker Histogram Vol strategy. Uses DeMarker 0.5-line crossover.
/// </summary>
public class XDeMarkerHistogramVolStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _demarkerPeriod;
	private decimal? _prevDm;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int DemarkerPeriod { get => _demarkerPeriod.Value; set => _demarkerPeriod.Value = value; }

	public XDeMarkerHistogramVolStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_demarkerPeriod = Param(nameof(DemarkerPeriod), 14).SetGreaterThanZero().SetDisplay("DeMarker Period", "DeMarker lookback", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevDm = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevDm = null;
		var dm = new DeMarker { Length = DemarkerPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(dm, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal dmVal)
	{
		if (candle.State != CandleStates.Finished) return;
		if (!IsFormedAndOnlineAndAllowTrading()) { _prevDm = dmVal; return; }
		if (_prevDm == null) { _prevDm = dmVal; return; }
		if (_prevDm.Value < 0.45m && dmVal >= 0.55m && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (_prevDm.Value > 0.55m && dmVal <= 0.45m && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
		_prevDm = dmVal;
	}
}
