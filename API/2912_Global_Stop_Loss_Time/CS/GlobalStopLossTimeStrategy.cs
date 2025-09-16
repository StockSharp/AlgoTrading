using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Risk manager that enforces a global stop loss and optional trading session filter.
/// </summary>
public class GlobalStopLossTimeStrategy : Strategy
{
	/// <summary>
	/// Loss measurement mode.
	/// </summary>
	public enum LossMeasurementMode
	{
		/// <summary>
		/// Stop loss is evaluated as percentage of the account balance.
		/// </summary>
		Percent,

		/// <summary>
		/// Stop loss is evaluated as absolute account currency value.
		/// </summary>
		Currency
	}

	private readonly StrategyParam<LossMeasurementMode> _lossMode;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;
	private readonly StrategyParam<DataType> _candleType;

	private bool _stopActivated;
	private decimal _baselinePnL;

	/// <summary>
	/// Initializes a new instance of <see cref="GlobalStopLossTimeStrategy"/>.
	/// </summary>
	public GlobalStopLossTimeStrategy()
	{
		_lossMode = Param(nameof(LossMode), LossMeasurementMode.Percent)
			.SetDisplay("Loss Mode", "How the loss threshold is measured", "Risk");

		_stopLoss = Param(nameof(StopLoss), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Loss threshold that triggers position closing", "Risk")
			.SetCanOptimize(true);

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
			.SetDisplay("Use Time Filter", "Enable restriction of trading hours", "Timing");

		_startTime = Param(nameof(StartTime), TimeSpan.Zero)
			.SetDisplay("Start Time", "Start of the trading window (UTC)", "Timing");

		_endTime = Param(nameof(EndTime), new TimeSpan(23, 59, 0))
			.SetDisplay("End Time", "End of the trading window (UTC)", "Timing");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles used to evaluate time and risk rules", "General");
	}

	/// <summary>
	/// Selected loss mode.
	/// </summary>
	public LossMeasurementMode LossMode
	{
		get => _lossMode.Value;
		set => _lossMode.Value = value;
	}

	/// <summary>
	/// Stop loss threshold value.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Flag that enables trading hour restriction.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Trading window start time in UTC.
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Trading window end time in UTC.
	/// </summary>
	public TimeSpan EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>
	/// Candle type that triggers evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_stopActivated = false;
		_baselinePnL = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_baselinePnL = PnL;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateBaselineIfFlat();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var inTradeWindow = !UseTimeFilter || IsInTradeWindow(candle.CloseTime.TimeOfDay);
		var profit = GetCurrentProfit();

		if (profit > 0m && !_stopActivated && inTradeWindow)
			return;

		if (!_stopActivated)
		{
			TryActivateStop(profit);
		}

		var shouldClose = _stopActivated || (UseTimeFilter && !inTradeWindow);
		if (shouldClose)
		{
			var reason = _stopActivated && UseTimeFilter && !inTradeWindow
				? "loss threshold reached and outside trading window"
				: _stopActivated
					? "loss threshold reached"
					: "outside trading window";

			CloseOpenPosition(reason);
		}

		if (Position == 0m)
		{
			_stopActivated = false;
			UpdateBaselineIfFlat();
		}
	}

	private void TryActivateStop(decimal profit)
	{
		switch (LossMode)
		{
			case LossMeasurementMode.Percent:
			{
				var balance = Portfolio.CurrentValue ?? 0m;
				if (balance <= 0m)
					return;

				var lossRatio = Math.Abs(profit) / balance * 100m;
				if (lossRatio >= StopLoss)
				{
					_stopActivated = true;
					LogInfo($"Loss ratio {lossRatio:0.##}% exceeded limit {StopLoss:0.##}%.");
				}
				break;
			}

			case LossMeasurementMode.Currency:
			{
				var loss = Math.Abs(profit);
				if (loss >= StopLoss)
				{
					_stopActivated = true;
					LogInfo($"Loss {loss:0.##} exceeded limit {StopLoss:0.##}.");
				}
				break;
			}
		}
	}

	private void CloseOpenPosition(string reason)
	{
		var position = Position;
		if (position == 0m)
			return;

		var volume = Math.Abs(position);

		if (position > 0m)
		{
			SellMarket(volume);
			LogInfo($"Closing long position because {reason}. Volume={volume:0.######}");
		}
		else
		{
			BuyMarket(volume);
			LogInfo($"Closing short position because {reason}. Volume={volume:0.######}");
		}
	}

	private decimal GetCurrentProfit()
	{
		return PnL - _baselinePnL;
	}

	private void UpdateBaselineIfFlat()
	{
		if (Position == 0m)
			_baselinePnL = PnL;
	}

	private bool IsInTradeWindow(TimeSpan time)
	{
		var start = StartTime;
		var end = EndTime;

		if (start == end)
			return time >= start && time < end;

		if (start < end)
			return time >= start && time < end;

		return time >= start || time < end;
	}
}
