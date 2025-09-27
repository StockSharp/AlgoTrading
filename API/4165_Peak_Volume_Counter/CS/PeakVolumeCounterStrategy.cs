using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Monitors aggregated trade volume and reports peaks without placing orders.
/// Converted from the MetaTrader 4 "Peak Volume Counter" utility.
/// </summary>
public class PeakVolumeCounterStrategy : Strategy
{
	private readonly StrategyParam<int> _volumeThreshold;

	private int _signalCount;
	private decimal _accumulatedVolume;
	private decimal _lastTradePrice;
	private DateTimeOffset? _frameStartTime;

	/// <summary>
	/// Minimum aggregated volume required to report a peak.
	/// </summary>
	public int VolumeThreshold
	{
		get => _volumeThreshold.Value;
		set => _volumeThreshold.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public PeakVolumeCounterStrategy()
	{
		_volumeThreshold = Param(nameof(VolumeThreshold), 7)
			.SetGreaterThanZero()
			.SetDisplay("Volume Threshold", "Sum of trade volume that triggers a peak alert", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Ticks)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Ensure counters are clean before receiving the first trade.
		ResetState();

		// Listen for tick data and accumulate volume bursts.
		SubscribeTicks()
			.Bind(ProcessTrade)
			.Start();
	}

	private void ResetState()
	{
		// Clear counters so the next burst starts with a fresh window.
		_signalCount = 0;
		_accumulatedVolume = 0m;
		_lastTradePrice = 0m;
		_frameStartTime = null;
	}

		private void ProcessTrade(ITickTradeMessage trade)
		{
			// Remember the most recent trade price for diagnostics.
			var price = trade.Price;
			_lastTradePrice = price;

		// MetaTrader counted ticks, so treat missing or zero volumes as a single unit.
		var volumeIncrement = trade.Volume ?? 0m;

		if (volumeIncrement <= 0m)
			volumeIncrement = 1m;

		_accumulatedVolume += volumeIncrement;

		// Capture both server and local timestamps for diagnostics.
		var serverTime = trade.ServerTime != default ? trade.ServerTime : CurrentTime;
		var localTime = trade.LocalTime != default ? trade.LocalTime : serverTime;

		// Remember when the current burst started.
		if (_frameStartTime is null)
			_frameStartTime = serverTime;

		// Wait until the configured threshold is met.
		if (_accumulatedVolume < VolumeThreshold)
			return;

		_signalCount++;

		var frameLength = serverTime - _frameStartTime.Value;

		if (frameLength < TimeSpan.Zero)
			frameLength = TimeSpan.Zero;

		var severity = GetVolumeSeverity(_accumulatedVolume);

		// Report the burst to the strategy log.
		LogInfo($"Peak volume #{_signalCount}: volume {_accumulatedVolume} ({severity}), window {frameLength.TotalSeconds:F1}s, price {price}, server {serverTime:O}, local {localTime:O}.");

		_accumulatedVolume = 0m;
		_frameStartTime = serverTime;
	}

	private static string GetVolumeSeverity(decimal volume)
	{
		// Translate the original colour bands into descriptive labels.
		return volume switch
		{
			>= 19m => "violet",
			>= 17m => "blue",
			>= 15m => "aqua",
			>= 13m => "lawn green",
			>= 11m => "yellow",
			>= 9m => "orange",
			_ => "red"
		};
	}
}
