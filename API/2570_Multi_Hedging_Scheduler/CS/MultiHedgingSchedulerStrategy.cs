using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hedging scheduler strategy that opens positions during a configurable time window
/// and closes when equity targets are reached or a separate exit window arrives.
/// Simplified to single-security from the original multi-symbol version.
/// </summary>
public class MultiHedgingSchedulerStrategy : Strategy
{
	private readonly StrategyParam<Sides> _tradeDirection;
	private readonly StrategyParam<TimeSpan> _tradeStartTime;
	private readonly StrategyParam<TimeSpan> _tradeDuration;
	private readonly StrategyParam<bool> _enableTimeClose;
	private readonly StrategyParam<TimeSpan> _closeTime;
	private readonly StrategyParam<bool> _enableEquityClose;
	private readonly StrategyParam<decimal> _profitPercent;
	private readonly StrategyParam<decimal> _lossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _initialBalance;
	private bool _positionOpened;

	/// <summary>
	/// Trading direction used when opening positions.
	/// </summary>
	public Sides TradeDirection
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
	}

	/// <summary>
	/// Time of day when the trading window starts.
	/// </summary>
	public TimeSpan TradeStartTime
	{
		get => _tradeStartTime.Value;
		set => _tradeStartTime.Value = value;
	}

	/// <summary>
	/// Duration of the trading and optional closing windows.
	/// </summary>
	public TimeSpan TradeDuration
	{
		get => _tradeDuration.Value;
		set => _tradeDuration.Value = value;
	}

	/// <summary>
	/// Enables the separate time based close window.
	/// </summary>
	public bool UseTimeClose
	{
		get => _enableTimeClose.Value;
		set => _enableTimeClose.Value = value;
	}

	/// <summary>
	/// Time of day when the closing window starts.
	/// </summary>
	public TimeSpan CloseTime
	{
		get => _closeTime.Value;
		set => _closeTime.Value = value;
	}

	/// <summary>
	/// Enables closing when equity reaches profit or loss thresholds.
	/// </summary>
	public bool CloseByEquityPercent
	{
		get => _enableEquityClose.Value;
		set => _enableEquityClose.Value = value;
	}

	/// <summary>
	/// Percentage profit target based on starting balance.
	/// </summary>
	public decimal PercentProfit
	{
		get => _profitPercent.Value;
		set => _profitPercent.Value = value;
	}

	/// <summary>
	/// Percentage loss threshold based on starting balance.
	/// </summary>
	public decimal PercentLoss
	{
		get => _lossPercent.Value;
		set => _lossPercent.Value = value;
	}

	/// <summary>
	/// Candle series driving the scheduling logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes the strategy.
	/// </summary>
	public MultiHedgingSchedulerStrategy()
	{
		_tradeDirection = Param(nameof(TradeDirection), Sides.Buy)
			.SetDisplay("Trade Direction", "Direction used for opening positions", "General");

		_tradeStartTime = Param(nameof(TradeStartTime), new TimeSpan(10, 0, 0))
			.SetDisplay("Trade Start", "Time of day to begin opening positions", "Scheduling");

		_tradeDuration = Param(nameof(TradeDuration), TimeSpan.FromMinutes(5))
			.SetDisplay("Window Length", "Duration of trading and closing windows", "Scheduling");

		_enableTimeClose = Param(nameof(UseTimeClose), true)
			.SetDisplay("Use Close Window", "Enable time based portfolio closing", "Scheduling");

		_closeTime = Param(nameof(CloseTime), new TimeSpan(17, 0, 0))
			.SetDisplay("Close Start", "Time of day to start the close window", "Scheduling");

		_enableEquityClose = Param(nameof(CloseByEquityPercent), true)
			.SetDisplay("Use Equity Targets", "Enable equity based exit", "Risk Management");

		_profitPercent = Param(nameof(PercentProfit), 1m)
			.SetDisplay("Profit %", "Equity percentage gain to close all positions", "Risk Management");

		_lossPercent = Param(nameof(PercentLoss), 55m)
			.SetDisplay("Loss %", "Equity percentage loss to close all positions", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series driving the scheduler", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_initialBalance = 0m;
		_positionOpened = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_initialBalance = Portfolio?.CurrentValue ?? 0m;

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormed)
			return;

		var timeOfDay = candle.OpenTime.TimeOfDay;

		if (CloseByEquityPercent && TryHandleEquityTargets())
			return;

		if (UseTimeClose && IsWithinWindow(timeOfDay, CloseTime, TradeDuration))
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			else if (Position < 0)
				BuyMarket(Math.Abs(Position));

			_positionOpened = false;
			return;
		}

		var direction = TradeDirection;

		if (!IsWithinWindow(timeOfDay, TradeStartTime, TradeDuration))
			return;

		if (_positionOpened)
			return;

		var volume = Volume;
		if (volume <= 0m)
			volume = 1m;

		if (direction == Sides.Buy && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(volume);
			_positionOpened = true;
		}
		else if (direction == Sides.Sell && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Position);
			SellMarket(volume);
			_positionOpened = true;
		}
	}

	private bool TryHandleEquityTargets()
	{
		if (_initialBalance <= 0m)
			return false;

		var equity = Portfolio?.CurrentValue;
		if (equity == null)
			return false;

		var profitLevel = _initialBalance * (1m + PercentProfit / 100m);
		var lossLevel = _initialBalance * (1m - PercentLoss / 100m);

		if (equity.Value >= profitLevel || equity.Value <= lossLevel)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			else if (Position < 0)
				BuyMarket(Math.Abs(Position));

			_positionOpened = false;
			return true;
		}

		return false;
	}

	private static bool IsWithinWindow(TimeSpan current, TimeSpan start, TimeSpan length)
	{
		if (length <= TimeSpan.Zero)
			return current == start;

		var end = start + length;

		if (end < TimeSpan.FromDays(1))
			return current >= start && current < end;

		var overflow = end - TimeSpan.FromDays(1);
		return current >= start || current < overflow;
	}
}
