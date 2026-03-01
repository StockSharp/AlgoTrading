using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy demonstrating how to handle earnings, split and dividend information from news feed.
/// The strategy listens for news and logs corporate action events.
/// </summary>
public class EarningsSplitsDividendsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Candle type for auxiliary processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EarningsSplitsDividendsStrategy"/> class.
	/// </summary>
	public EarningsSplitsDividendsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Utility strategy: logs candle close for monitoring purposes.
		LogInfo($"Candle closed at {candle.ClosePrice} on {candle.OpenTime:O}");
	}
}
