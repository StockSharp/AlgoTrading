namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// MACD Sample Hedging Grid: MACD crossover with grid-like position management.
/// </summary>
public class MacdSampleHedgingGridStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMacd;
	private bool _hasPrev;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public MacdSampleHedgingGridStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;

		var macd = new MovingAverageConvergenceDivergence();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(macd, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdLine)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevMacd = macdLine;
			_hasPrev = true;
			return;
		}

		if (_prevMacd <= 0 && macdLine > 0 && Position <= 0)
			BuyMarket();
		else if (_prevMacd >= 0 && macdLine < 0 && Position >= 0)
			SellMarket();

		_prevMacd = macdLine;
	}
}
