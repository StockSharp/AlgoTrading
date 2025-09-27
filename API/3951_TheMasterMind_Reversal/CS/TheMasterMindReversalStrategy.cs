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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic + Williams %R reversal system ported from the MetaTrader expert "TheMasterMind".
/// </summary>
public class TheMasterMindReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stochasticPeriod;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _williamsPeriod;
	private readonly StrategyParam<decimal> _stochasticBuyThreshold;
	private readonly StrategyParam<decimal> _stochasticSellThreshold;
	private readonly StrategyParam<decimal> _williamsBuyLevel;
	private readonly StrategyParam<decimal> _williamsSellLevel;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<DataType> _candleType;

	private StochasticOscillator _stochastic = null!;
	private WilliamsR _williams = null!;

	/// <summary>
	/// Trade volume in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Total period for the stochastic oscillator.
	/// </summary>
	public int StochasticPeriod
	{
		get => _stochasticPeriod.Value;
		set => _stochasticPeriod.Value = value;
	}

	/// <summary>
	/// %K smoothing period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// %D signal period.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Williams %R lookback length.
	/// </summary>
	public int WilliamsPeriod
	{
		get => _williamsPeriod.Value;
		set => _williamsPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic signal threshold for longs.
	/// </summary>
	public decimal StochasticBuyThreshold
	{
		get => _stochasticBuyThreshold.Value;
		set => _stochasticBuyThreshold.Value = value;
	}

	/// <summary>
	/// Stochastic signal threshold for shorts.
	/// </summary>
	public decimal StochasticSellThreshold
	{
		get => _stochasticSellThreshold.Value;
		set => _stochasticSellThreshold.Value = value;
	}

	/// <summary>
	/// Williams %R oversold level.
	/// </summary>
	public decimal WilliamsBuyLevel
	{
		get => _williamsBuyLevel.Value;
		set => _williamsBuyLevel.Value = value;
	}

	/// <summary>
	/// Williams %R overbought level.
	/// </summary>
	public decimal WilliamsSellLevel
	{
		get => _williamsSellLevel.Value;
		set => _williamsSellLevel.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in absolute price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take-profit distance in absolute price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Enables trailing stop management.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in absolute price units.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Trailing step distance in absolute price units.
	/// </summary>
	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
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
/// Initializes a new instance of the <see cref="TheMasterMindReversalStrategy"/> class.
/// </summary>
public TheMasterMindReversalStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Base order size", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_stochasticPeriod = Param(nameof(StochasticPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Length", "Total lookback for stochastic", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(50, 150, 10);

		_kPeriod = Param(nameof(KPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("%K Smoothing", "Stochastic %K smoothing length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("%D Signal", "Stochastic %D signal length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_williamsPeriod = Param(nameof(WilliamsPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Length", "Lookback period for Williams %R", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(50, 150, 10);

		_stochasticBuyThreshold = Param(nameof(StochasticBuyThreshold), 3m)
			.SetDisplay("Stoch Buy Threshold", "%D level required to buy", "Signals");

		_stochasticSellThreshold = Param(nameof(StochasticSellThreshold), 97m)
			.SetDisplay("Stoch Sell Threshold", "%D level required to sell", "Signals");

		_williamsBuyLevel = Param(nameof(WilliamsBuyLevel), -99.5m)
			.SetDisplay("Williams Buy Level", "Williams %R oversold level", "Signals");

		_williamsSellLevel = Param(nameof(WilliamsSellLevel), -0.5m)
			.SetDisplay("Williams Sell Level", "Williams %R overbought level", "Signals");

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetDisplay("Stop Loss", "Protective stop distance", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 0m)
			.SetDisplay("Take Profit", "Target distance", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
			.SetDisplay("Use Trailing", "Enable trailing stop management", "Risk");

		_trailingStop = Param(nameof(TrailingStop), 0m)
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk");

		_trailingStep = Param(nameof(TrailingStep), 0m)
			.SetDisplay("Trailing Step", "Trailing step distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_stochastic = new StochasticOscillator
		{
			Length = StochasticPeriod,
			K = { Length = KPeriod },
			D = { Length = DPeriod }
		};

		_williams = new WilliamsR
		{
			Length = WilliamsPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_stochastic, _williams, ProcessSignals)
			.Start();

		var takeProfit = TakeProfit > 0m ? new Unit(TakeProfit, UnitTypes.Absolute) : null;
		var stopLoss = StopLoss > 0m ? new Unit(StopLoss, UnitTypes.Absolute) : null;
		var trailingStop = UseTrailingStop && TrailingStop > 0m ? new Unit(TrailingStop, UnitTypes.Absolute) : null;
		var trailingStep = UseTrailingStop && TrailingStep > 0m ? new Unit(TrailingStep, UnitTypes.Absolute) : null;

		if (takeProfit != null || stopLoss != null || trailingStop != null)
		{
			StartProtection(
				takeProfit: takeProfit,
				stopLoss: stopLoss,
				trailingStop: trailingStop,
				trailingStopStep: trailingStep);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawIndicator(area, _williams);
			DrawOwnTrades(area);
		}
	}

	private void ProcessSignals(ICandleMessage candle, IIndicatorValue stochasticValue, decimal williamsValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var stochasticTyped = (StochasticOscillatorValue)stochasticValue;

		if (stochasticTyped.D is not decimal signalValue)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var buySignal = signalValue <= StochasticBuyThreshold && williamsValue <= WilliamsBuyLevel;
		var sellSignal = signalValue >= StochasticSellThreshold && williamsValue >= WilliamsSellLevel;

		if (buySignal)
		{
			LogInfo($"Buy setup detected. %D={signalValue:F2}, WilliamsR={williamsValue:F2}");

			if (Position < 0)
			{
				BuyMarket(Math.Abs(Position));
			}

			if (Position <= 0)
			{
				BuyMarket(Volume);
			}

			return;
		}

		if (sellSignal)
		{
			LogInfo($"Sell setup detected. %D={signalValue:F2}, WilliamsR={williamsValue:F2}");

			if (Position > 0)
			{
				SellMarket(Math.Abs(Position));
			}

			if (Position >= 0)
			{
				SellMarket(Volume);
			}
		}
	}
}

