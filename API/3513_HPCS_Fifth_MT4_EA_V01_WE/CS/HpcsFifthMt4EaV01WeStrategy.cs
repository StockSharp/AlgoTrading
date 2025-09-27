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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// HPCS Fifth MT4 EA V01 WE strategy converted from MetaTrader 4.
/// Generates informational alerts whenever a new candle is completed.
/// </summary>
public class HpcsFifthMt4EaV01WeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _alertMessage;
	private readonly StrategyParam<bool> _playSound;
	private readonly StrategyParam<string> _soundFile;

	private DateTimeOffset? _lastCandleOpenTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="HpcsFifthMt4EaV01WeStrategy"/>.
	/// </summary>
	public HpcsFifthMt4EaV01WeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for detecting new candles", "Data");

		_alertMessage = Param(nameof(AlertMessage), "New Candle Generated")
			.SetDisplay("Alert Message", "Text logged when a new candle appears", "Notifications");

		_playSound = Param(nameof(PlaySound), true)
			.SetDisplay("Play Sound", "Log the sound notification request", "Notifications");

		_soundFile = Param(nameof(SoundFile), "alert.wav")
			.SetDisplay("Sound File", "Name of the sound file associated with the alert", "Notifications");
	}

	/// <summary>
	/// Candle type and timeframe used to track chart updates.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Message included in the informational alert.
	/// </summary>
	public string AlertMessage
	{
		get => _alertMessage.Value;
		set => _alertMessage.Value = value;
	}

	/// <summary>
	/// Indicates whether the strategy should log the configured sound notification.
	/// </summary>
	public bool PlaySound
	{
		get => _playSound.Value;
		set => _playSound.Value = value;
	}

	/// <summary>
	/// Sound file associated with the alert.
	/// </summary>
	public string SoundFile
	{
		get => _soundFile.Value;
		set => _soundFile.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastCandleOpenTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Ignore unfinished candles to match the MetaTrader expert behavior.
		if (candle.State != CandleStates.Finished)
			return;

		// Guard against duplicate notifications for the same bar.
		if (_lastCandleOpenTime == candle.OpenTime)
			return;

		_lastCandleOpenTime = candle.OpenTime;

		var message = AlertMessage.IsEmptyOrWhiteSpace()
			? "New candle detected"
			: AlertMessage;

		LogInfo($"{message}. Time: {candle.OpenTime:O}.");

		if (PlaySound && !SoundFile.IsEmptyOrWhiteSpace())
		{
			LogInfo($"Sound notification requested: {SoundFile}.");
		}
	}
}

