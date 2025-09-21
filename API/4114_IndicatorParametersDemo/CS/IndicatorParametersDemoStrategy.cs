using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Demonstrates how to inspect indicator parameters and monitor their runtime values.
/// </summary>
public class IndicatorParametersDemoStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<bool> _logIndicatorValues;

	private readonly Dictionary<IIndicator, string> _trackedIndicators = new();

	private SimpleMovingAverage _sma = null!;
	private ExponentialMovingAverage _ema = null!;
	private RelativeStrengthIndex _rsi = null!;

	/// <summary>
	/// Initializes a new instance of <see cref="IndicatorParametersDemoStrategy"/>.
	/// </summary>
	public IndicatorParametersDemoStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Data type used to feed indicators", "General");

		_smaLength = Param(nameof(SmaLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("SMA Length", "Period of the simple moving average demonstrated in the sample", "Indicators")
		.SetCanOptimize(true);

		_emaLength = Param(nameof(EmaLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("EMA Length", "Period of the exponential moving average demonstrated in the sample", "Indicators")
		.SetCanOptimize(true);

		_rsiLength = Param(nameof(RsiLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Length", "Period of the relative strength index demonstrated in the sample", "Indicators")
		.SetCanOptimize(true);

		_logIndicatorValues = Param(nameof(LogIndicatorValues), false)
		.SetDisplay("Log Indicator Values", "Write indicator values to the log whenever a candle closes", "Logging");
	}

	/// <summary>
	/// Candle type that provides data for the indicators.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Simple moving average period.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Exponential moving average period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Relative strength index period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Enables value logging for every tracked indicator.
	/// </summary>
	public bool LogIndicatorValues
	{
		get => _logIndicatorValues.Value;
		set => _logIndicatorValues.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma = new SimpleMovingAverage
		{
			Length = SmaLength
		};

		_ema = new ExponentialMovingAverage
		{
			Length = EmaLength
		};

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiLength
		};

		TrackIndicator(_sma, $"SMA({SmaLength})");
		TrackIndicator(_ema, $"EMA({EmaLength})");
		TrackIndicator(_rsi, $"RSI({RsiLength})");

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_sma, OnSmaUpdated);
		subscription.Bind(_ema, OnEmaUpdated);
		subscription.Bind(_rsi, OnRsiUpdated);
		subscription.Start();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		ClearTrackedIndicators();
	}

	/// <summary>
	/// Forces a refresh of all indicator parameter descriptions.
	/// </summary>
	public void RefreshIndicatorSnapshots()
	{
		foreach (var pair in _trackedIndicators)
		{
			var report = BuildParametersReport(pair.Key, pair.Value);
			LogInfo(report);
		}
	}

	private void OnSmaUpdated(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!LogIndicatorValues)
		return;

		LogInfo($"[{candle.CloseTime:O}] SMA value: {FormatParameterValue(smaValue)}");
	}

	private void OnEmaUpdated(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!LogIndicatorValues)
		return;

		LogInfo($"[{candle.CloseTime:O}] EMA value: {FormatParameterValue(emaValue)}");
	}

	private void OnRsiUpdated(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!LogIndicatorValues)
		return;

		LogInfo($"[{candle.CloseTime:O}] RSI value: {FormatParameterValue(rsiValue)}");
	}

	private void TrackIndicator(IIndicator indicator, string alias)
	{
		if (_trackedIndicators.ContainsKey(indicator))
		return;

		_trackedIndicators.Add(indicator, alias);

		var message = new StringBuilder()
		.Append("+ added: name=")
		.Append(alias)
		.Append(", type=")
		.Append(indicator.GetType().Name)
		.ToString();

		LogInfo(message);
		LogInfo(BuildParametersReport(indicator, alias));
	}

	private void ClearTrackedIndicators()
	{
		foreach (var indicator in _trackedIndicators.Keys.ToArray())
		{
			UntrackIndicator(indicator);
		}
	}

	private void UntrackIndicator(IIndicator indicator)
	{
		if (!_trackedIndicators.TryGetValue(indicator, out var alias))
		return;

		_trackedIndicators.Remove(indicator);

		var message = new StringBuilder()
		.Append("- deleted: name=")
		.Append(alias)
		.Append(", type=")
		.Append(indicator.GetType().Name)
		.ToString();

		LogInfo(message);
	}

	private string BuildParametersReport(IIndicator indicator, string alias)
	{
		var builder = new StringBuilder();
		builder.AppendLine($"{alias} parameter snapshot:");

		var hasParameters = false;

		foreach (var line in GetParameterLines(indicator))
		{
			hasParameters = true;
			builder.AppendLine(line);
		}

		if (!hasParameters)
		builder.AppendLine("	No readable parameters detected.");

		return builder.ToString();
	}

	private static IEnumerable<string> GetParameterLines(IIndicator indicator)
	{
		var properties = indicator.GetType()
		.GetProperties(BindingFlags.Instance | BindingFlags.Public)
		.Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && IsSupportedParameterType(p.PropertyType));

		foreach (var property in properties)
		{
			object? value;

			try
			{
				value = property.GetValue(indicator);
			}
			catch
			{
				continue;
			}

			yield return $"	{property.Name} ({GetFriendlyTypeName(property.PropertyType)}) = {FormatParameterValue(value)}";
		}
	}

	private static bool IsSupportedParameterType(Type type)
	{
		var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

		if (underlyingType.IsEnum)
		return true;

		return underlyingType == typeof(bool)
		|| underlyingType == typeof(byte)
		|| underlyingType == typeof(sbyte)
		|| underlyingType == typeof(short)
		|| underlyingType == typeof(ushort)
		|| underlyingType == typeof(int)
		|| underlyingType == typeof(uint)
		|| underlyingType == typeof(long)
		|| underlyingType == typeof(ulong)
		|| underlyingType == typeof(float)
		|| underlyingType == typeof(double)
		|| underlyingType == typeof(decimal)
		|| underlyingType == typeof(string)
		|| underlyingType == typeof(TimeSpan)
		|| underlyingType == typeof(DateTime)
		|| underlyingType == typeof(DateTimeOffset)
		|| underlyingType == typeof(DataType);
	}

	private static string GetFriendlyTypeName(Type type)
	{
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
		return $"{GetFriendlyTypeName(type.GenericTypeArguments[0])}?";

		return type.Name;
	}

	private static string FormatParameterValue(object? value)
	{
		return value switch
		{
			null => "null",
			decimal dec => dec.ToString(CultureInfo.InvariantCulture),
			double dbl => dbl.ToString("G", CultureInfo.InvariantCulture),
			float flt => flt.ToString("G", CultureInfo.InvariantCulture),
			IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
			_ => value.ToString() ?? string.Empty,
		};
	}
}
