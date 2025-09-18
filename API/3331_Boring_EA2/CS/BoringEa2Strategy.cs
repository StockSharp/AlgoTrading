namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Port of the MetaTrader expert advisor boring-ea2.
/// Monitors three simple moving averages and reports crossover events through informational logs.
/// </summary>
public class BoringEa2Strategy : Strategy
{
	private readonly StrategyParam<int> _shortMaLength;
	private readonly StrategyParam<int> _mediumMaLength;
	private readonly StrategyParam<int> _longMaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousShort;
	private decimal? _previousMedium;
	private decimal? _previousLong;

	private bool _ma003CrossedUpMa020;
	private bool _ma003CrossedUpMa150;
	private bool _ma020CrossedUpMa150;

	/// <summary>
	/// Length for the 3-period simple moving average.
	/// </summary>
	public int ShortMaLength
	{
		get => _shortMaLength.Value;
		set => _shortMaLength.Value = value;
	}

	/// <summary>
	/// Length for the 20-period simple moving average.
	/// </summary>
	public int MediumMaLength
	{
		get => _mediumMaLength.Value;
		set => _mediumMaLength.Value = value;
	}

	/// <summary>
	/// Length for the 150-period simple moving average.
	/// </summary>
	public int LongMaLength
	{
		get => _longMaLength.Value;
		set => _longMaLength.Value = value;
	}

	/// <summary>
	/// Candle type used to calculate the moving averages.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="BoringEa2Strategy"/>.
	/// </summary>
	public BoringEa2Strategy()
	{
		_shortMaLength = Param(nameof(ShortMaLength), 3)
			.SetDisplay("Short SMA Length", "Length for the 3-period SMA", "Moving Averages")
			.SetCanOptimize(true);

		_mediumMaLength = Param(nameof(MediumMaLength), 20)
			.SetDisplay("Medium SMA Length", "Length for the 20-period SMA", "Moving Averages")
			.SetCanOptimize(true);

		_longMaLength = Param(nameof(LongMaLength), 150)
			.SetDisplay("Long SMA Length", "Length for the 150-period SMA", "Moving Averages")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for SMA calculations", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var shortSma = new SimpleMovingAverage { Length = ShortMaLength };
		var mediumSma = new SimpleMovingAverage { Length = MediumMaLength };
		var longSma = new SimpleMovingAverage { Length = LongMaLength };

		SubscribeCandles(CandleType)
			.Bind(shortSma, mediumSma, longSma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortValue, decimal mediumValue, decimal longValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_previousShort is null || _previousMedium is null || _previousLong is null)
		{
			InitializeState(shortValue, mediumValue, longValue);
			return;
		}

		if (_previousShort > _previousMedium && shortValue < mediumValue && _ma003CrossedUpMa020)
		{
			_ma003CrossedUpMa020 = false;
			PublishNotification("MA003 crossed down MA020");
		}

		if (_previousShort < _previousMedium && shortValue > mediumValue && !_ma003CrossedUpMa020)
		{
			_ma003CrossedUpMa020 = true;
			PublishNotification("MA003 crossed up MA020");
		}

		if (_previousShort > _previousLong && shortValue < longValue && _ma003CrossedUpMa150)
		{
			_ma003CrossedUpMa150 = false;
			PublishNotification("MA003 crossed down MA150");
		}

		if (_previousShort < _previousLong && shortValue > longValue && !_ma003CrossedUpMa150)
		{
			_ma003CrossedUpMa150 = true;
			PublishNotification("MA003 crossed up MA150");
		}

		if (_previousMedium > _previousLong && mediumValue < longValue && _ma020CrossedUpMa150)
		{
			_ma020CrossedUpMa150 = false;
			PublishNotification("MA020 crossed down MA150");
		}

		if (_previousMedium < _previousLong && mediumValue > longValue && !_ma020CrossedUpMa150)
		{
			_ma020CrossedUpMa150 = true;
			PublishNotification("MA020 crossed up MA150");
		}

		_previousShort = shortValue;
		_previousMedium = mediumValue;
		_previousLong = longValue;
	}

	private void InitializeState(decimal shortValue, decimal mediumValue, decimal longValue)
	{
		_previousShort = shortValue;
		_previousMedium = mediumValue;
		_previousLong = longValue;

		_ma003CrossedUpMa020 = shortValue > mediumValue;
		_ma003CrossedUpMa150 = shortValue > longValue;
		_ma020CrossedUpMa150 = mediumValue > longValue;
	}

	private void PublishNotification(string text)
	{
		var timeframe = GetTimeFrameName();
		var symbol = Security?.Id ?? "Unknown";
		var message = $"Alert!!! - {symbol} - {timeframe} - {text}";

		AddInfoLog(message);
	}

	private string GetTimeFrameName()
	{
		if (CandleType.Arg is TimeSpan span && span > TimeSpan.Zero)
		{
			return span switch
			{
				var value when value == TimeSpan.FromMinutes(1) => "M1",
				var value when value == TimeSpan.FromMinutes(5) => "M5",
				var value when value == TimeSpan.FromMinutes(15) => "M15",
				var value when value == TimeSpan.FromMinutes(30) => "M30",
				var value when value == TimeSpan.FromHours(1) => "H1",
				var value when value == TimeSpan.FromHours(4) => "H4",
				var value when value == TimeSpan.FromDays(1) => "D1",
				var value when value == TimeSpan.FromDays(7) => "W1",
				var value when value >= TimeSpan.FromDays(28) && value <= TimeSpan.FromDays(31) => "MN",
				_ => FormatCustomTimeFrame(span),
			};
		}

		return CandleType.ToString();
	}

	private static string FormatCustomTimeFrame(TimeSpan span)
	{
		if (span.TotalMinutes >= 1 && Math.Abs(span.TotalMinutes - Math.Round(span.TotalMinutes)) < 0.0001)
			return $"M{Math.Round(span.TotalMinutes)}";

		if (span.TotalHours >= 1 && Math.Abs(span.TotalHours - Math.Round(span.TotalHours)) < 0.0001)
			return $"H{Math.Round(span.TotalHours)}";

		if (span.TotalDays >= 1 && Math.Abs(span.TotalDays - Math.Round(span.TotalDays)) < 0.0001)
			return $"D{Math.Round(span.TotalDays)}";

		return span.ToString();
	}
}
