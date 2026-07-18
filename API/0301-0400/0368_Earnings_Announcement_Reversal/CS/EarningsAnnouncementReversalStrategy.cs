using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades short-term reversals around synthetic earnings announcement dates for the primary instrument.
/// </summary>
public class EarningsAnnouncementReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackDays;
	private readonly StrategyParam<int> _holdingDays;
	private readonly StrategyParam<int> _eventCycleBars;
	private readonly StrategyParam<decimal> _reversalThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private RateOfChange _momentum = null!;
	private int _barIndex;
	private int _holdingRemaining;
	private int _cooldownRemaining;
	private decimal _latestMomentum;

	/// <summary>
	/// Lookback period in bars used to determine winners and losers.
	/// </summary>
	public int LookbackDays
	{
		get => _lookbackDays.Value;
		set => _lookbackDays.Value = value;
	}

	/// <summary>
	/// Number of bars to hold the position.
	/// </summary>
	public int HoldingDays
	{
		get => _holdingDays.Value;
		set => _holdingDays.Value = value;
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
	/// Absolute momentum threshold used to classify recent winners and losers.
	/// </summary>
	public decimal ReversalThreshold
	{
		get => _reversalThreshold.Value;
		set => _reversalThreshold.Value = value;
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
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="EarningsAnnouncementReversalStrategy"/>.
	/// </summary>
	public EarningsAnnouncementReversalStrategy()
	{
		_lookbackDays = Param(nameof(LookbackDays), 6)
			.SetRange(2, 30)
			.SetDisplay("Lookback Days", "Number of bars used to calculate recent return", "Parameters");

		_holdingDays = Param(nameof(HoldingDays), 3)
			.SetRange(1, 20)
			.SetDisplay("Holding Days", "Bars to hold the position after the event", "Parameters");

		_eventCycleBars = Param(nameof(EventCycleBars), 20)
			.SetRange(8, 80)
			.SetDisplay("Event Cycle Bars", "Distance between synthetic earnings events", "Parameters");

		_reversalThreshold = Param(nameof(ReversalThreshold), 1.2m)
			.SetRange(0.1m, 20m)
			.SetDisplay("Reversal Threshold", "Absolute momentum threshold used to classify winners and losers", "Parameters");

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

		_momentum = null!;
		_barIndex = 0;
		_holdingRemaining = 0;
		_cooldownRemaining = 0;
		_latestMomentum = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Primary security is not specified.");

		_momentum = new RateOfChange { Length = LookbackDays };

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

		var momentumValue = _momentum.Process(candle);
		if (momentumValue.IsEmpty || !_momentum.IsFormed || !IsFormedAndOnlineAndAllowTrading())
		{
			_barIndex++;
			return;
		}

		_latestMomentum = momentumValue.ToDecimal();

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		if (_holdingRemaining > 0)
		{
			_holdingRemaining--;

			if (_holdingRemaining == 0 && Position != 0)
			{
				if (Position > 0)
					SellMarket(Position);
				else
					BuyMarket(Math.Abs(Position));

				_cooldownRemaining = CooldownBars;
			}
		}

		var isEventBar = _barIndex > 0 && _barIndex % EventCycleBars == 0;
		if (_cooldownRemaining == 0 && Position == 0 && isEventBar)
		{
			if (_latestMomentum >= ReversalThreshold)
			{
				SellMarket();
				_holdingRemaining = HoldingDays;
				_cooldownRemaining = CooldownBars;
			}
			else if (_latestMomentum <= -ReversalThreshold)
			{
				BuyMarket();
				_holdingRemaining = HoldingDays;
				_cooldownRemaining = CooldownBars;
			}
		}

		_barIndex++;
	}
}
