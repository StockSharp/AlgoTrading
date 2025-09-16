using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Tracks portfolio balance, equity, and margin and writes statistics to CSV.
/// Sends optional alerts when margin level exceeds predefined thresholds.
/// </summary>
public class CsvMarginTrackerStrategy : Strategy
{
	private readonly StrategyParam<int> _intervalSeconds;
	private readonly StrategyParam<bool> _mailAlert;
	private readonly StrategyParam<int> _mailAlertIntervalSeconds;
	private readonly StrategyParam<decimal> _mailAlertMarginLevel1;
	private readonly StrategyParam<decimal> _mailAlertMarginLevel2;

	private Timer _timer;
	private StreamWriter _writer;
	private DateTimeOffset _intervalStart;
	private DateTimeOffset _nextAlert1;
	private DateTimeOffset _nextAlert2;
	private decimal _minBalance;
	private decimal _minEquity;
	private decimal _maxMargin;

	/// <summary>
	/// Interval length in seconds.
	/// </summary>
	public int IntervalSeconds
	{
		get => _intervalSeconds.Value;
		set => _intervalSeconds.Value = value;
	}

	/// <summary>
	/// Enable sending margin alerts.
	/// </summary>
	public bool MailAlert
	{
		get => _mailAlert.Value;
		set => _mailAlert.Value = value;
	}

	/// <summary>
	/// Minimum delay between mail alerts in seconds.
	/// </summary>
	public int MailAlertIntervalSeconds
	{
		get => _mailAlertIntervalSeconds.Value;
		set => _mailAlertIntervalSeconds.Value = value;
	}

	/// <summary>
	/// First margin threshold (ratio of margin to equity).
	/// </summary>
	public decimal MailAlertMarginLevel1
	{
		get => _mailAlertMarginLevel1.Value;
		set => _mailAlertMarginLevel1.Value = value;
	}

	/// <summary>
	/// Second margin threshold (ratio of margin to equity).
	/// </summary>
	public decimal MailAlertMarginLevel2
	{
		get => _mailAlertMarginLevel2.Value;
		set => _mailAlertMarginLevel2.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public CsvMarginTrackerStrategy()
	{
		_intervalSeconds = Param(nameof(IntervalSeconds), 900)
			.SetGreaterThanZero()
			.SetDisplay("Interval Seconds", "Length of aggregation interval", "General");

		_mailAlert = Param(nameof(MailAlert), false)
			.SetDisplay("Mail Alert", "Enable margin level email alerts", "Alerts");

		_mailAlertIntervalSeconds = Param(nameof(MailAlertIntervalSeconds), 21600)
			.SetGreaterThanZero()
			.SetDisplay("Mail Alert Interval", "Minimal delay between alerts", "Alerts");

		_mailAlertMarginLevel1 = Param(nameof(MailAlertMarginLevel1), 0.6m)
			.SetDisplay("Margin Level 1", "First margin threshold", "Alerts");

		_mailAlertMarginLevel2 = Param(nameof(MailAlertMarginLevel2), 0.8m)
			.SetDisplay("Margin Level 2", "Second margin threshold", "Alerts");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield break;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fileName = $"margintracker_{Portfolio?.Name ?? "portfolio"}.csv";
		_writer = new StreamWriter(File.Open(fileName, FileMode.Append, FileAccess.Write, FileShare.Read))
		{
			AutoFlush = true
		};

		ResetInterval(time);
		_nextAlert1 = time;
		_nextAlert2 = time;

		_timer = new Timer(OnTimer, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
	}

	/// <inheritdoc />
	protected override void OnStopped(DateTimeOffset time)
	{
		_timer?.Dispose();
		_writer?.Dispose();
		base.OnStopped(time);
	}

	private void OnTimer(object state)
	{
		var now = CurrentTime;
		UpdateValues();

		if (now - _intervalStart >= TimeSpan.FromSeconds(IntervalSeconds))
		{
			WriteData(now);
			ResetInterval(now);
		}

		if (!MailAlert)
			return;

		CheckAlert(now, MailAlertMarginLevel1, ref _nextAlert1);
		CheckAlert(now, MailAlertMarginLevel2, ref _nextAlert2);
	}

	private void UpdateValues()
	{
		var balance = Portfolio?.BeginValue ?? 0m;
		var equity = Portfolio?.CurrentValue ?? balance;
		var margin = Portfolio?.BlockedValue ?? 0m;

		if (balance < _minBalance)
			_minBalance = balance;
		if (equity < _minEquity)
			_minEquity = equity;
		if (margin > _maxMargin)
			_maxMargin = margin;
	}

	private void WriteData(DateTimeOffset time)
	{
		_writer.WriteLine($"{time:O};{time:yyyy-MM-dd};{time:HH:mm};{_minBalance};{_minEquity};{_maxMargin}");
	}

	private void ResetInterval(DateTimeOffset time)
	{
		_intervalStart = time;
		_minBalance = decimal.MaxValue;
		_minEquity = decimal.MaxValue;
		_maxMargin = 0m;
	}

	private void CheckAlert(DateTimeOffset now, decimal level, ref DateTimeOffset nextTime)
	{
		var equity = Portfolio?.CurrentValue ?? 0m;
		var margin = Portfolio?.BlockedValue ?? 0m;

		if (equity <= 0 || margin / equity < level || now < nextTime)
			return;

		LogInfo($"Margin alert {level:P0}. Balance: {Portfolio?.BeginValue ?? 0m}, Equity: {equity}, Margin: {margin}.");
		nextTime = now + TimeSpan.FromSeconds(MailAlertIntervalSeconds);
	}
}
