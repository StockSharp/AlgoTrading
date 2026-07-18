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

		Indicators.Add(_supertrend);
		Indicators.Add(_stochastic);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stResult = _supertrend.Process(candle);
		var stochResult = _stochastic.Process(candle);

		if (!_supertrend.IsFormed || !_stochastic.IsFormed)
			return;

		if (stResult is not SuperTrendIndicatorValue stVal)
			return;

		if (stochResult is not StochasticOscillatorValue stochTyped || stochTyped.K is not decimal stochK)
			return;

		var isBullish = stVal.IsUpTrend;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		if (Position != 0)
			return;

		// Buy: bullish supertrend + stochastic oversold area
		if (isBullish && stochK < 30)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Sell: bearish supertrend + stochastic overbought area
		else if (!isBullish && stochK > 70)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
	}
}
