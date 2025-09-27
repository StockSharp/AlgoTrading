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

using System.Globalization;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Links multiple candle subscriptions to the main security to mimic the MetaTrader watch list linker.
/// </summary>
public class WatchListLinkerLiteStrategy : Strategy
{
	private readonly StrategyParam<string> _linkedTimeFrames;
	private readonly List<DataType> _linkedDataTypes = new();

	/// <summary>
	/// Comma separated list of candle time frames that should mirror the strategy security.
	/// </summary>
	public string LinkedTimeFrames
	{
		get => _linkedTimeFrames.Value;
		set => _linkedTimeFrames.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="WatchListLinkerLiteStrategy"/>.
	/// </summary>
	public WatchListLinkerLiteStrategy()
	{
		_linkedTimeFrames = Param(nameof(LinkedTimeFrames), "00:15:00,00:30:00,01:00:00")
			.SetDisplay("Linked Time Frames", "Comma separated list of candle time frames to link to the main security", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var security = Security;

		if (security == null)
			yield break;

		if (_linkedDataTypes.Count == 0)
			ResolveLinkedTimeFrames(false);

		foreach (var dataType in _linkedDataTypes)
			yield return (security, dataType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_linkedDataTypes.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResolveLinkedTimeFrames(true);

		var security = Security ?? throw new InvalidOperationException("Security must be assigned before starting the strategy.");

		foreach (var dataType in _linkedDataTypes)
		{
			var subscription = SubscribeCandles(dataType);
			subscription.Start();

			LogInfo($"Linked chart timeframe {DescribeDataType(dataType)} to security {security.Id}.");
		}
	}

	private void ResolveLinkedTimeFrames(bool logIssues)
	{
		_linkedDataTypes.Clear();

		var raw = LinkedTimeFrames ?? string.Empty;

		foreach (var segment in raw.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
		{
			if (TryParseTimeFrame(segment, out var frame))
			{
				_linkedDataTypes.Add(frame.TimeFrame());
			}
			else if (logIssues)
			{
				LogWarning($"Unable to parse time frame '{segment}'. Supported forms: 00:15:00, 15m, M15, 1H.");
			}
		}

		if (_linkedDataTypes.Count == 0)
		{
			_linkedDataTypes.Add(TimeSpan.FromMinutes(1).TimeFrame());

			if (logIssues)
				LogWarning("No valid time frames provided. Fallback to 1 minute chart.");
		}
	}

	private static string DescribeDataType(DataType dataType)
	{
		if (dataType.Arg is TimeSpan span)
			return span.ToString();

		return dataType.ToString();
	}

	private static bool TryParseTimeFrame(string text, out TimeSpan timeSpan)
	{
		var value = text.Trim();

		if (value.Length == 0)
		{
			timeSpan = default;
			return false;
		}

		if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out timeSpan))
			return true;

		var upper = value.ToUpperInvariant();

		if (TryParseWithSuffix(upper, out timeSpan))
			return true;

		if (TryParseWithPrefix(upper, out timeSpan))
			return true;

		if (double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var minutes))
		{
			timeSpan = TimeSpan.FromMinutes(minutes);
			return true;
		}

		timeSpan = default;
		return false;
	}

	private static bool TryParseWithSuffix(string value, out TimeSpan timeSpan)
	{
		timeSpan = default;

		if (value.Length < 2)
			return false;

		var unit = value[^1];
		var numberPart = value[..^1];

		if (!double.TryParse(numberPart, NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity))
			return false;

		timeSpan = unit switch
		{
			'S' => TimeSpan.FromSeconds(quantity),
			'M' => TimeSpan.FromMinutes(quantity),
			'H' => TimeSpan.FromHours(quantity),
			'D' => TimeSpan.FromDays(quantity),
			_ => default
		};

		return timeSpan != default;
	}

	private static bool TryParseWithPrefix(string value, out TimeSpan timeSpan)
	{
		timeSpan = default;

		if (value.Length < 2)
			return false;

		var unit = value[0];
		var numberPart = value[1..];

		if (!double.TryParse(numberPart, NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity))
			return false;

		timeSpan = unit switch
		{
			'S' => TimeSpan.FromSeconds(quantity),
			'M' => TimeSpan.FromMinutes(quantity),
			'H' => TimeSpan.FromHours(quantity),
			'D' => TimeSpan.FromDays(quantity),
			_ => default
		};

		return timeSpan != default;
	}
}

