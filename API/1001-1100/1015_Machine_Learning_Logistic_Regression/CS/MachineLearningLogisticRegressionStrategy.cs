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
/// Logistic regression based strategy.
/// Updates a simple online model on each finished candle and trades by prediction.
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
	private double _weight;

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
			
			.SetOptimize(2, 10, 1);

		_learningRate = Param(nameof(LearningRate), 0.0009m)
			.SetGreaterThanZero()
			.SetDisplay("Learning Rate", "Gradient descent step", "General")
			
			.SetOptimize(0.0001m, 0.01m, 0.0001m);

		_iterations = Param(nameof(Iterations), 10)
			.SetGreaterThanZero()
			.SetDisplay("Iterations", "Training iterations per candle", "General")
			
			.SetOptimize(1, 50, 1);

		_holdingPeriod = Param(nameof(HoldingPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Holding Period", "Bars to hold position", "General")
			
			.SetOptimize(1, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_baseSeries = new decimal[Lookback];
		_synthSeries = new decimal[Lookback];
		_filled = 0;
		_signal = 0;
		_hpCounter = 0;
		_isInitialized = false;
		_weight = 0d;
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
		_weight = 0d;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_baseSeries = new decimal[Lookback];
		_synthSeries = new decimal[Lookback];
		_filled = 0;
		_signal = 0;
		_hpCounter = 0;
		_isInitialized = false;
		_weight = 0d;

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

		// Bootstrap first direction once model buffers are initialized.
		if (_signal == 0)
		{
			_signal = candle.ClosePrice >= _baseSeries[^2] ? 1 : -1;
			_hpCounter = 0;

			if (_signal == 1 && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (_signal == -1 && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));

			return;
		}

		var prediction = TrainAndPredict();

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

	// The weight is kept between candles, so each candle applies Iterations gradient
	// steps to the existing model instead of retraining it from scratch.
	private decimal TrainAndPredict()
	{
		var p = Lookback;
		var lr = (double)LearningRate;
		var iterations = Iterations;

		for (var i = 0; i < iterations; i++)
		{
			var gradient = 0d;

			for (var j = 0; j < p; j++)
			{
				var x = (double)_baseSeries[j];
				var h = Sigmoid(_weight * x);
				gradient += (h - (double)_synthSeries[j]) * x;
			}

			gradient /= p;
			_weight -= lr * gradient;
		}

		return (decimal)Sigmoid(_weight * (double)_baseSeries[^1]);
	}

	private static double Sigmoid(double z)
	{
		return 1d / (1d + Math.Exp(-z));
	}
}
