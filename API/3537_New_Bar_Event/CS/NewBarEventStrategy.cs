namespace StockSharp.Samples.Strategies;

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

using StockSharp.Algo.Candles;

/// <summary>
/// Demonstrates how to detect new bar events using candle subscriptions.
/// </summary>
public class NewBarEventStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private DateTimeOffset? _previousBarOpenTime;

	/// <summary>
	/// Candle type used for new bar detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="NewBarEventStrategy"/>.
	/// </summary>
	public NewBarEventStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for monitoring new bars", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		// Reset cached bar time when the strategy is reset.
		_previousBarOpenTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Subscribe to candle updates for the selected timeframe.
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		// Optional visualization of incoming candles on the chart.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var currentBarTime = candle.OpenTime;
		var isFirstBar = _previousBarOpenTime is null;
		var isNewBar = isFirstBar || currentBarTime != _previousBarOpenTime;

		if (!isNewBar)
		{
			// Nothing to do if the candle belongs to the same bar (should not happen for finished candles).
			return;
		}

		if (isFirstBar)
		{
			// Handle the very first candle after the strategy starts.
			LogInfo($"First candle detected at {currentBarTime:O}. The bar might be mid-progress.");
		}
		else
		{
			// React to the start of a regular new bar.
			LogInfo($"New bar detected at {currentBarTime:O}.");
		}

		// Execute logic that should run for every new bar.
		_previousBarOpenTime = currentBarTime;
	}
}

