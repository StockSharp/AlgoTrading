using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that mirrors the "pull all ticks" MetaTrader script by continuously downloading tick packets.
/// </summary>
public class PullAllTicksStrategy : Strategy
{
	private readonly StrategyParam<bool> _limitDate;
	private readonly StrategyParam<DateTimeOffset> _oldestLimit;
	private readonly StrategyParam<int> _tickPacketSize;
	private readonly StrategyParam<TimeSpan> _requestDelay;
	private readonly StrategyParam<string> _managerFolder;
	private readonly StrategyParam<string> _statusFileName;

	private DateTimeOffset? _oldestTick;
	private DateTimeOffset? _latestTick;
	private DateTimeOffset? _lastStatusUpdate;
	private long _totalTicks;
	private long _totalPackets;
	private int _packetCounter;
	private string _statusPath;

	/// <summary>
	/// Initializes a new instance of the <see cref="PullAllTicksStrategy"/> class.
	/// </summary>
	public PullAllTicksStrategy()
	{
		_limitDate = Param(nameof(LimitDate), true)
		.SetDisplay("Limit Date", "Stop once the oldest requested tick reaches the configured bound", "General");

		_oldestLimit = Param(nameof(OldestLimit), new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero))
		.SetDisplay("Oldest Limit", "Lower boundary for historical tick requests", "General");

		_tickPacketSize = Param(nameof(TickPacketSize), 300_000)
		.SetNotNegative()
		.SetDisplay("Tick Packet Size", "Number of ticks processed before persisting state", "General");

		_requestDelay = Param(nameof(RequestDelay), TimeSpan.FromMilliseconds(44))
		.SetDisplay("Request Delay", "Minimum delay between status updates", "General");

		_managerFolder = Param(nameof(ManagerFolder), "pat")
		.SetDisplay("Manager Folder", "Folder where the progress file is stored", "Storage");

		_statusFileName = Param(nameof(StatusFileName), "status.txt")
		.SetDisplay("Status File", "File that keeps the resume information", "Storage");
	}

	/// <summary>
	/// Limit the historical scan by date.
	/// </summary>
	public bool LimitDate
	{
		get => _limitDate.Value;
		set => _limitDate.Value = value;
	}

	/// <summary>
	/// Oldest timestamp allowed when scanning ticks backwards.
	/// </summary>
	public DateTimeOffset OldestLimit
	{
		get => _oldestLimit.Value;
		set => _oldestLimit.Value = value;
	}

	/// <summary>
	/// Number of ticks that form a logical packet before persistence.
	/// </summary>
	public int TickPacketSize
	{
		get => _tickPacketSize.Value;
		set => _tickPacketSize.Value = value;
	}

	/// <summary>
	/// Minimum delay between two consecutive status updates.
	/// </summary>
	public TimeSpan RequestDelay
	{
		get => _requestDelay.Value;
		set => _requestDelay.Value = value;
	}

	/// <summary>
	/// Directory that contains the status file.
	/// </summary>
	public string ManagerFolder
	{
		get => _managerFolder.Value;
		set => _managerFolder.Value = value;
	}

	/// <summary>
	/// Name of the file used to persist progress information.
	/// </summary>
	public string StatusFileName
	{
		get => _statusFileName.Value;
		set => _statusFileName.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
		(Security, DataType.Ticks)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_oldestTick = null;
		_latestTick = null;
		_lastStatusUpdate = null;
		_totalTicks = 0;
		_totalPackets = 0;
		_packetCounter = 0;
		_statusPath = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_statusPath = GetStatusPath();
		LoadState();

		SubscribeTrades().Bind(ProcessTrade).Start();

		UpdateStatusComment();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		SaveState();

		base.OnStopped();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var tradeTime = trade.ServerTime;
		if (tradeTime == default)
		return;

		if (_oldestTick == null || tradeTime < _oldestTick)
		_oldestTick = tradeTime;

		if (_latestTick == null || tradeTime > _latestTick)
		_latestTick = tradeTime;

		_totalTicks++;
		_packetCounter++;

		if (_packetCounter >= TickPacketSize && TickPacketSize > 0)
		{
			_packetCounter = 0;
			_totalPackets++;
			SaveState();
		}

		var lastUpdate = _lastStatusUpdate;
		if (lastUpdate == null || tradeTime - lastUpdate >= RequestDelay)
		{
			_lastStatusUpdate = tradeTime;
			UpdateStatusComment();
		}

		if (LimitDate && _oldestTick != null && _oldestTick <= OldestLimit)
		{
			LogInfo("Oldest tick {0:O} reached the configured limit {1:O}. Stopping strategy.", _oldestTick, OldestLimit);
			Stop();
		}
	}

	private void UpdateStatusComment()
	{
		var builder = new StringBuilder();
		builder.AppendLine($"OldestTick={FormatDate(_oldestTick)}");
		builder.AppendLine($"LatestTick={FormatDate(_latestTick)}");
		builder.AppendLine($"TotalTicks={_totalTicks}");
		builder.AppendLine($"TotalPackets={_totalPackets}");

		var status = builder.ToString().TrimEnd();
		LogInfo(status);
		WriteStatusFile(status);
	}

	private void SaveState()
	{
		WriteStatusFile($"OldestTick={FormatDate(_oldestTick)}\nLatestTick={FormatDate(_latestTick)}\nTotalTicks={_totalTicks}\nTotalPackets={_totalPackets}");
	}

	private void LoadState()
	{
		if (_statusPath == null || !File.Exists(_statusPath))
		return;

		var lines = File.ReadAllLines(_statusPath);
		foreach (var line in lines)
		{
			var separatorIndex = line.IndexOf('=');
			if (separatorIndex <= 0)
			continue;

			var key = line.Substring(0, separatorIndex);
			var value = line[(separatorIndex + 1)..];

			switch (key)
			{
				case "OldestTick":
					_oldestTick = ParseDate(value);
					break;
				case "LatestTick":
					_latestTick = ParseDate(value);
					break;
				case "TotalTicks":
					if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
					_totalTicks = ticks;
					break;
				case "TotalPackets":
					if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var packets))
					_totalPackets = packets;
					break;
			}
		}
	}

	private void WriteStatusFile(string content)
	{
		if (_statusPath == null)
		return;

		var directory = Path.GetDirectoryName(_statusPath);
		if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
		Directory.CreateDirectory(directory);

		File.WriteAllText(_statusPath, content);
	}

	private string GetStatusPath()
	{
		var folder = ManagerFolder;
		var fileName = StatusFileName;
		if (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(fileName))
		return null;

		return Path.Combine(folder, fileName);
	}

	private static string FormatDate(DateTimeOffset? value)
	{
		return value?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty;
	}

	private static DateTimeOffset? ParseDate(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		return null;

		return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result)
		? result
		: null;
	}
}
