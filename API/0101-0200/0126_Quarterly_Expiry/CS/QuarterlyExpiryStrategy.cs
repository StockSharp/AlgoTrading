using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Quarterly Expiry strategy.
/// Days are UTC days of a market that trades around the clock.
/// Quarterly expiry is the third Friday of March, June, September and December. At the close of the first candle on the Monday of that
/// week it trades in the direction of the trend (long above the MaPeriod SMA, short below) and closes at Thursday's last candle,
/// before settlement; a percent stop limits the loss.
/// </summary>
public class QuarterlyExpiryStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _day;
	private DateTime _exitDay;

	/// <summary>
	/// SMA that defines the trend.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

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
	public QuarterlyExpiryStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "SMA that defines the trend", "Calendar");

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
		_exitDay = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_day = null;
		_exitDay = default;
		var sma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, ProcessCandle)
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
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue)
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
		decimal? ma = smaValue.IsFormed ? smaValue.GetValue<decimal>() : null;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
		{
			if (lastOfDay && day == _exitDay)
			{
				if (Position > 0)
					SellMarket(Position);
				else
					BuyMarket(-Position);
			}

			return;
		}

		if (!(firstOfDay && ma is decimal trend && candle.ClosePrice != trend && day.Month % 3 == 0 && day == ThirdFriday(day).AddDays(-4)))
			return;

		if ((candle.ClosePrice > ma ? 1 : -1) > 0)
			BuyMarket(Volume);
		else
			SellMarket(Volume);

			_exitDay = ThirdFriday(day).AddDays(-1);
	}

	private static DateTime ThirdFriday(DateTime day)
	{
		var first = new DateTime(day.Year, day.Month, 1, 0, 0, 0, day.Kind);
		return first.AddDays(((int)DayOfWeek.Friday - (int)first.DayOfWeek + 7) % 7 + 14);
	}
}
