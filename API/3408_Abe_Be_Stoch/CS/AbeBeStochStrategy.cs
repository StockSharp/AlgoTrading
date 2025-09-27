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
/// Engulfing candlestick strategy confirmed by the Stochastic oscillator.
/// The logic mirrors the Expert_ABE_BE_Stoch MetaTrader expert: bullish engulfing + oversold stochastic opens longs,
/// bearish engulfing + overbought stochastic opens shorts, and stochastic threshold crosses manage exits.
/// </summary>
public class AbeBeStochStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stochasticPeriodK;
	private readonly StrategyParam<int> _stochasticPeriodD;
	private readonly StrategyParam<int> _stochasticPeriodSlow;
	private readonly StrategyParam<decimal> _entryOversoldLevel;
	private readonly StrategyParam<decimal> _entryOverboughtLevel;
	private readonly StrategyParam<decimal> _exitLowerLevel;
	private readonly StrategyParam<decimal> _exitUpperLevel;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;

	private StochasticOscillator _stochastic;
	private ICandleMessage _previousCandle;
	private decimal? _previousSignal;

	/// <summary>
	/// Candle type used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// %K period of the stochastic oscillator.
	/// </summary>
	public int StochasticPeriodK
	{
		get => _stochasticPeriodK.Value;
		set => _stochasticPeriodK.Value = value;
	}

	/// <summary>
	/// %D period of the stochastic oscillator.
	/// </summary>
	public int StochasticPeriodD
	{
		get => _stochasticPeriodD.Value;
		set => _stochasticPeriodD.Value = value;
	}

	/// <summary>
	/// Slowing period applied to the %K line.
	/// </summary>
	public int StochasticPeriodSlow
	{
		get => _stochasticPeriodSlow.Value;
		set => _stochasticPeriodSlow.Value = value;
	}

	/// <summary>
	/// Oversold level that must be breached together with a bullish engulfing pattern.
	/// </summary>
	public decimal EntryOversoldLevel
	{
		get => _entryOversoldLevel.Value;
		set => _entryOversoldLevel.Value = value;
	}

	/// <summary>
	/// Overbought level that must be breached together with a bearish engulfing pattern.
	/// </summary>
	public decimal EntryOverboughtLevel
	{
		get => _entryOverboughtLevel.Value;
		set => _entryOverboughtLevel.Value = value;
	}

	/// <summary>
	/// Lower stochastic threshold used to manage exits.
	/// </summary>
	public decimal ExitLowerLevel
	{
		get => _exitLowerLevel.Value;
		set => _exitLowerLevel.Value = value;
	}

	/// <summary>
	/// Upper stochastic threshold used to manage exits.
	/// </summary>
	public decimal ExitUpperLevel
	{
		get => _exitUpperLevel.Value;
		set => _exitUpperLevel.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price points (price steps).
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price points (price steps).
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AbeBeStochStrategy"/> class.
	/// </summary>
	public AbeBeStochStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for analysis", "General");

		_stochasticPeriodK = Param(nameof(StochasticPeriodK), 47)
			.SetGreaterThanZero()
			.SetDisplay("%K Period", "Lookback period for stochastic %K", "Indicators")
			.SetCanOptimize(true);

		_stochasticPeriodD = Param(nameof(StochasticPeriodD), 9)
			.SetGreaterThanZero()
			.SetDisplay("%D Period", "Smoothing period for stochastic %D", "Indicators")
			.SetCanOptimize(true);

		_stochasticPeriodSlow = Param(nameof(StochasticPeriodSlow), 13)
			.SetGreaterThanZero()
			.SetDisplay("Slowing", "Additional smoothing applied to %K", "Indicators")
			.SetCanOptimize(true);

		_entryOversoldLevel = Param(nameof(EntryOversoldLevel), 30m)
			.SetDisplay("Oversold Threshold", "Maximum %D value for bullish entries", "Signals")
			.SetCanOptimize(true);

		_entryOverboughtLevel = Param(nameof(EntryOverboughtLevel), 70m)
			.SetDisplay("Overbought Threshold", "Minimum %D value for bearish entries", "Signals")
			.SetCanOptimize(true);

		_exitLowerLevel = Param(nameof(ExitLowerLevel), 20m)
			.SetDisplay("Lower Exit Level", "%D level that closes shorts on upward crosses", "Risk")
			.SetCanOptimize(true);

		_exitUpperLevel = Param(nameof(ExitUpperLevel), 80m)
			.SetDisplay("Upper Exit Level", "%D level that closes longs on downward crosses", "Risk")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take-profit distance in price steps", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop-loss distance in price steps", "Risk");
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
		_stochastic = null;
		_previousCandle = null;
		_previousSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_stochastic = new StochasticOscillator
		{
			Length = StochasticPeriodK,
			K = { Length = StochasticPeriodSlow },
			D = { Length = StochasticPeriodD },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_stochastic, ProcessCandle)
			.Start();

		var priceStep = Security?.PriceStep ?? 0m;
		Unit takeProfitUnit = null;
		Unit stopLossUnit = null;

		if (priceStep > 0m)
		{
			if (TakeProfitPoints > 0m)
			{
				takeProfitUnit = new Unit(TakeProfitPoints * priceStep, UnitTypes.Absolute);
			}

			if (StopLossPoints > 0m)
			{
				stopLossUnit = new Unit(StopLossPoints * priceStep, UnitTypes.Absolute);
			}
		}

		if (takeProfitUnit != null || stopLossUnit != null)
		{
			StartProtection(takeProfitUnit, stopLossUnit);
		}
		else
		{
			StartProtection();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!indicatorValue.IsFinal)
			return;

		var stochasticValue = (StochasticOscillatorValue)indicatorValue;
		if (stochasticValue.D is not decimal currentSignal)
			return;

		var previousSignal = _previousSignal;
		var previous = _previousCandle;

		var bullishEngulfing = IsBullishEngulfing(previous, candle);
		var bearishEngulfing = IsBearishEngulfing(previous, candle);

		if (bullishEngulfing && currentSignal < EntryOversoldLevel && Position <= 0m)
		{
			// Oversold stochastic confirmed by bullish engulfing -> go long or close short.
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (bearishEngulfing && currentSignal > EntryOverboughtLevel && Position >= 0m)
		{
			// Overbought stochastic confirmed by bearish engulfing -> go short or close long.
			SellMarket(Volume + Math.Abs(Position));
		}

		if (previousSignal.HasValue)
		{
			var crossedAboveLower = previousSignal.Value < ExitLowerLevel && currentSignal > ExitLowerLevel;
			var crossedAboveUpper = previousSignal.Value < ExitUpperLevel && currentSignal > ExitUpperLevel;
			if (Position < 0m && (crossedAboveLower || crossedAboveUpper))
			{
				// Stochastic crossed upwards -> exit short exposure.
				BuyMarket(Math.Abs(Position));
			}

			var crossedBelowUpper = previousSignal.Value > ExitUpperLevel && currentSignal < ExitUpperLevel;
			var crossedBelowLower = previousSignal.Value > ExitLowerLevel && currentSignal < ExitLowerLevel;
			if (Position > 0m && (crossedBelowUpper || crossedBelowLower))
			{
				// Stochastic crossed downwards -> exit long exposure.
				SellMarket(Position);
			}
		}

		_previousSignal = currentSignal;
		_previousCandle = candle;
	}

	private static bool IsBullishEngulfing(ICandleMessage previous, ICandleMessage current)
	{
		if (previous == null)
			return false;

		var previousBearish = previous.ClosePrice < previous.OpenPrice;
		var currentBullish = current.ClosePrice > current.OpenPrice;
		if (!previousBearish || !currentBullish)
			return false;

		var bodyEngulfed = current.OpenPrice <= previous.ClosePrice && current.ClosePrice >= previous.OpenPrice;
		return bodyEngulfed;
	}

	private static bool IsBearishEngulfing(ICandleMessage previous, ICandleMessage current)
	{
		if (previous == null)
			return false;

		var previousBullish = previous.ClosePrice > previous.OpenPrice;
		var currentBearish = current.ClosePrice < current.OpenPrice;
		if (!previousBullish || !currentBearish)
			return false;

		var bodyEngulfed = current.OpenPrice >= previous.ClosePrice && current.ClosePrice <= previous.OpenPrice;
		return bodyEngulfed;
	}
}

