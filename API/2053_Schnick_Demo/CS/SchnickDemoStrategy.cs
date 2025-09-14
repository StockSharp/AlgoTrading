using System;

using StockSharp.Algo.Strategies;
using StockSharp.Logging;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Demonstrates a simple classifier for identifying a fictional creature called "Schnick".
/// Generates training data, trains a nearest neighbor classifier and evaluates accuracy.
/// </summary>
public class SchnickDemoStrategy : Strategy
{
	private const int InputCount = 7;
	
	private readonly StrategyParam<int> _trainingPoints;
	private readonly StrategyParam<int> _testPoints;
	
	private readonly Random _rand = new();
	
	/// <summary>
	/// Number of samples used for training.
	/// </summary>
	public int TrainingPoints
	{
		get => _trainingPoints.Value;
		set => _trainingPoints.Value = value;
	}
	
	/// <summary>
	/// Number of samples used for testing.
	/// </summary>
	public int TestPoints
	{
		get => _testPoints.Value;
		set => _testPoints.Value = value;
	}
	
	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public SchnickDemoStrategy()
	{
		_trainingPoints = Param(nameof(TrainingPoints), 5000)
		.SetGreaterThanZero()
		.SetDisplay("Training Points", "Number of samples for training", "General")
		.SetCanOptimize(true)
		.SetOptimize(1000, 10000, 1000);
		
		_testPoints = Param(nameof(TestPoints), 5000)
		.SetGreaterThanZero()
		.SetDisplay("Test Points", "Number of samples for testing", "General")
		.SetCanOptimize(true)
		.SetOptimize(1000, 10000, 1000);
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var inputs = new double[TrainingPoints * InputCount];
		var outputs = new bool[TrainingPoints];
		
		// Generate training data without errors
		GenerateTrainingData(inputs, outputs, TrainingPoints);
		
		var classifier = new NearestNeighborClassifier();
		classifier.Train(inputs, outputs, InputCount);
		var accuracy1 = TestClassifier(classifier, TestPoints);
		
		// Copy data and insert random errors
		var inputsWithErrors = new double[inputs.Length];
		var outputsWithErrors = new bool[outputs.Length];
		Array.Copy(inputs, inputsWithErrors, inputs.Length);
		Array.Copy(outputs, outputsWithErrors, outputs.Length);
		InsertRandomErrors(inputsWithErrors, outputsWithErrors, 500);
		
		var classifierWithErrors = new NearestNeighborClassifier();
		classifierWithErrors.Train(inputsWithErrors, outputsWithErrors, InputCount);
		var accuracy2 = TestClassifier(classifierWithErrors, TestPoints);
		
		this.AddInfoLog("The classifier accuracy is {0:F2}% (using inputs without errors)", accuracy1);
		this.AddInfoLog("The classifier accuracy is {0:F2}% (using inputs with errors)", accuracy2);
		
		Stop();
	}
	
	private void GenerateTrainingData(double[] inputs, bool[] outputs, int count)
	{
		var temp = new double[InputCount];
		
		for (var i = 0; i < count; i++)
		{
			temp[0] = RandBetween(980, 1120);
			temp[1] = RandBetween(38, 52);
			temp[2] = RandBetween(7, 11);
			temp[3] = RandBetween(3, 4.2);
			temp[4] = RandBetween(380, 450);
			temp[5] = RandBetween(2, 2.6);
			temp[6] = RandBetween(10500, 15500);
			
			Array.Copy(temp, 0, inputs, i * InputCount, InputCount);
			outputs[i] = IsItASchnick(temp);
		}
	}
	
	private double TestClassifier(NearestNeighborClassifier classifier, int count)
	{
		var input = new double[InputCount];
		var correct = 0;
		
		for (var i = 0; i < count; i++)
		{
			input[0] = RandBetween(980, 1120);
			input[1] = RandBetween(38, 52);
			input[2] = RandBetween(7, 11);
			input[3] = RandBetween(3, 4.2);
			input[4] = RandBetween(380, 450);
			input[5] = RandBetween(2, 2.6);
			input[6] = RandBetween(10500, 15500);
			
			var actual = IsItASchnick(input);
			var predicted = classifier.Classify(input);
			
			if (actual == predicted)
			correct++;
		}
		
		return 100.0 * correct / count;
	}
	
	private void InsertRandomErrors(double[] inputs, bool[] outputs, int count)
	{
		var temp = new double[InputCount];
		
		for (var i = 0; i < count; i++)
		{
			temp[0] = RandBetween(980, 1120);
			temp[1] = RandBetween(38, 52);
			temp[2] = RandBetween(7, 11);
			temp[3] = RandBetween(3, 4.2);
			temp[4] = RandBetween(380, 450);
			temp[5] = RandBetween(2, 2.6);
			temp[6] = RandBetween(10500, 15500);
			
			var index = (int)Math.Round(RandBetween(0, outputs.Length - 1));
			var randomOutput = RandBetween(0, 1) > 0.5;
			
			Array.Copy(temp, 0, inputs, index * InputCount, InputCount);
			outputs[index] = randomOutput;
		}
	}
	
	private static bool IsItASchnick(double[] v)
	{
		if (v[0] < 1000 || v[0] > 1100)
		return false;
		if (v[1] < 40 || v[1] > 50)
		return false;
		if (v[2] < 8 || v[2] > 10)
		return false;
		if (v[3] < 3 || v[3] > 4)
		return false;
		if (v[4] < 400 || v[4] > 450)
		return false;
		if (v[5] < 2 || v[5] > 2.5)
		return false;
		if (v[6] < 11000 || v[6] > 15000)
		return false;
		return true;
	}
	
	private double RandBetween(double t1, double t2)
	{
		return (t2 - t1) * _rand.NextDouble() + t1;
	}
	
	private sealed class NearestNeighborClassifier
	{
		private double[,] _inputs = new double[0, 0];
		private bool[] _outputs = Array.Empty<bool>();
		private int _inputCount;
		
		public void Train(double[] inputs, bool[] outputs, int inputCount)
		{
			_inputCount = inputCount;
			_inputs = new double[outputs.Length, inputCount];
			_outputs = new bool[outputs.Length];
			
			for (var i = 0; i < outputs.Length; i++)
			{
				for (var j = 0; j < inputCount; j++)
				_inputs[i, j] = inputs[i * inputCount + j];
				
				_outputs[i] = outputs[i];
			}
		}
		
		public bool Classify(double[] input)
		{
			var bestIndex = 0;
			var bestDistance = double.MaxValue;
			
			for (var i = 0; i < _outputs.Length; i++)
			{
				var dist = 0.0;
				
				for (var j = 0; j < _inputCount; j++)
				{
					var diff = input[j] - _inputs[i, j];
					dist += diff * diff;
				}
				
				if (dist < bestDistance)
				{
					bestDistance = dist;
					bestIndex = i;
				}
			}
			
			return _outputs[bestIndex];
		}
	}
}
