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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Schaff Trend Cycle calculated over TRIX MACD.
/// Buys when the oscillator crosses above the high level and sells when it
/// crosses below the low level.
/// </summary>
public class ColorSchaffTrixTrendCycleStrategy : Strategy {
	private readonly StrategyParam<int> _fastTrixLength;
	private readonly StrategyParam<int> _slowTrixLength;
	private readonly StrategyParam<int> _cycle;
	private readonly StrategyParam<int> _highLevel;
	private readonly StrategyParam<int> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<decimal> _factor;

	private SchaffTrixTrendCycle _stc = null!;
	private decimal? _prevStc;

	/// <summary>
	/// Fast TRIX length.
	/// </summary>
	public int FastTrixLength {
	get => _fastTrixLength.Value;
	set => _fastTrixLength.Value = value;
	}

	/// <summary>
	/// Slow TRIX length.
	/// </summary>
	public int SlowTrixLength {
	get => _slowTrixLength.Value;
	set => _slowTrixLength.Value = value;
	}

	/// <summary>
	/// Cycle length used in stochastic calculations.
	/// </summary>
	public int Cycle {
	get => _cycle.Value;
	set => _cycle.Value = value;
	}

	/// <summary>
	/// Upper threshold for oscillator.
	/// </summary>
	public int HighLevel {
	get => _highLevel.Value;
	set => _highLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold for oscillator.
	/// </summary>
	public int LowLevel {
	get => _lowLevel.Value;
	set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Candle type for indicator calculation.
	/// </summary>
	public DataType CandleType {
	get => _candleType.Value;
	set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop loss value in percent.
	/// </summary>
	public decimal StopLoss {
	get => _stopLoss.Value;
	set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit value in percent.
	/// </summary>
	public decimal TakeProfit {
	get => _takeProfit.Value;
	set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyOpen {
	get => _buyOpen.Value;
	set => _buyOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellOpen {
	get => _sellOpen.Value;
	set => _sellOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyClose {
	get => _buyClose.Value;
	set => _buyClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellClose {
	get => _sellClose.Value;
	set => _sellClose.Value = value;
	}

	/// <summary>
	/// Smoothing factor used in the Schaff Trend Cycle calculations.
	/// </summary>
	public decimal Factor {
	get => _factor.Value;
	set => _factor.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see
	/// cref="ColorSchaffTrixTrendCycleStrategy"/>.
	/// </summary>
	public ColorSchaffTrixTrendCycleStrategy() {
	_fastTrixLength =
		Param(nameof(FastTrixLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Fast TRIX", "Fast TRIX length", "Indicator")
		.SetOptimize(3, 15, 2);

	_slowTrixLength =
		Param(nameof(SlowTrixLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("Slow TRIX", "Slow TRIX length", "Indicator")
		.SetOptimize(8, 30, 3);

	_cycle = Param(nameof(Cycle), 10)
			 .SetGreaterThanZero()
			 .SetDisplay("Cycle", "Cycle length", "Indicator")
			 
			 .SetOptimize(5, 20, 1);

	_highLevel =
		Param(nameof(HighLevel), 20)
		.SetDisplay("High Level", "Upper threshold", "Indicator");

	_lowLevel =
		Param(nameof(LowLevel), -20)
		.SetDisplay("Low Level", "Lower threshold", "Indicator");

	_candleType =
		Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for candles", "General");

	_stopLoss =
		Param(nameof(StopLoss), 1m)
		.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

	_takeProfit =
		Param(nameof(TakeProfit), 2m)
		.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

	_buyOpen = Param(nameof(BuyOpen), true)
			   .SetDisplay("Buy Open", "Allow buy entries", "Trading");

	_sellOpen =
		Param(nameof(SellOpen), true)
		.SetDisplay("Sell Open", "Allow sell entries", "Trading");

	_buyClose =
		Param(nameof(BuyClose), true)
		.SetDisplay("Buy Close", "Allow closing long", "Trading");

	_sellClose =
		Param(nameof(SellClose), true)
		.SetDisplay("Sell Close", "Allow closing short", "Trading");

	_factor =
		Param(nameof(Factor), 0.5m)
		.SetDisplay("Factor", "Smoothing factor for STC calculations", "Indicator");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time) {
	base.OnStarted2(time);

	_stc = new SchaffTrixTrendCycle { FastLength = FastTrixLength,
					  SlowLength = SlowTrixLength,
					  Cycle = Cycle };

	var subscription = SubscribeCandles(CandleType);

	subscription.Bind(_stc, ProcessStc).Start();

	StartProtection(stopLoss: new Unit(StopLoss, UnitTypes.Percent),
			takeProfit: new Unit(TakeProfit, UnitTypes.Percent));
	}

	private void ProcessStc(ICandleMessage candle, decimal stc) {
	if (candle.State != CandleStates.Finished)
		return;

	if (_prevStc is null) {
		_prevStc = stc;
		return;
	}

	var prev = _prevStc.Value;

	var crossedUp = stc > HighLevel && prev <= HighLevel;
	var crossedDown = stc < LowLevel && prev >= LowLevel;

	if (crossedUp) {
		if (SellClose && Position < 0)
		BuyMarket();

		if (BuyOpen && Position <= 0)
		BuyMarket();
	} else if (crossedDown) {
		if (BuyClose && Position > 0)
		SellMarket();

		if (SellOpen && Position >= 0)
		SellMarket();
	}

	_prevStc = stc;
	}

	/// <summary>
	/// Schaff Trend Cycle indicator based on TRIX MACD.
	/// </summary>
	private sealed class SchaffTrixTrendCycle : BaseIndicator {
	public int FastLength { get; set; }
	public int SlowLength { get; set; }
	public int Cycle { get; set; }
	public decimal Factor { get; set; } = 0.5m;

	private Trix _fast;
	private Trix _slow;
	private Highest _macdHigh;
	private Lowest _macdLow;
	private Highest _stHigh;
	private Lowest _stLow;
	private decimal _stPrev;
	private decimal _stcPrev;
	private bool _stPass;
	private bool _stcPass;
	private bool _inited;

	public override int NumValuesToInitialize => 1;

	protected override bool CalcIsFormed() => _inited && _slow.IsFormed && _stcPass;

	private void EnsureInit()
	{
		if (_inited) return;
		_fast = new Trix(FastLength);
		_slow = new Trix(SlowLength);
		_macdHigh = new Highest { Length = Cycle };
		_macdLow = new Lowest { Length = Cycle };
		_stHigh = new Highest { Length = Cycle };
		_stLow = new Lowest { Length = Cycle };
		_inited = true;
	}

	protected override IIndicatorValue OnProcess(IIndicatorValue input) {
		EnsureInit();

		var t = input.Time;

		var fast = _fast.Process(input).ToDecimal();
		var slow = _slow.Process(input).ToDecimal();

		var macd = fast - slow;

		var macdHigh = _macdHigh.Process(new DecimalIndicatorValue(_macdHigh, macd, t) { IsFinal = input.IsFinal }).ToDecimal();
		var macdLow = _macdLow.Process(new DecimalIndicatorValue(_macdLow, macd, t) { IsFinal = input.IsFinal }).ToDecimal();

		decimal st;
		if (macdHigh - macdLow != 0)
		st = (macd - macdLow) / (macdHigh - macdLow) * 100m;
		else
		st = _stPrev;

		if (_stPass)
		st = Factor * (st - _stPrev) + _stPrev;

		_stPrev = st;
		_stPass = true;

		var stHigh = _stHigh.Process(new DecimalIndicatorValue(_stHigh, st, t) { IsFinal = input.IsFinal }).ToDecimal();
		var stLow = _stLow.Process(new DecimalIndicatorValue(_stLow, st, t) { IsFinal = input.IsFinal }).ToDecimal();

		decimal stc;
		if (stHigh - stLow != 0)
		stc = (st - stLow) / (stHigh - stLow) * 200m - 100m;
		else
		stc = _stcPrev;

		if (_stcPass)
		stc = Factor * (stc - _stcPrev) + _stcPrev;

		_stcPrev = stc;
		_stcPass = true;

		return new DecimalIndicatorValue(this, stc, t) { IsFinal = input.IsFinal };
	}

	public override void Reset() {
		_fast?.Reset();
		_slow?.Reset();
		_macdHigh?.Reset();
		_macdLow?.Reset();
		_stHigh?.Reset();
		_stLow?.Reset();
		_stPrev = 0m;
		_stcPrev = 0m;
		_stPass = false;
		_stcPass = false;
		_inited = false;
		base.Reset();
	}
	}

	/// <summary>
	/// TRIX oscillator implementation.
	/// </summary>
	private sealed class Trix : BaseIndicator {
	private readonly ExponentialMovingAverage _ema1;
	private readonly ExponentialMovingAverage _ema2;
	private readonly ExponentialMovingAverage _ema3;
	private decimal? _prev;

	public Trix(int length)
	{
		_ema1 = new ExponentialMovingAverage { Length = length };
		_ema2 = new ExponentialMovingAverage { Length = length };
		_ema3 = new ExponentialMovingAverage { Length = length };
	}

	public override int NumValuesToInitialize => 1;

	protected override bool CalcIsFormed() => _ema3.IsFormed && _prev != null;

	protected override IIndicatorValue OnProcess(IIndicatorValue input) {
		var ema1 = _ema1.Process(input);
		var ema2 = _ema2.Process(ema1);
		var ema3 = _ema3.Process(ema2);

		var value = ema3.ToDecimal();
		decimal trix = 0m;

		if (_prev != null && _prev != 0)
		trix = (value - _prev.Value) / _prev.Value;

		_prev = value;

		return new DecimalIndicatorValue(this, trix, input.Time) { IsFinal = input.IsFinal };
	}

	public override void Reset() {
		_ema1.Reset();
		_ema2.Reset();
		_ema3.Reset();
		_prev = null;
		base.Reset();
	}
	}
}