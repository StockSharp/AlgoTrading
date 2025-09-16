namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

	/// <summary>
	/// MACD strategy with selectable trend detection modes.
	/// </summary>
public class MacdTrendModeStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<TrendMode> _trendMode;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHist;
	private decimal _prevPrevHist;
	private bool _hasPrevHist;

	private decimal _prevMacd;
	private decimal _prevSignal;
	private bool _hasPrevLines;

	private decimal _prevZeroHist;
	private bool _hasPrevZeroHist;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>
	/// Signal line period.
	/// </summary>
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }

	/// <summary>
	/// Selected trend detection mode.
	/// </summary>
	public TrendMode TrendMode { get => _trendMode.Value; set => _trendMode.Value = value; }

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes <see cref="MacdTrendModeStrategy"/>.
	/// </summary>
	public MacdTrendModeStrategy()
{
	_fastLength = Param(nameof(FastLength), 12)
	.SetGreaterThanZero()
	.SetDisplay("Fast Length", "Fast EMA period", "MACD");

	_slowLength = Param(nameof(SlowLength), 26)
	.SetGreaterThanZero()
	.SetDisplay("Slow Length", "Slow EMA period", "MACD");

	_signalLength = Param(nameof(SignalLength), 9)
	.SetGreaterThanZero()
	.SetDisplay("Signal Length", "Signal line period", "MACD");

	_trendMode = Param(nameof(TrendMode), TrendMode.Cloud)
	.SetDisplay("Trend Mode", "Trend detection mode", "General");

	_volume = Param(nameof(Volume), 1m)
	.SetGreaterThanZero()
	.SetDisplay("Volume", "Order volume", "Trading");

	_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
	.SetDisplay("Candle Type", "Type of candles", "General");
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

	var macd = new MovingAverageConvergenceDivergenceSignal
{
	Macd =
{
	ShortMa = { Length = FastLength },
	LongMa = { Length = SlowLength },
},
	SignalMa = { Length = SignalLength }
};

	var subscription = SubscribeCandles(CandleType);
	subscription
	.BindEx(macd, ProcessCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
{
	DrawCandles(area, subscription);
	DrawIndicator(area, macd);
	DrawOwnTrades(area);
}

	StartProtection();
}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
{
	if (candle.State != CandleStates.Finished)
	return;

	var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
	var macd = macdTyped.Macd;
	var signal = macdTyped.Signal;
	var hist = macd - signal;

	var buyOpen = false;
	var sellOpen = false;
	var buyClose = false;
	var sellClose = false;

	switch (TrendMode)
{
	case Strategies.TrendMode.Histogram:
	if (_hasPrevHist)
{
	if (_hasPrevPrevHist)
{
	if (_prevHist < _prevPrevHist)
{
	if (hist > _prevHist)
	buyOpen = true;
	sellClose = true;
}
	if (_prevHist > _prevPrevHist)
{
	if (hist < _prevHist)
	sellOpen = true;
	buyClose = true;
}
}
	_prevPrevHist = _prevHist;
}
	_prevHist = hist;
	_hasPrevHist = true;
	break;

	case Strategies.TrendMode.Cloud:
	if (_hasPrevLines)
{
	if (_prevMacd > _prevSignal)
{
	if (macd < signal)
	buyOpen = true;
	sellClose = true;
}
	else if (_prevMacd < _prevSignal)
{
	if (macd > signal)
	sellOpen = true;
	buyClose = true;
}
}
	_prevMacd = macd;
	_prevSignal = signal;
	_hasPrevLines = true;
	break;

	case Strategies.TrendMode.Zero:
	if (_hasPrevZeroHist)
{
	if (_prevZeroHist > 0m)
{
	if (hist <= 0m)
	buyOpen = true;
	sellClose = true;
}
	else if (_prevZeroHist < 0m)
{
	if (hist >= 0m)
	sellOpen = true;
	buyClose = true;
}
}
	_prevZeroHist = hist;
	_hasPrevZeroHist = true;
	break;
}

	if (sellClose && Position < 0)
	BuyMarket(-Position);
	if (buyClose && Position > 0)
	SellMarket(Position);

	if (buyOpen && Position <= 0)
	BuyMarket(Volume + Math.Abs(Position));
	if (sellOpen && Position >= 0)
	SellMarket(Volume + Position);
}
}

	/// <summary>
	/// Available trend detection modes.
	/// </summary>
public enum TrendMode
{
	/// <summary>
	/// Use MACD histogram slope reversals.
	/// </summary>
	Histogram = 1,
	/// <summary>
	/// Use MACD line and signal line cloud crossover.
	/// </summary>
	Cloud,
	/// <summary>
	/// Use MACD histogram zero line cross.
	/// </summary>
	Zero
}
