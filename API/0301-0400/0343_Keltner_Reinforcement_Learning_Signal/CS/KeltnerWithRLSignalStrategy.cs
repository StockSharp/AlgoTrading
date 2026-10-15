using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Keltner breakouts filtered by an online, one-hidden-layer neural Q learner.
/// </summary>
public class KeltnerWithRLSignalStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _stopLossAtr;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<double> _learningRate;
	private readonly StrategyParam<double> _discountFactor;
	private readonly StrategyParam<double> _exploration;
	private readonly StrategyParam<int> _randomSeed;

	private double[] _previousFeatures;
	private int _previousAction;
	private decimal _previousPrice;
	private decimal _previousAtr;
	private decimal _entryPrice;
	private int _cooldownRemaining;
	private bool _previousAboveUpperBand;
	private bool _previousBelowLowerBand;
	private Order _pendingOrder;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal StopLossAtr { get => _stopLossAtr.Value; set => _stopLossAtr.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public double LearningRate { get => _learningRate.Value; set => _learningRate.Value = value; }
	public double DiscountFactor { get => _discountFactor.Value; set => _discountFactor.Value = value; }
	public double Exploration { get => _exploration.Value; set => _exploration.Value = value; }
	public int RandomSeed { get => _randomSeed.Value; set => _randomSeed.Value = value; }

	// Read-only model diagnostics; learned weights are deliberately not strategy settings.
	public NeuralQModel LearningModel { get; private set; }
	public int CurrentSignal { get; private set; }

	public KeltnerWithRLSignalStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20).SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for the exponential moving average", "Keltner Settings").SetOptimize(10, 30, 5);
		_atrPeriod = Param(nameof(AtrPeriod), 14).SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for the average true range", "Keltner Settings").SetOptimize(7, 21, 7);
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m).SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Multiplier for ATR in Keltner Channels", "Keltner Settings").SetOptimize(1.5m, 3m, 0.5m);
		_stopLossAtr = Param(nameof(StopLossAtr), 2m).SetGreaterThanZero()
			.SetDisplay("Stop Loss (ATR)", "Stop Loss in multiples of ATR", "Risk Management").SetOptimize(1m, 3m, 0.5m);
		_cooldownBars = Param(nameof(CooldownBars), 48).SetNotNegative()
			.SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		_learningRate = Param(nameof(LearningRate), 0.05).SetNotNegative();
		_discountFactor = Param(nameof(DiscountFactor), 0.9).SetNotNegative();
		_exploration = Param(nameof(Exploration), 0.1).SetNotNegative();
		_randomSeed = Param(nameof(RandomSeed), 42).SetNotNegative();
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	public NeuralQModel CreateLearningModel()
		=> new(RandomSeed, LearningRate, DiscountFactor, Exploration);

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		LearningModel = null;
		CurrentSignal = 0;
		_previousFeatures = null;
		_previousAction = 0;
		_previousPrice = _previousAtr = _entryPrice = 0m;
		_cooldownRemaining = 0;
		_previousAboveUpperBand = _previousBelowLowerBand = false;
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetState();
		LearningModel = CreateLearningModel();
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal atr)
	{
		if (candle.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading() || atr <= 0m)
			return;

		var price = candle.ClosePrice;
		double[] features =
		[
			Math.Tanh((double)((price - middleBand) / atr)),
			_previousPrice == 0m ? 0.0 : Math.Tanh((double)((price - _previousPrice) / atr)),
			_previousAtr == 0m ? 0.0 : Math.Tanh((double)((atr - _previousAtr) / _previousAtr)),
			Math.Tanh((double)((price - candle.OpenPrice) / atr)),
		];

		// Reward the preceding policy action only after its next close is known.
		// This is a hypothetical one-bar return, not a fill-price PnL estimate.
		if (_previousFeatures != null)
		{
			var direction = _previousAction == 1 ? 1.0 : _previousAction == 2 ? -1.0 : 0.0;
			var reward = Math.Clamp(direction * (double)((price - _previousPrice) / _previousAtr), -1.0, 1.0);
			LearningModel.Learn(_previousFeatures, _previousAction, reward, features);
		}
		CurrentSignal = LearningModel.SelectAction(features);
		_previousFeatures = features;
		_previousAction = CurrentSignal;
		_previousPrice = price;
		_previousAtr = atr;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;
		if (_pendingOrder?.State is OrderStates.Done or OrderStates.Failed)
			_pendingOrder = null;
		if (Position == 0m)
			_entryPrice = 0m;

		var above = price > middleBand + AtrMultiplier * atr;
		var below = price < middleBand - AtrMultiplier * atr;
		var buy = !_previousAboveUpperBand && above && CurrentSignal == 1;
		var sell = !_previousBelowLowerBand && below && CurrentSignal == 2;
		_previousAboveUpperBand = above;
		_previousBelowLowerBand = below;

		if (_pendingOrder != null)
			return;

		// One order per callback: a reversal must not also submit an EMA/stop exit.
		if (_cooldownRemaining == 0 && buy && Position <= 0m)
			Submit(Sides.Buy, Volume + Math.Abs(Position), price);
		else if (_cooldownRemaining == 0 && sell && Position >= 0m)
			Submit(Sides.Sell, Volume + Math.Abs(Position), price);
		else if (Position > 0m && (price < middleBand || (_entryPrice > 0m && price < _entryPrice - StopLossAtr * atr)))
			Submit(Sides.Sell, Math.Abs(Position), 0m);
		else if (Position < 0m && (price > middleBand || (_entryPrice > 0m && price > _entryPrice + StopLossAtr * atr)))
			Submit(Sides.Buy, Math.Abs(Position), 0m);
	}

	private void Submit(Sides side, decimal volume, decimal entryPrice)
	{
		_entryPrice = entryPrice;
		_cooldownRemaining = CooldownBars;
		_pendingOrder = side == Sides.Buy ? BuyMarket(volume) : SellMarket(volume);
	}

	/// <summary>
	/// Four inputs, eight tanh hidden neurons and three linear Q outputs:
	/// neutral, buy and sell. Both layers learn with a detached TD target.
	/// </summary>
	public sealed class NeuralQModel
	{
		private readonly double[][] _hidden = new double[8][];
		private readonly double[][] _output = new double[3][];
		private readonly double _learningRate;
		private readonly double _discount;
		private readonly double _exploration;
		private uint _randomState;

		public int Updates { get; private set; }

		public NeuralQModel(int seed, double learningRate, double discount, double exploration)
		{
			if (seed < 0)
				throw new ArgumentOutOfRangeException(nameof(seed));
			if (!double.IsFinite(learningRate) || learningRate < 0.0 || learningRate > 1.0)
				throw new ArgumentOutOfRangeException(nameof(learningRate));
			if (!double.IsFinite(discount) || discount < 0.0 || discount > 1.0)
				throw new ArgumentOutOfRangeException(nameof(discount));
			if (!double.IsFinite(exploration) || exploration < 0.0 || exploration > 1.0)
				throw new ArgumentOutOfRangeException(nameof(exploration));
			_learningRate = learningRate;
			_discount = discount;
			_exploration = exploration;
			_randomState = (uint)seed;
			for (var i = 0; i < 8; i++)
				_hidden[i] = Enumerable.Range(0, 5).Select(_ => (NextRandom() - 0.5) * 0.2).ToArray();
			for (var i = 0; i < 3; i++)
				_output[i] = Enumerable.Range(0, 9).Select(_ => (NextRandom() - 0.5) * 0.2).ToArray();
		}

		private double NextRandom()
		{
			_randomState = unchecked(1664525u * _randomState + 1013904223u);
			return _randomState / 4294967296.0;
		}

		private double[] HiddenValues(double[] features)
		{
			if (features.Length != 4 || features.Any(value => !double.IsFinite(value)))
				throw new ArgumentException("Four finite features are required.", nameof(features));
			var values = new double[8];
			for (var neuron = 0; neuron < 8; neuron++)
			{
				var sum = _hidden[neuron][4];
				for (var feature = 0; feature < 4; feature++)
					sum += _hidden[neuron][feature] * features[feature];
				values[neuron] = Math.Tanh(sum);
			}
			return values;
		}

		public double[] Predict(double[] features)
		{
			var hidden = HiddenValues(features);
			var result = new double[3];
			for (var action = 0; action < 3; action++)
			{
				result[action] = _output[action][8];
				for (var neuron = 0; neuron < 8; neuron++)
					result[action] += _output[action][neuron] * hidden[neuron];
			}
			return result;
		}

		public int SelectAction(double[] features)
		{
			var values = Predict(features);
			if (NextRandom() < _exploration)
				return (int)(NextRandom() * 3);
			var best = 0;
			for (var action = 1; action < 3; action++)
				if (values[action] > values[best])
					best = action;
			return best;
		}

		public void Learn(double[] state, int action, double reward, double[] nextState)
		{
			if (action < 0 || action > 2 || !double.IsFinite(reward))
				throw new ArgumentOutOfRangeException(nameof(action));
			var hidden = HiddenValues(state);
			var error = Math.Clamp(reward + _discount * Predict(nextState).Max() - Predict(state)[action], -1.0, 1.0);
			if (_learningRate == 0.0)
				return;
			// Backprop uses the output weights from BEFORE their update.
			var previousOutput = (double[])_output[action].Clone();
			for (var neuron = 0; neuron < 8; neuron++)
			{
				_output[action][neuron] += _learningRate * error * hidden[neuron];
				var gradient = error * previousOutput[neuron] * (1.0 - hidden[neuron] * hidden[neuron]);
				for (var feature = 0; feature < 4; feature++)
					_hidden[neuron][feature] += _learningRate * gradient * state[feature];
				_hidden[neuron][4] += _learningRate * gradient;
			}
			_output[action][8] += _learningRate * error;
			Updates++;
		}

		public double[] GetWeights()
			=> _hidden.Concat(_output).SelectMany(layer => layer).ToArray();
	}
}
