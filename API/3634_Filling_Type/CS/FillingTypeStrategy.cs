using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that inspects the configured security and reports which order filling modes are supported by the exchange board.
/// </summary>
public class FillingTypeStrategy : Strategy
{
	private static readonly string[] _executionPropertyCandidates = new[]
	{
		"OrderExecutionTypes",
		"ExecutionTypes",
		"ExecutionType",
		"OrderExecutionMode",
		"OrderExecutionModes",
		"SupportedOrderExecutionTypes",
		"SupportedExecutionTypes",
		"AllowedExecutionTypes",
		"DefaultExecutionType"
	};

	private readonly StrategyParam<bool> _logBoardDiagnostics;

	/// <summary>
	/// Controls whether the strategy should print extended board diagnostics after detecting the filling rules.
	/// </summary>
	public bool LogBoardDiagnostics
	{
		get => _logBoardDiagnostics.Value;
		set => _logBoardDiagnostics.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public FillingTypeStrategy()
	{
		_logBoardDiagnostics = Param(nameof(LogBoardDiagnostics), true)
		.SetDisplay("Log board diagnostics", "Print additional exchange board flags once filling modes are detected.", "Output")
		.SetCanOptimize(false);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var security = Security ?? throw new InvalidOperationException("Security must be set before starting the strategy.");
		var board = security.Board;

		if (board is null)
		{
			LogWarning("The configured security does not expose an exchange board, so filling rules cannot be evaluated.");
			return;
		}

		LogInfo($"Detecting available order filling policies for board '{board.Code}'.");

		if (!TryLogKnownExecutionProperty(board))
		{
			if (!TryLogExecutionFromExtensionInfo(board))
			{
				LogInfo("No dedicated execution metadata was found. Falling back to generic board capability flags.");
				LogFallbackOrderSupport(board);
			}
		}

		if (LogBoardDiagnostics)
		{
			LogAdditionalBoardDiagnostics(board);
		}
	}

	private bool TryLogKnownExecutionProperty(ExchangeBoard board)
	{
		var type = board.GetType();
		const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

		foreach (var propertyName in _executionPropertyCandidates)
		{
			var property = type.GetProperty(propertyName, flags);

			if (property is null)
			continue;

			LogInfo($"Using board property '{property.Name}' to describe filling rules.");
			var value = SafeGetValue(property, board);
			LogExecutionValue($"board.{property.Name}", value);
			return true;
		}

		var properties = type.GetProperties(flags);

		foreach (var property in properties)
		{
			var name = property.Name;

			if (name.IndexOf("Execution", StringComparison.OrdinalIgnoreCase) < 0 &&
			name.IndexOf("Filling", StringComparison.OrdinalIgnoreCase) < 0)
			{
				continue;
			}

			LogInfo($"Using board property '{property.Name}' to describe filling rules.");
			var value = SafeGetValue(property, board);
			LogExecutionValue($"board.{property.Name}", value);
			return true;
		}

		return false;
	}

	private static object? SafeGetValue(PropertyInfo property, object instance)
	{
		try
		{
			return property.GetValue(instance);
		}
		catch (Exception exception)
		{
			return $"<error: {exception.Message}>";
		}
	}

	private bool TryLogExecutionFromExtensionInfo(ExchangeBoard board)
	{
		var extensionInfo = board.ExtensionInfo;

		if (extensionInfo is null || extensionInfo.Count == 0)
		{
			return false;
		}

		var found = false;

		foreach (var pair in extensionInfo)
		{
			var key = pair.Key?.ToString();

			if (string.IsNullOrEmpty(key))
			continue;

			if (key.IndexOf("fill", StringComparison.OrdinalIgnoreCase) < 0 &&
			key.IndexOf("exec", StringComparison.OrdinalIgnoreCase) < 0)
			{
				continue;
			}

			if (!found)
			{
				LogInfo("Inspecting exchange board extension info for execution hints:");
				found = true;
			}

			LogInfo($"ExtensionInfo[{key}] = {pair.Value ?? "<null>"}");
		}

		return found;
	}

	private void LogFallbackOrderSupport(ExchangeBoard board)
	{
		LogInfo($"Supports market orders: {board.IsSupportMarketOrders}");
		LogInfo($"Supports stop orders: {board.IsSupportStopOrders}");
		LogInfo($"Supports stop-limit orders: {board.IsSupportStopLimitOrders}");
		LogInfo($"Supports partial lots: {board.IsSupportOddLots}");
		LogInfo($"Supports margin trading: {board.IsSupportMargin}");
	}

	private void LogAdditionalBoardDiagnostics(ExchangeBoard board)
	{
		LogInfo($"Board name: {board.Name}");
		LogInfo($"Exchange: {board.Exchange?.Name ?? "<unknown>"}");
		LogInfo($"Country: {board.Country ?? "<unknown>"}");
		LogInfo($"Time zone: {board.TimeZone?.Id ?? "<unspecified>"}");
		LogInfo($"Settlement mode: {board.SettlementMode}");

		var deliveryBoard = board.DeliveryBoard;

		if (deliveryBoard is not null)
		{
			LogInfo($"Delivery board: {deliveryBoard.Code} ({deliveryBoard.Name})");
		}

		var workingTime = board.WorkingTime;

		if (workingTime is not null)
		{
			LogInfo($"Working time schedule: {workingTime}");
		}
	}

	private void LogExecutionValue(string source, object? value)
	{
		if (value is null)
		{
			LogInfo($"{source}: <null>");
			return;
		}

		if (value is string text)
		{
			LogInfo($"{source}: {text}");
			return;
		}

		if (value is IEnumerable enumerable and value is not string)
		{
			var index = 0;

			foreach (var item in enumerable)
			{
				LogInfo($"{source}[{index}] = {item ?? "<null>"}");
				index++;
			}

			if (index == 0)
			{
				LogInfo($"{source}: <empty>");
			}

			return;
		}

		LogInfo($"{source}: {value}");
	}
}
