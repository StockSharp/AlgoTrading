using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ATR Exhaustion strategy.
/// An ATR spike is an ATR above AtrMultiplier times its own AtrAvgPeriod moving average. While flat, a bullish candle on a spike
/// buys when the price moving average has been falling and a bearish one sells when it has been rising.
/// The only exit is a trailing percent stop.
/// </summary>
public class AtrExhaustionStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _atrAvgPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _atrAverage;
	private decimal? _prevMa;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Period of the moving average of ATR.
	/// </summary>
	public int AtrAvgPeriod
	{
		get => _atrAvgPeriod.Value;
		set => _atrAvgPeriod.Value = value;
	}

	/// <summary>
	/// How many times its average ATR must exceed to count as a spike.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Period of the price moving average that defines the prior move.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Trailing stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AtrExhaustionStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR", "Indicators");

		_atrAvgPeriod = Param(nameof(AtrAvgPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR Average Period", "Period of the moving average of ATR", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "How many times its average ATR must exceed", "Indicators");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Price moving average that defines the prior move", "Indicators");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Trailing stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_atrAverage = null;
		_prevMa = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_atrAverage = new SimpleMovingAverage { Length = AtrAvgPeriod };
		_prevMa = null;

		var sma = new SimpleMovingAverage { Length = MaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, atr, ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), isStopTrailing: true, useMarketOrders: true, isLocalStop: true);

		// The stop has to see prices between candles, not only at their close.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished || !atrValue.IsFormed)
			return;

		var atr = atrValue.GetValue<decimal>();
		var atrAverage = _atrAverage.Process(atr, candle.OpenTime, true);

		if (!smaValue.IsFormed)
			return;

		var ma = smaValue.GetValue<decimal>();
		var prevMa = _prevMa;
		_prevMa = ma;

		if (!_atrAverage.IsFormed || prevMa is not decimal lastMa || Position != 0 || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (atr <= atrAverage.GetValue<decimal>() * AtrMultiplier)
			return;

		var close = candle.ClosePrice;

		if (close > candle.OpenPrice && ma < lastMa)
			BuyMarket(Volume);
		else if (close < candle.OpenPrice && ma > lastMa)
			SellMarket(Volume);
	}
}
