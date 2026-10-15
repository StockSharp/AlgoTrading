using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pivot Point Reversal strategy.
/// Each day the classic floor pivots come from the previous day's high, low and close: P = (H + L + C) / 3, R1 = 2P - L, S1 = 2P - H.
/// While flat, a bullish candle that dips to S1 and closes above it buys, and a bearish candle that reaches R1 and closes below it sells.
/// The position closes when the close reaches the central pivot or at the percent stop.
/// </summary>
public class PivotPointReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _day;
	private decimal _dayHigh;
	private decimal _dayLow;
	private decimal _dayClose;
	private (decimal Pivot, decimal R1, decimal S1)? _levels;

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
	public PivotPointReversalStrategy()
	{
		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
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
		_day = null;
		_dayHigh = default;
		_dayLow = default;
		_dayClose = default;
		_levels = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_day = null;
		_levels = null;

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

		var day = candle.OpenTime.Date;

		if (_day != day)
		{
			// A new day takes its pivots from the day that has just ended.
			if (_day != null)
			{
				var pivot = (_dayHigh + _dayLow + _dayClose) / 3m;
				_levels = (pivot, 2 * pivot - _dayLow, 2 * pivot - _dayHigh);
			}

			_day = day;
			_dayHigh = candle.HighPrice;
			_dayLow = candle.LowPrice;
		}
		else
		{
			_dayHigh = Math.Max(_dayHigh, candle.HighPrice);
			_dayLow = Math.Min(_dayLow, candle.LowPrice);
		}

		_dayClose = candle.ClosePrice;

		if (_levels is not { } levels || !IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (close >= levels.Pivot)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (close <= levels.Pivot)
				BuyMarket(-Position);
		}
		else if (close > candle.OpenPrice && candle.LowPrice <= levels.S1 && close > levels.S1)
		{
			BuyMarket(Volume);
		}
		else if (close < candle.OpenPrice && candle.HighPrice >= levels.R1 && close < levels.R1)
		{
			SellMarket(Volume);
		}
	}
}
