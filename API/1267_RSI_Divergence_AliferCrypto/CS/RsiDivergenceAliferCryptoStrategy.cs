using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI divergence strategy with optional trend and RSI zone filters.
/// </summary>
public class RsiDivergenceAliferCryptoStrategy : Strategy {
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _lookLeft;
	private readonly StrategyParam<int> _lookRight;
	private readonly StrategyParam<int> _rangeLower;
	private readonly StrategyParam<int> _rangeUpper;
	private readonly StrategyParam<bool> _enableRsiFilter;
	private readonly StrategyParam<int> _oversoldLevel;
	private readonly StrategyParam<int> _overboughtLevel;
	private readonly StrategyParam<bool> _enableTrendFilter;
	private readonly StrategyParam<MaType> _maType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<SlTpMethod> _method;
	private readonly StrategyParam<ExitMode> _exitMode;
	private readonly StrategyParam<int> _swingLook;
	private readonly StrategyParam<decimal> _swingMarginPct;
	private readonly StrategyParam<decimal> _rrSwing;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _rrAtr;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _rsiBuffer = [];
	private readonly List<decimal> _lowBuffer = [];
	private readonly List<decimal> _highBuffer = [];
	private int _barIndex;

	private decimal? _prevPivotLowRsi;
	private decimal? _prevPivotLowPrice;
	private int _prevPivotLowIndex = int.MinValue;

	private decimal? _prevPivotHighRsi;
	private decimal? _prevPivotHighPrice;
	private int _prevPivotHighIndex = int.MinValue;

	private bool _rsiOversoldFlag;
	private bool _rsiOverboughtFlag;

	private decimal? _entrySl;
	private decimal? _entryTp;
	private decimal? _slPrice;
	private decimal? _tpPrice;

	public int RsiLength {
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}
	public int LookLeft {
		get => _lookLeft.Value;
		set => _lookLeft.Value = value;
	}
	public int LookRight {
		get => _lookRight.Value;
		set => _lookRight.Value = value;
	}
	public int RangeLower {
		get => _rangeLower.Value;
		set => _rangeLower.Value = value;
	}
	public int RangeUpper {
		get => _rangeUpper.Value;
		set => _rangeUpper.Value = value;
	}
	public bool EnableRsiFilter {
		get => _enableRsiFilter.Value;
		set => _enableRsiFilter.Value = value;
	}
	public int OversoldLevel {
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}
	public int OverboughtLevel {
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}
	public bool EnableTrendFilter {
		get => _enableTrendFilter.Value;
		set => _enableTrendFilter.Value = value;
	}
	public MaType TrendMaType {
		get => _maType.Value;
		set => _maType.Value = value;
	}
	public int TrendMaLength {
		get => _maLength.Value;
		set => _maLength.Value = value;
	}
	public SlTpMethod Method {
		get => _method.Value;
		set => _method.Value = value;
	}
	public ExitMode ExitMode {
		get => _exitMode.Value;
		set => _exitMode.Value = value;
	}
	public int SwingLook {
		get => _swingLook.Value;
		set => _swingLook.Value = value;
	}
	public decimal SwingMarginPct {
		get => _swingMarginPct.Value;
		set => _swingMarginPct.Value = value;
	}
	public decimal RrSwing {
		get => _rrSwing.Value;
		set => _rrSwing.Value = value;
	}
	public int AtrLength {
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}
	public decimal AtrMultiplier {
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}
	public decimal RrAtr {
		get => _rrAtr.Value;
		set => _rrAtr.Value = value;
	}
	public DataType CandleType {
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public RsiDivergenceAliferCryptoStrategy() {
		_rsiLength = Param(nameof(RsiLength), 14)
						 .SetDisplay("RSI Length", "RSI calculation length",
									 "Indicators");
		_lookLeft =
			Param(nameof(LookLeft), 5)
				.SetDisplay("Pivot Left", "Bars to the left", "General");
		_lookRight =
			Param(nameof(LookRight), 5)
				.SetDisplay("Pivot Right", "Bars to the right", "General");
		_rangeLower = Param(nameof(RangeLower), 5)
						  .SetDisplay("Range Lower",
									  "Minimum bars between pivots", "General");
		_rangeUpper = Param(nameof(RangeUpper), 60)
						  .SetDisplay("Range Upper",
									  "Maximum bars between pivots", "General");
		_enableRsiFilter = Param(nameof(EnableRsiFilter), false)
							   .SetDisplay("Enable RSI Filter",
										   "Use RSI zone filter", "Filters");
		_oversoldLevel = Param(nameof(OversoldLevel), 30)
							 .SetDisplay("Oversold Level",
										 "RSI oversold threshold", "Filters");
		_overboughtLevel =
			Param(nameof(OverboughtLevel), 70)
				.SetDisplay("Overbought Level", "RSI overbought threshold",
							"Filters");
		_enableTrendFilter =
			Param(nameof(EnableTrendFilter), false)
				.SetDisplay("Enable Trend Filter", "Use trend MA", "Filters");
		_maType = Param(nameof(TrendMaType), MaType.Sma)
					  .SetDisplay("MA Type", "Trend MA type", "Filters");
		_maLength = Param(nameof(TrendMaLength), 200)
						.SetDisplay("MA Length", "Trend MA length", "Filters");
		_method = Param(nameof(Method), SlTpMethod.Swing)
					  .SetDisplay("SL/TP Method", "Stop/target method", "Risk");
		_exitMode =
			Param(nameof(ExitMode), Strategies.ExitMode.Dynamic)
				.SetDisplay("Exit Mode", "Dynamic or static exits", "Risk");
		_swingLook = Param(nameof(SwingLook), 20)
						 .SetDisplay("Swing Lookback",
									 "Bars for swing detection", "Risk");
		_swingMarginPct =
			Param(nameof(SwingMarginPct), 1m)
				.SetDisplay("Swing Margin %", "Margin around swing", "Risk");
		_rrSwing = Param(nameof(RrSwing), 2m)
					   .SetDisplay("R/R Swing", "Risk/reward for swing method",
								   "Risk");
		_atrLength =
			Param(nameof(AtrLength), 14)
				.SetDisplay("ATR Length", "ATR calculation length", "Risk");
		_atrMultiplier =
			Param(nameof(AtrMultiplier), 1.5m)
				.SetDisplay("ATR Mult", "ATR SL multiplier", "Risk");
		_rrAtr =
			Param(nameof(RrAtr), 2m)
				.SetDisplay("R/R ATR", "Risk/reward for ATR method", "Risk");
		_candleType =
			Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)>
	GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnReseted() {
		base.OnReseted();
		_rsiBuffer.Clear();
		_lowBuffer.Clear();
		_highBuffer.Clear();
		_barIndex = 0;
		_prevPivotLowRsi = null;
		_prevPivotLowPrice = null;
		_prevPivotLowIndex = int.MinValue;
		_prevPivotHighRsi = null;
		_prevPivotHighPrice = null;
		_prevPivotHighIndex = int.MinValue;
		_rsiOversoldFlag = false;
		_rsiOverboughtFlag = false;
		_entrySl = null;
		_entryTp = null;
		_slPrice = null;
		_tpPrice = null;
	}

	protected override void OnStarted(DateTimeOffset time) {
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var ma = CreateMa(TrendMaType, TrendMaLength);
		var swingLow = new Lowest { Length = SwingLook };
		var swingHigh = new Highest { Length = SwingLook };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ma, swingLow, swingHigh, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null) {
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawIndicator(area, ma);
			DrawIndicator(area, swingLow);
			DrawIndicator(area, swingHigh);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi,
							   decimal trendMa, decimal swingLow,
							   decimal swingHigh, decimal atr) {
		if (candle.State != CandleStates.Finished)
			return;

		_rsiOversoldFlag |= rsi < OversoldLevel;
		_rsiOverboughtFlag |= rsi > OverboughtLevel;

		_barIndex++;
		_rsiBuffer.Add(rsi);
		_lowBuffer.Add(candle.LowPrice);
		_highBuffer.Add(candle.HighPrice);

		var maxCount = LookLeft + LookRight + 1;
		if (_rsiBuffer.Count > maxCount) {
			_rsiBuffer.RemoveAt(0);
			_lowBuffer.RemoveAt(0);
			_highBuffer.RemoveAt(0);
		}

		var rawBull = false;
		var rawBear = false;

		if (_rsiBuffer.Count == maxCount) {
			var idx = LookLeft;
			var pivotRsi = _rsiBuffer[idx];
			var pivotLow = _lowBuffer[idx];
			var pivotHigh = _highBuffer[idx];
			var isLow = true;
			var isHigh = true;

			for (var i = 0; i < maxCount; i++) {
				if (i == idx)
					continue;
				if (_rsiBuffer[i] <= pivotRsi)
					isLow = false;
				if (_rsiBuffer[i] >= pivotRsi)
					isHigh = false;
			}

			var pivotIndex = _barIndex - LookRight - 1;

			if (isLow) {
				if (_prevPivotLowRsi is decimal pr &&
					_prevPivotLowPrice is decimal pp) {
					var dist = pivotIndex - _prevPivotLowIndex;
					var inRange = dist >= RangeLower && dist <= RangeUpper;
					rawBull = pivotLow < pp && pivotRsi > pr && inRange;
				}
				_prevPivotLowRsi = pivotRsi;
				_prevPivotLowPrice = pivotLow;
				_prevPivotLowIndex = pivotIndex;
			}

			if (isHigh) {
				if (_prevPivotHighRsi is decimal pr &&
					_prevPivotHighPrice is decimal pp) {
					var dist = pivotIndex - _prevPivotHighIndex;
					var inRange = dist >= RangeLower && dist <= RangeUpper;
					rawBear = pivotHigh > pp && pivotRsi < pr && inRange;
				}
				_prevPivotHighRsi = pivotRsi;
				_prevPivotHighPrice = pivotHigh;
				_prevPivotHighIndex = pivotIndex;
			}
		}

		var bullCond = rawBull && (!EnableRsiFilter || _rsiOversoldFlag) &&
					   (!EnableTrendFilter || candle.ClosePrice > trendMa);
		var bearCond = rawBear && (!EnableRsiFilter || _rsiOverboughtFlag) &&
					   (!EnableTrendFilter || candle.ClosePrice < trendMa);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (bullCond && Position <= 0) {
			BuyMarket();
			_rsiOversoldFlag = false;
			_entrySl = null;
			_entryTp = null;
		} else if (bearCond && Position >= 0) {
			SellMarket();
			_rsiOverboughtFlag = false;
			_entrySl = null;
			_entryTp = null;
		}

		if (Position > 0) {
			var entryPrice = PositionAvgPrice;
			decimal slCalc;
			decimal tpCalc;
			decimal rr;

			if (Method == SlTpMethod.Swing) {
				slCalc = swingLow * (1m - SwingMarginPct / 100m);
				rr = RrSwing;
			} else {
				slCalc = entryPrice - atr * AtrMultiplier;
				rr = RrAtr;
			}
			var risk = entryPrice - slCalc;
			tpCalc = entryPrice + risk * rr;

			if (ExitMode == ExitMode.Static && _entrySl is null) {
				_entrySl = slCalc;
				_entryTp = tpCalc;
			}

			_slPrice = ExitMode == ExitMode.Dynamic ? slCalc : _entrySl;
			_tpPrice = ExitMode == ExitMode.Dynamic ? tpCalc : _entryTp;

			if (_slPrice is decimal sl && candle.LowPrice <= sl) {
				SellMarket(Math.Abs(Position));
				_entrySl = null;
				_entryTp = null;
				return;
			}
			if (_tpPrice is decimal tp && candle.HighPrice >= tp) {
				SellMarket(Math.Abs(Position));
				_entrySl = null;
				_entryTp = null;
			}
		} else if (Position < 0) {
			var entryPrice = PositionAvgPrice;
			decimal slCalc;
			decimal tpCalc;
			decimal rr;

			if (Method == SlTpMethod.Swing) {
				slCalc = swingHigh * (1m + SwingMarginPct / 100m);
				rr = RrSwing;
			} else {
				slCalc = entryPrice + atr * AtrMultiplier;
				rr = RrAtr;
			}
			var risk = slCalc - entryPrice;
			tpCalc = entryPrice - risk * rr;

			if (ExitMode == ExitMode.Static && _entrySl is null) {
				_entrySl = slCalc;
				_entryTp = tpCalc;
			}

			_slPrice = ExitMode == ExitMode.Dynamic ? slCalc : _entrySl;
			_tpPrice = ExitMode == ExitMode.Dynamic ? tpCalc : _entryTp;

			if (_slPrice is decimal sl && candle.HighPrice >= sl) {
				BuyMarket(Math.Abs(Position));
				_entrySl = null;
				_entryTp = null;
				return;
			}
			if (_tpPrice is decimal tp && candle.LowPrice <= tp) {
				BuyMarket(Math.Abs(Position));
				_entrySl = null;
				_entryTp = null;
			}
		}
	}

	private static MovingAverage CreateMa(MaType type, int length) {
		return type switch {
			MaType.Sma => new SimpleMovingAverage { Length = length },
			MaType.Ema => new ExponentialMovingAverage { Length = length },
			MaType.Smma => new SmoothedMovingAverage { Length = length },
			MaType.Wma => new WeightedMovingAverage { Length = length },
			MaType.Vwma => new VolumeWeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}
}

/// <summary>
/// Moving average types.
/// </summary>
public enum MaType {
	/// <summary> Simple moving average. </summary>
	Sma,
	/// <summary> Exponential moving average. </summary>
	Ema,
	/// <summary> Smoothed moving average. </summary>
	Smma,
	/// <summary> Weighted moving average. </summary>
	Wma,
	/// <summary> Volume weighted moving average. </summary>
	Vwma
}

/// <summary>
/// Stop loss and take profit calculation method.
/// </summary>
public enum SlTpMethod {
	/// <summary> Use recent swing high/low. </summary>
	Swing,
	/// <summary> Use ATR based calculation. </summary>
	Atr
}

/// <summary>
/// Exit levels mode.
/// </summary>
public enum ExitMode {
	/// <summary> Recalculate SL/TP each bar. </summary>
	Dynamic,
	/// <summary> Lock SL/TP at entry. </summary>
	Static
}
