using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Earnings announcement premium strategy that enters the primary instrument shortly before a synthetic earnings event and exits after the event passes.
/// </summary>
public class EarningsAnnouncementPremiumStrategy : Strategy
{
	private readonly StrategyParam<int> _daysBefore;
	private readonly StrategyParam<int> _daysAfter;
	private readonly StrategyParam<int> _eventCycleBars;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _trend = null!;
	private int _barsSinceEvent;
	private int _cooldownRemaining;
	private decimal _latestTrendValue;

	/// <summary>
	/// Number of bars before the synthetic earnings event to enter.
	/// </summary>
	public int DaysBefore
	{
		get => _daysBefore.Value;
		set => _daysBefore.Value = value;
	}

	/// <summary>
	/// Number of bars after the synthetic earnings event to exit.
	/// </summary>
	public int DaysAfter
	{
		get => _daysAfter.Value;
		set => _daysAfter.Value = value;
	}

	/// <summary>
	/// Distance between synthetic earnings events in finished bars.
	/// </summary>
	public int EventCycleBars
	{
		get => _eventCycleBars.Value;
		set => _eventCycleBars.Value = value;
	}

	/// <summary>
	/// Trend filter length.
	/// </summary>
	public int TrendLength
	{
		get => _trendLength.Value;
		set => _trendLength.Value = value;
	}

	/// <summary>
	/// Closed candles to wait before another position change.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Candle type to use for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="EarningsAnnouncementPremiumStrategy"/>.
	/// </summary>
	public EarningsAnnouncementPremiumStrategy()
	{
		_daysBefore = Param(nameof(DaysBefore), 3)
			.SetRange(1, 10)
			.SetDisplay("Days Before", "Bars before the synthetic earnings event to enter", "General");

		_daysAfter = Param(nameof(DaysAfter), 1)
			.SetRange(1, 10)
			.SetDisplay("Days After", "Bars after the synthetic earnings event to exit", "General");

		_eventCycleBars = Param(nameof(EventCycleBars), 18)
			.SetRange(8, 80)
			.SetDisplay("Event Cycle Bars", "Distance between synthetic earnings events in finished bars", "General");

		_trendLength = Param(nameof(TrendLength), 12)
			.SetRange(3, 50)
			.SetDisplay("Trend Length", "Trend filter length", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 2)
			.SetRange(0, 20)
			.SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk");

		_stopLoss = Param(nameof(StopLoss), 2.5m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_trend = null!;
		_barsSinceEvent = 0;
		_cooldownRemaining = 0;
		_latestTrendValue = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Primary security is not specified.");

		_trend = new SimpleMovingAverage { Length = TrendLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection(
			new Unit(2, UnitTypes.Percent),
			new Unit(StopLoss, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_latestTrendValue = _trend.Process(candle).ToDecimal();

		if (!_trend.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var barsToEvent = EventCycleBars - (_barsSinceEvent % EventCycleBars);
		var bullishWindow = barsToEvent <= DaysBefore && barsToEvent > 0 && candle.ClosePrice >= _latestTrendValue * 0.995m;
		var exitWindow = _barsSinceEvent % EventCycleBars == DaysAfter;

		if (_cooldownRemaining == 0 && Position == 0 && bullishWindow)
		{
			BuyMarket();
			_cooldownRemaining = CooldownBars;
		}
		else if (Position > 0 && exitWindow)
		{
			SellMarket(Position);
			_cooldownRemaining = CooldownBars;
		}

		_barsSinceEvent++;
	}
}
