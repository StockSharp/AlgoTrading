using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that plays a configurable sound and logs a notification when a position is closed.
/// </summary>
public class SoundAlertEntryOutStrategy : Strategy
{
	/// <summary>
	/// Available notification sounds.
	/// </summary>
	public enum NotificationSound
	{
		Alert,
		Alert2,
		Connect,
		Disconnect,
		Email,
		Expert,
		News,
		Ok,
		Request,
		Stops,
		Tick,
		Timeout,
		Wait,
	}

	private readonly StrategyParam<NotificationSound> _sound;
	private readonly StrategyParam<bool> _notificationEnabled;

	private decimal _previousPosition;
	private decimal _previousPnL;

	/// <summary>
	/// Selected sound that will be played on exit events.
	/// </summary>
	public NotificationSound Sound
	{
		get => _sound.Value;
		set => _sound.Value = value;
	}

	/// <summary>
	/// Enables additional informational notifications when true.
	/// </summary>
	public bool NotificationEnabled
	{
		get => _notificationEnabled.Value;
		set => _notificationEnabled.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public SoundAlertEntryOutStrategy()
	{
		_sound = Param(nameof(Sound), NotificationSound.Alert2)
			.SetDisplay("Sound", "Sound file used for alerts.", "Notifications")
			.SetCanOptimize(true);

		_notificationEnabled = Param(nameof(NotificationEnabled), false)
			.SetDisplay("Notification", "Enable informational notifications.", "Notifications")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_previousPosition = Position;
		_previousPnL = PnL;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		var positionBeforeTrade = _previousPosition;
		var pnlBeforeTrade = _previousPnL;

		base.OnNewMyTrade(trade);

		_previousPosition = Position;
		_previousPnL = PnL;

		if (positionBeforeTrade == 0m)
			return;

		var orderDirection = trade.Order?.Direction;

		if (orderDirection is null)
			return;

		var isClosingLong = positionBeforeTrade > 0m && orderDirection == Sides.Sell;
		var isClosingShort = positionBeforeTrade < 0m && orderDirection == Sides.Buy;

		if (!isClosingLong && !isClosingShort)
			return;

		AddInfoLog($"Play sound: {GetSoundFileName(Sound)}");

		if (!NotificationEnabled)
			return;

		var tradeId = trade.Trade?.Id;
		var tradeVolume = trade.Trade?.Volume ?? trade.Order?.Volume ?? 0m;
		var symbol = trade.Trade?.Security?.Id ?? Security?.Id.ToString() ?? "UNKNOWN";
		var profit = _previousPnL - pnlBeforeTrade;
		var directionText = orderDirection == Sides.Buy ? "buy" : "sell";

		AddInfoLog($"Deal #{tradeId?.ToString() ?? "N/A"} {directionText} {tradeVolume:F2} {symbol}, profit: {profit:F2}");
	}

	private static string GetSoundFileName(NotificationSound sound)
	{
		return sound switch
		{
			NotificationSound.Alert => "alert.wav",
			NotificationSound.Alert2 => "alert2.wav",
			NotificationSound.Connect => "connect.wav",
			NotificationSound.Disconnect => "disconnect.wav",
			NotificationSound.Email => "email.wav",
			NotificationSound.Expert => "expert.wav",
			NotificationSound.News => "news.wav",
			NotificationSound.Ok => "ok.wav",
			NotificationSound.Request => "request.wav",
			NotificationSound.Stops => "stops.wav",
			NotificationSound.Tick => "tick.wav",
			NotificationSound.Timeout => "timeout.wav",
			NotificationSound.Wait => "wait.wav",
			_ => "alert2.wav",
		};
	}
}
