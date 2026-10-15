using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Proffessor V3 strategy. Uses RSI 50-line crossover (period 10).
/// </summary>
public class ProffessorV3Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private decimal? _prevRsi;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	public ProffessorV3Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 10).SetGreaterThanZero().SetDisplay("RSI Period", "RSI lookback", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevRsi = null;
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal)
	{
		if (candle.State != CandleStates.Finished) return;
		if (!IsFormedAndOnlineAndAllowTrading()) { _prevRsi = rsiVal; return; }
		if (_prevRsi == null) { _prevRsi = rsiVal; return; }
		if (_prevRsi.Value < 45m && rsiVal >= 55m && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (_prevRsi.Value > 55m && rsiVal <= 45m && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
		_prevRsi = rsiVal;
	}
}
