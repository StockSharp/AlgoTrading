using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

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
	/// Initializes a new instance of <see
	/// cref="ColorSchaffTrixTrendCycleStrategy"/>.
	/// </summary>
	public ColorSchaffTrixTrendCycleStrategy() {
	_fastTrixLength =
		Param(nameof(FastTrixLength), 23)
		.SetGreaterThanZero()
		.SetDisplay("Fast TRIX", "Fast TRIX length", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 5);

	_slowTrixLength =
		Param(nameof(SlowTrixLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("Slow TRIX", "Slow TRIX length", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(40, 100, 5);

	_cycle = Param(nameof(Cycle), 10)
			 .SetGreaterThanZero()
			 .SetDisplay("Cycle", "Cycle length", "Indicator")
			 .SetCanOptimize(true)
			 .SetOptimize(5, 20, 1);

	_highLevel =
		Param(nameof(HighLevel), 60)
		.SetDisplay("High Level", "Upper threshold", "Indicator");

	_lowLevel =
		Param(nameof(LowLevel), -60)
		.SetDisplay("Low Level", "Lower threshold", "Indicator");

	_candleType =
		Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromHours(4)))
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
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time) {
	base.OnStarted(time);

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
	private sealed class SchaffTrixTrendCycle : IndicatorBase<decimal> {
	public int FastLength { get; set; }
	public int SlowLength { get; set; }
	public int Cycle { get; set; }

	private readonly Trix _fast = new();
	private readonly Trix _slow = new();
	private readonly Highest _macdHigh = new();
	private readonly Lowest _macdLow = new();
	private readonly Highest _stHigh = new();
	private readonly Lowest _stLow = new();
	private decimal _stPrev;
	private decimal _stcPrev;
	private bool _stPass;
	private bool _stcPass;
	private const decimal Factor = 0.5m;

	public override bool IsFormed => _slow.IsFormed && _stcPass;

	protected override IIndicatorValue OnProcess(IIndicatorValue input) {
		_fast.Length = FastLength;
		_slow.Length = SlowLength;
		_macdHigh.Length = Cycle;
		_macdLow.Length = Cycle;
		_stHigh.Length = Cycle;
		_stLow.Length = Cycle;

		var fast = _fast.Process(input).GetValue<decimal>();
		var slow = _slow.Process(input).GetValue<decimal>();

		var macd = fast - slow;

		var macdHigh =
		_macdHigh.Process(new DecimalIndicatorValue(this, macd))
			.GetValue<decimal>();
		var macdLow =
		_macdLow.Process(new DecimalIndicatorValue(this, macd))
			.GetValue<decimal>();

		decimal st;
		if (macdHigh - macdLow != 0)
		st = (macd - macdLow) / (macdHigh - macdLow) * 100m;
		else
		st = _stPrev;

		if (_stPass)
		st = Factor * (st - _stPrev) + _stPrev;

		_stPrev = st;
		_stPass = true;

		var stHigh = _stHigh.Process(new DecimalIndicatorValue(this, st))
				 .GetValue<decimal>();
		var stLow = _stLow.Process(new DecimalIndicatorValue(this, st))
				.GetValue<decimal>();

		decimal stc;
		if (stHigh - stLow != 0)
		stc = (st - stLow) / (stHigh - stLow) * 200m - 100m;
		else
		stc = _stcPrev;

		if (_stcPass)
		stc = Factor * (stc - _stcPrev) + _stcPrev;

		_stcPrev = stc;
		_stcPass = true;

		return new DecimalIndicatorValue(this, stc);
	}

	public override void Reset() {
		_fast.Reset();
		_slow.Reset();
		_macdHigh.Reset();
		_macdLow.Reset();
		_stHigh.Reset();
		_stLow.Reset();
		_stPrev = 0m;
		_stcPrev = 0m;
		_stPass = false;
		_stcPass = false;
		base.Reset();
	}
	}

	/// <summary>
	/// TRIX oscillator implementation.
	/// </summary>
	private sealed class Trix : IndicatorBase<decimal> {
	private readonly ExponentialMovingAverage _ema1 = new();
	private readonly ExponentialMovingAverage _ema2 = new();
	private readonly ExponentialMovingAverage _ema3 = new();
	private decimal? _prev;

	public int Length { get; set; }

	public override bool IsFormed => _ema3.IsFormed && _prev != null;

	protected override IIndicatorValue OnProcess(IIndicatorValue input) {
		_ema1.Length = Length;
		_ema2.Length = Length;
		_ema3.Length = Length;

		var ema1 = _ema1.Process(input);
		var ema2 = _ema2.Process(ema1);
		var ema3 = _ema3.Process(ema2);

		var value = ema3.GetValue<decimal>();
		decimal trix = 0m;

		if (_prev != null && _prev != 0)
		trix = (value - _prev.Value) / _prev.Value;

		_prev = value;

		return new DecimalIndicatorValue(this, trix);
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
