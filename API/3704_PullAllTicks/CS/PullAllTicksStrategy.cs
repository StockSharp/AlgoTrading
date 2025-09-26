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

	private DateTimeOffset? _oldestTick;
	private DateTimeOffset? _latestTick;
	private DateTimeOffset? _lastStatusUpdate;
	private long _totalTicks;
	private long _totalPackets;
	private int _packetCounter;

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
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeTrades().Bind(ProcessTrade).Start();

		UpdateStatusComment();
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
