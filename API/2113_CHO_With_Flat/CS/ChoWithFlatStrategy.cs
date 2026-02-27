using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Chaikin-style oscillator strategy with flat filter.
/// Uses difference between fast and slow EMA as oscillator, SMA as signal line,
/// and Bollinger Bands to detect flat market.
/// Trades on crossover of oscillator and signal when market is not flat.
/// </summary>
public class ChoWithFlatStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _stdDeviation;
	private readonly StrategyParam<decimal> _flatThreshold;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private SimpleMovingAverage _signalSma;
	private decimal _prevOsc;
	private decimal _prevSignal;
	private bool _isInitialized;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	public decimal StdDeviation
	{
		get => _stdDeviation.Value;
		set => _stdDeviation.Value = value;
	}

	public decimal FlatThreshold
	{
		get => _flatThreshold.Value;
		set => _flatThreshold.Value = value;
	}

	public ChoWithFlatStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");

		_fastPeriod = Param(nameof(FastPeriod), 3)
			.SetDisplay("Fast Period", "Fast EMA period", "Indicator");

		_slowPeriod = Param(nameof(SlowPeriod), 10)
			.SetDisplay("Slow Period", "Slow EMA period", "Indicator");

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetDisplay("Signal Period", "Signal line SMA period", "Indicator");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Flat Filter");

		_stdDeviation = Param(nameof(StdDeviation), 2.0m)
			.SetDisplay("Std Deviation", "Deviation for Bollinger Bands", "Flat Filter");

		_flatThreshold = Param(nameof(FlatThreshold), 0.005m)
			.SetDisplay("Flat Threshold", "Minimum band width ratio to detect trending", "Flat Filter");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		_slowEma = new ExponentialMovingAverage { Length = SlowPeriod };
		_signalSma = new SimpleMovingAverage { Length = SignalPeriod };
		_prevOsc = 0;
		_prevSignal = 0;
		_isInitialized = false;

		var bollinger = new BollingerBands { Length = BollingerPeriod, Width = StdDeviation };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bb = (BollingerBandsValue)bbValue;
		if (bb.UpBand is not decimal upperBand ||
			bb.LowBand is not decimal lowerBand ||
			bb.MovingAverage is not decimal middleBand)
			return;

		// Calculate oscillator manually: fast EMA - slow EMA
		var fastResult = _fastEma.Process(new DecimalIndicatorValue(_fastEma, candle.ClosePrice, candle.OpenTime) { IsFinal = true });
		var slowResult = _slowEma.Process(new DecimalIndicatorValue(_slowEma, candle.ClosePrice, candle.OpenTime) { IsFinal = true });

		if (!fastResult.IsFormed || !slowResult.IsFormed)
			return;

		var oscValue = fastResult.ToDecimal() - slowResult.ToDecimal();

		// Process signal line
		var sigResult = _signalSma.Process(new DecimalIndicatorValue(_signalSma, oscValue, candle.OpenTime) { IsFinal = true });
		if (!sigResult.IsFormed)
			return;

		var signalValue = sigResult.ToDecimal();

		if (!_isInitialized)
		{
			_prevOsc = oscValue;
			_prevSignal = signalValue;
			_isInitialized = true;
			return;
		}

		// Flat filter: check if Bollinger Bands are wide enough
		var bandWidth = upperBand - lowerBand;
		if (middleBand != 0 && (bandWidth / middleBand) < FlatThreshold)
		{
			_prevOsc = oscValue;
			_prevSignal = signalValue;
			return;
		}

		// Crossover detection
		var wasAbove = _prevOsc > _prevSignal;
		var isAbove = oscValue > signalValue;

		if (!wasAbove && isAbove)
		{
			// Oscillator crossed above signal - buy
			if (Position <= 0)
				BuyMarket();
		}
		else if (wasAbove && !isAbove)
		{
			// Oscillator crossed below signal - sell
			if (Position >= 0)
				SellMarket();
		}

		_prevOsc = oscValue;
		_prevSignal = signalValue;
	}
}
