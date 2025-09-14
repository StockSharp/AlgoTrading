using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// NRatio Sign strategy based on NRTR oscillator.
/// Generates buy or sell signals when the normalized ratio crosses thresholds.
/// </summary>
public class NRatioSignStrategy : Strategy
{
	// strategy parameters
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _kf;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _fast;
	private readonly StrategyParam<decimal> _sharp;
	private readonly StrategyParam<decimal> _upLevel;
	private readonly StrategyParam<decimal> _downLevel;
	private readonly StrategyParam<StrategyMode> _mode;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	// internal state variables
	private decimal _nrtr;
	private decimal _nratioPrev;
	private int _trend;
	private bool _isInitialized;
	private ExponentialMovingAverage _ema;

	/// <summary>
	/// NRatio calculation mode.
	/// </summary>
	public enum StrategyMode
	{
		ModeIn,
		ModeOut,
	}

	/// <summary>
	/// Candle series type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// NRTR coefficient.
	/// </summary>
	public decimal Kf
	{
		get => _kf.Value;
		set => _kf.Value = value;
	}

	/// <summary>
	/// Smoothing length for EMA.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Fast parameter affecting NRTR dynamics.
	/// </summary>
	public decimal Fast
	{
		get => _fast.Value;
		set => _fast.Value = value;
	}

	/// <summary>
	/// Exponent applied to the oscillator.
	/// </summary>
	public decimal Sharp
	{
		get => _sharp.Value;
		set => _sharp.Value = value;
	}

	/// <summary>
	/// Upper threshold of NRatio.
	/// </summary>
	public decimal UpLevel
	{
		get => _upLevel.Value;
		set => _upLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold of NRatio.
	/// </summary>
	public decimal DownLevel
	{
		get => _downLevel.Value;
		set => _downLevel.Value = value;
	}

	/// <summary>
	/// Signal generation mode.
	/// </summary>
	public StrategyMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="NRatioSignStrategy"/>.
	/// </summary>
	public NRatioSignStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Time frame for indicator calculation", "General");

		_kf = Param(nameof(Kf), 1m)
		.SetDisplay("Kf", "NRTR coefficient", "Indicator");

		_length = Param(nameof(Length), 3)
		.SetGreaterThanZero()
		.SetDisplay("Length", "EMA smoothing length", "Indicator");

		_fast = Param(nameof(Fast), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Fast", "Fast parameter", "Indicator");

		_sharp = Param(nameof(Sharp), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Sharp", "Exponent for oscillator", "Indicator");

		_upLevel = Param(nameof(UpLevel), 80m)
		.SetDisplay("Up Level", "Upper NRatio threshold", "Indicator");

		_downLevel = Param(nameof(DownLevel), 20m)
		.SetDisplay("Down Level", "Lower NRatio threshold", "Indicator");

		_mode = Param(nameof(Mode), StrategyMode.ModeOut)
		.SetDisplay("Mode", "Signal generation mode", "Indicator");

		_takeProfit = Param(nameof(TakeProfitPercent), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 1m);

		_stopLoss = Param(nameof(StopLossPercent), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 1m);
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

	_isInitialized = false;
	_nrtr = 0m;
	_nratioPrev = 50m;
	_trend = 1;
	_ema = new ExponentialMovingAverage { Length = Length };
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(ProcessCandle)
	.Start();

	StartProtection(
	takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
	stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
	);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
	// Process only completed candles
	if (candle.State != CandleStates.Finished)
	return;

	// Ensure trading is allowed and connection is established
	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	var price = candle.ClosePrice;

	if (!_isInitialized)
	{
	_trend = candle.ClosePrice >= candle.OpenPrice ? 1 : -1;
	_nrtr = _trend > 0 ? price * (1m - Kf / 100m) : price * (1m + Kf / 100m);
	_nratioPrev = 50m;
	_isInitialized = true;
	return;
	}

	var nrtr0 = _nrtr;
	var trend0 = _trend;

	if (_trend >= 0)
	{
	if (price < _nrtr)
	{
	trend0 = -1;
	nrtr0 = price * (1m + Kf / 100m);
	}
	else
	{
	trend0 = 1;
	var lPrice = price * (1m - Kf / 100m);
	nrtr0 = Math.Max(lPrice, _nrtr);
	}
	}
	else
	{
	if (price > _nrtr)
	{
	trend0 = 1;
	nrtr0 = price * (1m - Kf / 100m);
	}
	else
	{
	trend0 = -1;
	var hPrice = price * (1m + Kf / 100m);
	nrtr0 = Math.Min(hPrice, _nrtr);
	}
	}

	var oscil = (100m * Math.Abs(price - nrtr0) / price) / Kf;
	var xOscil = _ema.Process(oscil);

	if (!_ema.IsFormed)
	{
	_nrtr = nrtr0;
	_trend = trend0;
	return;
	}

	var nratio = 100m * (decimal)Math.Pow((double)xOscil, (double)Sharp);

	var buySignal = false;
	var sellSignal = false;

	if (Mode == StrategyMode.ModeIn)
	{
	if (nratio > UpLevel && _nratioPrev <= UpLevel)
	buySignal = true;
	if (nratio < DownLevel && _nratioPrev >= DownLevel)
	sellSignal = true;
	}
	else
	{
	if (nratio < UpLevel && _nratioPrev >= UpLevel)
	sellSignal = true;
	if (nratio > DownLevel && _nratioPrev <= DownLevel)
	buySignal = true;
	}

	_nrtr = nrtr0;
	_trend = trend0;
	_nratioPrev = nratio;

	if (buySignal && Position <= 0)
	BuyMarket(Volume + Math.Abs(Position));
	else if (sellSignal && Position >= 0)
	SellMarket(Volume + Math.Abs(Position));
	}
}