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
/// Port of the MetaTrader expert TestMnistOnnx.
/// Converts a rolling grid of candle closes into pattern classes and trades on a selected class.
/// The original mouse-drawn image is replaced with market data derived features.
/// </summary>
public class MnistPatternClassifierStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _targetClass;
	private readonly StrategyParam<decimal> _confidenceThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi = null!;
	private AverageTrueRange _atr = null!;

	private readonly Queue<decimal> _closeWindow = new();
	private decimal _firstClose;
	private decimal _previousClose;

	private int _lastClass = -1;
	private decimal _lastConfidence;

	private enum PatternBiases
	{
		Neutral,
		Bullish,
		Bearish,
	}

	/// <summary>
	/// Number of finished candles that form the MNIST-like grid.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Pattern class (0-9) that will trigger trading actions.
	/// </summary>
	public int TargetClass
	{
		get => _targetClass.Value;
		set => _targetClass.Value = value;
	}

	/// <summary>
	/// Minimum confidence required before orders are sent.
	/// </summary>
	public decimal ConfidenceThreshold
	{
		get => _confidenceThreshold.Value;
		set => _confidenceThreshold.Value = value;
	}


	/// <summary>
	/// Candle type that feeds the pattern grid.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MnistPatternClassifierStrategy"/> class.
	/// </summary>
	public MnistPatternClassifierStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 28)
		.SetRange(10, 200)
		.SetCanOptimize(true)
		.SetDisplay("Lookback", "Number of candles converted into the pattern grid", "Pattern");

		_targetClass = Param(nameof(TargetClass), 1)
		.SetRange(0, 9)
		.SetDisplay("Target Class", "Pattern class that should be traded", "Pattern");

		_confidenceThreshold = Param(nameof(ConfidenceThreshold), 0.6m)
		.SetRange(0m, 1m)
		.SetDisplay("Confidence", "Minimum classification confidence", "Pattern");


		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for the pattern", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_closeWindow.Clear();
		_firstClose = 0m;
		_previousClose = 0m;
		_lastClass = -1;
		_lastConfidence = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_rsi = new RelativeStrengthIndex
		{
			Length = LookbackPeriod,
		};

		_atr = new AverageTrueRange
		{
			Length = LookbackPeriod,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_rsi, _atr, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateWindow(candle.ClosePrice);

		if (_closeWindow.Count < LookbackPeriod)
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		var pattern = ClassifyPattern(candle.ClosePrice, rsiValue, atrValue);

		_lastClass = pattern.PatternClass;
		_lastConfidence = pattern.Confidence;

		if (pattern.PatternClass == TargetClass && pattern.Confidence >= ConfidenceThreshold)
		{
			ExecuteBias(pattern.Bias);
		}
		else
		{
			FlattenPosition();
		}

		_previousClose = candle.ClosePrice;
	}

	private void ExecuteBias(PatternBiases bias)
	{
		switch (bias)
		{
		case PatternBiases.Bullish:
			if (Position < 0)
			BuyMarket(-Position);

			if (Position <= 0)
			{
				BuyMarket(Volume);
				LogInfo($"Pattern {TargetClass} bullish with confidence {_lastConfidence:F2}. Open long.");
			}

			break;
		case PatternBiases.Bearish:
			if (Position > 0)
			SellMarket(Position);

			if (Position >= 0)
			{
				SellMarket(Volume);
				LogInfo($"Pattern {TargetClass} bearish with confidence {_lastConfidence:F2}. Open short.");
			}

			break;
		default:
			FlattenPosition();
			break;
		}
	}

	private void FlattenPosition()
	{
		if (Position > 0)
		{
			SellMarket(Position);
			LogInfo($"Pattern {_lastClass} confidence {_lastConfidence:F2}. Exit long.");
		}
		else if (Position < 0)
		{
			BuyMarket(-Position);
			LogInfo($"Pattern {_lastClass} confidence {_lastConfidence:F2}. Exit short.");
		}
	}

	private void UpdateWindow(decimal close)
	{
		_closeWindow.Enqueue(close);

		if (_closeWindow.Count > LookbackPeriod)
		{
			_closeWindow.Dequeue();
		}

		_firstClose = _closeWindow.Count > 0 ? _closeWindow.Peek() : 0m;
	}

	private PatternResult ClassifyPattern(decimal currentClose, decimal rsiValue, decimal atrValue)
	{
		var stats = CalculateStatistics(currentClose, rsiValue, atrValue);

		var trendStrength = stats.TrendStrength;
		var rangeStrength = stats.RangeStrength;
		var breakoutRange = stats.BreakoutThreshold;
		var rangePosition = stats.RangePosition;
		var momentum = stats.Momentum;
		var rsi = stats.Rsi;
		var atr = stats.AtrNormalized;

		// Compute a blended confidence score similar to the ONNX output probability.
		var confidence = Math.Min(1m, (trendStrength + rangeStrength + Math.Min(1m, Math.Abs(momentum) / stats.MomentumThreshold) + stats.RsiDeviation + atr) / 5m);

		if (rangeStrength < stats.FlatThreshold)
		{
			return new PatternResult(0, Math.Max(confidence, 0.4m), PatternBiases.Neutral);
		}

		if (trendStrength >= stats.TrendThreshold)
		{
			if (rangePosition >= 0.75m && rangeStrength >= breakoutRange)
			{
				return new PatternResult(3, confidence, PatternBiases.Bullish);
			}

			if (momentum < 0m)
			{
				return new PatternResult(6, confidence * 0.8m, PatternBiases.Bullish);
			}

			return new PatternResult(1, confidence, PatternBiases.Bullish);
		}

		if (trendStrength <= -stats.TrendThreshold)
		{
			if (rangePosition <= 0.25m && rangeStrength >= breakoutRange)
			{
				return new PatternResult(4, confidence, PatternBiases.Bearish);
			}

			if (momentum > 0m)
			{
				return new PatternResult(7, confidence * 0.8m, PatternBiases.Bearish);
			}

			return new PatternResult(2, confidence, PatternBiases.Bearish);
		}

		if (rangeStrength >= breakoutRange)
		{
			return new PatternResult(5, confidence * 0.9m, PatternBiases.Neutral);
		}

		if (rangePosition <= 0.4m && rsi >= 55m)
		{
			return new PatternResult(8, confidence * 0.85m, PatternBiases.Bullish);
		}

		if (rangePosition >= 0.6m && rsi <= 45m)
		{
			return new PatternResult(9, confidence * 0.85m, PatternBiases.Bearish);
		}

		return new PatternResult(0, confidence * 0.7m, PatternBiases.Neutral);
	}

	private PatternStatistics CalculateStatistics(decimal currentClose, decimal rsiValue, decimal atrValue)
	{
		decimal min = decimal.MaxValue;
		decimal max = decimal.MinValue;

		foreach (var value in _closeWindow)
		{
			if (value < min)
			min = value;

			if (value > max)
			max = value;
		}

		var first = _firstClose;
		var last = currentClose;
		var range = max - min;
		var rangeStrength = first != 0m ? range / first : 0m;
		var trend = first != 0m ? (last - first) / first : 0m;
		var momentum = _previousClose != 0m ? (last - _previousClose) / _previousClose : 0m;
		var rsiDeviation = Math.Min(1m, Math.Abs(rsiValue - 50m) / 50m);
		var atrNormalized = first != 0m ? Math.Min(1m, atrValue / first) : 0m;

		var rangePosition = range > 0m ? (last - min) / range : 0.5m;

		const decimal baseThreshold = 0.005m;
		var trendThreshold = baseThreshold;
		var breakoutThreshold = baseThreshold * 1.4m;
		var flatThreshold = baseThreshold * 0.3m;
		var momentumThreshold = baseThreshold;

		return new PatternStatistics
		{
			TrendStrength = trend,
			RangeStrength = rangeStrength,
			BreakoutThreshold = breakoutThreshold,
			FlatThreshold = flatThreshold,
			RangePosition = rangePosition,
			Momentum = momentum,
			Rsi = rsiValue,
			AtrNormalized = atrNormalized,
			TrendThreshold = trendThreshold,
			MomentumThreshold = momentumThreshold,
			RsiDeviation = rsiDeviation,
		};
	}

	private readonly struct PatternResult
	{
		public PatternResult(int patternClass, decimal confidence, PatternBiases bias)
		{
			PatternClass = patternClass;
			Confidence = confidence;
			Bias = bias;
		}

		public int PatternClass { get; }

		public decimal Confidence { get; }

		public PatternBiases Bias { get; }
	}

	private readonly struct PatternStatistics
	{
		public decimal TrendStrength { get; init; }
		public decimal RangeStrength { get; init; }
		public decimal BreakoutThreshold { get; init; }
		public decimal FlatThreshold { get; init; }
		public decimal RangePosition { get; init; }
		public decimal Momentum { get; init; }
		public decimal Rsi { get; init; }
		public decimal AtrNormalized { get; init; }
		public decimal TrendThreshold { get; init; }
		public decimal MomentumThreshold { get; init; }
		public decimal RsiDeviation { get; init; }
	}
}

