using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mirrors the original AutoTrading Scheduler expert advisor.
/// Maintains a weekly timetable that toggles automated trading and optionally flat positions.
/// </summary>
public class AutoTradingSchedulerStrategy : Strategy
{
	private readonly StrategyParam<bool> _schedulerEnabled;
	private readonly StrategyParam<TimeReference> _referenceClock;
	private readonly StrategyParam<bool> _closePositions;
	private readonly StrategyParam<string> _mondaySchedule;
	private readonly StrategyParam<string> _tuesdaySchedule;
	private readonly StrategyParam<string> _wednesdaySchedule;
	private readonly StrategyParam<string> _thursdaySchedule;
	private readonly StrategyParam<string> _fridaySchedule;
	private readonly StrategyParam<string> _saturdaySchedule;
	private readonly StrategyParam<string> _sundaySchedule;

	private Timer _timer;
	private bool _autoTradingEnabled = true;
	private string _scheduleSnapshot = string.Empty;
	private Dictionary<DayOfWeek, IReadOnlyList<TimeRange>> _weeklySchedule = new();

	/// <summary>
	/// Enables or disables the scheduler.
	/// </summary>
	public bool SchedulerEnabled
	{
		get => _schedulerEnabled.Value;
		set => _schedulerEnabled.Value = value;
	}

	/// <summary>
	/// Selects whether local or exchange/server time should be used.
	/// </summary>
	public TimeReference ReferenceClock
	{
		get => _referenceClock.Value;
		set => _referenceClock.Value = value;
	}

	/// <summary>
	/// When true the strategy closes open positions before disabling trading.
	/// </summary>
	public bool ClosePositionsBeforeDisable
	{
		get => _closePositions.Value;
		set => _closePositions.Value = value;
	}

	/// <summary>
	/// Monday schedule in "HH[:MM]-HH[:MM]" format separated by commas.
	/// </summary>
	public string MondaySchedule
	{
		get => _mondaySchedule.Value;
		set => _mondaySchedule.Value = value;
	}

	/// <summary>
	/// Tuesday schedule in "HH[:MM]-HH[:MM]" format separated by commas.
	/// </summary>
	public string TuesdaySchedule
	{
		get => _tuesdaySchedule.Value;
		set => _tuesdaySchedule.Value = value;
	}

	/// <summary>
	/// Wednesday schedule in "HH[:MM]-HH[:MM]" format separated by commas.
	/// </summary>
	public string WednesdaySchedule
	{
		get => _wednesdaySchedule.Value;
		set => _wednesdaySchedule.Value = value;
	}

	/// <summary>
	/// Thursday schedule in "HH[:MM]-HH[:MM]" format separated by commas.
	/// </summary>
	public string ThursdaySchedule
	{
		get => _thursdaySchedule.Value;
		set => _thursdaySchedule.Value = value;
	}

	/// <summary>
	/// Friday schedule in "HH[:MM]-HH[:MM]" format separated by commas.
	/// </summary>
	public string FridaySchedule
	{
		get => _fridaySchedule.Value;
		set => _fridaySchedule.Value = value;
	}

	/// <summary>
	/// Saturday schedule in "HH[:MM]-HH[:MM]" format separated by commas.
	/// </summary>
	public string SaturdaySchedule
	{
		get => _saturdaySchedule.Value;
		set => _saturdaySchedule.Value = value;
	}

	/// <summary>
	/// Sunday schedule in "HH[:MM]-HH[:MM]" format separated by commas.
	/// </summary>
	public string SundaySchedule
	{
		get => _sundaySchedule.Value;
		set => _sundaySchedule.Value = value;
	}

	/// <summary>
	/// Indicates whether automated trading is currently allowed by the scheduler.
	/// </summary>
	public bool IsAutoTradingEnabled => _autoTradingEnabled;

	/// <summary>
	/// Initializes a new instance of the <see cref="AutoTradingSchedulerStrategy"/> class.
	/// </summary>
	public AutoTradingSchedulerStrategy()
	{
		_schedulerEnabled = Param(nameof(SchedulerEnabled), false)
		.SetDisplay("Scheduler Enabled", "Turns the timetable module on or off", "General");

		_referenceClock = Param(nameof(ReferenceClock), TimeReference.Local)
		.SetDisplay("Reference Clock", "Pick Local or Exchange time base", "General");

		_closePositions = Param(nameof(ClosePositionsBeforeDisable), true)
		.SetDisplay("Close Positions", "Flatten and cancel orders before disabling", "General");

		_mondaySchedule = Param(nameof(MondaySchedule), string.Empty)
		.SetDisplay("Monday", "Trading windows for Monday", "Schedule");

		_tuesdaySchedule = Param(nameof(TuesdaySchedule), string.Empty)
		.SetDisplay("Tuesday", "Trading windows for Tuesday", "Schedule");

		_wednesdaySchedule = Param(nameof(WednesdaySchedule), string.Empty)
		.SetDisplay("Wednesday", "Trading windows for Wednesday", "Schedule");

		_thursdaySchedule = Param(nameof(ThursdaySchedule), string.Empty)
		.SetDisplay("Thursday", "Trading windows for Thursday", "Schedule");

		_fridaySchedule = Param(nameof(FridaySchedule), string.Empty)
		.SetDisplay("Friday", "Trading windows for Friday", "Schedule");

		_saturdaySchedule = Param(nameof(SaturdaySchedule), string.Empty)
		.SetDisplay("Saturday", "Trading windows for Saturday", "Schedule");

		_sundaySchedule = Param(nameof(SundaySchedule), string.Empty)
		.SetDisplay("Sunday", "Trading windows for Sunday", "Schedule");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		RefreshSchedule();
		EvaluateAutoTradingState(GetReferenceTime());

		_timer = new Timer(_ => OnTimer(), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		_timer?.Dispose();
		_timer = null;
		base.OnStopped();
	}

	private void OnTimer()
	{
		try
		{
			RefreshSchedule();
			EvaluateAutoTradingState(GetReferenceTime());
		}
		catch (Exception ex)
		{
			LogError("Scheduler timer error: {0}", ex.Message);
		}
	}

	private void RefreshSchedule()
	{
		var snapshot = string.Join("|", SchedulerEnabled, ReferenceClock, ClosePositionsBeforeDisable,
			MondaySchedule, TuesdaySchedule, WednesdaySchedule, ThursdaySchedule,
			FridaySchedule, SaturdaySchedule, SundaySchedule);

		if (snapshot == _scheduleSnapshot)
			return;

		var map = new Dictionary<DayOfWeek, IReadOnlyList<TimeRange>>
		{
			[DayOfWeek.Monday] = ParseSchedule(MondaySchedule, DayOfWeek.Monday),
			[DayOfWeek.Tuesday] = ParseSchedule(TuesdaySchedule, DayOfWeek.Tuesday),
			[DayOfWeek.Wednesday] = ParseSchedule(WednesdaySchedule, DayOfWeek.Wednesday),
			[DayOfWeek.Thursday] = ParseSchedule(ThursdaySchedule, DayOfWeek.Thursday),
			[DayOfWeek.Friday] = ParseSchedule(FridaySchedule, DayOfWeek.Friday),
			[DayOfWeek.Saturday] = ParseSchedule(SaturdaySchedule, DayOfWeek.Saturday),
			[DayOfWeek.Sunday] = ParseSchedule(SundaySchedule, DayOfWeek.Sunday)
		};

		_weeklySchedule = map;
		_scheduleSnapshot = snapshot;
	}

	private IReadOnlyList<TimeRange> ParseSchedule(string text, DayOfWeek day)
	{
		var ranges = new List<TimeRange>();

		if (text.IsEmptyOrWhiteSpace())
			return ranges;

		var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries);

		foreach (var part in parts)
		{
			var range = part.Trim();
			if (range.Length == 0)
				continue;

			var bounds = range.Split('-', StringSplitOptions.RemoveEmptyEntries);
			if (bounds.Length != 2)
			{
				LogWarning("{0}: invalid range '{1}'.", day, range);
				continue;
			}

			var start = ParseTime(bounds[0]);
			var end = ParseTime(bounds[1]);

			if (start == null || end == null)
			{
				LogWarning("{0}: failed to parse '{1}'.", day, range);
				continue;
			}

			ranges.Add(new TimeRange(start.Value, end.Value));
		}

		return ranges;
	}

	private static TimeSpan? ParseTime(string value)
	{
		if (value.IsEmptyOrWhiteSpace())
			return null;

		var cleaned = value.Trim();
		cleaned = cleaned.Replace('.', ':');

		if (!cleaned.Contains(':', StringComparison.Ordinal))
		{
			if (!int.TryParse(cleaned, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hour))
				return null;

			if (hour < 0 || hour > 23)
				return null;

			return new TimeSpan(hour, 0, 0);
		}

		var components = cleaned.Split(':', StringSplitOptions.RemoveEmptyEntries);
		if (components.Length != 2)
			return null;

		if (!int.TryParse(components[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours))
			return null;

		if (!int.TryParse(components[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes))
			return null;

		if (hours < 0 || hours > 23)
			return null;

		if (minutes < 0 || minutes > 59)
			return null;

		return new TimeSpan(hours, minutes, 0);
	}

	private void EvaluateAutoTradingState(DateTimeOffset time)
	{
		if (!SchedulerEnabled)
		{
			if (!_autoTradingEnabled)
				SetAutoTradingState(true, "Scheduler disabled");
			return;
		}

		var day = time.DayOfWeek;
		var timeOfDay = time.TimeOfDay;
		var ranges = _weeklySchedule.TryGetValue(day, out var dayRanges) ? dayRanges : Array.Empty<TimeRange>();

		var shouldEnable = ranges.Any(r => r.Contains(timeOfDay));

		if (shouldEnable == _autoTradingEnabled)
			return;

		SetAutoTradingState(shouldEnable, shouldEnable ? "Schedule window opened" : "Schedule window closed");
	}

	private void SetAutoTradingState(bool enable, string reason)
	{
		_autoTradingEnabled = enable;
		LogInfo("AutoTrading {0}: {1}.", enable ? "enabled" : "disabled", reason);

		if (!enable && ClosePositionsBeforeDisable)
		{
			CancelActiveOrders();
			FlattenPosition();
		}
	}

	private void FlattenPosition()
	{
		var position = Position;
		if (position == 0)
			return;

		var volume = Math.Abs(position);
		if (position > 0)
			SellMarket(volume);
		else
			BuyMarket(volume);
	}

	private DateTimeOffset GetReferenceTime()
	{
		var now = ReferenceClock == TimeReference.Local ? DateTimeOffset.Now : CurrentTime;
		if (now == default)
			now = DateTimeOffset.Now;
		return now;
	}

	private readonly record struct TimeRange(TimeSpan Start, TimeSpan End)
	{
		public bool Contains(TimeSpan time)
		{
			if (End == Start)
				return true;

			if (End > Start)
				return time >= Start && time < End;

			if (End == TimeSpan.Zero && Start >= End)
				return time >= Start;

			return time >= Start || time < End;
		}
	}

	/// <summary>
	/// Type of time base used by the scheduler.
	/// </summary>
	public enum TimeReference
	{
		/// <summary>
		/// Use the local machine time.
		/// </summary>
		Local,

		/// <summary>
		/// Use the exchange/server time supplied by the connector.
		/// </summary>
		Exchange
	}
}
