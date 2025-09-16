using System;
using System.Reflection;
using System.Text;
using System.Threading;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Defines how positions should be filtered by symbol.
/// </summary>
public enum PositionSelectionMode
{
	/// <summary>
	/// Include positions from all symbols available in the portfolio.
	/// </summary>
	AllSymbols,

	/// <summary>
	/// Include only positions that relate to the strategy security.
	/// </summary>
	CurrentSymbol,
}

/// <summary>
/// Lists open positions on a periodic timer similar to the original MQL implementation.
/// </summary>
public class ListPositionsStrategy : Strategy
{
	private static readonly PropertyInfo? StrategyIdProperty = typeof(Position).GetProperty("StrategyId");

	private readonly StrategyParam<string> _strategyIdFilter;
	private readonly StrategyParam<PositionSelectionMode> _selectionMode;
	private readonly StrategyParam<TimeSpan> _timerInterval;

	private Timer? _timer;
	private int _isProcessing;

	/// <summary>
	/// Gets or sets the strategy identifier to exclude from the report.
	/// </summary>
	public string StrategyIdFilter
	{
		get => _strategyIdFilter.Value;
		set => _strategyIdFilter.Value = value;
	}

	/// <summary>
	/// Gets or sets which symbols should be scanned for positions.
	/// </summary>
	public PositionSelectionMode SelectionMode
	{
		get => _selectionMode.Value;
		set => _selectionMode.Value = value;
	}

	/// <summary>
	/// Gets or sets how often the position list should be refreshed.
	/// </summary>
	public TimeSpan TimerInterval
	{
		get => _timerInterval.Value;
		set => _timerInterval.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ListPositionsStrategy()
	{
		_strategyIdFilter = Param(nameof(StrategyIdFilter), string.Empty)
			.SetDisplay("Strategy Id Filter", "Skip positions with this strategy id", "General");

		_selectionMode = Param(nameof(SelectionMode), PositionSelectionMode.AllSymbols)
			.SetDisplay("Selection Mode", "Choose whether to scan all symbols or only the current one", "General");

		_timerInterval = Param(nameof(TimerInterval), TimeSpan.FromSeconds(6))
			.SetDisplay("Timer Interval", "Interval used to refresh the position list", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Portfolio == null)
			throw new InvalidOperationException("Portfolio must be assigned before starting the strategy.");

		if (TimerInterval <= TimeSpan.Zero)
			throw new InvalidOperationException("Timer interval must be a positive value.");

		// Start timer immediately so the first snapshot is printed at once.
		_timer = new Timer(_ => ProcessPositions(), null, TimeSpan.Zero, TimerInterval);
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
		// Prevent overlapping timer executions.
		if (Interlocked.Exchange(ref _isProcessing, 1) == 1)
			return;

		try
		{
			var portfolio = Portfolio;
			if (portfolio == null)
				return;

			var selectionMode = SelectionMode;
			var currentSecurity = Security;
			var filter = StrategyIdFilter;

			var builder = new StringBuilder();
			builder.AppendLine("Idx | Symbol | PositionId | LastChange | Side | Quantity | AvgPrice | PnL");

			var hasEntries = false;
			var index = 0;

			foreach (var position in portfolio.Positions)
			{
				if (selectionMode == PositionSelectionMode.CurrentSymbol && currentSecurity != null && !Equals(position.Security, currentSecurity))
					continue;

				if (!string.IsNullOrEmpty(filter))
				{
					var strategyId = TryGetStrategyId(position);
					if (!string.IsNullOrEmpty(strategyId) && string.Equals(strategyId, filter, StringComparison.Ordinal))
						continue;
				}

				hasEntries = true;

				var symbol = position.Security?.Id ?? string.Empty;
				var changeTime = position.LastChangeTime;
				var side = position.Side;
				var quantity = position.CurrentValue;
				var averagePrice = position.AveragePrice;
				var pnl = position.PnL;

				builder.AppendLine($"{index} | {symbol} | {position.Id} | {changeTime:yyyy-MM-dd HH:mm:ss} | {side} | {quantity:0.###} | {averagePrice:0.#####} | {pnl:0.##}");
				index++;
			}

			if (!hasEntries)
			{
				LogInfo("No positions match the configured filters.");
				return;
			}

			// Log the snapshot so it is visible inside the Designer log.
			LogInfo(builder.ToString().TrimEnd());
		}
		catch (Exception error)
		{
			LogError($"Failed to list positions: {error.Message}");
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
}
