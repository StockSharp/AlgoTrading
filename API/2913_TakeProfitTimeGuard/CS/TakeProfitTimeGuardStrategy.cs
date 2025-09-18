using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Monitors account profit and trading window to forcefully flatten positions when conditions are met.
/// </summary>
public class TakeProfitTimeGuardStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<ProfitTargetMode> _targetMode;
	private readonly StrategyParam<decimal> _takeProfitValue;
	private readonly StrategyParam<bool> _useTradingWindow;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;

	private bool _stop;
	private decimal? _initialBalance;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ProfitTargetMode TargetMode
	{
		get => _targetMode.Value;
		set => _targetMode.Value = value;
	}

	public decimal TakeProfitValue
	{
		get => _takeProfitValue.Value;
		set => _takeProfitValue.Value = value;
	}

	public bool UseTradingWindow
	{
		get => _useTradingWindow.Value;
		set => _useTradingWindow.Value = value;
	}

	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	public TimeSpan EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	public TakeProfitTimeGuardStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to evaluate profit and schedule", "General");

		_targetMode = Param(nameof(TargetMode), ProfitTargetMode.Percent)
			.SetDisplay("Target Mode", "Use percent of capital or absolute currency", "Risk Management");

		_takeProfitValue = Param(nameof(TakeProfitValue), 100m)
			.SetDisplay("Target Value", "Profit target expressed in selected mode", "Risk Management")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_useTradingWindow = Param(nameof(UseTradingWindow), true)
			.SetDisplay("Use Trading Window", "Enable scheduled trading window", "Time Filter");

		_startTime = Param(nameof(StartTime), TimeSpan.Zero)
			.SetDisplay("Start Time", "Start of allowed trading window", "Time Filter");

		_endTime = Param(nameof(EndTime), new TimeSpan(23, 59, 0))
			.SetDisplay("End Time", "End of allowed trading window", "Time Filter");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_stop = false;
		_initialBalance = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_initialBalance = Portfolio?.CurrentValue;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_initialBalance is null && Portfolio?.CurrentValue > 0m)
			_initialBalance = Portfolio.CurrentValue;

		var inWindow = !UseTradingWindow || InTimeRange(candle.CloseTime);
		var totalProfit = CalculateTotalProfit(candle.ClosePrice);

		// Do not trigger stop logic while still in trading window with a floating loss.
		if (totalProfit < 0m && !_stop && inWindow)
			return;

		if (!_stop)
			CheckProfitTarget(totalProfit);

		if (_stop || (UseTradingWindow && !inWindow))
		{
			if (Position == 0m)
			{
				if (_stop)
					_stop = false; // Reset after positions were flattened.

				return;
			}

			// Close any open exposure according to the direction of the current position.
			CloseAllPositions();
		}
	}

	private decimal CalculateTotalProfit(decimal lastPrice)
	{
		var realized = PnL;
		if (Position == 0m)
			return realized;

		var avgPrice = PositionPrice;
		if (avgPrice == 0m)
			return realized;

		// Combine realized and unrealized profit based on the latest candle close.
		var unrealized = (lastPrice - avgPrice) * Position;
		return realized + unrealized;
	}

	private void CheckProfitTarget(decimal totalProfit)
	{
		switch (TargetMode)
		{
			case ProfitTargetMode.Percent:
			{
				var basis = _initialBalance ?? Portfolio?.CurrentValue ?? 0m;
				if (basis <= 0m)
					return;

				var ratio = Math.Abs(totalProfit) / basis * 100m;
				if (ratio >= TakeProfitValue)
				{
					_stop = true;
					LogInfo($"Profit target reached: {ratio:F2}% >= {TakeProfitValue:F2}%.");
				}

				break;
			}

			case ProfitTargetMode.Currency:
			{
				if (Math.Abs(totalProfit) >= TakeProfitValue)
				{
					_stop = true;
					LogInfo($"Profit target reached: {Math.Abs(totalProfit):F2} >= {TakeProfitValue:F2}.");
				}

				break;
			}
		}
	}

	private bool InTimeRange(DateTimeOffset time)
	{
		var current = time.TimeOfDay;

		if (StartTime == EndTime)
			return false;

		return StartTime <= EndTime
			? current >= StartTime && current < EndTime
			: current >= StartTime || current < EndTime;
	}

	private void CloseAllPositions()
	{
		if (Position > 0m)
		{
			// Liquidate long exposure with a market sell order.
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			// Cover short exposure with a market buy order.
			BuyMarket(Math.Abs(Position));
		}
	}

	public enum ProfitTargetMode
	{
		Percent,
		Currency
	}
}
