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
/// Detects MACD histogram divergence confirmed by a stochastic oscillator and trades breakouts of new extremes.
/// </summary>
public class DivergenceMacdStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<decimal> _macdDivergenceThreshold;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticSlowK;
	private readonly StrategyParam<int> _stochasticSlowD;
	private readonly StrategyParam<decimal> _stochasticUpperLevel;
	private readonly StrategyParam<decimal> _stochasticLowerLevel;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<decimal> _stopLossSteps;

	private MovingAverageConvergenceDivergenceSignal _macd;
	private StochasticOscillator _stochastic;

	private decimal? _previousHighPrice;
	private decimal? _previousHighHistogram;
	private decimal? _lastHighPrice;
	private decimal? _lastHighHistogram;

	private decimal? _previousLowPrice;
	private decimal? _previousLowHistogram;
	private decimal? _lastLowPrice;
	private decimal? _lastLowHistogram;

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA length of the MACD indicator.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length of the MACD indicator.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal smoothing length of the MACD indicator.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Minimum histogram difference required to confirm divergence.
	/// </summary>
	public decimal MacdDivergenceThreshold
	{
		get => _macdDivergenceThreshold.Value;
		set => _macdDivergenceThreshold.Value = value;
	}

	/// <summary>
	/// Base length for the stochastic oscillator.
	/// </summary>
	public int StochasticLength
	{
		get => _stochasticLength.Value;
		set => _stochasticLength.Value = value;
	}

	/// <summary>
	/// Slow %K smoothing length.
	/// </summary>
	public int StochasticSlowK
	{
		get => _stochasticSlowK.Value;
		set => _stochasticSlowK.Value = value;
	}

	/// <summary>
	/// Slow %D smoothing length.
	/// </summary>
	public int StochasticSlowD
	{
		get => _stochasticSlowD.Value;
		set => _stochasticSlowD.Value = value;
	}

	/// <summary>
	/// Overbought threshold for the stochastic oscillator.
	/// </summary>
	public decimal StochasticUpperLevel
	{
		get => _stochasticUpperLevel.Value;
		set => _stochasticUpperLevel.Value = value;
	}

	/// <summary>
	/// Oversold threshold for the stochastic oscillator.
	/// </summary>
	public decimal StochasticLowerLevel
	{
		get => _stochasticLowerLevel.Value;
		set => _stochasticLowerLevel.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public DivergenceMacdStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe for divergence detection.", "General");

		_macdFastLength = Param(nameof(MacdFastLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA length for MACD histogram.", "Indicators")
		.SetCanOptimize(true);

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA length for MACD histogram.", "Indicators")
		.SetCanOptimize(true);

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA length for MACD histogram.", "Indicators")
		.SetCanOptimize(true);

		_macdDivergenceThreshold = Param(nameof(MacdDivergenceThreshold), 0.0005m)
		.SetGreaterThanZero()
		.SetDisplay("Histogram Threshold", "Minimum histogram difference required for divergence.", "Filters")
		.SetCanOptimize(true);

		_stochasticLength = Param(nameof(StochasticLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic Length", "Primary stochastic length used for %K.", "Indicators")
		.SetCanOptimize(true);

		_stochasticSlowK = Param(nameof(StochasticSlowK), 9)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic SlowK", "Smoothing length applied to %K.", "Indicators")
		.SetCanOptimize(true);

		_stochasticSlowD = Param(nameof(StochasticSlowD), 9)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic SlowD", "Smoothing length applied to %D.", "Indicators")
		.SetCanOptimize(true);

		_stochasticUpperLevel = Param(nameof(StochasticUpperLevel), 80m)
		.SetDisplay("Overbought", "Level considered overbought for divergence confirmation.", "Filters");

		_stochasticLowerLevel = Param(nameof(StochasticLowerLevel), 20m)
		.SetDisplay("Oversold", "Level considered oversold for divergence confirmation.", "Filters");

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 50m)
		.SetDisplay("Take Profit (steps)", "Optional take profit distance in price steps.", "Risk")
		.SetCanOptimize(true);

		_stopLossSteps = Param(nameof(StopLossSteps), 50m)
		.SetDisplay("Stop Loss (steps)", "Optional stop loss distance in price steps.", "Risk")
		.SetCanOptimize(true);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
		yield break;

		if (CandleType != null)
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousHighPrice = null;
		_previousHighHistogram = null;
		_lastHighPrice = null;
		_lastHighHistogram = null;

		_previousLowPrice = null;
		_previousLowHistogram = null;
		_lastLowPrice = null;
		_lastLowHistogram = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortLength = MacdFastLength,
			LongLength = MacdSlowLength,
			SignalLength = MacdSignalLength
		};

		_stochastic = new StochasticOscillator
		{
			Length = StochasticLength,
			K = { Length = StochasticSlowK },
			D = { Length = StochasticSlowD },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_macd, _stochastic, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}

		Unit takeProfitUnit = TakeProfitSteps > 0 ? new Unit(TakeProfitSteps, UnitTypes.Step) : null;
		Unit stopLossUnit = StopLossSteps > 0 ? new Unit(StopLossSteps, UnitTypes.Step) : null;

		StartProtection(takeProfitUnit, stopLossUnit);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!macdValue.IsFinal || !stochasticValue.IsFinal)
		return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
		return;

		var histogram = macdLine - signalLine;

		var stochasticTyped = (StochasticOscillatorValue)stochasticValue;
		if (stochasticTyped.K is not decimal stochK)
		return;

		UpdateSwingExtremes(candle, histogram);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (HasBearishDivergence(stochK) && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			return;
		}

		if (HasBullishDivergence(stochK) && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
	}

	private void UpdateSwingExtremes(ICandleMessage candle, decimal histogram)
	{
		if (_lastHighPrice == null || candle.HighPrice >= _lastHighPrice)
		{
			_previousHighPrice = _lastHighPrice;
			_previousHighHistogram = _lastHighHistogram;
			_lastHighPrice = candle.HighPrice;
			_lastHighHistogram = histogram;
		}

		if (_lastLowPrice == null || candle.LowPrice <= _lastLowPrice)
		{
			_previousLowPrice = _lastLowPrice;
			_previousLowHistogram = _lastLowHistogram;
			_lastLowPrice = candle.LowPrice;
			_lastLowHistogram = histogram;
		}
	}

	private bool HasBearishDivergence(decimal stochK)
	{
		if (_previousHighPrice is not decimal previousHighPrice ||
		_previousHighHistogram is not decimal previousHighHistogram ||
		_lastHighPrice is not decimal lastHighPrice ||
		_lastHighHistogram is not decimal lastHighHistogram)
		return false;

		if (lastHighPrice <= previousHighPrice)
		return false;

		var histogramDelta = previousHighHistogram - lastHighHistogram;
		if (histogramDelta < MacdDivergenceThreshold)
		return false;

		return stochK >= StochasticUpperLevel;
	}

	private bool HasBullishDivergence(decimal stochK)
	{
		if (_previousLowPrice is not decimal previousLowPrice ||
		_previousLowHistogram is not decimal previousLowHistogram ||
		_lastLowPrice is not decimal lastLowPrice ||
		_lastLowHistogram is not decimal lastLowHistogram)
		return false;

		if (lastLowPrice >= previousLowPrice)
		return false;

		var histogramDelta = lastLowHistogram - previousLowHistogram;
		if (histogramDelta < MacdDivergenceThreshold)
		return false;

		return stochK <= StochasticLowerLevel;
	}
}

