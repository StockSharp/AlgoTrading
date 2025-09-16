using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive multi-layer perceptron strategy converted from the MetaTrader 5 "Perceptron" expert advisor.
/// Combines five discrete indicator signals and tunes their synaptic weights after every completed trade.
/// </summary>
public class PerceptronAdaptiveStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossOffset;
	private readonly StrategyParam<decimal> _takeProfitOffset;
	private readonly StrategyParam<int> _sinMax;
	private readonly StrategyParam<int> _sinMin;
	private readonly StrategyParam<decimal> _sinPlus;
	private readonly StrategyParam<decimal> _sinMinus;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _cciLength;
	private readonly StrategyParam<int> _slopeMaLength;
	private readonly StrategyParam<int> _aoShortLength;
	private readonly StrategyParam<int> _aoLongLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _baseWeights = new decimal[5];
	private readonly Dictionary<int, decimal>[] _indicatorWeights =
	{
		new(),
		new(),
		new(),
		new(),
		new(),
	};

	private readonly int[][] _neuronIndicators =
	{
		new[] { 2, 3, 4, 5 },
		new[] { 1, 3, 4, 5 },
		new[] { 1, 2, 4, 5 },
		new[] { 1, 2, 3, 5 },
		new[] { 1, 2, 3, 4 },
	};

	private readonly int[] _lastIndicatorSignals = new int[5];
	private readonly decimal[] _lastNeuronOutputs = new decimal[5];

	private SimpleMovingAverage _fastMa = null!;
	private SimpleMovingAverage _slowMa = null!;
	private RelativeStrengthIndex _rsi = null!;
	private CommodityChannelIndex _cci = null!;
	private SimpleMovingAverage _slopeMa = null!;
	private AwesomeOscillator _ao = null!;

	private decimal? _prevFastMa;
	private decimal? _prevPrevFastMa;
	private decimal? _prevSlowMa;
	private decimal? _prevRsi;
	private decimal? _prevPrevRsi;
	private decimal? _prevCci;
	private decimal? _prevPrevCci;
	private decimal? _prevSlopeMa;
	private decimal? _prevPrevSlopeMa;
	private decimal? _prevAo;

	private bool _hasLastSignals;
	private int _lastTradeDirection;
	private decimal _entryPrice;
	private decimal _stopLossPrice;
	private decimal _takeProfitPrice;
	private bool _isLongPosition;
	private DateTimeOffset? _entryCandleTime;

	/// <summary>
	/// Stop-loss distance in absolute price units.
	/// </summary>
	public decimal StopLossOffset
	{
		get => _stopLossOffset.Value;
		set => _stopLossOffset.Value = value;
	}

	/// <summary>
	/// Take-profit distance in absolute price units.
	/// </summary>
	public decimal TakeProfitOffset
	{
		get => _takeProfitOffset.Value;
		set => _takeProfitOffset.Value = value;
	}

	/// <summary>
	/// Upper boundary for neuron bias weights.
	/// </summary>
	public int SinMax
	{
		get => _sinMax.Value;
		set => _sinMax.Value = value;
	}

	/// <summary>
	/// Lower boundary for neuron bias weights.
	/// </summary>
	public int SinMin
	{
		get => _sinMin.Value;
		set => _sinMin.Value = value;
	}

	/// <summary>
	/// Increment applied when reinforcing synaptic weights.
	/// </summary>
	public decimal SinPlusStep
	{
		get => _sinPlus.Value;
		set => _sinPlus.Value = value;
	}

	/// <summary>
	/// Decrement applied when penalizing synaptic weights.
	/// </summary>
	public decimal SinMinusStep
	{
		get => _sinMinus.Value;
		set => _sinMinus.Value = value;
	}

	/// <summary>
	/// Length of the fast moving average used in the crossover signal.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Length of the slow moving average used in the crossover signal.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// RSI lookback length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// CCI lookback length.
	/// </summary>
	public int CciLength
	{
		get => _cciLength.Value;
		set => _cciLength.Value = value;
	}

	/// <summary>
	/// Length of the smoothing average used for the trend slope signal.
	/// </summary>
	public int SlopeMaLength
	{
		get => _slopeMaLength.Value;
		set => _slopeMaLength.Value = value;
	}

	/// <summary>
	/// Short period of the Awesome Oscillator.
	/// </summary>
	public int AoShortLength
	{
		get => _aoShortLength.Value;
		set => _aoShortLength.Value = value;
	}

	/// <summary>
	/// Long period of the Awesome Oscillator.
	/// </summary>
	public int AoLongLength
	{
		get => _aoLongLength.Value;
		set => _aoLongLength.Value = value;
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
	/// Initialize <see cref="PerceptronAdaptiveStrategy"/>.
	/// </summary>
	public PerceptronAdaptiveStrategy()
	{
		_stopLossOffset = Param(nameof(StopLossOffset), 0.001m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss Offset", "Stop-loss distance in absolute price units", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.0005m, 0.005m, 0.0005m);

		_takeProfitOffset = Param(nameof(TakeProfitOffset), 0.0004m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit Offset", "Take-profit distance in absolute price units", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.0004m, 0.006m, 0.0004m);

		_sinMax = Param(nameof(SinMax), 5)
			.SetDisplay("Synapse Upper Bound", "Maximum value for neuron bias weights", "Neural Network")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_sinMin = Param(nameof(SinMin), 0)
			.SetDisplay("Synapse Lower Bound", "Minimum value for neuron bias weights", "Neural Network")
			.SetCanOptimize(true)
			.SetOptimize(-5, 0, 1);

		_sinPlus = Param(nameof(SinPlusStep), 0.03m)
			.SetGreaterThanZero()
			.SetDisplay("Positive Adjustment", "Increment applied when trade result is favorable", "Neural Network")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.1m, 0.01m);

		_sinMinus = Param(nameof(SinMinusStep), 0.03m)
			.SetGreaterThanZero()
			.SetDisplay("Negative Adjustment", "Decrement applied when trade result is unfavorable", "Neural Network")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.1m, 0.01m);

		_fastMaLength = Param(nameof(FastMaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Fast simple moving average length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_slowMaLength = Param(nameof(SlowMaLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Length", "Slow simple moving average length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 1);

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Relative Strength Index period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 30, 1);

		_cciLength = Param(nameof(CciLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Length", "Commodity Channel Index period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 40, 1);

		_slopeMaLength = Param(nameof(SlopeMaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Slope MA Length", "Simple moving average used for slope detection", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_aoShortLength = Param(nameof(AoShortLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("AO Short Length", "Short period for the Awesome Oscillator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_aoLongLength = Param(nameof(AoLongLength), 34)
			.SetGreaterThanZero()
			.SetDisplay("AO Long Length", "Long period for the Awesome Oscillator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculations", "General");

		ResetState();
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
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new SimpleMovingAverage { Length = FastMaLength };
		_slowMa = new SimpleMovingAverage { Length = SlowMaLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_cci = new CommodityChannelIndex { Length = CciLength };
		_slopeMa = new SimpleMovingAverage { Length = SlopeMaLength };
		_ao = new AwesomeOscillator
		{
			ShortPeriod = AoShortLength,
			LongPeriod = AoLongLength,
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_fastMa, _slowMa, _rsi, _cci, _slopeMa, _ao, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastMaValue, decimal slowMaValue, decimal rsiValue, decimal cciValue, decimal slopeMaValue, decimal aoValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var maSignal = UpdateMaSignal(fastMaValue, slowMaValue);
		var rsiSignal = UpdateRsiSignal(rsiValue);
		var cciSignal = UpdateCciSignal(cciValue);
		var slopeSignal = UpdateSlopeSignal(slopeMaValue);
		var aoSignal = UpdateAoSignal(aoValue);

		HandlePositionManagement(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
			return;

		var indicatorSignals = new[] { maSignal, rsiSignal, cciSignal, slopeSignal, aoSignal };
		var neuronOutputs = CalculateNeuronOutputs(indicatorSignals);
		var brainReturn = CalculateBrainReturn(neuronOutputs);

		if (brainReturn > 0m && _lastTradeDirection != 2)
		{
			OpenPosition(true, candle.ClosePrice, candle.OpenTime, indicatorSignals, neuronOutputs);
		}
		else if (brainReturn < 0m && _lastTradeDirection != 1)
		{
			OpenPosition(false, candle.ClosePrice, candle.OpenTime, indicatorSignals, neuronOutputs);
		}
	}

	private void OpenPosition(bool isLong, decimal entryPrice, DateTimeOffset candleOpenTime, IReadOnlyList<int> indicatorSignals, IReadOnlyList<decimal> neuronOutputs)
	{
		var volume = Volume;

		if (isLong)
		{
			BuyMarket(volume + (Position < 0 ? -Position : 0m));
			_lastTradeDirection = 2;
		}
		else
		{
			SellMarket(volume + (Position > 0 ? Position : 0m));
			_lastTradeDirection = 1;
		}

		_entryPrice = entryPrice;
		_isLongPosition = isLong;
		_entryCandleTime = candleOpenTime;

		var stopOffset = StopLossOffset;
		var takeOffset = TakeProfitOffset;

		_stopLossPrice = 0m;
		_takeProfitPrice = 0m;

		if (stopOffset > 0m)
		{
			_stopLossPrice = isLong ? entryPrice - stopOffset : entryPrice + stopOffset;
		}

		if (takeOffset > 0m)
		{
			_takeProfitPrice = isLong ? entryPrice + takeOffset : entryPrice - takeOffset;
		}

		_hasLastSignals = true;

		for (var i = 0; i < indicatorSignals.Count; ++i)
			_lastIndicatorSignals[i] = indicatorSignals[i];

		for (var i = 0; i < neuronOutputs.Count; ++i)
			_lastNeuronOutputs[i] = neuronOutputs[i];
	}

	private void HandlePositionManagement(ICandleMessage candle)
	{
		if (Position == 0 || _entryCandleTime is null)
			return;

		if (candle.OpenTime <= _entryCandleTime.Value)
			return;

		var hasExit = false;
		decimal exitPrice = 0m;

		if (_isLongPosition)
		{
			if (_takeProfitPrice > 0m && candle.HighPrice >= _takeProfitPrice)
			{
				exitPrice = _takeProfitPrice;
				hasExit = true;
			}
			else if (_stopLossPrice > 0m && candle.LowPrice <= _stopLossPrice)
			{
				exitPrice = _stopLossPrice;
				hasExit = true;
			}
		}
		else
		{
			if (_takeProfitPrice > 0m && candle.LowPrice <= _takeProfitPrice)
			{
				exitPrice = _takeProfitPrice;
				hasExit = true;
			}
			else if (_stopLossPrice > 0m && candle.HighPrice >= _stopLossPrice)
			{
				exitPrice = _stopLossPrice;
				hasExit = true;
			}
		}

		if (!hasExit)
			return;

		ClosePosition();

		var profit = _isLongPosition ? exitPrice - _entryPrice : _entryPrice - exitPrice;

		if (_hasLastSignals)
		{
			AdjustWeights(_isLongPosition, profit);
		}

		ResetAfterExit();
	}

	private void AdjustWeights(bool wasLongTrade, decimal profit)
	{
		var outcomeSign = Math.Sign(profit);
		if (outcomeSign == 0)
			return;

		var directionSign = wasLongTrade ? -1 : 1;
		var sinPlus = SinPlusStep;
		var sinMinus = SinMinusStep;
		var sinMax = (decimal)SinMax;
		var sinMin = (decimal)SinMin;

		for (var neuronIndex = 0; neuronIndex < _baseWeights.Length; neuronIndex++)
		{
			var lastOutput = _lastNeuronOutputs[neuronIndex];
			var neuronSign = Math.Sign(lastOutput);

			if (neuronSign != 0)
			{
				var product = neuronSign * directionSign;

				if (product > 0)
				{
					if (outcomeSign > 0)
					{
						_baseWeights[neuronIndex] = Math.Min(_baseWeights[neuronIndex] + sinPlus, sinMax);
					}
					else
					{
						_baseWeights[neuronIndex] = Math.Max(_baseWeights[neuronIndex] - sinMinus, sinMin);
					}
				}
				else if (product < 0)
				{
					if (outcomeSign > 0)
					{
						_baseWeights[neuronIndex] = Math.Max(_baseWeights[neuronIndex] - sinMinus, sinMin);
					}
					else
					{
						_baseWeights[neuronIndex] = Math.Min(_baseWeights[neuronIndex] + sinPlus, sinMax);
					}
				}
			}

			var weights = _indicatorWeights[neuronIndex];
			foreach (var indicatorIndex in _neuronIndicators[neuronIndex])
			{
				var indicatorSignal = _lastIndicatorSignals[indicatorIndex - 1];
				if (indicatorSignal == 0)
					continue;

				var product = indicatorSignal * directionSign;

				if (product > 0)
				{
					weights[indicatorIndex] += outcomeSign > 0 ? sinPlus : -sinMinus;
				}
				else if (product < 0)
				{
					weights[indicatorIndex] += outcomeSign > 0 ? -sinMinus : sinPlus;
				}
			}
		}
	}

	private decimal[] CalculateNeuronOutputs(IReadOnlyList<int> indicatorSignals)
	{
		var outputs = new decimal[_baseWeights.Length];

		for (var neuronIndex = 0; neuronIndex < outputs.Length; neuronIndex++)
		{
			var sum = 0m;
			var weights = _indicatorWeights[neuronIndex];

			foreach (var indicatorIndex in _neuronIndicators[neuronIndex])
			{
				var signal = indicatorSignals[indicatorIndex - 1];
				if (signal == 0)
					continue;

				if (!weights.TryGetValue(indicatorIndex, out var weight))
					continue;

				sum += weight * signal;
			}

			outputs[neuronIndex] = sum;
		}

		return outputs;
	}

	private decimal CalculateBrainReturn(IReadOnlyList<decimal> neuronOutputs)
	{
		var total = 0m;
		for (var i = 0; i < neuronOutputs.Count; ++i)
			total += neuronOutputs[i] * _baseWeights[i];
		return total;
	}

	private int UpdateMaSignal(decimal fastMaValue, decimal slowMaValue)
	{
		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
		{
			_prevPrevFastMa = _prevFastMa;
			_prevFastMa = fastMaValue;
			_prevSlowMa = slowMaValue;
			return 0;
		}

		if (_prevFastMa is null || _prevPrevFastMa is null || _prevSlowMa is null)
		{
			_prevPrevFastMa = _prevFastMa;
			_prevFastMa = fastMaValue;
			_prevSlowMa = slowMaValue;
			return 0;
		}

		var previousFast = _prevFastMa.Value;
		var previousFast2 = _prevPrevFastMa.Value;
		var previousSlow = _prevSlowMa.Value;

		var signal = 0;

		if (previousFast2 < previousSlow && previousFast > previousSlow)
			signal = 1;
		else if (previousFast2 > previousSlow && previousFast < previousSlow)
			signal = -1;

		_prevPrevFastMa = _prevFastMa;
		_prevFastMa = fastMaValue;
		_prevSlowMa = slowMaValue;

		return signal;
	}

	private int UpdateRsiSignal(decimal rsiValue)
	{
		if (!_rsi.IsFormed)
		{
			_prevPrevRsi = _prevRsi;
			_prevRsi = rsiValue;
			return 0;
		}

		if (_prevRsi is null || _prevPrevRsi is null)
		{
			_prevPrevRsi = _prevRsi;
			_prevRsi = rsiValue;
			return 0;
		}

		var previous = _prevRsi.Value;
		var previous2 = _prevPrevRsi.Value;

		var signal = 0;

		if (previous2 < 30m && previous > 30m)
			signal = 1;
		else if (previous2 > 70m && previous < 70m)
			signal = -1;

		_prevPrevRsi = _prevRsi;
		_prevRsi = rsiValue;

		return signal;
	}

	private int UpdateCciSignal(decimal cciValue)
	{
		if (!_cci.IsFormed)
		{
			_prevPrevCci = _prevCci;
			_prevCci = cciValue;
			return 0;
		}

		if (_prevCci is null || _prevPrevCci is null)
		{
			_prevPrevCci = _prevCci;
			_prevCci = cciValue;
			return 0;
		}

		var previous = _prevCci.Value;
		var previous2 = _prevPrevCci.Value;

		var signal = 0;

		if (previous2 < -100m && previous > -100m)
			signal = 1;
		else if (previous2 > 100m && previous < 100m)
			signal = -1;

		_prevPrevCci = _prevCci;
		_prevCci = cciValue;

		return signal;
	}

	private int UpdateSlopeSignal(decimal slopeValue)
	{
		if (!_slopeMa.IsFormed)
		{
			_prevPrevSlopeMa = _prevSlopeMa;
			_prevSlopeMa = slopeValue;
			return 0;
		}

		if (_prevSlopeMa is null || _prevPrevSlopeMa is null)
		{
			_prevPrevSlopeMa = _prevSlopeMa;
			_prevSlopeMa = slopeValue;
			return 0;
		}

		var previous = _prevSlopeMa.Value;
		var previous2 = _prevPrevSlopeMa.Value;

		var signal = 0;

		if (previous > previous2)
			signal = 1;
		else if (previous < previous2)
			signal = -1;

		_prevPrevSlopeMa = _prevSlopeMa;
		_prevSlopeMa = slopeValue;

		return signal;
	}

	private int UpdateAoSignal(decimal aoValue)
	{
		if (!_ao.IsFormed)
		{
			_prevAo = aoValue;
			return 0;
		}

		if (_prevAo is null)
		{
			_prevAo = aoValue;
			return 0;
		}

		var previous = _prevAo.Value;

		var signal = 0;

		if (aoValue > previous)
			signal = 1;
		else if (aoValue < previous)
			signal = -1;

		_prevAo = aoValue;

		return signal;
	}

	private void ResetState()
	{
		for (var i = 0; i < _baseWeights.Length; ++i)
			_baseWeights[i] = 1m;

		for (var i = 0; i < _indicatorWeights.Length; ++i)
		{
			var weights = _indicatorWeights[i];
			weights.Clear();
			foreach (var indicatorIndex in _neuronIndicators[i])
				weights[indicatorIndex] = 1m;
		}

		Array.Clear(_lastIndicatorSignals, 0, _lastIndicatorSignals.Length);
		Array.Clear(_lastNeuronOutputs, 0, _lastNeuronOutputs.Length);

		_prevFastMa = null;
		_prevPrevFastMa = null;
		_prevSlowMa = null;
		_prevRsi = null;
		_prevPrevRsi = null;
		_prevCci = null;
		_prevPrevCci = null;
		_prevSlopeMa = null;
		_prevPrevSlopeMa = null;
		_prevAo = null;

		_hasLastSignals = false;
		_lastTradeDirection = 0;
		_entryPrice = 0m;
		_stopLossPrice = 0m;
		_takeProfitPrice = 0m;
		_isLongPosition = false;
		_entryCandleTime = null;
	}

	private void ResetAfterExit()
	{
		_entryPrice = 0m;
		_stopLossPrice = 0m;
		_takeProfitPrice = 0m;
		_isLongPosition = false;
		_entryCandleTime = null;
		_lastTradeDirection = 0;
		_hasLastSignals = false;

		Array.Clear(_lastIndicatorSignals, 0, _lastIndicatorSignals.Length);
		Array.Clear(_lastNeuronOutputs, 0, _lastNeuronOutputs.Length);
	}
}
