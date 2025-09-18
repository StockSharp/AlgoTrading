using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bruno trend-following strategy converted from the MetaTrader expert "Bruno_v1".
/// It enters long positions when the ADX, Stochastic, MACD, Parabolic SAR, and EMA filters agree on an uptrend.
/// The strategy exits when price drops below the Parabolic SAR level and optionally applies MetaTrader-style stops.
/// </summary>
public class BrunoTrendStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _signalMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<decimal> _stochasticUpperLimit;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaximum;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;

	private SimpleMovingAverage _fastSma = null!;
	private ExponentialMovingAverage _fastEma = null!;
	private ExponentialMovingAverage _slowEma = null!;
	private AverageDirectionalIndex _adx = null!;
	private StochasticOscillator _stochastic = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private ParabolicSar _sar = null!;

	private decimal? _fastSmaValue;
	private decimal? _fastEmaValue;
	private decimal? _slowEmaValue;
	private decimal? _previousSarValue;
	private decimal? _olderSarValue;
	private decimal? _currentSarValue;
	private decimal? _previousCloseValue;
	private decimal? _currentCloseValue;
	private decimal? _plusDiValue;
	private decimal? _minusDiValue;
	private decimal? _stochasticKValue;
	private decimal? _stochasticDValue;
	private decimal? _macdLineValue;
	private decimal? _macdSignalValue;

	/// <summary>
	/// Trading volume in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;
			Volume = value;
		}
	}

	/// <summary>
	/// Candle type used for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Length of the fast simple moving average (SMA).
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Length of the EMA that acts as the fast trend filter.
	/// </summary>
	public int SignalMaLength
	{
		get => _signalMaLength.Value;
		set => _signalMaLength.Value = value;
	}

	/// <summary>
	/// Length of the EMA that acts as the slow trend filter.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Period for the Average Directional Index (ADX).
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Minimum +DI level required for long entries.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// %K period for the stochastic oscillator.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// %D period for the stochastic oscillator.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing value for the stochastic oscillator.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <summary>
	/// Maximum %K value allowed for new long positions.
	/// </summary>
	public decimal StochasticUpperLimit
	{
		get => _stochasticUpperLimit.Value;
		set => _stochasticUpperLimit.Value = value;
	}

	/// <summary>
	/// Fast EMA length used by the MACD.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length used by the MACD.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal line length used by the MACD.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Acceleration step for the Parabolic SAR indicator.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor for the Parabolic SAR indicator.
	/// </summary>
	public decimal SarMaximum
	{
		get => _sarMaximum.Value;
		set => _sarMaximum.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in MetaTrader pips (price steps).
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in MetaTrader pips (price steps).
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BrunoTrendStrategy"/> class.
	/// </summary>
	public BrunoTrendStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetDisplay("Trade Volume", "Order volume measured in lots", "Trading")
		.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for signal calculations", "Data");

		_fastMaLength = Param(nameof(FastMaLength), 4)
		.SetDisplay("Fast SMA", "Length of the fast SMA filter", "Trend")
		.SetGreaterThanZero();

		_signalMaLength = Param(nameof(SignalMaLength), 8)
		.SetDisplay("Signal EMA", "Length of the fast EMA filter", "Trend")
		.SetGreaterThanZero();

		_slowMaLength = Param(nameof(SlowMaLength), 21)
		.SetDisplay("Slow EMA", "Length of the slow EMA filter", "Trend")
		.SetGreaterThanZero();

		_adxPeriod = Param(nameof(AdxPeriod), 13)
		.SetDisplay("ADX Period", "Lookback for the ADX filter", "Trend")
		.SetGreaterThanZero();

		_adxThreshold = Param(nameof(AdxThreshold), 20m)
		.SetDisplay("+DI Threshold", "Minimum +DI value required for longs", "Trend")
		.SetGreaterThan(0m);

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 21)
		.SetDisplay("Stochastic %K", "%K lookback for the stochastic", "Oscillator")
		.SetGreaterThanZero();

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
		.SetDisplay("Stochastic %D", "%D smoothing period", "Oscillator")
		.SetGreaterThanZero();

		_stochasticSlowing = Param(nameof(StochasticSlowing), 3)
		.SetDisplay("Stochastic Slowing", "Smoothing value for the stochastic", "Oscillator")
		.SetGreaterThanZero();

		_stochasticUpperLimit = Param(nameof(StochasticUpperLimit), 80m)
		.SetDisplay("Stochastic Upper Limit", "Maximum %K value for longs", "Oscillator")
		.SetRange(10m, 100m);

		_macdFastLength = Param(nameof(MacdFastLength), 13)
		.SetDisplay("MACD Fast", "Fast EMA length used by MACD", "Oscillator")
		.SetGreaterThanZero();

		_macdSlowLength = Param(nameof(MacdSlowLength), 34)
		.SetDisplay("MACD Slow", "Slow EMA length used by MACD", "Oscillator")
		.SetGreaterThanZero();

		_macdSignalLength = Param(nameof(MacdSignalLength), 8)
		.SetDisplay("MACD Signal", "Signal line length used by MACD", "Oscillator")
		.SetGreaterThanZero();

		_sarStep = Param(nameof(SarStep), 0.055m)
		.SetDisplay("SAR Step", "Acceleration step for the Parabolic SAR", "Trend")
		.SetRange(0.01m, 1m);

		_sarMaximum = Param(nameof(SarMaximum), 0.21m)
		.SetDisplay("SAR Maximum", "Maximum acceleration factor for the Parabolic SAR", "Trend")
		.SetRange(0.1m, 1m);

		_stopLossPips = Param(nameof(StopLossPips), 30)
		.SetDisplay("Stop-Loss (pips)", "Stop-loss distance in MetaTrader pips", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(10, 100, 5);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
		.SetDisplay("Take-Profit (pips)", "Take-profit distance in MetaTrader pips", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(20, 150, 10);

		Volume = _tradeVolume.Value;
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

		_fastSmaValue = null;
	_fastEmaValue = null;
	_slowEmaValue = null;
	_previousSarValue = null;
	_olderSarValue = null;
	_currentSarValue = null;
	_previousCloseValue = null;
	_currentCloseValue = null;
	_plusDiValue = null;
	_minusDiValue = null;
	_stochasticKValue = null;
	_stochasticDValue = null;
	_macdLineValue = null;
	_macdSignalValue = null;
	Volume = _tradeVolume.Value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastSma = new SimpleMovingAverage { Length = FastMaLength };
		_fastEma = new ExponentialMovingAverage { Length = SignalMaLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowMaLength };
		_adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_stochastic = new StochasticOscillator
		{
			Length = Math.Max(1, StochasticSlowing),
			KPeriod = StochasticKPeriod,
			DPeriod = StochasticDPeriod
		};
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength }
			},
			SignalMa = { Length = MacdSignalLength }
		};
		_sar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationMax = SarMaximum
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastSma, _fastEma, _slowEma, _sar, ProcessTrendFilters)
			.BindEx(_adx, _stochastic, _macd, ProcessOscillators)
			.Start();

		var takeProfitUnit = TakeProfitPips > 0 ? new Unit(TakeProfitPips, UnitTypes.PriceStep) : null;
		var stopLossUnit = StopLossPips > 0 ? new Unit(StopLossPips, UnitTypes.PriceStep) : null;

		StartProtection(
			takeProfit: takeProfitUnit,
			stopLoss: stopLossUnit,
			isStopTrailing: false,
			useMarketOrders: true);

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawIndicator(priceArea, _fastSma);
			DrawIndicator(priceArea, _fastEma);
			DrawIndicator(priceArea, _slowEma);
			DrawIndicator(priceArea, _sar);
			DrawOwnTrades(priceArea);

			var oscillatorArea = CreateChartArea();
			if (oscillatorArea != null)
			{
				DrawIndicator(oscillatorArea, _adx);
				DrawIndicator(oscillatorArea, _stochastic);
				DrawIndicator(oscillatorArea, _macd);
			}
		}
	}

	private void ProcessTrendFilters(ICandleMessage candle, decimal fastSma, decimal fastEma, decimal slowEma, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Store latest moving averages to evaluate the trend filters.
		_fastSmaValue = fastSma;
		_fastEmaValue = fastEma;
		_slowEmaValue = slowEma;

		// Track SAR history to replicate the shift-based access used in MetaTrader.
		_olderSarValue = _previousSarValue;
		_previousSarValue = _currentSarValue;
		_currentSarValue = sarValue;

		// Remember the previous close for the exit condition.
		_previousCloseValue = _currentCloseValue;
		_currentCloseValue = candle.ClosePrice;

		TryExecute();
	}

	private void ProcessOscillators(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue stochasticValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!adxValue.IsFinal || !stochasticValue.IsFinal || !macdValue.IsFinal)
		return;

		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		var stochasticTyped = (StochasticOscillatorValue)stochasticValue;
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (adxTyped.Dx.Plus is not decimal plusDi || adxTyped.Dx.Minus is not decimal minusDi)
		return;

		if (stochasticTyped.K is not decimal stochasticK || stochasticTyped.D is not decimal stochasticD)
		return;

		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal macdSignal)
		return;

		// Update oscillator-derived filters once all values are confirmed.
		_plusDiValue = plusDi;
		_minusDiValue = minusDi;
		_stochasticKValue = stochasticK;
		_stochasticDValue = stochasticD;
		_macdLineValue = macdLine;
		_macdSignalValue = macdSignal;

		TryExecute();
	}

	private void TryExecute()
	{
		// Ensure that every indicator delivered enough data before checking signals.
		if (_fastSmaValue is null || _fastEmaValue is null || _slowEmaValue is null)
		return;

		if (_previousSarValue is null || _olderSarValue is null || _currentSarValue is null)
		return;

		if (_plusDiValue is null || _minusDiValue is null)
		return;

		if (_stochasticKValue is null || _stochasticDValue is null)
		return;

		if (_macdLineValue is null || _macdSignalValue is null)
		return;

		if (_previousCloseValue is null)
		return;

		if (!_fastSma.IsFormed || !_fastEma.IsFormed || !_slowEma.IsFormed || !_sar.IsFormed || !_adx.IsFormed || !_stochastic.IsFormed || !_macd.IsFormed)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var emaTrendUp = _fastEmaValue > _slowEmaValue;
		var adxFilter = _plusDiValue > _minusDiValue && _plusDiValue > AdxThreshold;
		var stochasticFilter = _stochasticKValue > _stochasticDValue && _stochasticKValue < StochasticUpperLimit;
		var macdFilter = _macdLineValue > 0m && _macdLineValue > _macdSignalValue;
		var sarIncreasing = _previousSarValue > _olderSarValue;

		var longSignal = emaTrendUp && adxFilter && stochasticFilter && macdFilter && sarIncreasing && Position <= 0;

		if (longSignal)
		{
		// Enter long when every filter aligns with the bullish scenario.
		BuyMarket(Volume + Math.Abs(Position));
		return;
		}

		var exitSignal = Position > 0 && _previousCloseValue < _previousSarValue;

		if (exitSignal)
		{
		// Exit existing longs once price drops below the previous SAR value.
		SellMarket(Position);
		}
	}
}
