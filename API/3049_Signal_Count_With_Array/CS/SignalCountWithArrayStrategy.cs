using System;
using System.Collections.Generic;
using System.Text;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Counts how often Donchian channel extremes generate signals for multiple offsets.
/// </summary>
public class SignalCountWithArrayStrategy : Strategy
{
	private readonly StrategyParam<decimal> _positiveEmptyValue;
	private readonly StrategyParam<decimal> _negativeEmptyValue;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<int> _gapStart;
	private readonly StrategyParam<int> _gapStep;
	private readonly StrategyParam<int> _gapCount;
	private readonly StrategyParam<bool> _logOnEachCandle;

	private DonchianChannels _donchian;

	private decimal[,] _previousValues;
	private int[,] _changedCounts;
	private int[,] _emptyCounts;
	private int[,] _negativeEmptyCounts;
	private int[,] _zeroTransitionCounts;
	private int[,] _newFromEmptyCounts;
	private int[,] _backToEmptyCounts;

	private decimal? _previousUpperBand;
	private decimal? _previousLowerBand;

	public SignalCountWithArrayStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_channelPeriod = Param(nameof(ChannelPeriod), 24)
			.SetGreaterThanZero()
			.SetDisplay("Channel Period", "Donchian period for extremes", "Indicator");

		_gapStart = Param(nameof(GapStart), 0)
			.SetDisplay("Gap Start", "Initial offset applied to signals", "Indicator");

		_gapStep = Param(nameof(GapStep), 1)
			.SetGreaterThanZero()
			.SetDisplay("Gap Step", "Increment between offsets", "Indicator");

		_gapCount = Param(nameof(GapCount), 8)
			.SetGreaterThanZero()
			.SetDisplay("Gap Count", "Number of offsets evaluated", "Indicator");

		_positiveEmptyValue = Param(nameof(PositiveEmptyValue), 2147483647m)
			.SetDisplay("Positive Empty Value", "Placeholder for missing positive signals", "Diagnostics");

		_negativeEmptyValue = Param(nameof(NegativeEmptyValue), -2147483646m)
			.SetDisplay("Negative Empty Value", "Placeholder for missing negative signals", "Diagnostics");

		_logOnEachCandle = Param(nameof(LogOnEachCandle), false)
			.SetDisplay("Log On Each Candle", "Write diagnostics after every candle", "Diagnostics");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	public int GapStart
	{
		get => _gapStart.Value;
		set => _gapStart.Value = value;
	}

	public int GapStep
	{
		get => _gapStep.Value;
		set => _gapStep.Value = value;
	}

	public int GapCount
	{
		get => _gapCount.Value;
		set => _gapCount.Value = value;
	}

	public decimal PositiveEmptyValue
	{
		get => _positiveEmptyValue.Value;
		set => _positiveEmptyValue.Value = value;
	}

	public decimal NegativeEmptyValue
	{
		get => _negativeEmptyValue.Value;
		set => _negativeEmptyValue.Value = value;
	}

	public bool LogOnEachCandle
	{
		get => _logOnEachCandle.Value;
		set => _logOnEachCandle.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousValues = null;
		_changedCounts = null;
		_emptyCounts = null;
		_negativeEmptyCounts = null;
		_zeroTransitionCounts = null;
		_newFromEmptyCounts = null;
		_backToEmptyCounts = null;
		_previousUpperBand = null;
		_previousLowerBand = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var combinations = GapCount;

		_previousValues = new decimal[combinations, 2];
		_changedCounts = new int[combinations, 2];
		_emptyCounts = new int[combinations, 2];
		_negativeEmptyCounts = new int[combinations, 2];
		_zeroTransitionCounts = new int[combinations, 2];
		_newFromEmptyCounts = new int[combinations, 2];
		_backToEmptyCounts = new int[combinations, 2];
		_previousUpperBand = null;
		_previousLowerBand = null;

		_donchian = new DonchianChannels
		{
			Length = ChannelPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_donchian, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _donchian);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_donchian.IsFormed)
			return;

		var donchianTyped = (DonchianChannelsValue)donchianValue;

		if (donchianTyped.UpperBand is not decimal upperBand ||
			donchianTyped.LowerBand is not decimal lowerBand)
		{
			return;
		}

		if (_previousUpperBand is null || _previousLowerBand is null)
		{
			// Capture the first fully formed channel values before producing diagnostics.
			_previousUpperBand = upperBand;
			_previousLowerBand = lowerBand;
			return;
		}

		var hasUpperSignal = upperBand > _previousUpperBand.Value;
		var hasLowerSignal = lowerBand < _previousLowerBand.Value;

		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep == 0m)
		{
			// Avoid zero multipliers when the security lacks price step information.
			priceStep = 1m;
		}

		for (var index = 0; index < GapCount; index++)
		{
			var gapValue = GapStart + index * GapStep;
			var offset = gapValue * priceStep;

			// Build the virtual signal value for the upper band.
			var upperValue = hasUpperSignal
				? candle.High + offset
				: PositiveEmptyValue;

			// Build the virtual signal value for the lower band.
			var lowerValue = hasLowerSignal
				? candle.Low - offset
				: NegativeEmptyValue;

			UpdateCounters(index, 0, upperValue, PositiveEmptyValue);
			UpdateCounters(index, 1, lowerValue, NegativeEmptyValue);
		}

		if (hasUpperSignal || hasLowerSignal || LogOnEachCandle)
		{
			LogCounters(candle.OpenTime);
		}

		_previousUpperBand = upperBand;
		_previousLowerBand = lowerBand;
	}

	private void UpdateCounters(int gapIndex, int bufferIndex, decimal value, decimal sentinel)
	{
		var previous = _previousValues[gapIndex, bufferIndex];

		if (previous != value)
		{
			// Track any raw change between sequential indicator values.
			_changedCounts[gapIndex, bufferIndex]++;
		}

		if (sentinel == PositiveEmptyValue && value == PositiveEmptyValue)
		{
			// Count how often the upper buffer reports the empty sentinel.
			_emptyCounts[gapIndex, bufferIndex]++;
		}

		if (sentinel == NegativeEmptyValue && value == NegativeEmptyValue)
		{
			// Count how often the lower buffer reports the negative sentinel.
			_negativeEmptyCounts[gapIndex, bufferIndex]++;
		}

		if (value != 0m && previous == 0m)
		{
			// Detect transitions from the default zero value to a non-zero reading.
			_zeroTransitionCounts[gapIndex, bufferIndex]++;
		}

		if (value != sentinel && previous == sentinel)
		{
			// Record how many times a real signal replaces the sentinel value.
			_newFromEmptyCounts[gapIndex, bufferIndex]++;
		}

		if (value == sentinel && previous != sentinel)
		{
			// Record how many times the buffer returns back to the sentinel value.
			_backToEmptyCounts[gapIndex, bufferIndex]++;
		}

		_previousValues[gapIndex, bufferIndex] = value;
	}

	private void LogCounters(DateTimeOffset time)
	{
		var summary = BuildSummary(time);
		LogInfo(summary);
	}

	private string BuildSummary(DateTimeOffset time)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"[{time:yyyy-MM-dd HH:mm}] Signal diagnostics");

		for (var index = 0; index < GapCount; index++)
		{
			var gapValue = GapStart + index * GapStep;

			sb.Append("Gap ")
				.Append(gapValue)
				.Append(" Up -> Changed: ")
				.Append(_changedCounts[index, 0])
				.Append(", Empty: ")
				.Append(_emptyCounts[index, 0])
				.Append(", NegEmpty: ")
				.Append(_negativeEmptyCounts[index, 0])
				.Append(", Zero: ")
				.Append(_zeroTransitionCounts[index, 0])
				.Append(", NewFromEmpty: ")
				.Append(_newFromEmptyCounts[index, 0])
				.Append(", BackToEmpty: ")
				.Append(_backToEmptyCounts[index, 0]);
			sb.AppendLine();

			sb.Append("Gap ")
				.Append(gapValue)
				.Append(" Down -> Changed: ")
				.Append(_changedCounts[index, 1])
				.Append(", Empty: ")
				.Append(_emptyCounts[index, 1])
				.Append(", NegEmpty: ")
				.Append(_negativeEmptyCounts[index, 1])
				.Append(", Zero: ")
				.Append(_zeroTransitionCounts[index, 1])
				.Append(", NewFromEmpty: ")
				.Append(_newFromEmptyCounts[index, 1])
				.Append(", BackToEmpty: ")
				.Append(_backToEmptyCounts[index, 1]);
			sb.AppendLine();
		}

		return sb.ToString();
	}
}
