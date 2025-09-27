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
/// Strategy that plays a configurable sound and logs a notification when a position is closed.
/// </summary>
public class SoundAlertEntryOutStrategy : Strategy
{
	/// <summary>
	/// Available notification sounds.
	/// </summary>
	public enum NotificationSounds
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

	private readonly StrategyParam<NotificationSounds> _sound;
	private readonly StrategyParam<bool> _notificationEnabled;

	private decimal _previousPosition;
	private decimal _previousPnL;

	/// <summary>
	/// Selected sound that will be played on exit events.
	/// </summary>
	public NotificationSounds Sound
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
		_sound = Param(nameof(Sound), NotificationSounds.Alert2)
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

		var orderDirection = trade.Order.Direction;

		if (orderDirection is null)
			return;

		var isClosingLong = positionBeforeTrade > 0m && orderDirection == Sides.Sell;
		var isClosingShort = positionBeforeTrade < 0m && orderDirection == Sides.Buy;

		if (!isClosingLong && !isClosingShort)
			return;

		LogInfo($"Play sound: {GetSoundFileName(Sound)}");

		if (!NotificationEnabled)
			return;

		var tradeId = trade.Trade?.Id;
		var tradeVolume = trade.Trade?.Volume ?? trade.Order.Volume ?? 0m;
		var symbol = trade.Trade?.Security?.Id ?? Security?.Id.ToString() ?? "UNKNOWN";
		var profit = _previousPnL - pnlBeforeTrade;
		var directionText = orderDirection == Sides.Buy ? "buy" : "sell";

		LogInfo($"Deal #{tradeId ?? "N/A"} {directionText} {tradeVolume:F2} {symbol}, profit: {profit:F2}");
	}

	private static string GetSoundFileName(NotificationSounds sound)
	{
		return sound switch
		{
			NotificationSounds.Alert => "alert.wav",
			NotificationSounds.Alert2 => "alert2.wav",
			NotificationSounds.Connect => "connect.wav",
			NotificationSounds.Disconnect => "disconnect.wav",
			NotificationSounds.Email => "email.wav",
			NotificationSounds.Expert => "expert.wav",
			NotificationSounds.News => "news.wav",
			NotificationSounds.Ok => "ok.wav",
			NotificationSounds.Request => "request.wav",
			NotificationSounds.Stops => "stops.wav",
			NotificationSounds.Tick => "tick.wav",
			NotificationSounds.Timeout => "timeout.wav",
			NotificationSounds.Wait => "wait.wav",
			_ => "alert2.wav",
		};
	}
}