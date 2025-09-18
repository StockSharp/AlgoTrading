namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

/// <summary>
/// Strategy that periodically reports details about the Nth most recent open position.
/// Converted from the MetaTrader expert "Get_Last_Nth_Open_Trade".
/// </summary>
public class Get_Last_Nth_Open_TradeStrategy : Strategy
{
	private static readonly PropertyInfo? StrategyIdProperty = typeof(Position).GetProperty("StrategyId");

	private readonly StrategyParam<bool> _enableMagicNumber;
	private readonly StrategyParam<bool> _enableSymbolFilter;
	private readonly StrategyParam<string> _magicNumber;
	private readonly StrategyParam<int> _tradeIndex;
	private readonly StrategyParam<TimeSpan> _refreshInterval;

	private Timer? _timer;
	private int _isProcessing;

	/// <summary>
	/// Enable filtering by a specific strategy identifier.
	/// </summary>
	public bool EnableMagicNumber
	{
		get => _enableMagicNumber.Value;
		set => _enableMagicNumber.Value = value;
	}

	/// <summary>
	/// Restrict the scan to the strategy security.
	/// </summary>
	public bool EnableSymbolFilter
	{
		get => _enableSymbolFilter.Value;
		set => _enableSymbolFilter.Value = value;
	}

	/// <summary>
	/// Strategy identifier to match when filtering positions.
	/// </summary>
	public string MagicNumber
	{
		get => _magicNumber.Value;
		set => _magicNumber.Value = value;
	}

	/// <summary>
	/// Zero-based index of the trade to report.
	/// </summary>
	public int TradeIndex
	{
		get => _tradeIndex.Value;
		set => _tradeIndex.Value = value;
	}

	/// <summary>
	/// How often the position snapshot should be refreshed.
	/// </summary>
	public TimeSpan RefreshInterval
	{
		get => _refreshInterval.Value;
		set => _refreshInterval.Value = value;
	}

	/// <summary>
	/// Initializes default parameters.
	/// </summary>
	public Get_Last_Nth_Open_TradeStrategy()
	{
		_enableMagicNumber = Param(nameof(EnableMagicNumber), false)
			.SetDisplay("Enable Magic Number", "Filter positions by strategy identifier", "Filters");

		_enableSymbolFilter = Param(nameof(EnableSymbolFilter), false)
			.SetDisplay("Enable Symbol Filter", "Scan only the strategy security", "Filters");

		_magicNumber = Param(nameof(MagicNumber), "1234")
			.SetDisplay("Magic Number", "Strategy identifier that must match the position", "Filters");

		_tradeIndex = Param(nameof(TradeIndex), 0)
			.SetDisplay("Trade Index", "Zero-based index of the position to display", "General");

		_refreshInterval = Param(nameof(RefreshInterval), TimeSpan.FromSeconds(1))
			.SetDisplay("Refresh Interval", "How often the position snapshot should be refreshed", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Portfolio == null)
			throw new InvalidOperationException("Portfolio must be assigned before starting the strategy.");

		if (RefreshInterval <= TimeSpan.Zero)
			throw new InvalidOperationException("Refresh interval must be positive.");

		_timer = new Timer(_ => ProcessOpenTrades(), null, TimeSpan.Zero, RefreshInterval);
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		_timer?.Dispose();
		_timer = null;

		Interlocked.Exchange(ref _isProcessing, 0);

		base.OnStopped();
	}

	private void ProcessOpenTrades()
	{
		if (Interlocked.Exchange(ref _isProcessing, 1) == 1)
			return;

		try
		{
			var portfolio = Portfolio;
			if (portfolio == null)
				return;

			var trades = new List<Position>();

			foreach (var position in portfolio.Positions)
			{
				if (position.CurrentValue == 0m)
					continue;

				if (EnableSymbolFilter)
				{
					var strategySecurity = Security;
					if (strategySecurity != null && !Equals(position.Security, strategySecurity))
						continue;
				}

				if (EnableMagicNumber)
				{
					var strategyId = TryGetStrategyId(position);
					if (string.IsNullOrEmpty(strategyId))
						continue;

					if (!string.Equals(strategyId, MagicNumber, StringComparison.Ordinal))
						continue;
				}

				trades.Add(position);
			}

			if (trades.Count == 0)
			{
				LogInfo("No open trades match the configured filters.");
				return;
			}

			trades.Sort((left, right) => right.LastChangeTime.CompareTo(left.LastChangeTime));

			var index = TradeIndex;

			if (index < 0 || index >= trades.Count)
			{
				LogInfo($"Trade index {index} is outside the range of {trades.Count} open trades.");
				return;
			}

			var selected = trades[index];

			var builder = new StringBuilder();
			builder.AppendLine("Selected open trade snapshot:");
			builder.AppendLine($"Ticket: {selected.Id}");
			builder.AppendLine($"Symbol: {selected.Security?.Id ?? string.Empty}");
			builder.AppendLine($"Side: {selected.Side}");
			builder.AppendLine($"Quantity: {selected.CurrentValue:0.###}");
			builder.AppendLine($"AveragePrice: {selected.AveragePrice:0.#####}");
			builder.AppendLine($"PnL: {FormatDecimal(selected.PnL)}");
			builder.AppendLine($"UnrealizedPnL: {FormatDecimal(selected.UnrealizedPnL)}");
			builder.AppendLine($"LastChangeTime: {selected.LastChangeTime:yyyy-MM-dd HH:mm:ss}");

			var strategyIdText = TryGetStrategyId(selected);
			if (!string.IsNullOrEmpty(strategyIdText))
				builder.AppendLine($"StrategyId: {strategyIdText}");

			LogInfo(builder.ToString().TrimEnd());
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

	private static string? TryGetStrategyId(Position position)
	{
		if (StrategyIdProperty == null)
			return null;

		var value = StrategyIdProperty.GetValue(position);
		return value?.ToString();
	}

	private static string FormatDecimal(decimal? value)
	{
		return (value ?? 0m).ToString("0.##");
	}
}
