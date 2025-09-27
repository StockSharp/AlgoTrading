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

using System.Text;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy implementing a Hopfield neural network trained on price direction sequences.
/// </summary>
public class MatrixMachineLearningStrategy : Strategy
{
	private readonly StrategyParam<int> _maxIterations;
	private readonly StrategyParam<double> _accuracy;

	private readonly StrategyParam<int> _historyDepth;
	private readonly StrategyParam<int> _forwardDepth;
	private readonly StrategyParam<int> _predictorLength;
	private readonly StrategyParam<int> _forecastLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _enableDebugLog;

	private readonly List<decimal> _closes = new();

	private double[,] _weights;

	/// <summary>
	/// Number of most recent candle closes used for training.
	/// </summary>
	public int HistoryDepth
	{
		get => _historyDepth.Value;
		set => _historyDepth.Value = value;
	}

	/// <summary>
	/// Portion of the history reserved for forward evaluation.
	/// </summary>
	public int ForwardDepth
	{
		get => _forwardDepth.Value;
		set => _forwardDepth.Value = value;
	}

	/// <summary>
	/// Number of binary price movements forming the network input vector.
	/// </summary>
	public int PredictorLength
	{
		get => _predictorLength.Value;
		set => _predictorLength.Value = value;
	}

	/// <summary>
	/// Number of steps predicted by the network output vector.
	/// </summary>
	public int ForecastLength
	{
		get => _forecastLength.Value;
		set => _forecastLength.Value = value;
	}

	/// <summary>
	/// Candle type used to gather prices.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Maximum number of Hopfield iterations executed per forecast.
	/// </summary>
	public int MaxIterations
	{
		get => _maxIterations.Value;
		set => _maxIterations.Value = value;
	}

	/// <summary>
	/// Desired accuracy when checking convergence of neuron states.
	/// </summary>
	public double Accuracy
	{
		get => _accuracy.Value;
		set => _accuracy.Value = value;
	}

	/// <summary>
	/// Enables verbose logging of the neural network state.
	/// </summary>
	public bool EnableDebugLog
	{
		get => _enableDebugLog.Value;
		set => _enableDebugLog.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public MatrixMachineLearningStrategy()
	{
		_maxIterations = Param(nameof(MaxIterations), 100)
			.SetGreaterThanZero()
			.SetDisplay("Max Iterations", "Maximum number of Hopfield iterations executed per forecast.", "Machine Learning");

		_accuracy = Param(nameof(Accuracy), 0.00001)
			.SetDisplay("Accuracy", "Desired accuracy when checking convergence of neuron states.", "Machine Learning");

		_historyDepth = Param(nameof(HistoryDepth), 120)
			.SetGreaterThanZero()
			.SetDisplay("History Depth", "Total amount of closes stored for the Hopfield network.", "Machine Learning")
			.SetCanOptimize(true)
			.SetOptimize(80, 200, 10);

		_forwardDepth = Param(nameof(ForwardDepth), 60)
			.SetGreaterThanZero()
			.SetDisplay("Forward Depth", "Amount of closes kept for out-of-sample validation.", "Machine Learning")
			.SetCanOptimize(true)
			.SetOptimize(30, 120, 10);

		_predictorLength = Param(nameof(PredictorLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Predictor Length", "Length of binary vector passed to the network input.", "Machine Learning")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 2);

		_forecastLength = Param(nameof(ForecastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Forecast Length", "Length of the binary output vector produced by the network.", "Machine Learning")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles requested from the market data source.", "Data");

		_enableDebugLog = Param(nameof(EnableDebugLog), false)
			.SetDisplay("Debug Log", "Write detailed neural network diagnostics to the log.", "Machine Learning");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_closes.Clear();
		_weights = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

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

		_closes.Add(candle.ClosePrice);
		if (_closes.Count > HistoryDepth)
			_closes.RemoveAt(0);

		if (_closes.Count < PredictorLength + ForecastLength + 1)
			return;

		if (_closes.Count < ForwardDepth + 2)
			return;

		TrainNetwork();

		var forecast = Forecast();
		if (forecast == null || forecast.Length == 0)
			return;

		var direction = forecast[0];
		if (direction > 0 && Position <= 0m)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0m)
				BuyMarket(volume);
		}
		else if (direction < 0 && Position >= 0m)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0m)
				SellMarket(volume);
		}
	}

	private void TrainNetwork()
	{
		var historyCount = _closes.Count;
		var forwardCount = Math.Min(ForwardDepth, historyCount - 1);
		var trainingCount = historyCount - forwardCount;

		if (trainingCount <= PredictorLength + ForecastLength)
			return;

		var trainingData = BuildBinaryDiff(0, trainingCount);
		if (trainingData.Length < PredictorLength + ForecastLength)
			return;

		var weights = TrainWeights(trainingData, PredictorLength, ForecastLength);
		if (weights == null)
			return;

		_weights = weights;

		EvaluateWeights(trainingData, "Backtest evaluation");

		var forwardData = BuildBinaryDiff(trainingCount - 1, forwardCount + 1);
		if (forwardData.Length >= PredictorLength + ForecastLength)
			EvaluateWeights(forwardData, "Forward evaluation");
	}

	private double[] Forecast()
	{
		var weights = _weights;
		if (weights == null)
			return null;

		var pattern = BuildCurrentPattern();
		if (pattern == null)
			return null;

		var forecast = RunWeights(weights, pattern);

		if (EnableDebugLog)
		{
			LogInfo(FormattableString.Invariant($"Online pattern: {FormatVector(pattern)}"));
			LogInfo(FormattableString.Invariant($"Forecast: {FormatVector(forecast)}"));
		}

		return forecast;
	}

	private double[] BuildBinaryDiff(int startIndex, int length)
	{
		if (length <= 1 || startIndex < 0)
			return Array.Empty<double>();

		if (startIndex + length > _closes.Count)
			length = _closes.Count - startIndex;

		var resultLength = length - 1;
		if (resultLength <= 0)
			return Array.Empty<double>();

		var result = new double[resultLength];
		for (var i = 0; i < resultLength; i++)
		{
			var first = _closes[startIndex + i];
			var second = _closes[startIndex + i + 1];
			var diff = (double)(second - first);
			result[i] = diff >= 0 ? 1d : -1d;
		}

		return result;
	}

	private double[] BuildCurrentPattern()
	{
		var required = PredictorLength + 1;
		if (_closes.Count < required)
			return null;

		var startIndex = _closes.Count - required;
		var pattern = new double[PredictorLength];
		for (var i = 0; i < PredictorLength; i++)
		{
			var first = _closes[startIndex + i];
			var second = _closes[startIndex + i + 1];
			var diff = (double)(second - first);
			pattern[i] = diff >= 0 ? 1d : -1d;
		}

		return pattern;
	}

	private static double[,] TrainWeights(double[] data, int predictor, int response)
	{
		var sample = predictor + response;
		if (data.Length < sample)
			return null;

		var count = data.Length - sample + 1;
		var weights = new double[predictor, response];

		for (var index = 0; index < count; index++)
		{
			for (var row = 0; row < predictor; row++)
			{
				var inputValue = data[index + row];
				for (var column = 0; column < response; column++)
				{
					var outputValue = data[index + predictor + column];
					weights[row, column] += inputValue * outputValue;
				}
			}
		}

		return weights;
	}

	private void EvaluateWeights(double[] data, string title)
	{
		var weights = _weights;
		if (weights == null)
			return;

		var predictor = weights.GetLength(0);
		var response = weights.GetLength(1);
		var sample = predictor + response;

		if (data.Length < sample)
			return;

		var count = data.Length - sample + 1;
		if (count <= 0)
			return;

		var positive = 0;
		var negative = 0;
		double sum = 0;

		for (var index = 0; index < count; index++)
		{
			var input = new double[predictor];
			var target = new double[response];

			for (var i = 0; i < predictor; i++)
				input[i] = data[index + i];

			for (var i = 0; i < response; i++)
				target[i] = data[index + predictor + i];

			var forecast = RunWeights(weights, input);

			double match = 0;
			for (var i = 0; i < response; i++)
				match += forecast[i] * target[i];

			if (match > 0)
				positive++;
			else if (match < 0)
				negative++;

			sum += match;

			if (EnableDebugLog)
			{
				LogInfo(FormattableString.Invariant($"Sample {index}: forecast={FormatVector(forecast)} target={FormatVector(target)} match={match:0.###}"));
			}
		}

		var average = sum / count;
		var accuracy = (average + response) / (2.0 * response) * 100.0;

		LogInfo(FormattableString.Invariant($"{title}: count={count} positive={positive} negative={negative} accuracy={accuracy:0.##}%"));
	}

	private static double[] RunWeights(double[,] weights, double[] input)
	{
		var predictor = weights.GetLength(0);
		var response = weights.GetLength(1);
		var forecast = new double[response];

		if (input.Length != predictor)
			return forecast;

		var a = new double[predictor];
		var b = new double[response];

		for (var i = 0; i < predictor; i++)
			a[i] = input[i];

		for (var iteration = 0; iteration < MaxIterations; iteration++)
		{
			var previousA = new double[predictor];
			var previousB = new double[response];

			for (var i = 0; i < predictor; i++)
				previousA[i] = a[i];

			for (var i = 0; i < response; i++)
				previousB[i] = b[i];

			for (var column = 0; column < response; column++)
			{
				double sum = 0;
				for (var row = 0; row < predictor; row++)
					sum += a[row] * weights[row, column];
				b[column] = Math.Tanh(sum);
			}

			for (var row = 0; row < predictor; row++)
			{
				double sum = 0;
				for (var column = 0; column < response; column++)
					sum += b[column] * weights[row, column];
				a[row] = Math.Tanh(sum);
			}

			var diffA = 0d;
			for (var i = 0; i < predictor; i++)
			{
				var delta = Math.Abs(a[i] - previousA[i]);
				if (delta > diffA)
					diffA = delta;
			}

			var diffB = 0d;
			for (var i = 0; i < response; i++)
			{
				var delta = Math.Abs(b[i] - previousB[i]);
				if (delta > diffB)
					diffB = delta;
			}

			if (diffA < Accuracy && diffB < Accuracy)
				break;
		}

		for (var i = 0; i < response; i++)
			forecast[i] = b[i] >= 0 ? 1d : -1d;

		return forecast;
	}

	private static string FormatVector(IReadOnlyList<double> values)
	{
		var builder = new StringBuilder();
		builder.Append('[');
		for (var i = 0; i < values.Count; i++)
		{
			builder.Append(FormattableString.Invariant($"{values[i]:0.###}"));
			if (i + 1 < values.Count)
				builder.Append(',');
		}
		builder.Append(']');
		return builder.ToString();
	}
}

