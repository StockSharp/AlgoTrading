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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy demonstrating new bar detection using candle subscriptions.
/// </summary>
public class NewBarStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private DateTimeOffset? _currentBarTime;
	private bool _isFirstObservation;

	/// <summary>
	/// Candle series used for detecting new bar events.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NewBarStrategy"/> class.
	/// </summary>
	public NewBarStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used for detecting new bar events.", "General");

		_isFirstObservation = true;
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

		_currentBarTime = null;
		_isFirstObservation = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		var isNewBar = _currentBarTime != candle.OpenTime;
		var wasFirstObservation = _isFirstObservation;

		if (isNewBar)
		{
			var previousBarTime = _currentBarTime;
			_currentBarTime = candle.OpenTime;

			if (_isFirstObservation)
			{
				_isFirstObservation = false;

				// Handle the very first update that arrives after the strategy starts.
				HandleFirstObservation(candle);
			}
			else
			{
				// Handle the start of a normal bar that follows the previous one.
				HandleNewBar(candle, previousBarTime);
			}
		}
		else
		{
			// Handle additional ticks that arrive while the current bar is still forming.
			HandleSameBarTick(candle);
		}

		if (candle.State == CandleStates.Finished && !wasFirstObservation)
		{
			// Notify when the bar is completed.
			HandleBarClosed(candle);
		}
	}

	private void HandleFirstObservation(ICandleMessage candle)
	{
		Log.Info("First tick observed inside an already opened bar at {0:o}.", candle.OpenTime);
	}

	private void HandleNewBar(ICandleMessage candle, DateTimeOffset? previousBarTime)
	{
		Log.Info("New bar started at {0:o} (previous bar opened at {1:o}).", candle.OpenTime, previousBarTime);
	}

	private void HandleSameBarTick(ICandleMessage candle)
	{
		Log.Debug("Tick inside the current bar. Time: {0:o}, state: {1}.", candle.OpenTime, candle.State);
	}

	private void HandleBarClosed(ICandleMessage candle)
	{
		Log.Info("Bar closed at {0:o}.", candle.CloseTime);
	}
}

