using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive neural network strategy with ATR based risk control.
/// Combines normalized candle features with a lightweight neural layer
/// to generate probabilistic long and short signals.
/// Applies daily and total drawdown checks together with ATR driven
/// position sizing and protective orders.
/// </summary>
public class NeuralNetworkAtrStrategy : Strategy
{
	private readonly StrategyParam<decimal> _maxRiskPerTrade;
	private readonly StrategyParam<decimal> _dailyLossLimit;
	private readonly StrategyParam<decimal> _totalLossLimit;
	private readonly StrategyParam<decimal> _dailyProfitTarget;
	private readonly StrategyParam<decimal> _initialLearningRate;
	private readonly StrategyParam<int> _hiddenLayerSize;
	private readonly StrategyParam<decimal> _buyThreshold;
	private readonly StrategyParam<decimal> _sellThreshold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _maxSpreadPoints;
	private readonly StrategyParam<decimal> _riskRewardRatio;
	private readonly StrategyParam<int> _fallbackStopLossPoints;
	private readonly StrategyParam<int> _inputSize;
	private readonly StrategyParam<decimal> _minimumLearningRate;
	private readonly StrategyParam<decimal> _featureClamp;

	private decimal _accountEquityAtStart;
	private decimal _dailyEquityAtStart;
	private DateTime _lastTradeDay;
	private DateTime _lastPenaltyDay;
	private bool _tradingHalted;

	private decimal _learningRate;
	private ATR _atrIndicator;

	private decimal[] _weightsInputHidden = Array.Empty<decimal>();
	private decimal[] _biasHidden = Array.Empty<decimal>();
	private decimal[] _weightsHiddenOutput = Array.Empty<decimal>();
	private decimal[] _hiddenOutputs = Array.Empty<decimal>();
	private decimal _biasOutput;

	private ICandleMessage _previousCandle;
	private decimal _bestBidPrice;
	private decimal _bestAskPrice;
	private bool _hasBestBid;
	private bool _hasBestAsk;

	/// <summary>
	/// Maximum share of equity risked in a single trade (percentage).
	/// </summary>
	public decimal MaxRiskPerTrade
	{
		get => _maxRiskPerTrade.Value;
		set => _maxRiskPerTrade.Value = value;
	}

	/// <summary>
	/// Daily drawdown threshold in percent.
	/// </summary>
	public decimal DailyLossLimit
	{
		get => _dailyLossLimit.Value;
		set => _dailyLossLimit.Value = value;
	}

	/// <summary>
	/// Total drawdown threshold in percent.
	/// </summary>
	public decimal TotalLossLimit
	{
		get => _totalLossLimit.Value;
		set => _totalLossLimit.Value = value;
	}

	/// <summary>
	/// Minimum daily profit target before penalty is removed.
	/// </summary>
	public decimal DailyProfitTarget
	{
		get => _dailyProfitTarget.Value;
		set => _dailyProfitTarget.Value = value;
	}

	/// <summary>
	/// Initial learning rate that scales signal intensity.
	/// </summary>
	public decimal InitialLearningRate
	{
		get => _initialLearningRate.Value;
		set => _initialLearningRate.Value = value;
	}

	/// <summary>
	/// Number of neurons in the hidden layer.
	/// </summary>
	public int HiddenLayerSize
	{
		get => _hiddenLayerSize.Value;
		set => _hiddenLayerSize.Value = value;
	}

	/// <summary>
	/// Threshold for opening long positions.
	/// </summary>
	public decimal BuyThreshold
	{
		get => _buyThreshold.Value;
		set => _buyThreshold.Value = value;
	}

	/// <summary>
	/// Threshold for opening short positions.
	/// </summary>
	public decimal SellThreshold
	{
		get => _sellThreshold.Value;
		set => _sellThreshold.Value = value;
	}

	/// <summary>
	/// Selected candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// ATR period used to measure volatility.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Maximum acceptable spread in price steps.
	/// </summary>
	public decimal MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	/// <summary>
	/// Risk to reward ratio for protective orders.
	/// </summary>
	public decimal RiskRewardRatio
	{
		get => _riskRewardRatio.Value;
		set => _riskRewardRatio.Value = value;
	}

	/// <summary>
	/// Fallback stop-loss distance in price steps when ATR is unavailable.
	/// </summary>
	public int FallbackStopLossPoints
	{
		get => _fallbackStopLossPoints.Value;
		set => _fallbackStopLossPoints.Value = value;
	}

	/// <summary>
	/// Number of input features processed by the neural layer.
	/// </summary>
	public int InputSize
	{
		get => _inputSize.Value;
		set => _inputSize.Value = value;
	}

	/// <summary>
	/// Minimum learning rate applied when adapting the network weights.
	/// </summary>
	public decimal MinimumLearningRate
	{
		get => _minimumLearningRate.Value;
		set => _minimumLearningRate.Value = value;
	}

	/// <summary>
	/// Absolute value used to clamp normalized features.
	/// </summary>
	public decimal FeatureClamp
	{
		get => _featureClamp.Value;
		set => _featureClamp.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public NeuralNetworkAtrStrategy()
	{
		_maxRiskPerTrade = Param(nameof(MaxRiskPerTrade), 1.0m)
		.SetGreaterThanZero()
		.SetDisplay("Max Risk %", "Maximum risk per trade as percentage of equity", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 5.0m, 0.5m);

		_dailyLossLimit = Param(nameof(DailyLossLimit), 5.0m)
		.SetGreaterThanZero()
		.SetDisplay("Daily Loss %", "Maximum permitted daily drawdown", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(2.0m, 10.0m, 1.0m);

		_totalLossLimit = Param(nameof(TotalLossLimit), 10.0m)
		.SetGreaterThanZero()
		.SetDisplay("Total Loss %", "Maximum permitted total drawdown", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(5.0m, 20.0m, 1.0m);

		_dailyProfitTarget = Param(nameof(DailyProfitTarget), 1.0m)
		.SetGreaterThanZero()
		.SetDisplay("Daily Profit %", "Target daily profit before penalty is avoided", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 3.0m, 0.5m);

		_initialLearningRate = Param(nameof(InitialLearningRate), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Learning Rate", "Scaling factor for neural output", "Signal")
		.SetCanOptimize(true)
		.SetOptimize(0.005m, 0.05m, 0.005m);

		_hiddenLayerSize = Param(nameof(HiddenLayerSize), 5)
		.SetGreaterThanZero()
		.SetDisplay("Hidden Layer", "Number of neurons in hidden layer", "Signal")
		.SetCanOptimize(true)
		.SetOptimize(3, 9, 2);

		_buyThreshold = Param(nameof(BuyThreshold), 0.6m)
		.SetDisplay("Buy Threshold", "Prediction level to open long trades", "Signal")
		.SetCanOptimize(true)
		.SetOptimize(0.55m, 0.75m, 0.05m);

		_sellThreshold = Param(nameof(SellThreshold), 0.4m)
		.SetDisplay("Sell Threshold", "Prediction level to open short trades", "Signal")
		.SetCanOptimize(true)
		.SetOptimize(0.25m, 0.45m, 0.05m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Candles used for signal calculations", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Period of Average True Range indicator", "Signal")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 2);

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Max Spread", "Maximum allowed spread in points", "Execution")
		.SetCanOptimize(true)
		.SetOptimize(5m, 40m, 5m);

		_riskRewardRatio = Param(nameof(RiskRewardRatio), 2.0m)
		.SetGreaterThanZero()
		.SetDisplay("Risk Reward", "Take profit multiple of stop distance", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(1.0m, 3.0m, 0.5m);

		_fallbackStopLossPoints = Param(nameof(FallbackStopLossPoints), 50)
		.SetGreaterThanZero()
		.SetDisplay("Fallback Stop", "Stop distance when ATR is not formed", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(30, 100, 10);

		_inputSize = Param(nameof(InputSize), 5)
		.SetGreaterThanZero()
		.SetDisplay("Input Size", "Number of features processed by the neural layer", "Neural Network")
		.SetCanOptimize(true)
		.SetOptimize(3, 9, 2);

		_minimumLearningRate = Param(nameof(MinimumLearningRate), 0.0001m)
		.SetGreaterThanZero()
		.SetDisplay("Min Learning Rate", "Lower bound applied when adapting learning rate", "Neural Network");

		_featureClamp = Param(nameof(FeatureClamp), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Feature Clamp", "Absolute value used to clamp normalized features", "Neural Network");
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

	_accountEquityAtStart = 0m;
	_dailyEquityAtStart = 0m;
	_lastTradeDay = default;
	_lastPenaltyDay = default;
	_tradingHalted = false;
	_learningRate = InitialLearningRate;
	_previousCandle = null;
	_bestBidPrice = 0m;
	_bestAskPrice = 0m;
	_hasBestBid = false;
	_hasBestAsk = false;
	_atrIndicator = null;

	InitializeNetwork();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_learningRate = InitialLearningRate;
	InitializeNetwork();

	// Enable protective order handling once at startup.
	StartProtection(new());

	// Prepare ATR indicator and candle subscription.
	var atr = new ATR { Length = AtrPeriod };
	_atrIndicator = atr;

	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(atr, ProcessCandle)
	.Start();

	// Subscribe to level1 updates to evaluate spread in points.
	SubscribeLevel1()
	.Bind(ProcessLevel1)
	.Start();

	LogInfo("Neural network strategy started.");
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
	if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid && bid > 0m)
	{
	_bestBidPrice = bid;
	_hasBestBid = true;
	}

	if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask && ask > 0m)
	{
	_bestAskPrice = ask;
	_hasBestAsk = true;
	}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	if (!_tradingHalted)
	{
	_tradingHalted = !UpdateEquity(candle.OpenTime);
	}

	if (_tradingHalted)
	return;

	if (!EnsureNetworkInitialized())
	return;

	var spreadPoints = GetSpreadPoints();
	if (spreadPoints > 0m && spreadPoints > MaxSpreadPoints)
	{
	LogInfo($"Spread {spreadPoints:F2} exceeds limit {MaxSpreadPoints}.");
	_previousCandle = candle;
	return;
	}

	if (_atrIndicator is { IsFormed: false })
	{
	_previousCandle = candle;
	return;
	}

	if (_previousCandle is null)
	{
	_previousCandle = candle;
	return;
	}

	var inputs = BuildInputs(_previousCandle, candle, atrValue);
	var prediction = ComputePrediction(inputs);
	var adjustedPrediction = AdjustPrediction(prediction);

	LogInfo($"Candle {candle.OpenTime:yyyy-MM-dd HH:mm}, Close {candle.ClosePrice}, ATR {atrValue}, Prediction {adjustedPrediction:F4}.");

	var volume = CalculateTradeVolume(atrValue);
	if (volume <= 0m)
	{
	_previousCandle = candle;
	return;
	}

	var currentPosition = Position;

	if (adjustedPrediction >= BuyThreshold && currentPosition <= 0m)
	{
	var totalVolume = volume + Math.Abs(currentPosition);
	var resultingPosition = currentPosition + totalVolume;

	BuyMarket(totalVolume);
	AttachProtection(candle.ClosePrice, atrValue, resultingPosition);

	LogInfo($"Buy signal. Prediction {adjustedPrediction:F4} above {BuyThreshold}. Volume {totalVolume}.");
	}
	else if (adjustedPrediction <= SellThreshold && currentPosition >= 0m)
	{
	var totalVolume = volume + Math.Abs(currentPosition);
	var resultingPosition = currentPosition - totalVolume;

	SellMarket(totalVolume);
	AttachProtection(candle.ClosePrice, atrValue, resultingPosition);

	LogInfo($"Sell signal. Prediction {adjustedPrediction:F4} below {SellThreshold}. Volume {totalVolume}.");
	}

	_previousCandle = candle;
	}

	private decimal[] BuildInputs(ICandleMessage previous, ICandleMessage current, decimal atrValue)
	{
	var inputs = new decimal[InputSize];

	var previousClose = previous.ClosePrice;
	var currentClose = current.ClosePrice;
	var priceChange = previousClose != 0m ? (currentClose - previousClose) / previousClose : 0m;
	inputs[0] = NormalizeFeature(priceChange);

	var range = current.ClosePrice != 0m ? (current.HighPrice - current.LowPrice) / current.ClosePrice : 0m;
	inputs[1] = NormalizeFeature(range);

	var body = current.HighPrice != current.LowPrice ? (current.ClosePrice - current.OpenPrice) / (current.HighPrice - current.LowPrice) : 0m;
	inputs[2] = NormalizeFeature(body);

	var previousVolume = previous.TotalVolume;
	var currentVolume = current.TotalVolume;
	var volumeChange = previousVolume > 0 ? (currentVolume - previousVolume) / previousVolume : 0m;
	inputs[3] = NormalizeFeature(volumeChange);

	var atrNormalized = current.ClosePrice != 0m ? atrValue / current.ClosePrice : 0m;
	inputs[4] = NormalizeFeature(atrNormalized);

	return inputs;
	}

	private decimal NormalizeFeature(decimal value)
	{
	if (value > FeatureClamp)
	value = FeatureClamp;
	else if (value < -FeatureClamp)
	value = -FeatureClamp;

	return (value + FeatureClamp) / (2m * FeatureClamp);
	}

	private decimal ComputePrediction(IReadOnlyList<decimal> inputs)
	{
	var hiddenLength = _biasHidden.Length;
	for (var j = 0; j < hiddenLength; j++)
	{
	var activation = _biasHidden[j];
	for (var i = 0; i < InputSize; i++)
	{
	activation += inputs[i] * _weightsInputHidden[i * hiddenLength + j];
	}

	_hiddenOutputs[j] = activation > 0m ? activation : 0m;
	}

	var output = _biasOutput;
	for (var j = 0; j < hiddenLength; j++)
	{
	output += _hiddenOutputs[j] * _weightsHiddenOutput[j];
	}

	return Sigmoid(output);
	}

	private decimal AdjustPrediction(decimal prediction)
	{
	var adjusted = prediction * (1m + _learningRate);
	if (adjusted > 1m)
	adjusted = 1m;
	else if (adjusted < 0m)
	adjusted = 0m;

	return adjusted;
	}

	private decimal Sigmoid(decimal value)
	{
	var v = (double)value;
	var result = 1.0 / (1.0 + Math.Exp(-v));
	return (decimal)result;
	}

	private decimal CalculateTradeVolume(decimal atrValue)
	{
	var portfolio = Portfolio;
	if (portfolio is null)
	return Volume;

	var equity = portfolio.CurrentValue;
	if (equity <= 0m)
	return Volume;

	var stopLossPoints = CalculateStopLossPoints(atrValue);
	if (stopLossPoints <= 0)
	return Volume;

	var stepPrice = Security?.StepPrice ?? 0m;
	if (stepPrice <= 0m)
	return Volume;

	var riskAmount = equity * (MaxRiskPerTrade / 100m);
	if (riskAmount <= 0m)
	return Volume;

	var riskPerContract = stopLossPoints * stepPrice;
	if (riskPerContract <= 0m)
	return Volume;

	var rawVolume = riskAmount / riskPerContract;

	var volumeStep = Security?.VolumeStep ?? 0m;
	if (volumeStep > 0m)
	rawVolume = Math.Floor(rawVolume / volumeStep) * volumeStep;

	var minVolume = Security?.MinVolume ?? 0m;
	if (minVolume > 0m && rawVolume < minVolume)
	rawVolume = minVolume;

	var maxVolume = Security?.MaxVolume ?? 0m;
	if (maxVolume > 0m && rawVolume > maxVolume)
	rawVolume = maxVolume;

	return rawVolume > 0m ? rawVolume : Volume;
	}

	private int CalculateStopLossPoints(decimal atrValue)
	{
	var priceStep = Security?.PriceStep ?? 0m;
	if (priceStep > 0m && atrValue > 0m)
	{
	var rawPoints = atrValue / priceStep;
	var rounded = (int)Math.Round((double)rawPoints);
	if (rounded > 0)
	return rounded;
	}

	return FallbackStopLossPoints;
	}

	private void AttachProtection(decimal referencePrice, decimal atrValue, decimal resultingPosition)
	{
	var stopLossPoints = CalculateStopLossPoints(atrValue);
	if (stopLossPoints <= 0)
	return;

	var takeProfitPoints = (int)Math.Round(stopLossPoints * (double)RiskRewardRatio);
	if (takeProfitPoints <= 0)
	takeProfitPoints = stopLossPoints * 2;

	SetStopLoss(stopLossPoints, referencePrice, resultingPosition);
	SetTakeProfit(takeProfitPoints, referencePrice, resultingPosition);
	}

	private bool UpdateEquity(DateTimeOffset time)
	{
	var portfolio = Portfolio;
	if (portfolio is null)
	return true;

	var equity = portfolio.CurrentValue;
	if (equity <= 0m)
	return true;

	var currentDay = time.Date;

	if (_accountEquityAtStart <= 0m)
	{
	_accountEquityAtStart = equity;
	_dailyEquityAtStart = equity;
	_lastTradeDay = currentDay;
	_lastPenaltyDay = default;
	return true;
	}

	if (_lastTradeDay != currentDay)
	{
	_dailyEquityAtStart = equity;
	_lastTradeDay = currentDay;
	_lastPenaltyDay = default;
	}

	var totalDrawdown = _accountEquityAtStart > 0m ? (_accountEquityAtStart - equity) / _accountEquityAtStart * 100m : 0m;
	var dailyDrawdown = _dailyEquityAtStart > 0m ? (_dailyEquityAtStart - equity) / _dailyEquityAtStart * 100m : 0m;

	if (dailyDrawdown >= DailyLossLimit || totalDrawdown >= TotalLossLimit)
	{
	LogInfo($"Drawdown protection activated. Daily {dailyDrawdown:F2}% Total {totalDrawdown:F2}%.");
	return false;
	}

	var dailyProfit = _dailyEquityAtStart > 0m ? (equity - _dailyEquityAtStart) / _dailyEquityAtStart * 100m : 0m;
	if (dailyProfit < DailyProfitTarget && _lastPenaltyDay != _lastTradeDay)
	{
	ApplyPenalty();
	_lastPenaltyDay = _lastTradeDay;
	}

	return true;
	}

	private void ApplyPenalty()
	{
	var updated = _learningRate * 0.9m;
	if (updated < MinimumLearningRate)
	updated = MinimumLearningRate;

	if (updated < _learningRate)
	{
	_learningRate = updated;
	LogInfo($"Penalty applied. Learning rate reduced to {_learningRate:F6}.");
	}
	}

	private decimal GetSpreadPoints()
	{
	if (!_hasBestBid || !_hasBestAsk)
	return 0m;

	var priceStep = Security?.PriceStep ?? 0m;
	if (priceStep <= 0m)
	return 0m;

	var spread = _bestAskPrice - _bestBidPrice;
	if (spread <= 0m)
	return 0m;

	return spread / priceStep;
	}

	private bool EnsureNetworkInitialized()
	{
	var hiddenLength = Math.Max(1, HiddenLayerSize);
	if (_weightsInputHidden.Length == InputSize * hiddenLength)
	return true;

	InitializeNetwork();
	return _weightsInputHidden.Length == InputSize * hiddenLength;
	}

	private void InitializeNetwork()
	{
	var hiddenLength = Math.Max(1, HiddenLayerSize);
	_weightsInputHidden = new decimal[InputSize * hiddenLength];
	_biasHidden = new decimal[hiddenLength];
	_weightsHiddenOutput = new decimal[hiddenLength];
	_hiddenOutputs = new decimal[hiddenLength];

	var random = new Random(42);

	for (var i = 0; i < _weightsInputHidden.Length; i++)
	{
	_weightsInputHidden[i] = (decimal)(random.NextDouble() * 0.1 - 0.05);
	}

	for (var j = 0; j < hiddenLength; j++)
	{
	_biasHidden[j] = (decimal)(random.NextDouble() * 0.1 - 0.05);
	_weightsHiddenOutput[j] = (decimal)(random.NextDouble() * 0.1 - 0.05);
	}

	_biasOutput = (decimal)(random.NextDouble() * 0.1 - 0.05);
	}
}
