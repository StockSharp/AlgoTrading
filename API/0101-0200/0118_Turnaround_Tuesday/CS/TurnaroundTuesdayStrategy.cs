using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Turnaround Tuesday strategy.
/// Days are UTC days of a market that trades around the clock.
/// When Monday closed below its open, it buys at the close of Tuesday's first candle and sells at Tuesday's last candle
/// or once the close reaches ProfitTargetPercent above the entry; a percent stop limits the loss.
/// </summary>
public class TurnaroundTuesdayStrategy : Strategy
{
	private readonly StrategyParam<decimal> _profitTargetPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _day;
	private decimal _dayOpen;
	private decimal _dayClose;
	private DateTime? _prevDay;
	private decimal _prevDayOpen;
	private decimal _prevDayClose;
	private decimal _entryPrice;

	/// <summary>
	/// Close once the price is this far above the entry, in percent.
	/// </summary>
	public decimal ProfitTargetPercent
	{
		get => _profitTargetPercent.Value;
		set => _profitTargetPercent.Value = value;
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
	public TurnaroundTuesdayStrategy()
	{
		_profitTargetPercent = Param(nameof(ProfitTargetPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Target %", "Close once the price is this far above the entry, in percent", "Calendar");

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
		_dayClose = default;
		_prevDay = null;
		_prevDayOpen = default;
		_prevDayClose = default;
		_entryPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_day = null;
		_dayOpen = default;
		_dayClose = default;
		_prevDay = null;
		_prevDayOpen = default;
		_prevDayClose = default;
		_entryPrice = default;
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
			if (_day is DateTime previousDay)
			{
				_prevDay = previousDay;
				_prevDayOpen = _dayOpen;
				_prevDayClose = _dayClose;
			}

			_day = day;
			_dayOpen = candle.OpenPrice;
		}

		_dayClose = candle.ClosePrice;
		// The candle is the day's last when the next one would open on another day.
		var frame = CandleType.Arg is TimeSpan tf ? tf : TimeSpan.Zero;
		var lastOfDay = (candle.OpenTime + frame).Date != day;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
		{
			if (lastOfDay || candle.ClosePrice >= _entryPrice * (1 + ProfitTargetPercent / 100m))
			{
				if (Position > 0)
					SellMarket(Position);
				else
					BuyMarket(-Position);
			}

			return;
		}

		if (!(firstOfDay && day.DayOfWeek == DayOfWeek.Tuesday && _prevDay == day.AddDays(-1) && _prevDayClose < _prevDayOpen))
			return;

		if ((1) > 0)
			BuyMarket(Volume);
		else
			SellMarket(Volume);

			_entryPrice = candle.ClosePrice;
	}
}
