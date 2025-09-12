using System;
using System.Collections.Generic;
using Ecng.Common;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining moving averages, RSI, MACD and volume filter with
/// stop-loss and trailing profit.
/// </summary>
public class MaxProfitMinLossOptionsStrategy : Strategy {
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<bool> _useEma;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _volSmaLength;
	private readonly StrategyParam<decimal> _stopLossPerc;
	private readonly StrategyParam<decimal> _trailProfitPerc;
	private readonly StrategyParam<DataType> _candleType;

	private LengthIndicator<decimal> _maFast;
	private LengthIndicator<decimal> _maSlow;
	private RelativeStrengthIndex _rsi;
	private MovingAverageConvergenceDivergence _macd;
	private SimpleMovingAverage _volumeSma;

	private decimal _prevMacd;
	private decimal _prevSignal;
	private bool _hasPrevMacd;

	private decimal _rsiPrev1;
	private decimal _rsiPrev2;
	private decimal _rsiPrev3;
	private bool _hasRsiHistory;

	private decimal _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;

	/// <summary>
	/// Fast MA length.
	/// </summary>
	public int FastLength {
	get => _fastLength.Value;
	set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow MA length.
	/// </summary>
	public int SlowLength {
	get => _slowLength.Value;
	set => _slowLength.Value = value;
	}

	/// <summary>
	/// Use EMA (false = SMA).
	/// </summary>
	public bool UseEma {
	get => _useEma.Value;
	set => _useEma.Value = value;
	}

	/// <summary>
	/// RSI period length.
	/// </summary>
	public int RsiLength {
	get => _rsiLength.Value;
	set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought {
	get => _rsiOverbought.Value;
	set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold {
	get => _rsiOversold.Value;
	set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int MacdFast {
	get => _macdFast.Value;
	set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int MacdSlow {
	get => _macdSlow.Value;
	set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignal {
	get => _macdSignal.Value;
	set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Volume SMA length.
	/// </summary>
	public int VolSmaLength {
	get => _volSmaLength.Value;
	set => _volSmaLength.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPerc {
	get => _stopLossPerc.Value;
	set => _stopLossPerc.Value = value;
	}

	/// <summary>
	/// Trailing profit percentage.
	/// </summary>
	public decimal TrailProfitPerc {
	get => _trailProfitPerc.Value;
	set => _trailProfitPerc.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType {
	get => _candleType.Value;
	set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="MaxProfitMinLossOptionsStrategy"/>.
	/// </summary>
	public MaxProfitMinLossOptionsStrategy() {
	_fastLength =
		Param(nameof(FastLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA Length", "Fast MA period", "General")
		.SetCanOptimize(true);

	_slowLength =
		Param(nameof(SlowLength), 21)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA Length", "Slow MA period", "General")
		.SetCanOptimize(true);

	_useEma =
		Param(nameof(UseEma), true)
		.SetDisplay("Use EMA", "True - EMA, False - SMA", "General");

	_rsiLength = Param(nameof(RsiLength), 14)
			 .SetGreaterThanZero()
			 .SetDisplay("RSI Length", "RSI period", "Indicators")
			 .SetCanOptimize(true);

	_rsiOverbought = Param(nameof(RsiOverbought), 70m)
				 .SetDisplay("RSI Overbought",
					 "Overbought threshold", "Indicators");

	_rsiOversold =
		Param(nameof(RsiOversold), 30m)
		.SetDisplay("RSI Oversold", "Oversold threshold", "Indicators");

	_macdFast =
		Param(nameof(MacdFast), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "MACD fast period", "Indicators");

	_macdSlow =
		Param(nameof(MacdSlow), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "MACD slow period", "Indicators");

	_macdSignal =
		Param(nameof(MacdSignal), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "MACD signal period", "Indicators");

	_volSmaLength = Param(nameof(VolSmaLength), 20)
				.SetGreaterThanZero()
				.SetDisplay("Volume SMA Length",
					"Volume SMA period", "Indicators");

	_stopLossPerc =
		Param(nameof(StopLossPerc), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss %", "Stop loss percent", "Risk");

	_trailProfitPerc = Param(nameof(TrailProfitPerc), 4m)
				   .SetGreaterThanZero()
				   .SetDisplay("Trailing Profit %",
					   "Trailing profit percent", "Risk");

	_candleType =
		Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)>
	GetWorkingSecurities() {
	return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted() {
	base.OnReseted();

	_maFast = null;
	_maSlow = null;
	_rsi = null;
	_macd = null;
	_volumeSma = null;

	_prevMacd = default;
	_prevSignal = default;
	_hasPrevMacd = false;

	_rsiPrev1 = default;
	_rsiPrev2 = default;
	_rsiPrev3 = default;
	_hasRsiHistory = false;

	_entryPrice = 0m;
	_highestPrice = 0m;
	_lowestPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time) {
	base.OnStarted(time);

	_maFast = UseEma ? new ExponentialMovingAverage { Length = FastLength }
			 : new SimpleMovingAverage { Length = FastLength };
	_maSlow = UseEma ? new ExponentialMovingAverage { Length = SlowLength }
			 : new SimpleMovingAverage { Length = SlowLength };
	_rsi = new RelativeStrengthIndex { Length = RsiLength };
	_macd = new MovingAverageConvergenceDivergence { ShortPeriod = MacdFast,
							 LongPeriod = MacdSlow,
							 SignalPeriod =
								 MacdSignal };
	_volumeSma = new SimpleMovingAverage { Length = VolSmaLength };

	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(_maFast, _maSlow, _rsi, _macd, ProcessCandle).Start();

	var area = CreateChartArea();
	if (area != null) {
		DrawCandles(area, subscription);
		DrawIndicator(area, _maFast);
		DrawIndicator(area, _maSlow);
		DrawOwnTrades(area);
	}

	StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maFast,
				   decimal maSlow, decimal rsiValue,
				   decimal macdLine, decimal macdSignal,
				   decimal _) {
	if (candle.State != CandleStates.Finished)
		return;

	var volumeAvg = _volumeSma
				.Process(candle.TotalVolume, candle.ServerTime,
					 candle.State == CandleStates.Finished)
				.ToDecimal();

	if (!_maFast.IsFormed || !_maSlow.IsFormed || !_rsi.IsFormed ||
		!_macd.IsFormed || !_volumeSma.IsFormed) {
		UpdateRsiHistory(rsiValue);
		_prevMacd = macdLine;
		_prevSignal = macdSignal;
		_hasPrevMacd = true;
		return;
	}

	UpdateRsiHistory(rsiValue);

	if (!_hasPrevMacd || !_hasRsiHistory) {
		_prevMacd = macdLine;
		_prevSignal = macdSignal;
		return;
	}

	if (!IsFormedAndOnlineAndAllowTrading()) {
		_prevMacd = macdLine;
		_prevSignal = macdSignal;
		return;
	}

	var macdUp = macdLine > macdSignal && _prevMacd <= _prevSignal;
	var macdDown = macdLine < macdSignal && _prevMacd >= _prevSignal;

	var rsiRising = rsiValue > _rsiPrev1 && _rsiPrev1 > _rsiPrev2 &&
			_rsiPrev2 > _rsiPrev3;
	var rsiFalling = rsiValue < _rsiPrev1 && _rsiPrev1 < _rsiPrev2 &&
			 _rsiPrev2 < _rsiPrev3;

	var bullishTrend = maFast > maSlow;
	var bearishTrend = maFast < maSlow;

	var volAboveAvg = candle.TotalVolume > volumeAvg;

	var buyCall = bullishTrend && macdUp && rsiValue > RsiOversold &&
			  rsiRising && volAboveAvg;
	var buyPut = bearishTrend && macdDown && rsiValue < RsiOverbought &&
			 rsiFalling && volAboveAvg;

	if (buyCall && Position <= 0) {
		BuyMarket(Volume + Math.Abs(Position));
		_entryPrice = candle.ClosePrice;
		_highestPrice = candle.ClosePrice;
		_lowestPrice = candle.ClosePrice;
	} else if (buyPut && Position >= 0) {
		SellMarket(Volume + Math.Abs(Position));
		_entryPrice = candle.ClosePrice;
		_highestPrice = candle.ClosePrice;
		_lowestPrice = candle.ClosePrice;
	}

	if (Position > 0) {
		_highestPrice = Math.Max(_highestPrice, candle.ClosePrice);
		var stop = _entryPrice * (1m - StopLossPerc / 100m);
		var trail = _highestPrice * (1m - TrailProfitPerc / 100m);
		var exit = Math.Max(stop, trail);
		if (candle.ClosePrice <= exit)
		SellMarket(Position);
	} else if (Position < 0) {
		_lowestPrice = Math.Min(_lowestPrice, candle.ClosePrice);
		var stop = _entryPrice * (1m + StopLossPerc / 100m);
		var trail = _lowestPrice * (1m + TrailProfitPerc / 100m);
		var exit = Math.Min(stop, trail);
		if (candle.ClosePrice >= exit)
		BuyMarket(Math.Abs(Position));
	}

	_prevMacd = macdLine;
	_prevSignal = macdSignal;
	}

	private void UpdateRsiHistory(decimal rsi) {
	_rsiPrev3 = _rsiPrev2;
	_rsiPrev2 = _rsiPrev1;
	_rsiPrev1 = rsi;
	if (!_hasRsiHistory && _rsiPrev3 != 0m)
		_hasRsiHistory = true;
	}
}
