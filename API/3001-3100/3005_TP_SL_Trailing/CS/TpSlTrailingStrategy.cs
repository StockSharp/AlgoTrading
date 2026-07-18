using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// TP SL Trailing strategy. Uses EMA with price crossover for entries.
/// </summary>
public class TpSlTrailingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private decimal? _prevClose;
	private decimal? _prevEma;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	public TpSlTrailingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 20).SetGreaterThanZero().SetDisplay("EMA Period", "EMA lookback", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = null;
		_prevEma = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevClose = null; _prevEma = null;
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawIndicator(area, ema); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished) return;
		var close = candle.ClosePrice;
		if (!IsFormedAndOnlineAndAllowTrading()) { _prevClose = close; _prevEma = emaVal; return; }
		if (_prevClose == null || _prevEma == null) { _prevClose = close; _prevEma = emaVal; return; }
		if (_prevClose.Value < _prevEma.Value && close >= emaVal && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (_prevClose.Value > _prevEma.Value && close <= emaVal && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
		_prevClose = close; _prevEma = emaVal;
	}
}
