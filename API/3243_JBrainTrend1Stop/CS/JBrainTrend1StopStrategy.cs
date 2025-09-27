using System;
using System.Collections.Generic;
using System.Reflection;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader "Exp_JBrainTrend1Stop" expert advisor.
/// Detects BrainTrend reversals using ATR, Stochastic and Jurik smoothing.
/// </summary>
public class JBrainTrend1StopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _rangeDivisor;
	private readonly StrategyParam<decimal> _rangeMultiplier;
	private readonly StrategyParam<decimal> _upperThreshold;
	private readonly StrategyParam<decimal> _lowerThreshold;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _stochasticPeriod;
	private readonly StrategyParam<int> _stopDPeriod;
	private readonly StrategyParam<int> _jmaLength;
	private readonly StrategyParam<int> _jmaPhase;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;

	private AverageTrueRange _atr = null!;
	private AverageTrueRange _atrExtended = null!;
	private StochasticOscillator _stochastic = null!;
	private JurikMovingAverage _jmaHigh = null!;
	private JurikMovingAverage _jmaLow = null!;
	private JurikMovingAverage _jmaClose = null!;

	private readonly Queue<PendingSignal> _pendingSignals = new();

	private decimal? _prevJmaClose;
	private decimal? _prevPrevJmaClose;
	private int _trendState;
	private decimal? _trailStop;

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Period of the main ATR.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// Period of the Stochastic oscillator.
	/// </summary>
	public int StochasticPeriod { get => _stochasticPeriod.Value; set => _stochasticPeriod.Value = value; }

	/// <summary>
	/// Additional ATR shift used for stop calculation.
	/// </summary>
	public int StopDPeriod { get => _stopDPeriod.Value; set => _stopDPeriod.Value = value; }

	/// <summary>
	/// Jurik moving average length.
	/// </summary>
	public int JmaLength { get => _jmaLength.Value; set => _jmaLength.Value = value; }

	/// <summary>
	/// Jurik moving average phase.
	/// </summary>
	public int JmaPhase { get => _jmaPhase.Value; set => _jmaPhase.Value = value; }

	/// <summary>
	/// Number of completed bars to wait before acting on a signal.
	/// </summary>
	public int SignalBar { get => _signalBar.Value; set => _signalBar.Value = value; }

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }

	/// <summary>
	/// Divisor applied to ATR when calculating the trailing range.
	/// </summary>
	public decimal RangeDivisor { get => _rangeDivisor.Value; set => _rangeDivisor.Value = value; }

	/// <summary>
	/// Multiplier applied to the calculated trailing range.
	/// </summary>
	public decimal RangeMultiplier { get => _rangeMultiplier.Value; set => _rangeMultiplier.Value = value; }

	/// <summary>
	/// Upper threshold of the BrainTrend oscillator.
	/// </summary>
	public decimal UpperThreshold { get => _upperThreshold.Value; set => _upperThreshold.Value = value; }

	/// <summary>
	/// Lower threshold of the BrainTrend oscillator.
	/// </summary>
	public decimal LowerThreshold { get => _lowerThreshold.Value; set => _lowerThreshold.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="JBrainTrend1StopStrategy"/> class.
	/// </summary>
	public JBrainTrend1StopStrategy()
	{
		_rangeDivisor = Param(nameof(RangeDivisor), 2.3m)
			.SetGreaterThanZero()
			.SetDisplay("Range Divisor", "Divisor applied to ATR when calculating the trail range", "Indicator");

		_rangeMultiplier = Param(nameof(RangeMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Range Multiplier", "Multiplier applied to the calculated range", "Indicator");

		_upperThreshold = Param(nameof(UpperThreshold), 53m)
			.SetDisplay("Upper Threshold", "BrainTrend oscillator upper threshold", "Indicator");

		_lowerThreshold = Param(nameof(LowerThreshold), 47m)
			.SetDisplay("Lower Threshold", "BrainTrend oscillator lower threshold", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for indicator calculations", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 7)
			.SetDisplay("ATR Period", "Length of the base Average True Range", "Indicator")
			.SetCanOptimize(true);

		_stochasticPeriod = Param(nameof(StochasticPeriod), 9)
			.SetDisplay("Stochastic Period", "Length of the Stochastic oscillator", "Indicator")
			.SetCanOptimize(true);

		_stopDPeriod = Param(nameof(StopDPeriod), 3)
			.SetDisplay("ATR Shift", "Additional bars added to the stop ATR period", "Indicator")
			.SetCanOptimize(true);

		_jmaLength = Param(nameof(JmaLength), 7)
			.SetDisplay("JMA Length", "Length of the Jurik moving averages", "Indicator")
			.SetCanOptimize(true);

		_jmaPhase = Param(nameof(JmaPhase), 100)
			.SetDisplay("JMA Phase", "Phase forwarded to the Jurik moving averages", "Indicator")
			.SetCanOptimize(true);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Delay", "Number of completed bars before acting", "Trading")
			.SetCanOptimize(true);

		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Allow Long Entry", "Enable opening long positions", "Trading");

		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Allow Short Entry", "Enable opening short positions", "Trading");

		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Allow Long Exit", "Enable closing long positions", "Trading");

		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Allow Short Exit", "Enable closing short positions", "Trading");
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

		_pendingSignals.Clear();
		_prevJmaClose = null;
		_prevPrevJmaClose = null;
		_trendState = 0;
		_trailStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_atrExtended = new AverageTrueRange { Length = AtrPeriod + Math.Max(0, StopDPeriod) };
		_stochastic = new StochasticOscillator
		{
			Length = StochasticPeriod,
			K = { Length = 1 },
			D = { Length = StochasticPeriod }
		};

		_jmaHigh = CreateJurik(JmaLength, JmaPhase);
		_jmaLow = CreateJurik(JmaLength, JmaPhase);
		_jmaClose = CreateJurik(JmaLength, JmaPhase);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(new IIndicator[] { _atr, _atrExtended, _stochastic }, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _jmaClose);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!TryGetIndicatorData(candle, values, out var data))
			return;

		ProcessPendingSignals();

		var signal = UpdateState(data);
		if (signal == SignalType.None)
			return;

		if (SignalBar <= 0)
		{
			ExecuteSignal(signal);
		}
		else
		{
			_pendingSignals.Enqueue(new PendingSignal(signal, SignalBar));
		}
	}

	private bool TryGetIndicatorData(ICandleMessage candle, IReadOnlyList<IIndicatorValue> values, out IndicatorData data)
	{
		data = default;

		if (values.Count < 3)
			return false;

		var atrValue = values[0];
		if (!atrValue.IsFinal)
			return false;

		var atr = atrValue.GetValue<decimal>();

		var atrExtendedValue = values[1];
		if (!atrExtendedValue.IsFinal)
			return false;

		var atrExtended = atrExtendedValue.GetValue<decimal>();

		if (values[2] is not StochasticOscillatorValue stochValue || !stochValue.IsFinal)
			return false;

		if (stochValue.K is not decimal stochastic)
			return false;

		var highValue = _jmaHigh.Process(new CandleIndicatorValue(candle, candle.HighPrice));
		var lowValue = _jmaLow.Process(new CandleIndicatorValue(candle, candle.LowPrice));
		var closeValue = _jmaClose.Process(new CandleIndicatorValue(candle, candle.ClosePrice));

		if (!highValue.IsFinal || !lowValue.IsFinal || !closeValue.IsFinal)
			return false;

		var jmaHigh = highValue.GetValue<decimal>();
		var jmaLow = lowValue.GetValue<decimal>();
		var jmaClose = closeValue.GetValue<decimal>();

		data = new IndicatorData(atr, atrExtended, stochastic, jmaHigh, jmaLow, jmaClose);
		return true;
	}

	private void ProcessPendingSignals()
	{
		if (_pendingSignals.Count == 0)
			return;

		var count = _pendingSignals.Count;
		for (var i = 0; i < count; i++)
		{
			var pending = _pendingSignals.Dequeue();
			var remaining = pending.RemainingBars - 1;

			if (remaining <= 0)
			{
				ExecuteSignal(pending.Type);
			}
			else
			{
				_pendingSignals.Enqueue(pending with { RemainingBars = remaining });
			}
		}
	}

	private SignalType UpdateState(IndicatorData data)
	{
		if (_prevJmaClose is null)
		{
			_prevJmaClose = data.JmaClose;
			return SignalType.None;
		}

		if (_prevPrevJmaClose is null)
		{
			_prevPrevJmaClose = _prevJmaClose;
			_prevJmaClose = data.JmaClose;
			return SignalType.None;
		}

		var signal = SignalType.None;
		var range = data.Atr / RangeDivisor;
		var range1 = data.AtrExtended * RangeMultiplier;
		var val3 = Math.Abs(data.JmaClose - _prevPrevJmaClose.Value);

		if (range > 0m && val3 > range)
		{
			if (data.Stochastic < LowerThreshold && _trendState != -1)
			{
				_trendState = -1;
				_trailStop = data.JmaHigh + range1 / 4m;
				signal = SignalType.Sell;
			}
			else if (data.Stochastic > UpperThreshold && _trendState != 1)
			{
				_trendState = 1;
				_trailStop = data.JmaLow - range1 / 4m;
				signal = SignalType.Buy;
			}
		}
		else if (_trendState == -1)
		{
			var candidate = data.JmaHigh + range1;
			if (_trailStop is null || candidate < _trailStop)
				_trailStop = candidate;
		}
		else if (_trendState == 1)
		{
			var candidate = data.JmaLow - range1;
			if (_trailStop is null || candidate > _trailStop)
				_trailStop = candidate;
		}

		_prevPrevJmaClose = _prevJmaClose;
		_prevJmaClose = data.JmaClose;

		return signal;
	}

	private void ExecuteSignal(SignalType type)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		switch (type)
		{
			case SignalType.Buy:
				if (SellClose && Position < 0)
					BuyMarket(Math.Abs(Position));

				if (BuyOpen && Position <= 0)
					BuyMarket();
				break;

			case SignalType.Sell:
				if (BuyClose && Position > 0)
					SellMarket(Position);

				if (SellOpen && Position >= 0)
					SellMarket();
				break;
		}
	}

	private static JurikMovingAverage CreateJurik(int length, int phase)
	{
		var jurik = new JurikMovingAverage { Length = Math.Max(1, length) };
		var property = jurik.GetType().GetProperty("Phase", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (property != null && property.CanWrite)
		{
			var clamped = Math.Max(-100, Math.Min(100, phase));
			property.SetValue(jurik, clamped);
		}

		return jurik;
	}

	private readonly record struct IndicatorData(decimal Atr, decimal AtrExtended, decimal Stochastic, decimal JmaHigh, decimal JmaLow, decimal JmaClose);

	private readonly record struct PendingSignal(SignalType Type, int RemainingBars);

	private enum SignalType
	{
		None,
		Buy,
		Sell
	}
}
