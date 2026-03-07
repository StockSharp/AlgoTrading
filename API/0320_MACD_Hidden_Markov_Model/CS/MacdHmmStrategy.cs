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
	private readonly StrategyParam<int> _signalCooldownBars;

	private MovingAverageConvergenceDivergenceSignal _macd;

	// Hidden Markov Model states
	private enum MarketStates
	{
		Bullish,
		Neutral,
		Bearish
	}

	private MarketStates _currentState = MarketStates.Neutral;

	// Data for HMM calculations
	private readonly List<decimal> _priceChanges = [];
	private readonly List<decimal> _volumes = [];
	private decimal _prevPrice;
	private decimal? _prevMacd;
	private decimal? _prevSignal;
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

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");

		_hmmHistoryLength = Param(nameof(HmmHistoryLength), 100)
		.SetDisplay("HMM History Length", "Length of history for Hidden Markov Model", "HMM Parameters")
		
		.SetOptimize(50, 200, 10);

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
		_cooldownRemaining = 0;
		_priceChanges.Clear();
		_volumes.Clear();

		_macd?.Reset();
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
		// Create subscription and bind indicator
		var subscription = SubscribeCandles(CandleType);

		subscription
		.BindEx(_macd, ProcessCandle)
		.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}

		// Setup position protection
		StartProtection(
		new Unit(2, UnitTypes.Percent), 
		new Unit(2, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
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

		var crossUp = previousMacd <= previousSignal && macd > signal;
		var crossDown = previousMacd >= previousSignal && macd < signal;
		var longExit = Position > 0 && (_currentState == MarketStates.Bearish || crossDown);
		var shortExit = Position < 0 && (_currentState == MarketStates.Bullish || crossUp);

		// Generate trade signals based on MACD transitions and HMM state.
		if (longExit)
		{
			SellMarket(Position);
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (shortExit)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (_cooldownRemaining == 0 && crossUp && _currentState == MarketStates.Bullish && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (_cooldownRemaining == 0 && crossDown && _currentState == MarketStates.Bearish && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
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
			decimal priceChange = candle.ClosePrice - _prevPrice;
			_priceChanges.Add(priceChange);
			_volumes.Add(candle.TotalVolume);

			// Maintain the desired history length
			while (_priceChanges.Count > HmmHistoryLength)
			{
				_priceChanges.RemoveAt(0);
				_volumes.RemoveAt(0);
			}
		}

		_prevPrice = candle.ClosePrice;
	}

	private void CalculateMarketState()
	{
		// Only perform state calculation when we have enough data
		if (_priceChanges.Count < 10)
		return;

		// Simple HMM approximation using recent price changes and volume patterns
		// Note: This is a simplified implementation - a real HMM would use proper state transition probabilities

		// Calculate statistics of recent price changes
		var priceChanges = _priceChanges.ToArray();
		var volumes = _volumes.ToArray();
		var startIndex = Math.Max(0, priceChanges.Length - 10);
		var positiveChanges = 0;
		var negativeChanges = 0;

		for (var i = startIndex; i < priceChanges.Length; i++)
		{
			if (priceChanges[i] > 0)
				positiveChanges++;
			else if (priceChanges[i] < 0)
				negativeChanges++;
		}

		// Calculate average volume for up and down days
		decimal upVolume = 0;
		decimal downVolume = 0;
		int upCount = 0;
		int downCount = 0;

		for (var i = startIndex; i < priceChanges.Length; i++)
		{
			if (priceChanges[i] > 0)
			{
				upVolume += volumes[i];
				upCount++;
			}
			else if (priceChanges[i] < 0)
			{
				downVolume += volumes[i];
				downCount++;
			}
		}

		upVolume = upCount > 0 ? upVolume / upCount : 0;
		downVolume = downCount > 0 ? downVolume / downCount : 0;

		// Determine market state based on price change direction and volume
		if (positiveChanges >= 7 || (positiveChanges >= 6 && upVolume > downVolume * 1.5m))
		{
			_currentState = MarketStates.Bullish;
		}
		else if (negativeChanges >= 7 || (negativeChanges >= 6 && downVolume > upVolume * 1.5m))
		{
			_currentState = MarketStates.Bearish;
		}
		else
		{
			_currentState = MarketStates.Neutral;
		}
	}
}
