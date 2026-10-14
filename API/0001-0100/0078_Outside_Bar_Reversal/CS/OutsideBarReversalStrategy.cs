using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Outside Bar Reversal strategy.
/// An outside bar's high and low both exceed the previous candle's. While flat, a bullish outside bar after a bearish candle buys
/// and a bearish one after a bullish candle sells. The position closes when a close breaks through the outside bar's opposite
/// extreme, and a percent stop from the entry price, watched between candles, limits the loss.
/// </summary>
public class OutsideBarReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _prevCandle;
	private decimal _exitLevel;

	/// <summary>
	/// Stop-loss percentage from the entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type and timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public OutsideBarReversalStrategy()
	{
		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

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
		_prevCandle = null;
		_exitLevel = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevCandle = null;
		_exitLevel = default;

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

		var previous = _prevCandle;
		_prevCandle = candle;

		if (previous == null || !IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (close < _exitLevel)
				SellMarket(Position);

			return;
		}

		if (Position < 0)
		{
			if (close > _exitLevel)
				BuyMarket(-Position);

			return;
		}

		var isOutsideBar = candle.HighPrice > previous.HighPrice && candle.LowPrice < previous.LowPrice;

		if (!isOutsideBar)
			return;

		var afterDecline = previous.ClosePrice < previous.OpenPrice;
		var afterRally = previous.ClosePrice > previous.OpenPrice;

		if (close > candle.OpenPrice && afterDecline)
		{
			BuyMarket(Volume);
			_exitLevel = candle.LowPrice;
		}
		else if (close < candle.OpenPrice && afterRally)
		{
			SellMarket(Volume);
			_exitLevel = candle.HighPrice;
		}
	}
}
