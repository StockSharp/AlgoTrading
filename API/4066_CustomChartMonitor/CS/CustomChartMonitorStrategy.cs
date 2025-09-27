using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that monitors custom chart candles and logs updates similar to the Example1_LibCustomChart expert.
/// It prints the current close price of the active bar and notifies when a new bar appears.
/// </summary>
public class CustomChartMonitorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private DateTimeOffset? _lastFinishedBar;
	private DateTimeOffset? _lastLoggedBar;
	private decimal? _lastLoggedClose;

	/// <summary>
	/// The candle type that emulates the custom chart feed.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="CustomChartMonitorStrategy"/>.
	/// </summary>
	public CustomChartMonitorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles that emulate the custom chart feed.", "General");
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

		_lastFinishedBar = null;
		_lastLoggedBar = null;
		_lastLoggedClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		LogInfo($"Subscribed to {CandleType} candles to monitor custom chart data.");
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State == CandleStates.None)
			return;

		if (candle.State == CandleStates.Active || candle.State == CandleStates.Finished)
			LogCurrentClose(candle);

		if (candle.State == CandleStates.Finished)
			CheckNewBar(candle);
	}

	private void LogCurrentClose(ICandleMessage candle)
	{
		var isNewBar = _lastLoggedBar != candle.OpenTime;
		var isNewClose = _lastLoggedClose != candle.ClosePrice;

		if (!isNewBar && !isNewClose)
			return;

		_lastLoggedBar = candle.OpenTime;
		_lastLoggedClose = candle.ClosePrice;

		LogInfo($"Custom chart close price: {candle.ClosePrice} (state: {candle.State}).");
	}

	private void CheckNewBar(ICandleMessage candle)
	{
		if (_lastFinishedBar == candle.OpenTime)
			return;

		_lastFinishedBar = candle.OpenTime;

		LogInfo($"New bar detected at {candle.OpenTime:O}.");
	}
}
