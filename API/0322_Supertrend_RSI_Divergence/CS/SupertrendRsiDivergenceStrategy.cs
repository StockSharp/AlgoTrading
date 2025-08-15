using System;
using System.Collections.Generic;

using Ecng.Collections;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that uses Supertrend indicator along with RSI divergence to identify trading opportunities.
/// </summary>
public class SupertrendRsiDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private SuperTrend _supertrend;
	private RelativeStrengthIndex _rsi;

	// Data for divergence detection
	private readonly SynchronizedList<decimal> _prices = [];
	private readonly SynchronizedList<decimal> _rsiValues = [];
	private bool _isLongPosition;
	private bool _isShortPosition;

	// Supertrend state tracking
	private decimal _supertrendValue;
	private TrendDirection _trendDirection = TrendDirection.None;

	/// <summary>
	/// Supertrend period.
	/// </summary>
	public int SupertrendPeriod
	{
		get => _supertrendPeriod.Value;
		set => _supertrendPeriod.Value = value;
	}

	/// <summary>
	/// Supertrend multiplier.
	/// </summary>
	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
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
	/// Initializes a new instance of the <see cref="SupertrendRsiDivergenceStrategy"/>.
	/// </summary>
	public SupertrendRsiDivergenceStrategy()
	{
		_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
		.SetDisplay("Supertrend Period", "Supertrend ATR period", "Supertrend")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3.0m)
		.SetDisplay("Supertrend Multiplier", "Supertrend ATR multiplier", "Supertrend")
		.SetCanOptimize(true)
		.SetOptimize(2.0m, 5.0m, 0.5m);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetDisplay("RSI Period", "RSI period for divergence detection", "RSI")
		.SetCanOptimize(true)
		.SetOptimize(8, 20, 2);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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

		_prices.Clear();
		_rsiValues.Clear();
		_isLongPosition = false;
		_isShortPosition = false;
		_trendDirection = TrendDirection.None;
		_supertrendValue = 0;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create indicators
		_supertrend = new()
		{
			Length = SupertrendPeriod,
			Multiplier = SupertrendMultiplier
		};

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(
		_supertrend,
		_rsi,
		ProcessCandle)
		.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _supertrend);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal supertrendValue, decimal rsiValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
		return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		// Extract values from indicators
		_supertrendValue = supertrendValue;
		decimal rsi = rsiValue;

		// Store values for divergence calculation
		_prices.Add(candle.ClosePrice);
		_rsiValues.Add(rsi);

		// Keep reasonable history
		while (_prices.Count > 50)
		{
			_prices.RemoveAt(0);
			_rsiValues.RemoveAt(0);
		}

		// Determine Supertrend trend direction
		TrendDirection previousDirection = _trendDirection;

		if (candle.ClosePrice > _supertrendValue)
		_trendDirection = TrendDirection.Up;
		else if (candle.ClosePrice < _supertrendValue)
		_trendDirection = TrendDirection.Down;

		// Check for trend direction change
		bool trendDirectionChanged = previousDirection != TrendDirection.None && previousDirection != _trendDirection;

		// Check for divergence
		bool bullishDivergence = CheckBullishDivergence();
		bool bearishDivergence = CheckBearishDivergence();

		// Trading logic
		if (candle.ClosePrice > _supertrendValue && bullishDivergence && Position <= 0)
		{
			// Bullish setup - price above Supertrend with bullish divergence
			BuyMarket(Volume);
			LogInfo($"Buy Signal: Price {candle.ClosePrice:F2} > Supertrend {_supertrendValue:F2} with bullish RSI divergence");
			_isLongPosition = true;
			_isShortPosition = false;
		}
		else if (candle.ClosePrice < _supertrendValue && bearishDivergence && Position >= 0)
		{
			// Bearish setup - price below Supertrend with bearish divergence
			SellMarket(Volume + Math.Abs(Position));
			LogInfo($"Sell Signal: Price {candle.ClosePrice:F2} < Supertrend {_supertrendValue:F2} with bearish RSI divergence");
			_isLongPosition = false;
			_isShortPosition = true;
		}
		else if (_isLongPosition && candle.ClosePrice < _supertrendValue)
		{
			// Exit long position when price falls below Supertrend
			SellMarket(Position);
			LogInfo($"Exit Long: Price {candle.ClosePrice:F2} fell below Supertrend {_supertrendValue:F2}");
			_isLongPosition = false;
		}
		else if (_isShortPosition && candle.ClosePrice > _supertrendValue)
		{
			// Exit short position when price rises above Supertrend
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exit Short: Price {candle.ClosePrice:F2} rose above Supertrend {_supertrendValue:F2}");
			_isShortPosition = false;
		}
	}

	private bool CheckBullishDivergence()
	{
		// Need at least a few candles for divergence check
		if (_prices.Count < 5 || _rsiValues.Count < 5)
		return false;

		// Check for bullish divergence: price making lower lows while RSI making higher lows
		// Look at the last 5 candles for a simple check
		decimal currentPrice = _prices[_prices.Count - 1];
		decimal previousPrice = _prices[_prices.Count - 2];

		decimal currentRsi = _rsiValues[_rsiValues.Count - 1];
		decimal previousRsi = _rsiValues[_rsiValues.Count - 2];

		// Bullish divergence: price lower but RSI higher
		bool divergence = currentPrice < previousPrice && currentRsi > previousRsi;

		if (divergence)
		{
			LogInfo($"Bullish Divergence Detected: Price {previousPrice:F2}->{currentPrice:F2}, RSI {previousRsi:F2}->{currentRsi:F2}");
		}

		return divergence;
	}

	private bool CheckBearishDivergence()
	{
		// Need at least a few candles for divergence check
		if (_prices.Count < 5 || _rsiValues.Count < 5)
		return false;

		// Check for bearish divergence: price making higher highs while RSI making lower highs
		// Look at the last 5 candles for a simple check
		decimal currentPrice = _prices[_prices.Count - 1];
		decimal previousPrice = _prices[_prices.Count - 2];

		decimal currentRsi = _rsiValues[_rsiValues.Count - 1];
		decimal previousRsi = _rsiValues[_rsiValues.Count - 2];

		// Bearish divergence: price higher but RSI lower
		bool divergence = currentPrice > previousPrice && currentRsi < previousRsi;

		if (divergence)
		{
			LogInfo($"Bearish Divergence Detected: Price {previousPrice:F2}->{currentPrice:F2}, RSI {previousRsi:F2}->{currentRsi:F2}");
		}

		return divergence;
	}

	// Trend direction enum for tracking Supertrend state
	private enum TrendDirection
	{
		None,
		Up,
		Down
	}
}
