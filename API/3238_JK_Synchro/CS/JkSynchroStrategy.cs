using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// JK Synchro strategy: counts bearish vs bullish candles in a rolling window
/// and trades when one side dominates.
/// </summary>
public class JkSynchroStrategy : Strategy
{
	private readonly StrategyParam<int> _analysisPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<int> _directionWindow = new();
	private int _bearishCount;
	private int _bullishCount;

	/// <summary>
	/// Constructor.
	/// </summary>
	public JkSynchroStrategy()
	{
		_analysisPeriod = Param(nameof(AnalysisPeriod), 18)
			.SetGreaterThanZero()
			.SetDisplay("Analysis Period", "Number of candles in the vote window", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public int AnalysisPeriod
	{
		get => _analysisPeriod.Value;
		set => _analysisPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_directionWindow.Clear();
		_bearishCount = 0;
		_bullishCount = 0;

		var sma = new SMA { Length = AnalysisPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Track candle direction in rolling window
		var direction = 0;
		if (candle.OpenPrice > candle.ClosePrice)
			direction = 1; // bearish
		else if (candle.OpenPrice < candle.ClosePrice)
			direction = -1; // bullish

		_directionWindow.Enqueue(direction);
		if (direction > 0) _bearishCount++;
		else if (direction < 0) _bullishCount++;

		while (_directionWindow.Count > AnalysisPeriod)
		{
			var removed = _directionWindow.Dequeue();
			if (removed > 0) _bearishCount--;
			else if (removed < 0) _bullishCount--;
		}

		if (_directionWindow.Count < AnalysisPeriod)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// When bearish candles dominate, expect reversal -> buy
		if (_bearishCount > _bullishCount && Position <= 0)
		{
			BuyMarket();
		}
		// When bullish candles dominate, expect reversal -> sell
		else if (_bullishCount > _bearishCount && Position >= 0)
		{
			SellMarket();
		}
	}
}
