using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the RSI of moving average indicator (RSIOMA).
/// Trades according to selected mode using RSI and its moving average.
/// </summary>
public class ExpRsiomaStrategy : Strategy
{
	private readonly StrategyParam<AlgMode> _mode;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _highLevel;
	private readonly StrategyParam<int> _lowLevel;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private decimal _prevSignal;
	private decimal _prevHist;
	private decimal _prevHist2;
	private decimal _prevSignal2;

	public AlgMode Mode { get => _mode.Value; set => _mode.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int SignalPeriod { get => _signalPeriod.Value; set => _signalPeriod.Value = value; }
	public int HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public int LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }
	public bool BuyPosOpen { get => _buyPosOpen.Value; set => _buyPosOpen.Value = value; }
	public bool SellPosOpen { get => _sellPosOpen.Value; set => _sellPosOpen.Value = value; }
	public bool BuyPosClose { get => _buyPosClose.Value; set => _buyPosClose.Value = value; }
	public bool SellPosClose { get => _sellPosClose.Value; set => _sellPosClose.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ExpRsiomaStrategy()
	{
		_mode = Param(nameof(Mode), AlgMode.Breakdown)
			.SetDisplay("Mode", "Algorithm mode", "Parameters");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI calculation length", "Parameters")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_signalPeriod = Param(nameof(SignalPeriod), 21)
			.SetDisplay("Signal Period", "Moving average length", "Parameters")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_highLevel = Param(nameof(HighLevel), 20)
			.SetDisplay("High Level", "Upper threshold", "Parameters");

		_lowLevel = Param(nameof(LowLevel), -20)
			.SetDisplay("Low Level", "Lower threshold", "Parameters");

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Buy Open", "Allow opening long positions", "Trading");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Sell Open", "Allow opening short positions", "Trading");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Buy Close", "Allow closing long positions", "Trading");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Sell Close", "Allow closing short positions", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsioma = new RsiOmaIndicator
		{
			RsiPeriod = RsiPeriod,
			SignalPeriod = SignalPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(rsioma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (!value.IsFinal || candle.State != CandleStates.Finished)
		return;

		var rsiomaValue = (RsiOmaValue)value;
		var rsi = rsiomaValue.Rsi;
		var signal = rsiomaValue.Signal;
		var hist = rsiomaValue.Hist;

		var buyOpen = false;
		var sellOpen = false;
		var buyClose = false;
		var sellClose = false;

		switch (Mode)
		{
		case AlgMode.Breakdown:
		if (BuyPosOpen && _prevRsi <= HighLevel && rsi > HighLevel)
		buyOpen = true;
		if (SellPosOpen && _prevRsi >= LowLevel && rsi < LowLevel)
		sellOpen = true;
		if (SellPosClose && _prevRsi >= HighLevel && rsi > HighLevel)
		sellClose = true;
		if (BuyPosClose && _prevRsi <= LowLevel && rsi < LowLevel)
		buyClose = true;
		break;

		case AlgMode.HistTwist:
		if (BuyPosOpen && _prevHist < _prevHist2 && hist > _prevHist)
		buyOpen = true;
		if (SellPosOpen && _prevHist > _prevHist2 && hist < _prevHist)
		sellOpen = true;
		if (SellPosClose && _prevHist < _prevHist2 && hist > _prevHist)
		sellClose = true;
		if (BuyPosClose && _prevHist > _prevHist2 && hist < _prevHist)
		buyClose = true;
		break;

		case AlgMode.SignalTwist:
		if (BuyPosOpen && _prevSignal < _prevSignal2 && signal > _prevSignal)
		buyOpen = true;
		if (SellPosOpen && _prevSignal > _prevSignal2 && signal < _prevSignal)
		sellOpen = true;
		if (SellPosClose && _prevSignal < _prevSignal2 && signal > _prevSignal)
		sellClose = true;
		if (BuyPosClose && _prevSignal > _prevSignal2 && signal < _prevSignal)
		buyClose = true;
		break;

		case AlgMode.HistDisposition:
		if (BuyPosOpen && _prevHist > _prevSignal && hist <= signal)
		buyOpen = true;
		if (SellPosOpen && _prevHist < _prevSignal && hist >= signal)
		sellOpen = true;
		if (SellPosClose && _prevHist > _prevSignal && hist <= signal)
		sellClose = true;
		if (BuyPosClose && _prevHist < _prevSignal && hist >= signal)
		buyClose = true;
		break;
		}

		if (buyClose && Position > 0)
		SellMarket(Position);
		if (sellClose && Position < 0)
		BuyMarket(Math.Abs(Position));

		if (buyOpen && Position <= 0)
		BuyMarket(Volume + Math.Abs(Position));
		if (sellOpen && Position >= 0)
		SellMarket(Volume + Math.Abs(Position));

		_prevHist2 = _prevHist;
		_prevHist = hist;
		_prevSignal2 = _prevSignal;
		_prevSignal = signal;
		_prevRsi = rsi;
	}
}

/// <summary>
/// Mode of RSIOMA signal processing.
/// </summary>
public enum AlgMode
{
	/// <summary>
	/// Open on level breakout.
	/// </summary>
	Breakdown,

	/// <summary>
	/// Histogram direction change.
	/// </summary>
	HistTwist,

	/// <summary>
	/// Signal line direction change.
	/// </summary>
	SignalTwist,

	/// <summary>
	/// Cross between histogram and signal line.
	/// </summary>
	HistDisposition
}

/// <summary>
/// Indicator calculating RSI of moving average with signal and histogram.
/// </summary>
public class RsiOmaIndicator : BaseIndicator<decimal>
{
	public int RsiPeriod { get; set; } = 14;
	public int SignalPeriod { get; set; } = 21;

	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _signal;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
		return new DecimalIndicatorValue(this, default, input.Time);

		_rsi ??= new RelativeStrengthIndex { Length = RsiPeriod };
		_signal ??= new ExponentialMovingAverage { Length = SignalPeriod };

		var rsiVal = _rsi.Process(input);
		if (!rsiVal.IsFinal)
		return new DecimalIndicatorValue(this, default, input.Time);

		var signalVal = _signal.Process(new DecimalIndicatorValue(_signal, rsiVal.ToDecimal(), input.Time));
		if (!signalVal.IsFinal)
		return new DecimalIndicatorValue(this, default, input.Time);

		var rsi = rsiVal.ToDecimal();
		var signal = signalVal.ToDecimal();
		var hist = rsi - signal;

		return new RsiOmaValue(this, input, rsi, signal, hist);
	}
}

/// <summary>
/// Indicator value for <see cref="RsiOmaIndicator"/>.
/// </summary>
public class RsiOmaValue : ComplexIndicatorValue
{
	public RsiOmaValue(IIndicator indicator, IIndicatorValue input, decimal rsi, decimal signal, decimal hist)
	: base(indicator, input, (nameof(Rsi), rsi), (nameof(Signal), signal), (nameof(Hist), hist))
	{
	}

	/// <summary>
	/// RSI value.
	/// </summary>
	public decimal Rsi => (decimal)GetValue(nameof(Rsi));

	/// <summary>
	/// Signal line value.
	/// </summary>
	public decimal Signal => (decimal)GetValue(nameof(Signal));

	/// <summary>
	/// Histogram value.
	/// </summary>
	public decimal Hist => (decimal)GetValue(nameof(Hist));
}
