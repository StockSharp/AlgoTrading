using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that sends notifications on order close and daily status.
/// </summary>
public class StatusMailAndAlertOnOrderCloseStrategy : Strategy
{
	private readonly StrategyParam<bool> _sendReportEmail;
	private readonly StrategyParam<int> _statusEmailMinute;
	private readonly StrategyParam<bool> _sendClosedEmail;
	private readonly StrategyParam<decimal> _startBalance;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _lastReportDate;
	private DateTimeOffset _lastCloseTime;

	/// <summary>
	/// Enable daily status notifications.
	/// </summary>
	public bool SendReportEmail
	{
		get => _sendReportEmail.Value;
		set => _sendReportEmail.Value = value;
	}

	/// <summary>
	/// Minute of hour to send status notification.
	/// </summary>
	public int StatusEmailMinute
	{
		get => _statusEmailMinute.Value;
		set => _statusEmailMinute.Value = value;
	}

	/// <summary>
	/// Notify on each closed order.
	/// </summary>
	public bool SendClosedEmail
	{
		get => _sendClosedEmail.Value;
		set => _sendClosedEmail.Value = value;
	}

	/// <summary>
	/// Initial account balance.
	/// </summary>
	public decimal StartBalance
	{
		get => _startBalance.Value;
		set => _startBalance.Value = value;
	}

	/// <summary>
	/// Candle type used for time checks.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public StatusMailAndAlertOnOrderCloseStrategy()
	{
		_sendReportEmail = Param(nameof(SendReportEmail), false)
			.SetDisplay("Send Report Email", "Enable daily status email", "Notifications");

		_statusEmailMinute = Param(nameof(StatusEmailMinute), 55)
			.SetDisplay("Status Email Minute", "Minute to send status email", "Notifications");

		_sendClosedEmail = Param(nameof(SendClosedEmail), false)
			.SetDisplay("Send Closed Email", "Notify when an order closes", "Notifications");

		_startBalance = Param(nameof(StartBalance), 500m)
			.SetDisplay("Start Balance", "Initial account balance", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for monitoring", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!SendReportEmail)
			return;

		var closeTime = candle.CloseTime.UtcDateTime;

		if (closeTime.Minute != StatusEmailMinute)
			return;

		if (closeTime.Date == _lastReportDate)
			return;

		_lastReportDate = closeTime.Date;

		var balance = Portfolio?.CurrentValue ?? 0m;
		var profit = balance - StartBalance;
		var percent = StartBalance == 0 ? 0 : (balance * 100m / StartBalance - 100m);

		AddInfo($"Account balance {balance:0.##}. Profit {profit:0.##} ({percent:0.##}%). Open orders {Orders.Count}.");
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (!SendClosedEmail)
			return;

		var time = trade.Trade.Time;

		if (time <= _lastCloseTime)
			return;

		_lastCloseTime = time;

		var order = trade.Order;

		AddInfo($"Closed {order.Direction} order #{order.Id} at {trade.Trade.Price:0.####}. Balance {Portfolio?.CurrentValue ?? 0m:0.##}. PnL {PnL:0.##}.");
	}
}

