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

using StockSharp.Algo.Candles;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dynamic averaging strategy converted from MetaTrader 5 implementation.
/// The system combines a Stochastic oscillator with a volatility filter based on standard deviation.
/// </summary>
public class DynamicAveragingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<int> _slidingWindowDays;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowPeriod;
	private readonly StrategyParam<int> _stdDevPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;

	private StochasticOscillator _stochastic = null!;
	private StandardDeviation _stdDev = null!;
	private SimpleMovingAverage _stdDevAverage = null!;

	private decimal? _previousK1;
	private decimal? _previousK2;

	private decimal _currentVolume;
	private decimal _lastRealizedPnL;

	/// <summary>
	/// Trade volume for new entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;

			if (State == StrategyStates.Started)
			{
				_currentVolume = value;
			}
		}
	}

	/// <summary>
	/// Minimum floating profit required to flatten the position.
	/// </summary>
	public decimal MinimumProfit
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Sliding window length expressed in calendar days for volatility averaging.
	/// </summary>
	public int SlidingWindowDays
	{
		get => _slidingWindowDays.Value;
		set => _slidingWindowDays.Value = value;
	}

	/// <summary>
	/// Lookback length for the Stochastic oscillator.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing length for %D line of the Stochastic oscillator.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Final slowing parameter for the Stochastic oscillator.
	/// </summary>
	public int StochasticSlowPeriod
	{
		get => _stochasticSlowPeriod.Value;
		set => _stochasticSlowPeriod.Value = value;
	}

	/// <summary>
	/// Lookback length for the standard deviation filter.
	/// </summary>
	public int StdDevPeriod
	{
		get => _stdDevPeriod.Value;
		set => _stdDevPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic oversold level triggering long entries.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Stochastic overbought level triggering short entries.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
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
	/// Initializes a new instance of <see cref="DynamicAveragingStrategy"/>.
	/// </summary>
	public DynamicAveragingStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetDisplay("Trade Volume", "Order volume for new positions", "Trading")
		.SetGreaterThanZero();

		_profitTarget = Param(nameof(MinimumProfit), 15m)
		.SetDisplay("Profit Target", "Floating profit threshold that closes all positions", "Trading")
		.SetGreaterThanZero();

		_slidingWindowDays = Param(nameof(SlidingWindowDays), 30)
		.SetDisplay("Sliding Window (Days)", "Days used to average the volatility", "Indicators")
		.SetGreaterThanZero();

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
		.SetDisplay("Stochastic Length", "Lookback for the %K calculation", "Indicators")
		.SetGreaterThanZero();

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
		.SetDisplay("Stochastic %D", "Smoothing length for the %D line", "Indicators")
		.SetGreaterThanZero();

		_stochasticSlowPeriod = Param(nameof(StochasticSlowPeriod), 3)
		.SetDisplay("Stochastic Slowing", "Final smoothing for %K", "Indicators")
		.SetGreaterThanZero();

		_stdDevPeriod = Param(nameof(StdDevPeriod), 20)
		.SetDisplay("StdDev Length", "Lookback for the standard deviation filter", "Indicators")
		.SetGreaterThanZero();

		_oversoldLevel = Param(nameof(OversoldLevel), 25m)
		.SetDisplay("Oversold Level", "%K threshold for long entries", "Indicators");

		_overboughtLevel = Param(nameof(OverboughtLevel), 75m)
		.SetDisplay("Overbought Level", "%K threshold for short entries", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Source candles for indicator calculations", "Market Data");
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

		_previousK1 = null;
		_previousK2 = null;
		_currentVolume = TradeVolume;
		_lastRealizedPnL = PnLManager?.RealizedPnL ?? 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_currentVolume = TradeVolume;
		_lastRealizedPnL = PnLManager?.RealizedPnL ?? 0m;

		_stochastic = new StochasticOscillator
		{
			Length = StochasticKPeriod,
			K = { Length = StochasticSlowPeriod },
			D = { Length = StochasticDPeriod },
			Slowing = StochasticSlowPeriod,
		};

		_stdDev = new StandardDeviation
		{
			Length = StdDevPeriod,
		};

		_stdDevAverage = new SimpleMovingAverage
		{
			Length = CalculateStdDevAverageLength(),
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
		.BindEx(_stochastic, ProcessIndicators)
		.Start();

		var mainArea = CreateChartArea();
		if (mainArea != null)
		{
			DrawCandles(mainArea, subscription);
			DrawIndicator(mainArea, _stochastic);
			DrawOwnTrades(mainArea);
		}

		var volatilityArea = CreateChartArea("Volatility");
		if (volatilityArea != null)
		{
			DrawIndicator(volatilityArea, _stdDev);
			DrawIndicator(volatilityArea, _stdDevAverage);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position != 0m)
		return;

		var realized = PnLManager?.RealizedPnL ?? _lastRealizedPnL;
		var lastTradeResult = realized - _lastRealizedPnL;

		if (lastTradeResult < 0m)
		{
			// Increase volume after a loss to mimic the original martingale step.
			_currentVolume *= 2m;
		}
		else
		{
			// Reset to the base volume after profitable or breakeven sequences.
			_currentVolume = TradeVolume;
		}

		_lastRealizedPnL = realized;
	}

	private void ProcessIndicators(ICandleMessage candle, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		CheckFloatingProfit(candle);

		var stoch = (StochasticOscillatorValue)stochasticValue;

		if (stoch.K is not decimal currentK)
		return;

		var stdDevValue = _stdDev.Process(candle.ClosePrice, candle.ServerTime, true);
		var stdDevAverageValue = _stdDevAverage.Process(stdDevValue.ToDecimal(), candle.ServerTime, true);

		if (!IsFormedAndOnlineAndAllowTrading() || !_stdDev.IsFormed || !_stdDevAverage.IsFormed)
		{
			UpdateStochasticHistory(currentK);
			return;
		}

		var slope = CalculateSlope();

		if (stdDevValue.ToDecimal() <= stdDevAverageValue.ToDecimal())
		{
			if (_previousK1.HasValue && _previousK2.HasValue)
			{
				if (currentK < OversoldLevel && slope > 0m)
				{
					TryOpenLong();
				}
				else if (currentK > OverboughtLevel && slope < 0m)
				{
					TryOpenShort();
				}
			}
		}

		UpdateStochasticHistory(currentK);
	}

	private void CheckFloatingProfit(ICandleMessage candle)
	{
		if (MinimumProfit <= 0m || Position == 0m)
		return;

		var floatingPnL = CalculateUnrealizedPnL(candle.ClosePrice);

		if (floatingPnL > MinimumProfit)
		{
			// Flatten the position when the floating profit target is reached.
			CloseActivePosition();
		}
	}

	private decimal CalculateUnrealizedPnL(decimal currentPrice)
	{
		if (Position == 0m)
		return 0m;

		var entryPrice = PositionAvgPrice;

		if (entryPrice == 0m)
		return 0m;

		return (currentPrice - entryPrice) * Position;
	}

	private void CloseActivePosition()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			BuyMarket(-Position);
		}
	}

	private void TryOpenLong()
	{
		var volume = Math.Max(_currentVolume, 0m);

		if (volume <= 0m)
		return;

		var orderVolume = volume;

		if (Position < 0m)
		{
			// Add enough volume to close the short position and open a new long.
			orderVolume += Math.Abs(Position);
		}

		BuyMarket(orderVolume);
	}

	private void TryOpenShort()
	{
		var volume = Math.Max(_currentVolume, 0m);

		if (volume <= 0m)
		return;

		var orderVolume = volume;

		if (Position > 0m)
		{
			// Add enough volume to close the long position and open a new short.
			orderVolume += Position;
		}

		SellMarket(orderVolume);
	}

	private void UpdateStochasticHistory(decimal currentK)
	{
		_previousK2 = _previousK1;
		_previousK1 = currentK;
	}

	private decimal CalculateSlope()
	{
		if (!_previousK1.HasValue || !_previousK2.HasValue)
		return 0m;

		return _previousK1.Value - _previousK2.Value;
	}

	private int CalculateStdDevAverageLength()
	{
		var days = SlidingWindowDays;

		if (days <= 0)
		return 1;

		TimeSpan? timeFrame = CandleType.TimeFrame;

		if (timeFrame == null)
		{
			if (CandleType.Arg is TimeSpan span)
			timeFrame = span;
			else if (CandleType.Arg is CandleTimeFrame frame)
			timeFrame = frame.TimeSpan;
		}

		if (timeFrame is { } tf && tf > TimeSpan.Zero)
		{
			double barsPerDay = TimeSpan.FromDays(1).TotalMinutes / tf.TotalMinutes;
			var length = (int)Math.Max(1, Math.Round(days * barsPerDay, MidpointRounding.AwayFromZero));
			return length;
		}

		return Math.Max(1, days);
	}
}

