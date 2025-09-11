using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Autocorrelation Price Forecasting.
/// Opens long positions when forecasted price gain exceeds threshold.
/// </summary>
public class SimpleApfStrategyBacktestingStrategy : Strategy
{
	private const int CorrelationLength = 200;

	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _thresholdGain;
	private readonly StrategyParam<decimal> _signalThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _prices = new();
	private readonly Queue<decimal> _returns = new();

	private decimal _storedCycleValue;
	private decimal _exitPrice;

	/// <summary>
	/// Number of bars used for autocorrelation and regression.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Minimum expected price increase to enter a trade.
	/// </summary>
	public decimal ThresholdGain
	{
		get => _thresholdGain.Value;
		set => _thresholdGain.Value = value;
	}

	/// <summary>
	/// Autocorrelation level required to store a forecast.
	/// </summary>
	public decimal SignalThreshold
	{
		get => _signalThreshold.Value;
		set => _signalThreshold.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public SimpleApfStrategyBacktestingStrategy()
	{
		_length = Param(nameof(Length), 20)
		.SetGreaterThanZero()
		.SetDisplay("Length", "Bars used for autocorrelation and regression", "APF")
		.SetCanOptimize(true)
		.SetOptimize(5, 50, 5);

		_thresholdGain = Param(nameof(ThresholdGain), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Threshold Gain", "Minimum expected price increase", "Strategy")
		.SetCanOptimize(true)
		.SetOptimize(1m, 20m, 1m);

		_signalThreshold = Param(nameof(SignalThreshold), 0.5m)
		.SetRange(0.1m, 1m)
		.SetDisplay("Signal Threshold", "Autocorrelation level to store forecast", "APF")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 0.9m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prices.Clear();
		_returns.Clear();
		_storedCycleValue = 0m;
		_exitPrice = 0m;
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
		DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		_prices.Enqueue(candle.ClosePrice);
		if (_prices.Count > CorrelationLength + Length)
		_prices.Dequeue();

		if (_prices.Count >= 2)
		{
		var arr = _prices.ToArray();
		var last = arr[^1];
		var prev = arr[^2];
		var ret = (last - prev) / prev * 100m;
		_returns.Enqueue(ret);
		if (_returns.Count > Length)
		_returns.Dequeue();
		}

		if (_prices.Count >= CorrelationLength + Length)
		{
		var autocorr = ComputeAutocorrelation(_prices.ToArray(), Length);
		if (autocorr > SignalThreshold && _returns.Count == Length)
		{
		_storedCycleValue = ComputeRegression(_returns.ToArray());
		}
		}

		var futurePrice = candle.ClosePrice * (1 + _storedCycleValue / 100m);
		var gain = futurePrice - candle.ClosePrice;

		if (gain > ThresholdGain && Position <= 0)
		{
		_exitPrice = candle.ClosePrice + gain;
		BuyMarket(Volume + Math.Abs(Position));
		}

		if (Position > 0 && candle.ClosePrice >= _exitPrice)
		{
		SellMarket(Position);
		}
	}

	private static decimal ComputeAutocorrelation(decimal[] prices, int lag)
	{
	int n = prices.Length;
	int start = n - CorrelationLength;
	int startLag = start - lag;

	decimal sumX = 0m, sumY = 0m, sumXY = 0m, sumX2 = 0m, sumY2 = 0m;

	for (var i = 0; i < CorrelationLength; i++)
	{
	var x = prices[start + i];
	var y = prices[startLag + i];
	sumX += x;
	sumY += y;
	sumXY += x * y;
	sumX2 += x * x;
	sumY2 += y * y;
	}

	var numerator = CorrelationLength * sumXY - sumX * sumY;
	var denom = Math.Sqrt((double)(CorrelationLength * sumX2 - sumX * sumX) * (double)(CorrelationLength * sumY2 - sumY * sumY));
	return denom <= 0 ? 0 : (decimal)(numerator / denom);
	}

	private static decimal ComputeRegression(decimal[] values)
	{
	int n = values.Length;
	decimal sumX = 0m, sumY = 0m, sumXY = 0m, sumX2 = 0m;

	for (var i = 0; i < n; i++)
	{
	sumX += i;
	sumY += values[i];
	sumXY += i * values[i];
	sumX2 += i * i;
	}

	var denom = n * sumX2 - sumX * sumX;
	if (denom == 0)
	return 0m;

	var slope = (n * sumXY - sumX * sumY) / denom;
	var intercept = (sumY - slope * sumX) / n;
	return intercept + slope * (n - 1);
	}
}
