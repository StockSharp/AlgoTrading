using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Midday Reversal strategy.
/// The market trades around the clock, so the session is the UTC day.
/// The morning move runs from the UTC day's open to the close at MiddayHour. From then until AfternoonHour, the first candle that closes
/// against that move opens a position against it, once a day. The position closes at the candle ending at AfternoonHour,
/// and a percent stop limits the loss.
/// </summary>
public class MiddayReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _middayHour;
	private readonly StrategyParam<int> _afternoonHour;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _day;
	private decimal _dayOpen;
	private int _morning;
	private bool _tradedToday;

	/// <summary>
	/// UTC hour that ends the morning.
	/// </summary>
	public int MiddayHour
	{
		get => _middayHour.Value;
		set => _middayHour.Value = value;
	}

	/// <summary>
	/// UTC hour by which the reversal must have worked.
	/// </summary>
	public int AfternoonHour
	{
		get => _afternoonHour.Value;
		set => _afternoonHour.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
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
	public MiddayReversalStrategy()
	{
		_middayHour = Param(nameof(MiddayHour), 12)
			.SetRange(0, 23)
			.SetDisplay("Midday Hour", "UTC hour that ends the morning", "Session");

		_afternoonHour = Param(nameof(AfternoonHour), 16)
			.SetRange(0, 23)
			.SetDisplay("Afternoon Hour", "UTC hour by which the reversal must have worked", "Session");

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
		_dayOpen = default;
		_morning = 0;
		_tradedToday = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_day = null;
		_dayOpen = default;
		_morning = 0;
		_tradedToday = false;

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

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
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
			_dayOpen = candle.OpenPrice;
			_morning = 0;
			_tradedToday = false;
		}

		var frame = CandleType.Arg is TimeSpan tf ? tf : TimeSpan.Zero;
		var closeTime = candle.OpenTime + frame;
		var endsAtMidday = closeTime == day.AddHours(MiddayHour);

		if (endsAtMidday)
			_morning = Math.Sign(candle.ClosePrice - _dayOpen);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
		{
			if (closeTime >= day.AddHours(AfternoonHour))
				ClosePosition();

			return;
		}

		var window = closeTime > day.AddHours(MiddayHour) && closeTime < day.AddHours(AfternoonHour);

		if (!window || _tradedToday || _morning == 0)
			return;

		var direction = Math.Sign(candle.ClosePrice - candle.OpenPrice);

		if (direction == -_morning)
		{
			if (_morning > 0)
				SellMarket(Volume);
			else
				BuyMarket(Volume);

			_tradedToday = true;
		}
	}
}
