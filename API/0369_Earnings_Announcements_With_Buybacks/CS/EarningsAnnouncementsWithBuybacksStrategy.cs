using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that buys the primary instrument before synthetic earnings events when a synthetic buyback regime is active and exits after the event.
/// </summary>
public class EarningsAnnouncementsWithBuybacksStrategy : Strategy
{
	private readonly StrategyParam<int> _daysBefore;
	private readonly StrategyParam<int> _daysAfter;
	private readonly StrategyParam<int> _eventCycleBars;
	private readonly StrategyParam<int> _buybackLength;
	private readonly StrategyParam<decimal> _buybackThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _buybackProxy = null!;
	private int _barIndex;
	private int _holdingRemaining;
	private int _cooldownRemaining;
	private decimal _latestBuybackValue;

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
	/// Distance between synthetic earnings events in bars.
	/// </summary>
	public int EventCycleBars
	{
		get => _eventCycleBars.Value;
		set => _eventCycleBars.Value = value;
	}

	/// <summary>
	/// Smoothing length for the synthetic buyback activity proxy.
	/// </summary>
	public int BuybackLength
	{
		get => _buybackLength.Value;
		set => _buybackLength.Value = value;
	}

	/// <summary>
	/// Minimum synthetic buyback score required to enter.
	/// </summary>
	public decimal BuybackThreshold
	{
		get => _buybackThreshold.Value;
		set => _buybackThreshold.Value = value;
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
	/// Candle type used for price data.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="EarningsAnnouncementsWithBuybacksStrategy"/>.
	/// </summary>
	public EarningsAnnouncementsWithBuybacksStrategy()
	{
		_daysBefore = Param(nameof(DaysBefore), 3)
			.SetRange(1, 10)
			.SetDisplay("Days Before", "Bars before the synthetic earnings event to enter", "Trading");

		_daysAfter = Param(nameof(DaysAfter), 1)
			.SetRange(1, 10)
			.SetDisplay("Days After", "Bars after the synthetic earnings event to exit", "Trading");

		_eventCycleBars = Param(nameof(EventCycleBars), 20)
			.SetRange(8, 80)
			.SetDisplay("Event Cycle Bars", "Distance between synthetic earnings events", "Trading");

		_buybackLength = Param(nameof(BuybackLength), 8)
			.SetRange(2, 40)
			.SetDisplay("Buyback Length", "Smoothing length for the synthetic buyback proxy", "Indicators");

		_buybackThreshold = Param(nameof(BuybackThreshold), 0.7m)
			.SetRange(-5m, 5m)
			.SetDisplay("Buyback Threshold", "Minimum synthetic buyback score required to enter", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 2)
			.SetRange(0, 20)
			.SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk");

		_stopLoss = Param(nameof(StopLoss), 2.5m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_buybackProxy = null!;
		_barIndex = 0;
		_holdingRemaining = 0;
		_cooldownRemaining = 0;
		_latestBuybackValue = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Primary security is not specified.");

		_buybackProxy = new ExponentialMovingAverage { Length = BuybackLength };

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

		var buybackSignal = CalculateBuybackSignal(candle);
		_latestBuybackValue = _buybackProxy.Process(buybackSignal, candle.OpenTime, true).ToDecimal();

		if (!_buybackProxy.IsFormed || !IsFormedAndOnlineAndAllowTrading())
		{
			_barIndex++;
			return;
		}

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		if (_holdingRemaining > 0)
		{
			_holdingRemaining--;

			if (_holdingRemaining == 0 && Position > 0)
			{
				SellMarket(Position);
				_cooldownRemaining = CooldownBars;
			}
		}

		var barsToEvent = EventCycleBars - (_barIndex % EventCycleBars);
		var inEntryWindow = barsToEvent <= DaysBefore && barsToEvent > 0;
		var buybackActive = _latestBuybackValue >= BuybackThreshold;

		if (_cooldownRemaining == 0 && Position == 0 && inEntryWindow && buybackActive)
		{
			BuyMarket();
			_holdingRemaining = DaysAfter + 1;
			_cooldownRemaining = CooldownBars;
		}

		_barIndex++;
	}

	private decimal CalculateBuybackSignal(ICandleMessage candle)
	{
		var priceBase = Math.Max(candle.OpenPrice, 1m);
		var range = Math.Max(candle.HighPrice - candle.LowPrice, Security?.PriceStep ?? 1m);
		var closeLocation = ((candle.ClosePrice - candle.LowPrice) - (candle.HighPrice - candle.ClosePrice)) / range;
		var compression = 1m - Math.Min(0.2m, range / priceBase);

		return (closeLocation * 2m) + compression;
	}
}
