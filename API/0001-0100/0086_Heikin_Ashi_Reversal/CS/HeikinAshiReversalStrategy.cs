using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Heikin Ashi Reversal strategy.
/// Computes Heikin-Ashi candles from regular candles. A bullish Heikin-Ashi candle after bearish ones turns the position long,
/// a bearish one after bullish ones turns it short; a percent stop limits the loss.
/// </summary>
public class HeikinAshiReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _haOpen;
	private decimal _haClose;
	private bool? _prevBullish;

	/// <summary>
	/// Stop-loss percentage.
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
	public HeikinAshiReversalStrategy()
	{
		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_haOpen = null;
		_haClose = default;
		_prevBullish = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_haOpen = null;
		_haClose = default;
		_prevBullish = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

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
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4;
		var haOpen = _haOpen is decimal prevOpen
			? (prevOpen + _haClose) / 2
			: (candle.OpenPrice + candle.ClosePrice) / 2;

		_haOpen = haOpen;
		_haClose = haClose;

		// A Heikin-Ashi candle without a body keeps the previous color.
		if (haClose == haOpen)
			return;

		var isBullish = haClose > haOpen;
		var wasBullish = _prevBullish;
		_prevBullish = isBullish;

		if (wasBullish is not bool previous || previous == isBullish || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (isBullish && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (!isBullish && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
