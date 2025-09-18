using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining Three White Soldiers / Three Black Crows patterns with Stochastic confirmation.
/// The logic waits for a strong three-candle reversal formation and requires the Stochastic signal line
/// to confirm oversold or overbought conditions before entering positions.
/// </summary>
public class ThreeSoldiersStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<int> _stochSlowing;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _exitLowerLevel;
	private readonly StrategyParam<decimal> _exitUpperLevel;

	private ICandleMessage _firstCandle;
	private ICandleMessage _secondCandle;
	private ICandleMessage _currentCandle;

	private decimal? _previousSignal;
	private decimal? _previousPreviousSignal;

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int StochKPeriod
	{
		get => _stochKPeriod.Value;
		set => _stochKPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D smoothing period.
	/// </summary>
	public int StochDPeriod
	{
		get => _stochDPeriod.Value;
		set => _stochDPeriod.Value = value;
	}

	/// <summary>
	/// Additional slowing factor applied to %K.
	/// </summary>
	public int StochSlowing
	{
		get => _stochSlowing.Value;
		set => _stochSlowing.Value = value;
	}

	/// <summary>
	/// Oversold level used for long entries.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Overbought level used for short entries.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold for detecting signal-line crossovers when closing long positions.
	/// </summary>
	public decimal ExitLowerLevel
	{
		get => _exitLowerLevel.Value;
		set => _exitLowerLevel.Value = value;
	}

	/// <summary>
	/// Upper threshold for detecting signal-line crossovers when closing short positions.
	/// </summary>
	public decimal ExitUpperLevel
	{
		get => _exitUpperLevel.Value;
		set => _exitUpperLevel.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ThreeSoldiersStochasticStrategy"/> class.
	/// </summary>
	public ThreeSoldiersStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for detecting the pattern", "General");

		_stochKPeriod = Param(nameof(StochKPeriod), 47)
			.SetDisplay("%K Period", "Lookback period for %K line", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(10, 80, 5);

		_stochDPeriod = Param(nameof(StochDPeriod), 9)
			.SetDisplay("%D Period", "Smoothing period for %K line", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_stochSlowing = Param(nameof(StochSlowing), 13)
			.SetDisplay("Slowing", "Additional slowing applied to %K", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_oversoldLevel = Param(nameof(OversoldLevel), 30m)
			.SetDisplay("Oversold Level", "Stochastic signal level for bullish confirmation", "Thresholds")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 5m);

		_overboughtLevel = Param(nameof(OverboughtLevel), 70m)
			.SetDisplay("Overbought Level", "Stochastic signal level for bearish confirmation", "Thresholds")
			.SetCanOptimize(true)
			.SetOptimize(60m, 90m, 5m);

		_exitLowerLevel = Param(nameof(ExitLowerLevel), 20m)
			.SetDisplay("Exit Lower Level", "Lower crossover level for closing long positions", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 5m);

		_exitUpperLevel = Param(nameof(ExitUpperLevel), 80m)
			.SetDisplay("Exit Upper Level", "Upper crossover level for closing short positions", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(60m, 90m, 5m);
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

		_firstCandle = null;
		_secondCandle = null;
		_currentCandle = null;
		_previousSignal = null;
		_previousPreviousSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Configure the Stochastic oscillator with parameters matching the MetaTrader template.
		var stochastic = new StochasticOscillator
		{
			KPeriod = StochKPeriod,
			DPeriod = StochDPeriod,
			Smooth = StochSlowing
		};

		// Subscribe to candle flow and bind the indicator.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		// Enable default position protection (no stops are set but risk control stays active).
		StartProtection();

		// Setup visualization when running inside the Strategy Designer.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue)
	{
		// Only work with finished candles to avoid intrabar noise.
		if (candle.State != CandleStates.Finished)
			return;

		// Indicator can return interim values; wait until the calculation is complete.
		if (!stochasticValue.IsFinal)
			return;

		var stochTyped = (StochasticOscillatorValue)stochasticValue;
		if (stochTyped.D is not decimal currentSignal)
			return;

		// Shift candle references to track the most recent three bars.
		_firstCandle = _secondCandle;
		_secondCandle = _currentCandle;
		_currentCandle = candle;

		var previousSignal = _previousSignal;
		var previousPreviousSignal = _previousPreviousSignal;

		// Update stored signal values for the next iteration.
		_previousPreviousSignal = _previousSignal;
		_previousSignal = currentSignal;

		// Ensure trading is allowed and we have enough historical context.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_firstCandle == null || _secondCandle == null)
			return;

		if (previousSignal is not decimal signalLine || previousPreviousSignal is not decimal olderSignalLine)
			return;

		// Detect Three White Soldiers pattern.
		var isWhiteSoldiers =
			_firstCandle.OpenPrice < _firstCandle.ClosePrice &&
			_secondCandle.OpenPrice < _secondCandle.ClosePrice &&
			_currentCandle.OpenPrice < _currentCandle.ClosePrice &&
			_secondCandle.ClosePrice > _firstCandle.ClosePrice &&
			_currentCandle.ClosePrice > _secondCandle.ClosePrice;

		// Detect Three Black Crows pattern.
		var isBlackCrows =
			_firstCandle.OpenPrice > _firstCandle.ClosePrice &&
			_secondCandle.OpenPrice > _secondCandle.ClosePrice &&
			_currentCandle.OpenPrice > _currentCandle.ClosePrice &&
			_secondCandle.ClosePrice < _firstCandle.ClosePrice &&
			_currentCandle.ClosePrice < _secondCandle.ClosePrice;

		// Confirmation from the Stochastic signal line for bullish entries.
		if (isWhiteSoldiers && signalLine < OversoldLevel && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			return;
		}

		// Confirmation from the Stochastic signal line for bearish entries.
		if (isBlackCrows && signalLine > OverboughtLevel && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			return;
		}

		// Long exit when the signal line crosses above defined thresholds.
		var exitLong =
			(signalLine > ExitLowerLevel && olderSignalLine < ExitLowerLevel) ||
			(signalLine > ExitUpperLevel && olderSignalLine < ExitUpperLevel);

		if (Position > 0 && exitLong)
		{
			SellMarket(Position);
			return;
		}

		// Short exit when the signal line crosses back below the thresholds.
		var exitShort =
			(signalLine < ExitUpperLevel && olderSignalLine > ExitUpperLevel) ||
			(signalLine < ExitLowerLevel && olderSignalLine > ExitLowerLevel);

		if (Position < 0 && exitShort)
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}
