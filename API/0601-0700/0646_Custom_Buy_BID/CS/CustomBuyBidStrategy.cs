using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Custom Buy BID strategy.
/// Goes long when the close crosses above the Supertrend line inside the StartDate..EndDate window.
/// The position is closed only by the percent take profit or stop loss.
/// </summary>
public class CustomBuyBidStrategy : Strategy
{
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;

	private decimal? _prevClose;
	private decimal? _prevSupertrend;

	/// <summary>
	/// ATR period of Supertrend.
	/// </summary>
	public int SupertrendPeriod
	{
		get => _supertrendPeriod.Value;
		set => _supertrendPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier of Supertrend.
	/// </summary>
	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
	}

	/// <summary>
	/// Take profit percentage from entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
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
	/// First date when entries are allowed.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// Date after which no entries are made.
	/// </summary>
	public DateTimeOffset EndDate
	{
		get => _endDate.Value;
		set => _endDate.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public CustomBuyBidStrategy()
	{
		_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend Period", "ATR period of Supertrend", "Indicators");

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend Multiplier", "ATR multiplier of Supertrend", "Indicators");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 5m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(2018, 9, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Date", "First date when entries are allowed", "General");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(9999, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Date", "Date after which no entries are made", "General");
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
		_prevClose = null;
		_prevSupertrend = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;
		_prevSupertrend = null;

		var supertrend = new SuperTrend { Length = SupertrendPeriod, Multiplier = SupertrendMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(supertrend, ProcessCandle)
			.Start();

		StartProtection(
			new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue supertrendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!supertrendValue.IsFormed)
			return;

		var line = supertrendValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		var crossedUp = _prevClose is decimal prevClose && _prevSupertrend is decimal prevLine
			&& prevClose <= prevLine && close > line;

		_prevClose = close;
		_prevSupertrend = line;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var time = candle.OpenTime;
		var inWindow = time >= StartDate.UtcDateTime && time <= EndDate.UtcDateTime;

		if (crossedUp && inWindow && Position == 0)
			BuyMarket();
	}
}
