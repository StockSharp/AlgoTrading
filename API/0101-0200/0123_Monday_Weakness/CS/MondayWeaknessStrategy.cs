using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Monday Weakness strategy.
/// Days are UTC days of a market that trades around the clock.
/// It sells short at the close of Monday's first candle and covers at Monday's last candle; a percent stop limits the loss.
/// </summary>
public class MondayWeaknessStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _day;

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
	public MondayWeaknessStrategy()
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
		_day = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_day = null;
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
		var firstOfDay = _day != day;

		if (firstOfDay)
		{
			_day = day;
		}

		// The candle is the day's last when the next one would open on another day.
		var frame = CandleType.Arg is TimeSpan tf ? tf : TimeSpan.Zero;
		var lastOfDay = (candle.OpenTime + frame).Date != day;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
		{
			if (lastOfDay)
			{
				if (Position > 0)
					SellMarket(Position);
				else
					BuyMarket(-Position);
			}

			return;
		}

		if (!(firstOfDay && day.DayOfWeek == DayOfWeek.Monday))
			return;

		if ((-1) > 0)
			BuyMarket(Volume);
		else
			SellMarket(Volume);
	}
}
