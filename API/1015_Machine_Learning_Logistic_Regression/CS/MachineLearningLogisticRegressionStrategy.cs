using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Logistic regression based strategy.
/// Retrains a simple model on each finished candle and trades by prediction.
/// </summary>
public class MachineLearningLogisticRegressionStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _learningRate;
	private readonly StrategyParam<int> _iterations;
	private readonly StrategyParam<int> _holdingPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal[] _baseSeries = Array.Empty<decimal>();
	private decimal[] _synthSeries = Array.Empty<decimal>();
	private int _filled;
	private int _signal;
	private int _hpCounter;
	private bool _isInitialized;

	/// <summary>
	/// Training window size.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Learning rate for gradient descent.
	/// </summary>
	public decimal LearningRate
	{
		get => _learningRate.Value;
		set => _learningRate.Value = value;
	}

	/// <summary>
	/// Number of training iterations.
	/// </summary>
	public int Iterations
	{
		get => _iterations.Value;
		set => _iterations.Value = value;
	}

	/// <summary>
	/// Bars to hold position before exit.
	/// </summary>
	public int HoldingPeriod
	{
		get => _holdingPeriod.Value;
		set => _holdingPeriod.Value = value;
	}

	/// <summary>
	/// Type of candles used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public MachineLearningLogisticRegressionStrategy()
	{
		_lookback = Param(nameof(Lookback), 3)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Number of bars for training", "General")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);

		_learningRate = Param(nameof(LearningRate), 0.0009m)
			.SetGreaterThanZero()
			.SetDisplay("Learning Rate", "Gradient descent step", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.0001m, 0.01m, 0.0001m);

		_iterations = Param(nameof(Iterations), 1000)
			.SetGreaterThanZero()
			.SetDisplay("Iterations", "Training iterations", "General")
			.SetCanOptimize(true)
			.SetOptimize(50, 5000, 50);

		_holdingPeriod = Param(nameof(HoldingPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Holding Period", "Bars to hold position", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		_baseSeries = new decimal[Lookback];
		_synthSeries = new decimal[Lookback];
		_filled = 0;
		_signal = 0;
		_hpCounter = 0;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		Shift(_baseSeries, candle.ClosePrice);
		var synthetic = (decimal)Math.Log(Math.Abs(Math.Pow((double)candle.ClosePrice, 2) - 1) + 0.5);
		Shift(_synthSeries, synthetic);

		if (_filled < Lookback)
		{
			_filled++;
			return;
		}

		if (!_isInitialized)
		{
			_isInitialized = true;
			return;
		}

		var prediction = RunLogisticRegression(_baseSeries, _synthSeries, Lookback, LearningRate, Iterations);

		var newSignal = prediction > 0.5m ? 1 : -1;

		if (newSignal != _signal)
		{
			_hpCounter = 0;
			if (newSignal == 1 && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (newSignal == -1 && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}
		else
		{
			_hpCounter++;
			if (_signal == 1 && _hpCounter >= HoldingPeriod && Position > 0)
				SellMarket(Position);
			else if (_signal == -1 && _hpCounter >= HoldingPeriod && Position < 0)
				BuyMarket(-Position);
		}

		_signal = newSignal;
	}

	private static void Shift(decimal[] buffer, decimal value)
	{
		for (var i = 0; i < buffer.Length - 1; i++)
			buffer[i] = buffer[i + 1];

		buffer[^1] = value;
	}

	private static decimal RunLogisticRegression(decimal[] x, decimal[] y, int p, decimal lr, int iterations)
	{
		var w = 0m;

		for (var i = 0; i < iterations; i++)
		{
			var gradient = 0m;

			for (var j = 0; j < p; j++)
			{
				var z = w * x[j];
				var h = Sigmoid(z);
				gradient += (h - y[j]) * x[j];
			}

			gradient /= p;
			w -= lr * gradient;
		}

		var prediction = Sigmoid(w * x[^1]);
		return prediction;
	}

	private static decimal Sigmoid(decimal z)
	{
		var exp = (decimal)Math.Exp((double)(-z));
		return 1m / (1m + exp);
	}
}