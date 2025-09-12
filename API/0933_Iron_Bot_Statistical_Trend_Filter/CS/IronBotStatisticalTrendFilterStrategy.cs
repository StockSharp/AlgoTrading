using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades when price breaks statistical trend levels using Z-Score and Fibonacci ranges.
/// </summary>
public class IronBotStatisticalTrendFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _zLength;
	private readonly StrategyParam<int> _analysisWindow;
	private readonly StrategyParam<decimal> _highTrendLimit;
	private readonly StrategyParam<decimal> _lowTrendLimit;
	private readonly StrategyParam<bool> _useEma;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _slRatio;
	private readonly StrategyParam<decimal> _tp1Ratio;
	private readonly StrategyParam<decimal> _tp2Ratio;
	private readonly StrategyParam<decimal> _tp3Ratio;
	private readonly StrategyParam<decimal> _tp4Ratio;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _longStop;
	private decimal _longTp1;
	private decimal _longTp2;
	private decimal _longTp3;
	private decimal _longTp4;
	private decimal _shortStop;
	private decimal _shortTp1;
	private decimal _shortTp2;
	private decimal _shortTp3;
	private decimal _shortTp4;

	private SimpleMovingAverage _sma;
	private StandardDeviation _std;
	private ExponentialMovingAverage _ema;
	private Highest _highest;
	private Lowest _lowest;

	/// <summary>
	/// Z-score length.
	/// </summary>
	public int ZLength
	{
		get => _zLength.Value;
		set => _zLength.Value = value;
	}

	/// <summary>
	/// Trend analysis window.
	/// </summary>
	public int AnalysisWindow
	{
		get => _analysisWindow.Value;
		set => _analysisWindow.Value = value;
	}

	/// <summary>
	/// High trend Fibonacci level.
	/// </summary>
	public decimal HighTrendLimit
	{
		get => _highTrendLimit.Value;
		set => _highTrendLimit.Value = value;
	}

	/// <summary>
	/// Low trend Fibonacci level.
	/// </summary>
	public decimal LowTrendLimit
	{
		get => _lowTrendLimit.Value;
		set => _lowTrendLimit.Value = value;
	}

	/// <summary>
	/// Use EMA trend filter.
	/// </summary>
	public bool UseEma
	{
		get => _useEma.Value;
		set => _useEma.Value = value;
	}

	/// <summary>
	/// EMA length.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal SlRatio
	{
		get => _slRatio.Value;
		set => _slRatio.Value = value;
	}

	/// <summary>
	/// Take profit 1 percent.
	/// </summary>
	public decimal Tp1Ratio
	{
		get => _tp1Ratio.Value;
		set => _tp1Ratio.Value = value;
	}

	/// <summary>
	/// Take profit 2 percent.
	/// </summary>
	public decimal Tp2Ratio
	{
		get => _tp2Ratio.Value;
		set => _tp2Ratio.Value = value;
	}

	/// <summary>
	/// Take profit 3 percent.
	/// </summary>
	public decimal Tp3Ratio
	{
		get => _tp3Ratio.Value;
		set => _tp3Ratio.Value = value;
	}

	/// <summary>
	/// Take profit 4 percent.
	/// </summary>
	public decimal Tp4Ratio
	{
		get => _tp4Ratio.Value;
		set => _tp4Ratio.Value = value;
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
	/// Initializes a new instance of the <see cref="IronBotStatisticalTrendFilterStrategy"/> class.
	/// </summary>
	public IronBotStatisticalTrendFilterStrategy()
	{
		_zLength = Param(nameof(ZLength), 40)
		.SetDisplay("Z Length", "Length for Z-score", "General");

		_analysisWindow = Param(nameof(AnalysisWindow), 44)
		.SetDisplay("Analysis Window", "Lookback for trend", "General");

		_highTrendLimit = Param(nameof(HighTrendLimit), 0.236m)
		.SetDisplay("Fibo High", "High trend Fibonacci", "General");

		_lowTrendLimit = Param(nameof(LowTrendLimit), 0.786m)
		.SetDisplay("Fibo Low", "Low trend Fibonacci", "General");

		_useEma = Param(nameof(UseEma), false)
		.SetDisplay("Use EMA", "Apply EMA filter", "General");

		_emaLength = Param(nameof(EmaLength), 200)
		.SetDisplay("EMA Length", "Length of EMA", "General");

		_slRatio = Param(nameof(SlRatio), 0.008m)
		.SetDisplay("Stop %", "Stop loss percent", "Risk");

		_tp1Ratio = Param(nameof(Tp1Ratio), 0.0075m)
		.SetDisplay("TP1 %", "Take profit level 1", "Risk");

		_tp2Ratio = Param(nameof(Tp2Ratio), 0.011m)
		.SetDisplay("TP2 %", "Take profit level 2", "Risk");

		_tp3Ratio = Param(nameof(Tp3Ratio), 0.015m)
		.SetDisplay("TP3 %", "Take profit level 3", "Risk");

		_tp4Ratio = Param(nameof(Tp4Ratio), 0.02m)
		.SetDisplay("TP4 %", "Take profit level 4", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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
	_longStop = _longTp1 = _longTp2 = _longTp3 = _longTp4 = 0m;
	_shortStop = _shortTp1 = _shortTp2 = _shortTp3 = _shortTp4 = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);
	StartProtection();

	_sma = new SimpleMovingAverage { Length = ZLength };
	_std = new StandardDeviation { Length = ZLength };
	_ema = new ExponentialMovingAverage { Length = EmaLength };
	_highest = new Highest { Length = AnalysisWindow };
	_lowest = new Lowest { Length = AnalysisWindow };

	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(_sma, _std, _ema, ProcessCandle)
	.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal stdValue, decimal emaValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	var highestValue = _highest.Process(candle.HighPrice);
	var lowestValue = _lowest.Process(candle.LowPrice);

	if (!highestValue.IsFinal || !lowestValue.IsFinal)
	return;

	var highestHigh = highestValue.GetValue<decimal>();
	var lowestLow = lowestValue.GetValue<decimal>();

	var zScore = stdValue == 0m ? 0m : (candle.ClosePrice - smaValue) / stdValue;

	var range = highestHigh - lowestLow;
	var highTrendLevel = highestHigh - range * HighTrendLimit;
	var trendLine = highestHigh - range * 0.5m;
	var lowTrendLevel = highestHigh - range * LowTrendLimit;

	if (Position > 0)
	{
	if (candle.LowPrice <= _longStop || candle.HighPrice >= _longTp1 || candle.HighPrice >= _longTp2 || candle.HighPrice >= _longTp3 || candle.HighPrice >= _longTp4)
	SellMarket();
	return;
	}
	else if (Position < 0)
	{
	if (candle.HighPrice >= _shortStop || candle.LowPrice <= _shortTp1 || candle.LowPrice <= _shortTp2 || candle.LowPrice <= _shortTp3 || candle.LowPrice <= _shortTp4)
	BuyMarket();
	return;
	}

	var emaBullish = candle.ClosePrice >= emaValue;
	var emaBearish = candle.ClosePrice <= emaValue;

	var canLong = candle.ClosePrice >= trendLine && candle.ClosePrice >= highTrendLevel && (!UseEma || emaBullish) && zScore >= 0m;
	var canShort = candle.ClosePrice <= trendLine && candle.ClosePrice <= lowTrendLevel && (!UseEma || emaBearish) && zScore <= 0m;

	if (canLong)
	{
	BuyMarket();
	_longStop = candle.ClosePrice * (1m - SlRatio);
	_longTp1 = candle.ClosePrice * (1m + Tp1Ratio);
	_longTp2 = candle.ClosePrice * (1m + Tp2Ratio);
	_longTp3 = candle.ClosePrice * (1m + Tp3Ratio);
	_longTp4 = candle.ClosePrice * (1m + Tp4Ratio);
	}
	else if (canShort)
	{
	SellMarket();
	_shortStop = candle.ClosePrice * (1m + SlRatio);
	_shortTp1 = candle.ClosePrice * (1m - Tp1Ratio);
	_shortTp2 = candle.ClosePrice * (1m - Tp2Ratio);
	_shortTp3 = candle.ClosePrice * (1m - Tp3Ratio);
	_shortTp4 = candle.ClosePrice * (1m - Tp4Ratio);
	}
	}
}