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
/// Displays candles from a higher timeframe on the chart.
/// Visualization only, no trading logic.
/// </summary>
public class MultiTimeFrameCandlesStrategy : Strategy
{
	private readonly StrategyParam<int> _numberOfCandles;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<ICandleMessage> _candles = new();

	/// <summary>
	/// Number of candles to keep and display.
	/// </summary>
	public int NumberOfCandles
	{
	get => _numberOfCandles.Value;
	set => _numberOfCandles.Value = value;
	}

	/// <summary>
	/// The type of candles to subscribe to.
	/// </summary>
	public DataType CandleType
	{
	get => _candleType.Value;
	set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MultiTimeFrameCandlesStrategy"/> class.
	/// </summary>
	public MultiTimeFrameCandlesStrategy()
	{
	_numberOfCandles = Param(nameof(NumberOfCandles), 8)
		.SetGreaterThanZero()
		.SetDisplay("Number of Candles", "How many candles from the higher timeframe to show", "General")
		
		.SetOptimize(1, 10, 1);

	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Higher timeframe for candles", "General");
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
	_candles.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
	base.OnStarted2(time);

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
	if (candle.State != CandleStates.Finished)
		return;

	_candles.Enqueue(candle);
	while (_candles.Count > NumberOfCandles)
		_candles.Dequeue();

	LogInfo($"MTF Candle: O={candle.OpenPrice} H={candle.HighPrice} L={candle.LowPrice} C={candle.ClosePrice}");
	}
}
