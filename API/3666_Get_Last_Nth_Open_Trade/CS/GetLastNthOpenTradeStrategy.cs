using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that periodically inspects open positions and reports the N-th most recent one.
/// </summary>
public class GetLastNthOpenTradeStrategy : Strategy
{
	private static readonly PropertyInfo StrategyIdProperty = typeof(Position).GetProperty("StrategyId");
	private static readonly PropertyInfo CommentProperty = typeof(Position).GetProperty("Comment");
	private static readonly PropertyInfo OpenTimeProperty = typeof(Position).GetProperty("OpenTime");
	private static readonly PropertyInfo CloseTimeProperty = typeof(Position).GetProperty("CloseTime");

	private readonly StrategyParam<bool> _enableMagicNumber;
	private readonly StrategyParam<bool> _enableSymbolFilter;
	private readonly StrategyParam<int> _magicNumber;
	private readonly StrategyParam<int> _tradeIndex;
	private readonly StrategyParam<TimeSpan> _refreshInterval;

	private Timer _timer;
	private int _isProcessing;
	private string _lastSnapshot = string.Empty;

	/// <summary>
	/// Gets or sets whether the strategy should filter positions by strategy identifier.
	/// </summary>
	public bool EnableMagicNumber
	{
		get => _enableMagicNumber.Value;
		set => _enableMagicNumber.Value = value;
	}

	/// <summary>
	/// Gets or sets whether only positions for the assigned security should be considered.
	/// </summary>
	public bool EnableSymbolFilter
	{
		get => _enableSymbolFilter.Value;
		set => _enableSymbolFilter.Value = value;
	}

	/// <summary>
	/// Gets or sets the numeric identifier used when <see cref="EnableMagicNumber"/> is enabled.
	/// </summary>
	public int MagicNumber
	{
		get => _magicNumber.Value;
		set => _magicNumber.Value = value;
	}

	/// <summary>
	/// Gets or sets the index of the open trade to report. Zero corresponds to the most recent trade.
	/// </summary>
	public int TradeIndex
	{
		get => _tradeIndex.Value;
		set => _tradeIndex.Value = value;
	}

	/// <summary>
	/// Gets or sets how often the position list should be refreshed.
	/// </summary>
	public TimeSpan RefreshInterval
	{
		get => _refreshInterval.Value;
		set => _refreshInterval.Value = value;
	}

	/// <summary>
	/// Gets the last snapshot written by the strategy.
	/// </summary>
	public string LastTradeSnapshot => _lastSnapshot;

	/// <summary>
	/// Initializes a new instance of <see cref="GetLastNthOpenTradeStrategy"/>.
	/// </summary>
	public GetLastNthOpenTradeStrategy()
	{
		_enableMagicNumber = Param(nameof(EnableMagicNumber), false)
			.SetDisplay("Enable Magic Number", "Filter positions by their strategy identifier", "Filters");

		_enableSymbolFilter = Param(nameof(EnableSymbolFilter), false)
			.SetDisplay("Enable Symbol Filter", "Restrict to the strategy security", "Filters");

		_magicNumber = Param(nameof(MagicNumber), 1234)
			.SetDisplay("Magic Number", "Strategy identifier used for filtering", "Filters");

		_tradeIndex = Param(nameof(TradeIndex), 2)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Trade Index", "Zero-based index of the trade to display", "General");

		_refreshInterval = Param(nameof(RefreshInterval), TimeSpan.FromSeconds(1))
			.SetDisplay("Refresh Interval", "Delay between position scans", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_lastSnapshot = string.Empty;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Portfolio == null)
			throw new InvalidOperationException("Portfolio must be assigned before starting the strategy.");

		if (RefreshInterval <= TimeSpan.Zero)
			throw new InvalidOperationException("Refresh interval must be positive.");

		_timer = new Timer(_ => ProcessPositions(), null, TimeSpan.Zero, RefreshInterval);
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		_timer?.Dispose();
		_timer = null;

		base.OnStopped();
	}

	private void ProcessPositions()
	{
		if (Interlocked.Exchange(ref _isProcessing, 1) == 1)
			return;

		try
		{
			var portfolio = Portfolio;
			if (portfolio == null)
				return;

			var matches = new List<Position>();
			var targetSecurity = EnableSymbolFilter ? Security : null;

			foreach (var position in portfolio.Positions)
			{
				if (position == null)
					continue;

				if (position.CurrentValue == 0m)
					continue;

				if (targetSecurity != null && !Equals(position.Security, targetSecurity))
					continue;

				if (EnableMagicNumber)
				{
					var strategyId = TryGetStrategyId(position);

					if (!int.TryParse(strategyId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) || id != MagicNumber)
						continue;
				}

				matches.Add(position);
			}

			if (matches.Count == 0)
			{
				UpdateSnapshot("No open trades match the configured filters.");
				return;
			}

			matches.Sort(CompareByRecency);

			var index = TradeIndex;
			if (index < 0 || index >= matches.Count)
			{
				UpdateSnapshot($"Trade index {index} is out of range. Total trades: {matches.Count}.");
				return;
			}

			var position = matches[index];
			var snapshot = BuildSnapshot(position, index, matches.Count);
			UpdateSnapshot(snapshot);
		}
		catch (Exception error)
		{
			LogError($"Failed to process open trades: {error.Message}");
		}
		finally
		{
			Interlocked.Exchange(ref _isProcessing, 0);
		}
	}

	private void UpdateSnapshot(string message)
	{
		if (string.Equals(_lastSnapshot, message, StringComparison.Ordinal))
			return;

		_lastSnapshot = message;
		LogInfo(message);
	}

	private static int CompareByRecency(Position left, Position right)
	{
		var timeComparison = right.LastChangeTime.CompareTo(left.LastChangeTime);
		if (timeComparison != 0)
			return timeComparison;

		return string.CompareOrdinal(right.Id ?? string.Empty, left.Id ?? string.Empty);
	}

	private static string BuildSnapshot(Position position, int index, int total)
	{
		var builder = new StringBuilder();
		builder.AppendLine(FormattableString.Invariant($"index={index} of {total}"));
		builder.AppendLine(FormattableString.Invariant($"ticket={position.Id}"));
		builder.AppendLine(FormattableString.Invariant($"symbol={position.Security?.Id ?? string.Empty}"));
		builder.AppendLine(FormattableString.Invariant($"lots={Math.Abs(position.CurrentValue):0.####}"));
		builder.AppendLine(FormattableString.Invariant($"openPrice={position.AveragePrice:0.#####}"));
		builder.AppendLine(FormattableString.Invariant($"stopLoss={(position.StopLoss.HasValue ? position.StopLoss.Value.ToString("0.#####", CultureInfo.InvariantCulture) : string.Empty)}"));
		builder.AppendLine(FormattableString.Invariant($"takeProfit={(position.TakeProfit.HasValue ? position.TakeProfit.Value.ToString("0.#####", CultureInfo.InvariantCulture) : string.Empty)}"));
		builder.AppendLine(FormattableString.Invariant($"profit={position.PnL:0.##}"));
		builder.AppendLine(FormattableString.Invariant($"comment={TryGetComment(position) ?? string.Empty}"));
		builder.AppendLine(FormattableString.Invariant($"side={position.Side}"));
		builder.AppendLine(FormattableString.Invariant($"orderOpenTime={FormatDateTime(TryGetDateTime(position, OpenTimeProperty))}"));
		builder.Append(FormattableString.Invariant($"orderCloseTime={FormatDateTime(TryGetDateTime(position, CloseTimeProperty))}"));
		return builder.ToString();
	}

	private static string FormatDateTime(DateTimeOffset? value)
	{
		return value?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty;
	}

	private static string TryGetStrategyId(Position position)
	{
		if (StrategyIdProperty == null)
			return null;

		return StrategyIdProperty.GetValue(position)?.ToString();
	}

	private static string TryGetComment(Position position)
	{
		if (CommentProperty == null)
			return null;

		return CommentProperty.GetValue(position)?.ToString();
	}

	private static DateTimeOffset? TryGetDateTime(Position position, PropertyInfo property)
	{
		if (property == null)
			return null;

		var value = property.GetValue(position);

		return value switch
		{
			DateTimeOffset dto => dto,
			DateTime dt => new DateTimeOffset(dt),
			_ => null,
		};
	}
}
