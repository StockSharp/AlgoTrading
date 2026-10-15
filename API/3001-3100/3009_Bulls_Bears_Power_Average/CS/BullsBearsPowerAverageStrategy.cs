using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bulls Bears Power Average strategy. Uses EMA with bulls/bears power crossover.
/// </summary>
public class BullsBearsPowerAverageStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private decimal? _prevPower;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	public BullsBearsPowerAverageStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 13).SetGreaterThanZero().SetDisplay("EMA Period", "EMA lookback for power calc", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevPower = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevPower = null;
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawIndicator(area, ema); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished) return;
		var bullsPower = candle.HighPrice - emaVal;
		var bearsPower = candle.LowPrice - emaVal;
		var avgPower = (bullsPower + bearsPower) / 2m;
		if (!IsFormedAndOnlineAndAllowTrading()) { _prevPower = avgPower; return; }
		if (_prevPower == null) { _prevPower = avgPower; return; }
		if (_prevPower.Value < 0m && avgPower >= 0m && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (_prevPower.Value > 0m && avgPower <= 0m && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
		_prevPower = avgPower;
	}
}
