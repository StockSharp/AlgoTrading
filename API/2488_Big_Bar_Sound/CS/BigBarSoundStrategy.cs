using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that emulates the BigBarSound MetaTrader expert advisor.
/// It monitors candle sizes and raises a log notification when a bar grows beyond the configured threshold.
/// </summary>
public enum BigBarDifferenceMode
{
	/// <summary>
	/// Measure the difference between close and open prices.
	/// </summary>
	OpenClose,

	/// <summary>
	/// Measure the distance between the high and low of the candle.
	/// </summary>
	HighLow,
}

/// <summary>
/// Strategy that logs an alert when a candle exceeds a configurable size.
/// </summary>
public class BigBarSoundStrategy : Strategy
{
	private readonly StrategyParam<int> _barPoint;
	private readonly StrategyParam<BigBarDifferenceMode> _differenceMode;
	private readonly StrategyParam<string> _soundFile;
	private readonly StrategyParam<bool> _showAlert;
	private readonly StrategyParam<DataType> _candleType;

	private DateTimeOffset? _lastProcessedTime;

	/// <summary>
	/// Number of price steps required to trigger the alert.
	/// </summary>
	public int BarPoint
	{
		get => _barPoint.Value;
		set => _barPoint.Value = value;
	}

	/// <summary>
	/// Defines how the candle size is calculated.
	/// </summary>
	public BigBarDifferenceMode DifferenceMode
	{
		get => _differenceMode.Value;
		set => _differenceMode.Value = value;
	}

	/// <summary>
	/// Name of the WAV file that should be played when the alert triggers.
	/// </summary>
	public string SoundFile
	{
		get => _soundFile.Value;
		set => _soundFile.Value = value;
	}

	/// <summary>
	/// Enables an additional alert log entry.
	/// </summary>
	public bool ShowAlert
	{
		get => _showAlert.Value;
		set => _showAlert.Value = value;
	}

	/// <summary>
	/// Candle type used to monitor the market.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BigBarSoundStrategy"/> class.
	/// </summary>
	public BigBarSoundStrategy()
	{
		_barPoint = Param(nameof(BarPoint), 200)
			.SetGreaterThanZero()
			.SetDisplay("Point Threshold", "Number of price steps required to trigger the alert", "General")
			.SetCanOptimize(true)
			.SetOptimize(50, 500, 50);

		_differenceMode = Param(nameof(DifferenceMode), BigBarDifferenceMode.HighLow)
			.SetDisplay("Difference Mode", "How the candle size is calculated", "General");

		_soundFile = Param(nameof(SoundFile), "alert.wav")
			.SetDisplay("Sound File", "Name of the WAV file to emulate", "Notifications");

		_showAlert = Param(nameof(ShowAlert), false)
			.SetDisplay("Show Alert", "Write an additional alert message to the log", "Notifications");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to monitor", "Data");
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
		_lastProcessedTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Subscribe to candle updates for the configured timeframe.
		var subscription = SubscribeCandles(CandleType);

		// Process each candle update to evaluate the bar size.
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Skip incomplete candles because their size may still change.
		if (candle.State != CandleStates.Finished)
			return;

		// Avoid processing the same candle multiple times.
		if (_lastProcessedTime == candle.CloseTime)
			return;

		_lastProcessedTime = candle.CloseTime;

		// Calculate the candle size according to the selected measurement mode.
		var difference = DifferenceMode == BigBarDifferenceMode.OpenClose
			? Math.Abs(candle.ClosePrice - candle.OpenPrice)
			: Math.Abs(candle.HighPrice - candle.LowPrice);

		var priceStep = Security?.PriceStep;
		var step = priceStep is null or <= 0m ? 1m : priceStep.Value;
		var threshold = step * BarPoint;

		// Only react when the candle size crosses the threshold expressed in price steps.
		if (difference < threshold)
			return;

		// Log the simulated sound playback for visibility in the strategy logs.
		LogInfo($"Play sound '{SoundFile}' - candle size {difference:F4} exceeded threshold {threshold:F4}.");

		if (ShowAlert)
		{
			// Emit an additional alert style log message to mimic the MetaTrader alert window.
			AddInfoLog($"Alert: large candle detected on {Security?.Id ?? "unknown"} at {candle.CloseTime:O}.");
		}
	}
}
