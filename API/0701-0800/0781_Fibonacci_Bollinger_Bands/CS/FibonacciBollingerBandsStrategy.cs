using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-only mean reversion: below the lower Bollinger band while above the
/// preceding swing's Fibonacci price, with exits at the upper band or prior high.
/// </summary>
public class FibonacciBollingerBandsStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _fibLookback;
	private readonly StrategyParam<decimal> _fibonacciLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly Queue<decimal> _closes = new();
	private readonly Queue<decimal> _previousHighs = new();
	private readonly Queue<decimal> _previousLows = new();
	private Order _pendingOrder;

	public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }
	public decimal BollingerDeviation { get => _bollingerDeviation.Value; set => _bollingerDeviation.Value = value; }
	public int FibLookback { get => _fibLookback.Value; set => _fibLookback.Value = value; }
	public decimal FibonacciLevel { get => _fibonacciLevel.Value; set => _fibonacciLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FibonacciBollingerBandsStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20).SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Number of completed closes for band calculation.", "Bollinger");
		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m).SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Number of population standard deviations.", "Bollinger");
		_fibLookback = Param(nameof(FibLookback), 50).SetGreaterThanZero()
			.SetDisplay("Fib Lookback", "Number of previous candles used for the swing range.", "Fibonacci");
		_fibonacciLevel = Param(nameof(FibonacciLevel), 0.618m).SetGreaterThanZero()
			.SetDisplay("Fibonacci Level", "Fraction of preceding swing range measured up from its low.", "Fibonacci");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe.", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_closes.Clear();
		_previousHighs.Clear();
		_previousLows.Clear();
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (FibonacciLevel >= 1m)
			throw new ArgumentOutOfRangeException(nameof(FibonacciLevel), FibonacciLevel, "The Fibonacci fraction must be less than one.");
		OrderRegistering += order => _pendingOrder = order;
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Enqueue(candle.ClosePrice);
		while (_closes.Count > BollingerPeriod) _closes.Dequeue();

		var complete = _closes.Count == BollingerPeriod
			&& _previousHighs.Count == FibLookback && _previousLows.Count == FibLookback;
		var pendingActive = _pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed);
		if (complete && !pendingActive)
		{
			var mean = _closes.Average();
			var variance = _closes.Sum(price => (price - mean) * (price - mean)) / BollingerPeriod;
			var offset = (decimal)Math.Sqrt((double)variance) * BollingerDeviation;
			var upper = mean + offset;
			var lower = mean - offset;
			var swingHigh = _previousHighs.Max();
			var swingLow = _previousLows.Min();
			var fib = swingLow + (swingHigh - swingLow) * FibonacciLevel;

			if (Position > 0m && (candle.ClosePrice >= upper || candle.ClosePrice >= swingHigh))
				SellMarket(Position);
			else if (Position == 0m && candle.ClosePrice < lower && candle.ClosePrice > fib)
				BuyMarket(Volume);
		}

		// The signal candle never contributes to the swing extremes used for itself.
		_previousHighs.Enqueue(candle.HighPrice);
		_previousLows.Enqueue(candle.LowPrice);
		while (_previousHighs.Count > FibLookback) _previousHighs.Dequeue();
		while (_previousLows.Count > FibLookback) _previousLows.Dequeue();
	}
}
