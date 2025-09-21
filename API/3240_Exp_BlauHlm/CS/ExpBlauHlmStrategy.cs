using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the Exp_BlauHLM expert advisor.
/// The strategy analyses a smoothed Blau HLM oscillator and reacts to
/// three operating modes from the original MQL implementation.
/// </summary>
public class ExpBlauHlmStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<SmoothMethod> _smoothingMethod;
	private readonly StrategyParam<int> _xLength;
	private readonly StrategyParam<int> _firstLength;
	private readonly StrategyParam<int> _secondLength;
	private readonly StrategyParam<int> _thirdLength;
	private readonly StrategyParam<int> _fourthLength;
	private readonly StrategyParam<int> _phase;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<Mode> _mode;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	
	private BlauHlmCalculator? _calculator;
	/// <summary>
	/// Enumeration of available smoothing techniques.
	/// Unsupported options from the original library fall back to EMA.
	/// </summary>
	public enum SmoothMethod
	{
		Simple,
		Exponential,
		Smoothed,
		LinearWeighted,
		Jurik,
		TripleExponential,
		Adaptive,
	}
	
	/// <summary>
	/// Operating modes reproduced from the expert advisor.
	/// </summary>
	public enum Mode
	{
		Breakdown,
		Twist,
		CloudTwist,
	}
	
	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// XLength parameter controlling the raw HLM span.
	/// </summary>
	public int XLength
	{
		get => _xLength.Value;
		set => _xLength.Value = value;
	}
	
	/// <summary>
	/// Length of the first smoothing stage.
	/// </summary>
	public int FirstLength
	{
		get => _firstLength.Value;
		set => _firstLength.Value = value;
	}
	
	/// <summary>
	/// Length of the second smoothing stage.
	/// </summary>
	public int SecondLength
	{
		get => _secondLength.Value;
		set => _secondLength.Value = value;
	}
	
	/// <summary>
	/// Length of the third smoothing stage.
	/// </summary>
	public int ThirdLength
	{
		get => _thirdLength.Value;
		set => _thirdLength.Value = value;
	}
	
	/// <summary>
	/// Length of the final smoothing stage.
	/// </summary>
	public int FourthLength
	{
		get => _fourthLength.Value;
		set => _fourthLength.Value = value;
	}
	
	/// <summary>
	/// Jurik phase parameter forwarded to supported smoothers.
	/// </summary>
	public int Phase
	{
		get => _phase.Value;
		set => _phase.Value = value;
	}
	
	/// <summary>
	/// Signal bar offset taken from the original expert.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}
	
	/// <summary>
	/// Operating mode of the strategy.
	/// </summary>
	public Mode EntryMode
	{
		get => _entryMode.Value;
		set => _entryMode.Value = value;
	}
	
	/// <summary>
	/// Selected smoothing method.
	/// </summary>
	public SmoothMethod SmoothingMethod
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}
	
	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyOpen
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}
	
	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellOpen
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}
	
	/// <summary>
	/// Allow closing long positions on opposite signals.
	/// </summary>
	public bool BuyClose
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}
	
	/// <summary>
	/// Allow closing short positions on opposite signals.
	/// </summary>
	public bool SellClose
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}
	/// <summary>
	/// Initializes a new instance of <see cref="ExpBlauHlmStrategy"/>.
	/// </summary>
	public ExpBlauHlmStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for the oscillator", "Data");
	
		_smoothingMethod = Param(nameof(SmoothingMethod), SmoothMethod.Exponential)
			.SetDisplay("Smoothing", "XMA smoothing mode", "Indicator");
	
		_xLength = Param(nameof(XLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("XLength", "Base HLM difference span", "Indicator");
	
		_firstLength = Param(nameof(FirstLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("First Length", "First smoothing period", "Indicator");
	
		_secondLength = Param(nameof(SecondLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Second Length", "Second smoothing period", "Indicator");
	
		_thirdLength = Param(nameof(ThirdLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Third Length", "Third smoothing period", "Indicator");
	
		_fourthLength = Param(nameof(FourthLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fourth Length", "Signal smoothing period", "Indicator");
	
		_phase = Param(nameof(Phase), 15)
			.SetDisplay("Phase", "Phase parameter for Jurik-like filters", "Indicator");
	
		_signalBar = Param(nameof(SignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Signal Bar", "Offset applied to historical values", "Trading");
	
		_mode = Param(nameof(EntryMode), Mode.Twist)
			.SetDisplay("Mode", "Trading logic used for entries", "Trading");
	
		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Allow Long", "Enable long entries", "Trading");
	
		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Allow Short", "Enable short entries", "Trading");
	
		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Close Long", "Close long positions on opposite signals", "Trading");
	
		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Close Short", "Close short positions on opposite signals", "Trading");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
	
		_calculator = null;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
	
		_calculator = new BlauHlmCalculator(
			SmoothingMethod,
			XLength,
			FirstLength,
			SecondLength,
			ThirdLength,
			FourthLength,
			Phase);
	
		_calculator.SetHistoryLimit(Math.Max(SignalBar + 4, 8));
	
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_calculator is null)
		return;

		var point = Security?.PriceStep ?? 0m;

		if (!_calculator.Process(candle, point))
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var buyOpen = false;
		var sellOpen = false;
		var buyClose = false;
		var sellClose = false;

		switch (EntryMode)
		{
			case Mode.Breakdown:
			{
				if (!_calculator.TryGetHistogram(SignalBar, out var hist0) ||
					!_calculator.TryGetHistogram(SignalBar + 1, out var hist1))
				return;

				if (hist1 > 0m)
				{
					if (BuyOpen && hist0 <= 0m)
						buyOpen = true;

					if (SellClose)
						sellClose = true;
				}

				if (hist1 < 0m)
				{
					if (SellOpen && hist0 >= 0m)
						sellOpen = true;

					if (BuyClose)
						buyClose = true;
				}

				break;
			}

			case Mode.Twist:
			{
				if (!_calculator.TryGetHistogram(SignalBar, out var hist0) ||
					!_calculator.TryGetHistogram(SignalBar + 1, out var hist1) ||
					!_calculator.TryGetHistogram(SignalBar + 2, out var hist2))
				return;

				if (hist1 < hist2)
				{
					if (BuyOpen && hist0 > hist1)
						buyOpen = true;

					if (SellClose)
						sellClose = true;
				}

				if (hist1 > hist2)
				{
					if (SellOpen && hist0 < hist1)
						sellOpen = true;

					if (BuyClose)
						buyClose = true;
				}

				break;
			}

			case Mode.CloudTwist:
			{
				if (!_calculator.TryGetUpSeries(SignalBar, out var up0) ||
					!_calculator.TryGetUpSeries(SignalBar + 1, out var up1) ||
					!_calculator.TryGetDownSeries(SignalBar, out var dn0) ||
					!_calculator.TryGetDownSeries(SignalBar + 1, out var dn1))
				return;

				if (up1 > dn1)
				{
					if (BuyOpen && up0 <= dn0)
						buyOpen = true;

					if (SellClose)
						sellClose = true;
				}

				if (up1 < dn1)
				{
					if (SellOpen && up0 >= dn0)
						sellOpen = true;

					if (BuyClose)
						buyClose = true;
				}

				break;
			}
		}

		if (buyClose && Position > 0m)
			SellMarket(Position);

		if (sellClose && Position < 0m)
			BuyMarket(-Position);

		var volume = Volume;

		if (volume <= 0m)
			return;

		if (buyOpen && Position <= 0m)
			BuyMarket(volume);

		if (sellOpen && Position >= 0m)
			SellMarket(volume);
	}

	private sealed class BlauHlmCalculator
	{
		private readonly SmoothMethod _method;
		private readonly int _xLength;
		private readonly int _phase;
		private readonly LengthIndicator<decimal> _ma1;
		private readonly LengthIndicator<decimal> _ma2;
		private readonly LengthIndicator<decimal> _ma3;
		private readonly LengthIndicator<decimal> _ma4;
		private readonly Queue<decimal> _highWindow = new();
		private readonly Queue<decimal> _lowWindow = new();
		private readonly List<decimal> _histogram = new();
		private readonly List<decimal> _upSeries = new();
		private readonly List<decimal> _downSeries = new();
		private int _historyLimit = 16;

		public BlauHlmCalculator(
			SmoothMethod method,
			int xLength,
			int firstLength,
			int secondLength,
			int thirdLength,
			int fourthLength,
			int phase)
		{
			_method = method;
			_xLength = Math.Max(1, xLength);
			_phase = Math.Max(-100, Math.Min(100, phase));

			_ma1 = CreateMovingAverage(method, Math.Max(1, firstLength), _phase);
			_ma2 = CreateMovingAverage(method, Math.Max(1, secondLength), _phase);
			_ma3 = CreateMovingAverage(method, Math.Max(1, thirdLength), _phase);
			_ma4 = CreateMovingAverage(method, Math.Max(1, fourthLength), _phase);
		}

		public void SetHistoryLimit(int limit)
		{
			_historyLimit = Math.Max(4, limit);
		}

		public bool Process(ICandleMessage candle, decimal point)
		{
			_highWindow.Enqueue(candle.HighPrice);
			_lowWindow.Enqueue(candle.LowPrice);

			if (_highWindow.Count > _xLength)
				_highWindow.Dequeue();

			if (_lowWindow.Count > _xLength)
				_lowWindow.Dequeue();

			if (_highWindow.Count < _xLength || _lowWindow.Count < _xLength)
				return false;

			var previousHigh = _highWindow.Peek();
			var previousLow = _lowWindow.Peek();

			var hmu = candle.HighPrice - previousHigh;
			var lmd = previousLow - candle.LowPrice;

			if (hmu < 0m)
				hmu = 0m;

			if (lmd < 0m)
				lmd = 0m;

			var hlm = hmu - lmd;

			if (point > 0m)
				hlm /= point;

			var time = candle.OpenTime;

			var stage1 = _ma1.Process(hlm, time, true).ToDecimal();
			var stage2 = _ma2.Process(stage1, time, true).ToDecimal();
			var stage3 = _ma3.Process(stage2, time, true).ToDecimal();
			var signal = _ma4.Process(stage3, time, true).ToDecimal();

			if (!_ma4.IsFormed)
				return false;

			_histogram.Add(stage3);
			_upSeries.Add(stage3);
			_downSeries.Add(signal);

			TrimHistory(_histogram);
			TrimHistory(_upSeries);
			TrimHistory(_downSeries);

			return true;
		}

		public bool TryGetHistogram(int shift, out decimal value)
			=> TryGetValue(_histogram, shift, out value);

		public bool TryGetUpSeries(int shift, out decimal value)
			=> TryGetValue(_upSeries, shift, out value);

		public bool TryGetDownSeries(int shift, out decimal value)
			=> TryGetValue(_downSeries, shift, out value);

		private void TrimHistory(List<decimal> list)
		{
			var excess = list.Count - _historyLimit;
			if (excess > 0)
				list.RemoveRange(0, excess);
		}

		private static bool TryGetValue(List<decimal> list, int shift, out decimal value)
		{
			var index = list.Count - 1 - shift;
			if (index < 0)
			{
				value = default;
				return false;
			}

			value = list[index];
			return true;
		}

		private LengthIndicator<decimal> CreateMovingAverage(SmoothMethod method, int length, int phase)
		{
			return method switch
			{
				SmoothMethod.Simple => new SimpleMovingAverage { Length = length },
				SmoothMethod.Exponential => new ExponentialMovingAverage { Length = length },
				SmoothMethod.Smoothed => new SmoothedMovingAverage { Length = length },
				SmoothMethod.LinearWeighted => new WeightedMovingAverage { Length = length },
				SmoothMethod.Jurik => new JurikMovingAverage { Length = length, Phase = phase },
				SmoothMethod.TripleExponential => new TripleExponentialMovingAverage { Length = length },
				SmoothMethod.Adaptive => new KaufmanAdaptiveMovingAverage { Length = length },
				_ => new ExponentialMovingAverage { Length = length },
			};
		}
	}
}
