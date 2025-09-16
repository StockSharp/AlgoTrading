using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Global stop management strategy with an optional trading window.
/// </summary>
public class GlobalStopTimerStrategy : Strategy
{
	private readonly StrategyParam<StopMode> _stopMode;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _useTradingWindow;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _initialBalance;
	private bool _isStopped;

	/// <summary>
	/// Determines how the stop levels are evaluated.
	/// </summary>
	public StopMode StopCalculationMode
	{
		get => _stopMode.Value;
		set => _stopMode.Value = value;
	}

	/// <summary>
	/// Global stop loss threshold.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Global take profit threshold.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Enables the time filter for trading.
	/// </summary>
	public bool UseTradingWindow
	{
		get => _useTradingWindow.Value;
		set => _useTradingWindow.Value = value;
	}

	/// <summary>
	/// Hour when the session starts.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Minute when the session starts.
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	/// <summary>
	/// Hour when the session ends.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Minute when the session ends.
	/// </summary>
	public int EndMinute
	{
		get => _endMinute.Value;
		set => _endMinute.Value = value;
	}

	/// <summary>
	/// Candle series used for time-based evaluations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GlobalStopTimerStrategy"/> class.
	/// </summary>
	public GlobalStopTimerStrategy()
	{
		_stopMode = Param(nameof(StopCalculationMode), StopMode.Percent)
			.SetDisplay("Stop Mode", "Use percent or currency based stops", "Risk");

		_stopLoss = Param(nameof(StopLoss), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Global loss limit", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Global profit target", "Risk");

		_useTradingWindow = Param(nameof(UseTradingWindow), true)
			.SetDisplay("Use Trading Window", "Restrict trading to a time window", "Session");

		_startHour = Param(nameof(StartHour), 0)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Session start hour", "Session");

		_startMinute = Param(nameof(StartMinute), 0)
			.SetRange(0, 59)
			.SetDisplay("Start Minute", "Session start minute", "Session");

		_endHour = Param(nameof(EndHour), 23)
			.SetRange(0, 23)
			.SetDisplay("End Hour", "Session end hour", "Session");

		_endMinute = Param(nameof(EndMinute), 59)
			.SetRange(0, 59)
			.SetDisplay("End Minute", "Session end minute", "Session");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for evaluations", "General");
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
		_initialBalance = 0m;
		_isStopped = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		_initialBalance = Portfolio?.CurrentValue ?? 0m;
		_isStopped = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var portfolioValue = Portfolio?.CurrentValue;
		if (portfolioValue is null)
			return;

		if (_initialBalance <= 0m)
			_initialBalance = portfolioValue.Value;

		if (!_isStopped)
		{
			var profit = portfolioValue.Value - _initialBalance;

			switch (StopCalculationMode)
			{
				case StopMode.Percent:
				{
					if (_initialBalance == 0m)
						break;

					var profitPercent = profit / _initialBalance * 100m;

					if (profitPercent <= -StopLoss)
					{
						_isStopped = true;
						this.AddInfoLog($"Global loss limit reached at {profitPercent:F2}%.");
					}
					else if (profitPercent >= TakeProfit)
					{
						_isStopped = true;
						this.AddInfoLog($"Global profit target reached at {profitPercent:F2}%.");
					}

					break;
				}
				case StopMode.Currency:
				{
					if (profit <= -StopLoss)
					{
						_isStopped = true;
						this.AddInfoLog($"Global loss limit reached at {profit:F2}.");
					}
					else if (profit >= TakeProfit)
					{
						_isStopped = true;
						this.AddInfoLog($"Global profit target reached at {profit:F2}.");
					}

					break;
				}
			}
		}

		var referenceTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;
		var tradeAllowed = !UseTradingWindow || IsWithinTradingWindow(referenceTime);

		if (_isStopped || (UseTradingWindow && !tradeAllowed))
		{
			if (Position == 0)
			{
				if (_isStopped)
					_isStopped = false;

				return;
			}

			ClosePosition();

			if (Position == 0 && _isStopped)
				_isStopped = false;
		}
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var hour = time.Hour;
		var minute = time.Minute;

		if (StartHour < EndHour)
		{
			if (hour == StartHour && minute >= StartMinute)
				return true;

			if (hour > StartHour && hour < EndHour)
				return true;

			if (hour > StartHour && hour == EndHour && minute < EndMinute)
				return true;
		}
		else if (StartHour == EndHour)
		{
			if (hour == StartHour && minute >= StartMinute && minute < EndMinute)
				return true;
		}
		else
		{
			if (hour >= StartHour && minute >= StartMinute)
				return true;

			if (hour < EndHour)
				return true;

			if (hour == EndHour && minute < EndMinute)
				return true;
		}

		return false;
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Math.Abs(Position));
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));
	}

	/// <summary>
	/// Stop calculation modes.
	/// </summary>
	public enum StopMode
	{
		/// <summary>
		/// Evaluate stops using percentage change of the portfolio value.
		/// </summary>
		Percent,

		/// <summary>
		/// Evaluate stops using absolute profit or loss in account currency.
		/// </summary>
		Currency
	}
}
