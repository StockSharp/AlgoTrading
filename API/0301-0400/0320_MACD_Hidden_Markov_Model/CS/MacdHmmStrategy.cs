using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD strategy with Hidden Markov Model for state detection.
/// </summary>
public class MacdHmmStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _hmmHistoryLength;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrStopMultiplier;
	private readonly StrategyParam<int> _signalCooldownBars;

	private MovingAverageConvergenceDivergenceSignal _macd;
	private AverageTrueRange _atr;

	// Hidden Markov Model states, listed in the order used by the model tables below.
	private enum MarketStates
	{
		Bullish,
		Neutral,
		Bearish
	}

	// Typical move of every state measured in average ranges: the bullish state rises by one
	// average range, the bearish state falls by one and the neutral state goes nowhere.
	private static readonly double[] _stateMeans = [1.0, 0.0, -1.0];

	// Transition matrix of the hidden chain. States are sticky, and a jump from bullish
	// straight to bearish is far less likely than a stop in the neutral state.
	private static readonly double[][] _transitions =
	[
		[0.80, 0.15, 0.05],
		[0.15, 0.70, 0.15],
		[0.05, 0.15, 0.80],
	];

	private MarketStates _currentState = MarketStates.Neutral;

	// Data for HMM calculations
	private readonly List<decimal> _priceChanges = [];
	private decimal _prevPrice;
	private decimal? _prevMacd;
	private decimal? _prevSignal;
	private decimal? _stopPrice;
	private int _cooldownRemaining;

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Candle type to use for the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Length of history for Hidden Markov Model.
	/// </summary>
	public int HmmHistoryLength
	{
		get => _hmmHistoryLength.Value;
		set => _hmmHistoryLength.Value = value;
	}

	/// <summary>
	/// ATR period used to measure the stop distance.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Stop distance expressed in ATR multiples.
	/// </summary>
	public decimal AtrStopMultiplier
	{
		get => _atrStopMultiplier.Value;
		set => _atrStopMultiplier.Value = value;
	}

	/// <summary>
	/// Bars to wait between trading actions.
	/// </summary>
	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MacdHmmStrategy"/>.
	/// </summary>
	public MacdHmmStrategy()
	{
		_macdFast = Param(nameof(MacdFast), 12)
		.SetDisplay("MACD Fast Period", "Fast EMA period for MACD", "Indicators")
		.SetOptimize(8, 20, 2);

		_macdSlow = Param(nameof(MacdSlow), 26)
		.SetDisplay("MACD Slow Period", "Slow EMA period for MACD", "Indicators")
		.SetOptimize(20, 40, 2);

		_macdSignal = Param(nameof(MacdSignal), 9)
		.SetDisplay("MACD Signal Period", "Signal EMA period for MACD", "Indicators")
		.SetOptimize(7, 15, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");

		_hmmHistoryLength = Param(nameof(HmmHistoryLength), 100)
		.SetGreaterThanZero()
		.SetDisplay("HMM History Length", "Number of observations the model is estimated on", "HMM Parameters")
		.SetOptimize(50, 200, 10);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR period used to measure the stop distance", "Protection")
		.SetOptimize(7, 28, 7);

		_atrStopMultiplier = Param(nameof(AtrStopMultiplier), 2m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Stop Multiplier", "Stop distance in ATR multiples", "Protection")
		.SetOptimize(1m, 4m, 0.5m);

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 12)
		.SetGreaterThanZero()
		.SetDisplay("Signal Cooldown", "Bars to wait between position changes", "Trading");
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

		_currentState = MarketStates.Neutral;
		_prevPrice = 0;
		_prevMacd = null;
		_prevSignal = null;
		_stopPrice = null;
		_cooldownRemaining = 0;
		_priceChanges.Clear();

		_macd?.Reset();
		_atr?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create MACD indicator

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};

		// ATR measures the current range and sets how far the protective stop sits from the entry
		_atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);

		subscription
		.BindEx(_macd, _atr, ProcessCandle)
		.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue atrValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
		return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		// Update HMM data
		UpdateHmmData(candle);

		// Determine market state using HMM
		CalculateMarketState();

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		if (macdValue is not IMovingAverageConvergenceDivergenceSignalValue macdTyped ||
			macdTyped.Macd is not decimal macd ||
			macdTyped.Signal is not decimal signal)
			return;

		if (_prevMacd is not decimal previousMacd || _prevSignal is not decimal previousSignal)
		{
			_prevMacd = macd;
			_prevSignal = signal;
			return;
		}

		// Stop distance follows volatility: the wider the average range, the wider the stop.
		var stopDistance = atrValue.ToDecimal() * AtrStopMultiplier;

		var crossUp = previousMacd <= previousSignal && macd > signal;
		var crossDown = previousMacd >= previousSignal && macd < signal;
		var longStop = Position > 0 && _stopPrice is decimal longLevel && candle.LowPrice <= longLevel;
		var shortStop = Position < 0 && _stopPrice is decimal shortLevel && candle.HighPrice >= shortLevel;
		var longExit = Position > 0 && (_currentState == MarketStates.Bearish || crossDown);
		var shortExit = Position < 0 && (_currentState == MarketStates.Bullish || crossUp);

		// Generate trade signals based on MACD transitions and HMM state.
		if (longStop || longExit)
		{
			SellMarket(Position);
			_stopPrice = null;
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (shortStop || shortExit)
		{
			BuyMarket(Math.Abs(Position));
			_stopPrice = null;
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (_cooldownRemaining == 0 && crossUp && _currentState == MarketStates.Bullish && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = candle.ClosePrice - stopDistance;
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (_cooldownRemaining == 0 && crossDown && _currentState == MarketStates.Bearish && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = candle.ClosePrice + stopDistance;
			_cooldownRemaining = SignalCooldownBars;
		}

		_prevMacd = macd;
		_prevSignal = signal;
	}

	private void UpdateHmmData(ICandleMessage candle)
	{
		// Calculate price change
		if (_prevPrice > 0)
		{
			_priceChanges.Add(candle.ClosePrice - _prevPrice);

			// Maintain the desired history length
			while (_priceChanges.Count > HmmHistoryLength)
				_priceChanges.RemoveAt(0);
		}

		_prevPrice = candle.ClosePrice;
	}

	private void CalculateMarketState()
	{
		// The model observes exactly HmmHistoryLength price changes, so it stays neutral
		// until that much history is collected.
		if (_priceChanges.Count < HmmHistoryLength)
			return;

		// The average absolute move of the window scales the observations, so the same
		// emission shapes fit both a quiet and a volatile market.
		var scale = 0m;

		foreach (var change in _priceChanges)
			scale += Math.Abs(change);

		scale /= _priceChanges.Count;

		if (scale <= 0)
			return;

		// Forward pass of the Hidden Markov Model: the belief starts uniform and every
		// observation of the window moves it, so the window length shapes the result.
		var states = _stateMeans.Length;
		var belief = new double[states];
		var updated = new double[states];

		for (var i = 0; i < states; i++)
			belief[i] = 1.0 / states;

		foreach (var change in _priceChanges)
		{
			var observation = (double)(change / scale);
			var total = 0.0;

			for (var next = 0; next < states; next++)
			{
				// Chance of standing in "next" before the observation is taken into account.
				var predicted = 0.0;

				for (var current = 0; current < states; current++)
					predicted += belief[current] * _transitions[current][next];

				// Cauchy-shaped likelihood: the closer the move is to the typical move of the
				// state, the stronger the evidence, and an extreme move never kills a state.
				var distance = observation - _stateMeans[next];

				updated[next] = predicted / (1.0 + distance * distance);
				total += updated[next];
			}

			for (var i = 0; i < states; i++)
				belief[i] = updated[i] / total;
		}

		// The state the filter considers most likely after the last observation.
		var best = 0;

		for (var i = 1; i < states; i++)
		{
			if (belief[i] > belief[best])
				best = i;
		}

		_currentState = (MarketStates)best;
	}
}
