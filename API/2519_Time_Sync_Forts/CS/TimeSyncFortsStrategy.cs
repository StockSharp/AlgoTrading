using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Synchronizes the local Windows clock with the exchange server time during predefined maintenance windows.
/// </summary>
public class TimeSyncFortsStrategy : Strategy
{
	private readonly StrategyParam<DayOfWeek> _firstSkippedDay;
	private readonly StrategyParam<DayOfWeek> _secondSkippedDay;
	private readonly StrategyParam<int> _latencyCompensation;

	private readonly (TimeSpan Start, TimeSpan End)[] _syncWindows =
	{
		(TimeSpan.FromHours(9) + TimeSpan.FromMinutes(45), TimeSpan.FromHours(10)),
		(TimeSpan.FromHours(10) + TimeSpan.FromMinutes(1), TimeSpan.FromHours(10) + TimeSpan.FromMinutes(2)),
		(TimeSpan.FromHours(13) + TimeSpan.FromMinutes(58), TimeSpan.FromHours(14)),
		(TimeSpan.FromHours(14) + TimeSpan.FromMinutes(5), TimeSpan.FromHours(14) + TimeSpan.FromMinutes(6)),
		(TimeSpan.FromHours(18) + TimeSpan.FromMinutes(43), TimeSpan.FromHours(18) + TimeSpan.FromMinutes(45)),
		(TimeSpan.FromHours(19) + TimeSpan.FromMinutes(5), TimeSpan.FromHours(19) + TimeSpan.FromMinutes(6)),
		(TimeSpan.FromHours(23) + TimeSpan.FromMinutes(48), TimeSpan.FromHours(23) + TimeSpan.FromMinutes(50))
	};

	private bool _isSyncCompleted;
	private bool _isWindows;

	/// <summary>
	/// First weekday when synchronization should be skipped.
	/// </summary>
	public DayOfWeek FirstSkippedDay
	{
		get => _firstSkippedDay.Value;
		set => _firstSkippedDay.Value = value;
	}

	/// <summary>
	/// Second weekday when synchronization should be skipped.
	/// </summary>
	public DayOfWeek SecondSkippedDay
	{
		get => _secondSkippedDay.Value;
		set => _secondSkippedDay.Value = value;
	}

	/// <summary>
	/// Additional milliseconds added to server time to compensate for latency.
	/// </summary>
	public int LatencyCompensationMilliseconds
	{
		get => _latencyCompensation.Value;
		set => _latencyCompensation.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TimeSyncFortsStrategy"/>.
	/// </summary>
	public TimeSyncFortsStrategy()
	{
		_firstSkippedDay = Param(nameof(FirstSkippedDay), DayOfWeek.Saturday)
			.SetDisplay("First skipped day", "Weekday when synchronization should be skipped", "Scheduling");

		_secondSkippedDay = Param(nameof(SecondSkippedDay), DayOfWeek.Sunday)
			.SetDisplay("Second skipped day", "Additional weekday to skip synchronization", "Scheduling");

		_latencyCompensation = Param(nameof(LatencyCompensationMilliseconds), 0)
			.SetDisplay("Latency compensation", "Milliseconds added to server time before syncing", "Timing")
			.SetCanOptimize(true)
			.SetOptimize(-20, 20, 5);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Ticks)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_isSyncCompleted = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

		if (!_isWindows)
		{
			LogWarning("Time synchronization requires Windows. The strategy will only monitor schedule windows.");
		}

		SubscribeTrades()
			.Bind(ProcessTrade)
			.Start();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var serverTime = trade.ServerTime;

		if (serverTime == default)
			return;

		if (ShouldSkipDay(serverTime.DayOfWeek))
		{
			_isSyncCompleted = false;
			return;
		}

		var timeOfDay = serverTime.TimeOfDay;

		if (!IsWithinSyncWindow(timeOfDay))
		{
			_isSyncCompleted = false;
			return;
		}

		if (_isSyncCompleted)
			return;

		if (!_isWindows)
			return;

		var localTime = serverTime.LocalDateTime.AddMilliseconds(LatencyCompensationMilliseconds);

		if (!TryBuildSystemTime(localTime, out var systemTime))
		{
			LogWarning($"Cannot convert server time {serverTime:O} into SYSTEMTIME structure.");
			return;
		}

		if (SetLocalTime(ref systemTime))
		{
			_isSyncCompleted = true;
			LogInfo($"Local time synchronized to {localTime:yyyy-MM-dd HH:mm:ss.fff}. Server time: {serverTime:O}.");
		}
		else
		{
			var error = Marshal.GetLastWin32Error();
			LogError($"Failed to set local time. Win32 error: {error}.");
		}
	}

	private bool ShouldSkipDay(DayOfWeek day)
	{
		return day == FirstSkippedDay || day == SecondSkippedDay;
	}

	private bool IsWithinSyncWindow(TimeSpan time)
	{
		foreach (var window in _syncWindows)
		{
			if (time >= window.Start && time < window.End)
				return true;
		}

		return false;
	}

	private static bool TryBuildSystemTime(DateTime dateTime, out SystemTime systemTime)
	{
		systemTime = default;

		if (dateTime.Year < 1601 || dateTime.Year > 30827)
			return false;

		systemTime = new SystemTime
		{
			Year = (short)dateTime.Year,
			Month = (short)dateTime.Month,
			Day = (short)dateTime.Day,
			DayOfWeek = (short)dateTime.DayOfWeek,
			Hour = (short)dateTime.Hour,
			Minute = (short)dateTime.Minute,
			Second = (short)dateTime.Second,
			Milliseconds = (short)dateTime.Millisecond
		};

		return true;
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetLocalTime(ref SystemTime systemTime);

	[StructLayout(LayoutKind.Sequential)]
	private struct SystemTime
	{
		public short Year;
		public short Month;
		public short DayOfWeek;
		public short Day;
		public short Hour;
		public short Minute;
		public short Second;
		public short Milliseconds;
	}
}
