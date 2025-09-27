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
/// Multi-filter trend strategy converted from the MQL "The Predator" expert.
/// Combines DMI/ADX structure, moving averages, momentum, Bollinger Bands and stochastic filters.
/// </summary>
public class ThePredatorStrategy : Strategy
{
	private readonly StrategyParam<PredatorModes> _mode;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _dmiPeriod;
	private readonly StrategyParam<int> _adxSmoothing;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _tightBandWidth;
	private readonly StrategyParam<decimal> _wideBandWidth;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticSmooth;
	private readonly StrategyParam<decimal> _stochasticUpper;
	private readonly StrategyParam<decimal> _stochasticLower;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<DataType> _candleType;

	private LinearWeightedMovingAverage _fastMa = null!;
	private LinearWeightedMovingAverage _slowMa = null!;
	private DirectionalIndex _directionalIndex = null!;
	private AverageDirectionalIndex _adx = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergence _macd = null!;
	private BollingerBands _tightBands = null!;
	private BollingerBands _wideBands = null!;
	private StochasticOscillator _stochastic = null!;

	private decimal _prevMomentumDeviation1;
	private decimal _prevMomentumDeviation2;
	private decimal _prevClose;
	private decimal _prevTightLower;
	private decimal _prevTightUpper;
	private bool _hasPrevCandle;

	/// <summary>
	/// Available entry templates taken from the original expert.
	/// </summary>
	public enum PredatorModes
	{
		Strategy1,
		Strategy2
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ThePredatorStrategy()
	{
		_mode = Param(nameof(Mode), PredatorModes.Strategy1)
			.SetDisplay("Mode", "Select entry template", "General");

		_fastMaLength = Param(nameof(FastMaLength), 1)
			.SetGreaterThanZero()
			.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average", "Indicators");

		_slowMaLength = Param(nameof(SlowMaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average", "Indicators");

		_dmiPeriod = Param(nameof(DmiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("DMI Period", "Directional Movement Index length", "Indicators");

		_adxSmoothing = Param(nameof(AdxSmoothing), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Smoothing", "Smoothing factor for the ADX line", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Momentum lookback", "Indicators");

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
			.SetNotNegative()
			.SetDisplay("Momentum Buy Threshold", "Minimum distance from 100 required for long trades", "Filters");

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
			.SetNotNegative()
			.SetDisplay("Momentum Sell Threshold", "Minimum distance from 100 required for short trades", "Filters");

		_adxThreshold = Param(nameof(AdxThreshold), 20m)
			.SetNotNegative()
			.SetDisplay("ADX Threshold", "Trend strength filter", "Filters");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Length used for both Bollinger bands", "Indicators");

		_tightBandWidth = Param(nameof(TightBandWidth), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Tight Band Width", "Standard deviation multiplier for the inner band", "Indicators");

		_wideBandWidth = Param(nameof(WideBandWidth), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Wide Band Width", "Standard deviation multiplier for the outer band", "Indicators");

		_stochasticLength = Param(nameof(StochasticLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Length", "K period of the stochastic oscillator", "Indicators");

		_stochasticSmooth = Param(nameof(StochasticSmooth), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Smooth", "D period of the stochastic oscillator", "Indicators");

		_stochasticUpper = Param(nameof(StochasticUpper), 70m)
			.SetNotNegative()
			.SetDisplay("Stochastic Upper", "Overbought threshold used by Strategy 2", "Filters");

		_stochasticLower = Param(nameof(StochasticLower), 30m)
			.SetNotNegative()
			.SetDisplay("Stochastic Lower", "Oversold threshold used by Strategy 2", "Filters");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 200m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Initial stop loss in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 200m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Initial take profit in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 40m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Trailing distance in pips", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "General");
	}

	/// <summary>
	/// Selected template.
	/// </summary>
	public PredatorModes Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// DMI calculation length.
	/// </summary>
	public int DmiPeriod
	{
		get => _dmiPeriod.Value;
		set => _dmiPeriod.Value = value;
	}

	/// <summary>
	/// ADX smoothing length.
	/// </summary>
	public int AdxSmoothing
	{
		get => _adxSmoothing.Value;
		set => _adxSmoothing.Value = value;
	}

	/// <summary>
	/// Momentum lookback period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum absolute momentum deviation required for long trades.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum absolute momentum deviation required for short trades.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Minimum ADX value to accept trend signals.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Period for both Bollinger bands.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Width multiplier for the inner Bollinger band.
	/// </summary>
	public decimal TightBandWidth
	{
		get => _tightBandWidth.Value;
		set => _tightBandWidth.Value = value;
	}

	/// <summary>
	/// Width multiplier for the outer Bollinger band.
	/// </summary>
	public decimal WideBandWidth
	{
		get => _wideBandWidth.Value;
		set => _wideBandWidth.Value = value;
	}

	/// <summary>
	/// Stochastic oscillator K length.
	/// </summary>
	public int StochasticLength
	{
		get => _stochasticLength.Value;
		set => _stochasticLength.Value = value;
	}

	/// <summary>
	/// Stochastic oscillator smoothing factor.
	/// </summary>
	public int StochasticSmooth
	{
		get => _stochasticSmooth.Value;
		set => _stochasticSmooth.Value = value;
	}

	/// <summary>
	/// Upper threshold for stochastic based filters.
	/// </summary>
	public decimal StochasticUpper
	{
		get => _stochasticUpper.Value;
		set => _stochasticUpper.Value = value;
	}

	/// <summary>
	/// Lower threshold for stochastic based filters.
	/// </summary>
	public decimal StochasticLower
	{
		get => _stochasticLower.Value;
		set => _stochasticLower.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevMomentumDeviation1 = 0m;
		_prevMomentumDeviation2 = 0m;
		_prevClose = 0m;
		_prevTightLower = 0m;
		_prevTightUpper = 0m;
		_hasPrevCandle = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new LinearWeightedMovingAverage { Length = FastMaLength };
		_slowMa = new LinearWeightedMovingAverage { Length = SlowMaLength };
		_directionalIndex = new DirectionalIndex { Length = DmiPeriod };
		_adx = new AverageDirectionalIndex { Length = AdxSmoothing };
		_momentum = new Momentum { Length = MomentumPeriod };
		_macd = new MovingAverageConvergenceDivergence();
		_tightBands = new BollingerBands { Length = BollingerPeriod, Width = TightBandWidth };
		_wideBands = new BollingerBands { Length = BollingerPeriod, Width = WideBandWidth };
		_stochastic = new StochasticOscillator
		{
			Length = StochasticLength,
			Smooth = StochasticSmooth,
			K = { Length = StochasticLength },
			D = { Length = StochasticSmooth }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_fastMa, _slowMa, _directionalIndex, _adx, _momentum, _macd, _wideBands, _tightBands, _stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _wideBands);
			DrawIndicator(area, _tightBands);
			DrawOwnTrades(area);
		}

		var step = Security?.Step ?? 0m;
		StartProtection(
			takeProfit: TakeProfitPips > 0m && step > 0m ? new Unit(TakeProfitPips * step, UnitTypes.Absolute) : null,
			stopLoss: StopLossPips > 0m && step > 0m ? new Unit(StopLossPips * step, UnitTypes.Absolute) : null,
			isStopTrailing: TrailingStopPips > 0m && step > 0m,
			trailingStop: TrailingStopPips > 0m && step > 0m ? new Unit(TrailingStopPips * step, UnitTypes.Absolute) : null
		);
	}

	private void ProcessCandle(ICandleMessage candle,
		IIndicatorValue fastValue,
		IIndicatorValue slowValue,
		IIndicatorValue dmiValue,
		IIndicatorValue adxValue,
		IIndicatorValue momentumValue,
		IIndicatorValue macdValue,
		IIndicatorValue wideValue,
		IIndicatorValue tightValue,
		IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!fastValue.IsFinal || !slowValue.IsFinal || !momentumValue.IsFinal || !macdValue.IsFinal)
			return;

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();

		var dmiTyped = (DirectionalIndexValue)dmiValue;
		if (dmiTyped.Plus is not decimal diPlus || dmiTyped.Minus is not decimal diMinus)
			return;

		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adx)
			return;

		var momentumRaw = momentumValue.ToDecimal();
		var momentumDeviation = Math.Abs(momentumRaw - 100m);

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal macdSignal)
			return;

		var wideBands = (BollingerBandsValue)wideValue;
		if (wideBands.UpBand is not decimal || wideBands.LowBand is not decimal || wideBands.MovingAverage is not decimal)
			return;

	var tightBands = (BollingerBandsValue)tightValue;
	if (tightBands.UpBand is not decimal tightUpper ||
		tightBands.LowBand is not decimal tightLower ||
		tightBands.MovingAverage is not decimal)
		return;

	var stochTyped = (StochasticOscillatorValue)stochasticValue;
	if (stochTyped.K is not decimal stochMain || stochTyped.D is not decimal stochSignal)
		return;

	var longMomentum = momentumDeviation > MomentumBuyThreshold ||
		_prevMomentumDeviation1 > MomentumBuyThreshold ||
		_prevMomentumDeviation2 > MomentumBuyThreshold;

	var shortMomentum = momentumDeviation > MomentumSellThreshold ||
		_prevMomentumDeviation1 > MomentumSellThreshold ||
		_prevMomentumDeviation2 > MomentumSellThreshold;

	var longSignal = false;
	var shortSignal = false;

	switch (Mode)
	{
		case PredatorModes.Strategy1:
		{
			longSignal = adx > AdxThreshold && diPlus > diMinus && fast > slow && longMomentum && macd > macdSignal;
			shortSignal = adx > AdxThreshold && diMinus > diPlus && fast < slow && shortMomentum && macd < macdSignal;
			break;
		}

		case PredatorModes.Strategy2:
		{
			var hasPrevious = _hasPrevCandle;
			var buyBandCheck = hasPrevious && _prevClose >= _prevTightLower;
			var sellBandCheck = hasPrevious && _prevClose <= _prevTightUpper;

			var stochasticBuy = stochSignal >= StochasticUpper && stochMain >= StochasticUpper;
			var stochasticSell = stochSignal >= StochasticUpper && stochMain <= StochasticLower;

			var relaxedMomentum = momentumDeviation < MomentumBuyThreshold ||
				_prevMomentumDeviation1 < MomentumBuyThreshold ||
				_prevMomentumDeviation2 < MomentumBuyThreshold;

			var relaxedMomentumSell = momentumDeviation < MomentumSellThreshold ||
				_prevMomentumDeviation1 < MomentumSellThreshold ||
				_prevMomentumDeviation2 < MomentumSellThreshold;

			longSignal = adx > AdxThreshold && diPlus > diMinus && fast > slow && buyBandCheck && stochasticBuy && relaxedMomentum && macd > macdSignal;
			shortSignal = adx > AdxThreshold && diMinus > diPlus && fast < slow && sellBandCheck && stochasticSell && relaxedMomentumSell && macd < macdSignal;
			break;
		}
	}

	if (longSignal && Position <= 0m)
	{
		CancelActiveOrders();

		if (Position < 0m)
			BuyMarket(Math.Abs(Position) + TradeVolume);
		else
			BuyMarket(TradeVolume);
	}
	else if (shortSignal && Position >= 0m)
	{
		CancelActiveOrders();

		if (Position > 0m)
			SellMarket(Position + TradeVolume);
		else
			SellMarket(TradeVolume);
	}

	_prevMomentumDeviation2 = _prevMomentumDeviation1;
	_prevMomentumDeviation1 = momentumDeviation;
	_prevClose = candle.ClosePrice;
	_prevTightLower = tightLower;
	_prevTightUpper = tightUpper;
	_hasPrevCandle = true;
	}
}

