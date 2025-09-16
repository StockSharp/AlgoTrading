using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Binary Wave strategy.
/// Combines multiple indicators into a binary wave and trades on its signals.
/// </summary>
public class BinaryWaveStrategy : Strategy
{
	/// <summary>
	/// Mode for generating entry signals.
	/// </summary>
	public enum EntryMode
	{
		/// <summary>
		/// Enter when the wave crosses zero.
		/// </summary>
		Breakdown,

		/// <summary>
		/// Enter when the wave changes direction.
		/// </summary>
		Twist
	}

	private readonly StrategyParam<EntryMode> _mode;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _fastMacd;
	private readonly StrategyParam<int> _slowMacd;
	private readonly StrategyParam<int> _signalMacd;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _weightMa;
	private readonly StrategyParam<decimal> _weightMacd;
	private readonly StrategyParam<decimal> _weightOsma;
	private readonly StrategyParam<decimal> _weightCci;
	private readonly StrategyParam<decimal> _weightMomentum;
	private readonly StrategyParam<decimal> _weightRsi;
	private readonly StrategyParam<decimal> _weightAdx;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private ExponentialMovingAverage _ma;
	private MovingAverageConvergenceDivergenceSignal _macd;
	private CommodityChannelIndex _cci;
	private Momentum _momentum;
	private RelativeStrengthIndex _rsi;
	private AverageDirectionalIndex _adx;

	private decimal _prevWave;
	private decimal _prevWave2;
	private decimal _prevMacd;

	/// <summary>
	/// Mode for generating entry signals.
	/// </summary>
	public EntryMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
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
	/// Moving Average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Fast MACD period.
	/// </summary>
	public int FastMacd
	{
		get => _fastMacd.Value;
		set => _fastMacd.Value = value;
	}

	/// <summary>
	/// Slow MACD period.
	/// </summary>
	public int SlowMacd
	{
		get => _slowMacd.Value;
		set => _slowMacd.Value = value;
	}

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int SignalMacd
	{
		get => _signalMacd.Value;
		set => _signalMacd.Value = value;
	}

	/// <summary>
	/// CCI period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Momentum period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Weight for MA comparison.
	/// </summary>
	public decimal WeightMa
	{
		get => _weightMa.Value;
		set => _weightMa.Value = value;
	}

	/// <summary>
	/// Weight for MACD slope.
	/// </summary>
	public decimal WeightMacd
	{
		get => _weightMacd.Value;
		set => _weightMacd.Value = value;
	}

	/// <summary>
	/// Weight for MACD histogram.
	/// </summary>
	public decimal WeightOsma
	{
		get => _weightOsma.Value;
		set => _weightOsma.Value = value;
	}

	/// <summary>
	/// Weight for CCI sign.
	/// </summary>
	public decimal WeightCci
	{
		get => _weightCci.Value;
		set => _weightCci.Value = value;
	}

	/// <summary>
	/// Weight for Momentum position.
	/// </summary>
	public decimal WeightMomentum
	{
		get => _weightMomentum.Value;
		set => _weightMomentum.Value = value;
	}

	/// <summary>
	/// Weight for RSI position.
	/// </summary>
	public decimal WeightRsi
	{
		get => _weightRsi.Value;
		set => _weightRsi.Value = value;
	}

	/// <summary>
	/// Weight for ADX directional movement.
	/// </summary>
	public decimal WeightAdx
	{
		get => _weightAdx.Value;
		set => _weightAdx.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
	}

	/// <summary>
	/// Stop loss in percent.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in percent.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="BinaryWaveStrategy"/>.
	/// </summary>
	public BinaryWaveStrategy()
	{
		_mode = Param(nameof(Mode), EntryMode.Breakdown)
		.SetDisplay("Mode", "Entry signal mode", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_maPeriod = Param(nameof(MaPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Moving Average period", "Indicators");

		_fastMacd = Param(nameof(FastMacd), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "MACD fast period", "Indicators");

		_slowMacd = Param(nameof(SlowMacd), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "MACD slow period", "Indicators");

		_signalMacd = Param(nameof(SignalMacd), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal line period", "Indicators");

		_cciPeriod = Param(nameof(CciPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("CCI Period", "Commodity Channel Index period", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Period", "Momentum indicator period", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Relative Strength Index period", "Indicators");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ADX Period", "Average Directional Index period", "Indicators");

		_weightMa = Param(nameof(WeightMa), 1m)
		.SetDisplay("MA Weight", "Weight for MA comparison", "Weights");

		_weightMacd = Param(nameof(WeightMacd), 1m)
		.SetDisplay("MACD Weight", "Weight for MACD slope", "Weights");

		_weightOsma = Param(nameof(WeightOsma), 1m)
		.SetDisplay("Histogram Weight", "Weight for MACD histogram", "Weights");

		_weightCci = Param(nameof(WeightCci), 1m)
		.SetDisplay("CCI Weight", "Weight for CCI sign", "Weights");

		_weightMomentum = Param(nameof(WeightMomentum), 1m)
		.SetDisplay("Momentum Weight", "Weight for Momentum position", "Weights");

		_weightRsi = Param(nameof(WeightRsi), 1m)
		.SetDisplay("RSI Weight", "Weight for RSI position", "Weights");

		_weightAdx = Param(nameof(WeightAdx), 1m)
		.SetDisplay("ADX Weight", "Weight for ADX direction", "Weights");

		_allowLongEntry = Param(nameof(AllowLongEntry), true)
		.SetDisplay("Allow Long Entry", "Enable opening long positions", "Trading");

		_allowShortEntry = Param(nameof(AllowShortEntry), true)
		.SetDisplay("Allow Short Entry", "Enable opening short positions", "Trading");

		_allowLongExit = Param(nameof(AllowLongExit), true)
		.SetDisplay("Allow Long Exit", "Enable closing long positions", "Trading");

		_allowShortExit = Param(nameof(AllowShortExit), true)
		.SetDisplay("Allow Short Exit", "Enable closing short positions", "Trading");

		_stopLoss = Param(nameof(StopLoss), 0m)
		.SetRange(0m, 100m)
		.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 0m)
		.SetRange(0m, 100m)
		.SetDisplay("Take Profit %", "Take profit percentage", "Risk");
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

		_prevWave = 0m;
		_prevWave2 = 0m;
		_prevMacd = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = new ExponentialMovingAverage { Length = MaPeriod };
		_macd = new()
		{
			Macd =
			{
				ShortMa = new ExponentialMovingAverage { Length = FastMacd },
				LongMa = new ExponentialMovingAverage { Length = SlowMacd },
			},
			SignalMa = new ExponentialMovingAverage { Length = SignalMacd }
		};
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_momentum = new Momentum { Length = MomentumPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_adx = new AverageDirectionalIndex { Length = AdxPeriod };

		StartProtection(
			takeProfit: TakeProfit > 0 ? new Unit(TakeProfit, UnitTypes.Percent) : null,
			stopLoss: StopLoss > 0 ? new Unit(StopLoss, UnitTypes.Percent) : null
		);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_ma, _macd, _cci, _momentum, _rsi, _adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _cci);
			DrawIndicator(area, _momentum);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(
	ICandleMessage candle,
	IIndicatorValue maValue,
	IIndicatorValue macdValue,
	IIndicatorValue cciValue,
	IIndicatorValue momentumValue,
	IIndicatorValue rsiValue,
	IIndicatorValue adxValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	var close = candle.ClosePrice;
	var ma = maValue.ToDecimal();
	var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
	var macdLine = macdTyped.Macd;
	var macdSignal = macdTyped.Signal;
	var cci = cciValue.ToDecimal();
	var mom = momentumValue.ToDecimal();
	var rsi = rsiValue.ToDecimal();
	var adxTyped = (AverageDirectionalIndexValue)adxValue;
	var plusDi = adxTyped.Dx.Plus;
	var minusDi = adxTyped.Dx.Minus;

	var wave = 0m;
	if (WeightMa > 0)
	wave += WeightMa * (close > ma ? 1m : close < ma ? -1m : 0m);
	if (WeightMacd > 0)
	wave += WeightMacd * (macdLine > _prevMacd ? 1m : macdLine < _prevMacd ? -1m : 0m);
	if (WeightOsma > 0)
	wave += WeightOsma * (macdLine - macdSignal > 0 ? 1m : macdLine - macdSignal < 0 ? -1m : 0m);
	if (WeightCci > 0)
	wave += WeightCci * (cci > 0 ? 1m : cci < 0 ? -1m : 0m);
	if (WeightMomentum > 0)
	wave += WeightMomentum * (mom > 100m ? 1m : mom < 100m ? -1m : 0m);
	if (WeightRsi > 0)
	wave += WeightRsi * (rsi > 50m ? 1m : rsi < 50m ? -1m : 0m);
	if (WeightAdx > 0)
	wave += WeightAdx * (plusDi > minusDi ? 1m : plusDi < minusDi ? -1m : 0m);

	var buyOpen = false;
	var sellOpen = false;
	var buyClose = false;
	var sellClose = false;

	if (Mode == EntryMode.Breakdown)
	{
	if (_prevWave <= 0 && wave > 0)
	{
	if (AllowLongEntry)
	buyOpen = true;
	if (AllowShortExit)
	sellClose = true;
	}
	else if (_prevWave >= 0 && wave < 0)
	{
	if (AllowShortEntry)
	sellOpen = true;
	if (AllowLongExit)
	buyClose = true;
	}
	}
	else
	{
	if (_prevWave < _prevWave2 && wave > _prevWave)
	{
	if (AllowLongEntry)
	buyOpen = true;
	if (AllowShortExit)
	sellClose = true;
	}
	else if (_prevWave > _prevWave2 && wave < _prevWave)
	{
	if (AllowShortEntry)
	sellOpen = true;
	if (AllowLongExit)
	buyClose = true;
	}
	}

	if (buyClose && Position > 0)
	SellMarket(Position);
	if (sellClose && Position < 0)
	BuyMarket(Math.Abs(Position));
	if (buyOpen && Position <= 0)
	BuyMarket(Volume + Math.Abs(Position));
	if (sellOpen && Position >= 0)
	SellMarket(Volume + Math.Abs(Position));

	_prevWave2 = _prevWave;
	_prevWave = wave;
	_prevMacd = macdLine;
	}
}
