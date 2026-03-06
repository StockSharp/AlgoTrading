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
/// Supertrend + Stochastic strategy.
/// Strategy enters trades when Supertrend indicates trend direction and Stochastic confirms with oversold/overbought conditions.
/// </summary>
public class SupertrendStochasticStrategy : Strategy
{
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<int> _stochK;
	private readonly StrategyParam<int> _stochD;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private int _cooldown;
	private bool _hasPrevTrend;
	private bool _prevBullish;

	// Indicators
	private SuperTrend _supertrend;
	private StochasticOscillator _stochastic;

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
	/// Stochastic period.
	/// </summary>
	public int StochPeriod
	{
		get => _stochPeriod.Value;
		set => _stochPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int StochK
	{
		get => _stochK.Value;
		set => _stochK.Value = value;
	}

	/// <summary>
	/// Stochastic %D period.
	/// </summary>
	public int StochD
	{
		get => _stochD.Value;
		set => _stochD.Value = value;
	}

	/// <summary>
	/// Bars to wait between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public SupertrendStochasticStrategy()
	{
		_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend Period", "Supertrend ATR period length", "Supertrend")
			
			.SetOptimize(5, 20, 1);

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3.0m)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend Multiplier", "Supertrend ATR multiplier", "Supertrend")
			
			.SetOptimize(1.0m, 5.0m, 0.5m);

		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Stochastic oscillator period", "Stochastic")
			
			.SetOptimize(5, 30, 5);

		_stochK = Param(nameof(StochK), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "Stochastic %K period", "Stochastic")
			
			.SetOptimize(1, 10, 1);

		_stochD = Param(nameof(StochD), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "Stochastic %D period", "Stochastic")
			
			.SetOptimize(1, 10, 1);

		_cooldownBars = Param(nameof(CooldownBars), 8)
			.SetRange(1, 50)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
			
			.SetOptimize(0.5m, 2.0m, 0.5m);
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
		_supertrend = null;
		_stochastic = null;
		_cooldown = 0;
		_hasPrevTrend = false;
		_prevBullish = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create indicators
		_supertrend = new()
		{
			Length = SupertrendPeriod,
			Multiplier = SupertrendMultiplier
		};

		_stochastic = new()
		{
			K = { Length = StochK },
			D = { Length = StochD },
		};

		// Subscribe to candles and bind indicators
		var subscription = SubscribeCandles(CandleType);
		
		subscription
			.BindEx(_supertrend, _stochastic, ProcessCandle)
			.Start();

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _supertrend);
			
			var secondArea = CreateChartArea();
			if (secondArea != null)
			{
				DrawIndicator(secondArea, _stochastic);
			}
			
			DrawOwnTrades(area);
		}

	}

	private void ProcessCandle(
		ICandleMessage candle, 
		IIndicatorValue supertrendValue, 
		IIndicatorValue stochasticValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Get indicator values
		var supertrend = (SuperTrendIndicatorValue)supertrendValue;
		decimal supertrendLine = supertrend.Value;
		
		// Is trend bullish or bearish
		bool isBullish = supertrend.IsUpTrend;
		bool isBearish = !isBullish;
		
		var stochTyped = (StochasticOscillatorValue)stochasticValue;

		if (stochTyped.K is not decimal stochK)
			return;

		if (!_hasPrevTrend)
		{
			_hasPrevTrend = true;
			_prevBullish = isBullish;
			return;
		}

		bool isAboveSupertrend = candle.ClosePrice > supertrendLine;
		bool isBelowSupertrend = candle.ClosePrice < supertrendLine;
		bool trendFlipUp = !_prevBullish && isBullish;
		bool trendFlipDown = _prevBullish && isBearish;

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevBullish = isBullish;
			return;
		}

		if (trendFlipUp && isAboveSupertrend && stochK < 35 && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (trendFlipDown && isBelowSupertrend && stochK > 65 && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && trendFlipDown)
		{
			SellMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && trendFlipUp)
		{
			BuyMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}

		_prevBullish = isBullish;
	}
}
