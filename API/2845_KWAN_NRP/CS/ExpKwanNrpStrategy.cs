using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy replicating the Exp KWAN NRP expert advisor by combining stochastic, RSI, and momentum ratios.
/// </summary>
public class ExpKwanNrpStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowingPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<SmoothingMethodOption> _smoothingMethod;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _enableBuyEntries;
	private readonly StrategyParam<bool> _enableSellEntries;
	private readonly StrategyParam<bool> _enableBuyExits;
	private readonly StrategyParam<bool> _enableSellExits;
	private readonly StrategyParam<bool> _useProtection;
	private readonly StrategyParam<int> _stopLossSteps;
	private readonly StrategyParam<int> _takeProfitSteps;

	private RelativeStrengthIndex _rsi;
	private StochasticOscillator _stochastic;
	private Momentum _momentum;
	private IIndicator _smoother;
	private readonly List<decimal> _kwanHistory = new();

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D period.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic slowing parameter.
	/// </summary>
	public int SlowingPeriod
	{
		get => _slowingPeriod.Value;
		set => _slowingPeriod.Value = value;
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
	/// Momentum period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Length of the smoothing moving average.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Moving average type used to smooth the KWAN ratio.
	/// </summary>
	public SmoothingMethodOption SmoothingMethod
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Bar offset used when evaluating signals.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Enable long entries when the oscillator turns upward.
	/// </summary>
	public bool EnableBuyEntries
	{
		get => _enableBuyEntries.Value;
		set => _enableBuyEntries.Value = value;
	}

	/// <summary>
	/// Enable short entries when the oscillator turns downward.
	/// </summary>
	public bool EnableSellEntries
	{
		get => _enableSellEntries.Value;
		set => _enableSellEntries.Value = value;
	}

	/// <summary>
	/// Allow closing existing long positions on bearish signals.
	/// </summary>
	public bool EnableBuyExits
	{
		get => _enableBuyExits.Value;
		set => _enableBuyExits.Value = value;
	}

	/// <summary>
	/// Allow closing existing short positions on bullish signals.
	/// </summary>
	public bool EnableSellExits
	{
		get => _enableSellExits.Value;
		set => _enableSellExits.Value = value;
	}

	/// <summary>
	/// Enable stop-loss and take-profit protection in price steps.
	/// </summary>
	public bool UseProtection
	{
		get => _useProtection.Value;
		set => _useProtection.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price steps.
	/// </summary>
	public int StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price steps.
	/// </summary>
	public int TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ExpKwanNrpStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_kPeriod = Param(nameof(KPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "Stochastic %K period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 21, 2);

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "Stochastic %D period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 9, 1);

		_slowingPeriod = Param(nameof(SlowingPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Slowing", "Smoothing applied to %K", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 9, 1);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 1);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Momentum length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 1);

		_smoothingMethod = Param(nameof(SmoothingMethod), SmoothingMethodOption.Simple)
			.SetDisplay("Smoothing Method", "Type of moving average", "Indicators");

		_smoothingLength = Param(nameof(SmoothingLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "Moving average length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2, 15, 1);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Signal Bar", "Bar offset used for signals", "Trading");

		_enableBuyEntries = Param(nameof(EnableBuyEntries), true)
			.SetDisplay("Enable Buy Entries", "Allow long entries", "Trading");

		_enableSellEntries = Param(nameof(EnableSellEntries), true)
			.SetDisplay("Enable Sell Entries", "Allow short entries", "Trading");

		_enableBuyExits = Param(nameof(EnableBuyExits), true)
			.SetDisplay("Enable Buy Exits", "Allow closing longs", "Trading");

		_enableSellExits = Param(nameof(EnableSellExits), true)
			.SetDisplay("Enable Sell Exits", "Allow closing shorts", "Trading");

		_useProtection = Param(nameof(UseProtection), true)
			.SetDisplay("Use Protection", "Enable stop-loss/take-profit", "Risk");

		_stopLossSteps = Param(nameof(StopLossSteps), 1000)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Stop-Loss Steps", "Stop-loss distance in steps", "Risk");

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 2000)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Take-Profit Steps", "Take-profit distance in steps", "Risk");
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
		_kwanHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		_stochastic = new StochasticOscillator
		{
			Length = KPeriod
		};

		_stochastic.K.Length = Math.Max(1, SlowingPeriod);
		_stochastic.D.Length = Math.Max(1, DPeriod);

		_momentum = new Momentum
		{
			Length = MomentumPeriod
		};

		_smoother = CreateSmoother(SmoothingMethod, SmoothingLength);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(_rsi, _stochastic, _momentum, ProcessIndicators)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _smoother);
			DrawOwnTrades(area);
		}

		if (UseProtection && (StopLossSteps > 0 || TakeProfitSteps > 0))
		{
			StartProtection(
				takeProfit: TakeProfitSteps > 0 ? new Unit(TakeProfitSteps, UnitTypes.Step) : default,
				stopLoss: StopLossSteps > 0 ? new Unit(StopLossSteps, UnitTypes.Step) : default);
		}
	}

	private void ProcessIndicators(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue stochValue, IIndicatorValue momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!rsiValue.IsFinal || !stochValue.IsFinal || !momentumValue.IsFinal)
			return;

		var rsi = rsiValue.ToDecimal();
		var stoch = (StochasticOscillatorValue)stochValue;

		if (stoch.D is not decimal stochSignal)
			return;

		var momentum = momentumValue.ToDecimal();

		if (momentum == 0m)
			return;

		// Combine indicators into the KWAN ratio before smoothing.
		var rawKwan = stochSignal * rsi / momentum;

		// Smooth the ratio with the selected moving average.
		var smoothedValue = _smoother.Process(rawKwan, candle.OpenTime, true);

		if (!smoothedValue.IsFinal)
			return;

		var kwan = smoothedValue.ToDecimal();

		_kwanHistory.Insert(0, kwan);

		var maxHistory = SignalBar + 2;
		if (_kwanHistory.Count > maxHistory)
			_kwanHistory.RemoveAt(_kwanHistory.Count - 1);

		if (_kwanHistory.Count <= SignalBar + 1)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var signalValue = _kwanHistory[SignalBar];
		var previousValue = _kwanHistory[SignalBar + 1];

		var isUpSignal = signalValue > previousValue;
		var isDownSignal = signalValue < previousValue;

		if (isUpSignal)
		{
			if (EnableSellExits && Position < 0)
			{
				// Close short positions when momentum turns bullish.
				BuyMarket(Math.Abs(Position));
			}

			if (EnableBuyEntries && Position <= 0)
			{
				// Open or flip into a long position on bullish confirmation.
				BuyMarket(Volume + Math.Abs(Position));
			}
		}
		else if (isDownSignal)
		{
			if (EnableBuyExits && Position > 0)
			{
				// Close long positions when momentum turns bearish.
				SellMarket(Math.Abs(Position));
			}

			if (EnableSellEntries && Position >= 0)
			{
				// Open or flip into a short position on bearish confirmation.
				SellMarket(Volume + Math.Abs(Position));
			}
		}
	}

	private static IIndicator CreateSmoother(SmoothingMethodOption method, int length)
	{
		return method switch
		{
			SmoothingMethodOption.Simple => new SimpleMovingAverage { Length = length },
			SmoothingMethodOption.Exponential => new ExponentialMovingAverage { Length = length },
			SmoothingMethodOption.Smoothed => new SmoothedMovingAverage { Length = length },
			SmoothingMethodOption.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}

	/// <summary>
	/// Supported moving average types for smoothing.
	/// </summary>
	public enum SmoothingMethodOption
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential,

		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		Smoothed,

		/// <summary>
		/// Weighted moving average.
		/// </summary>
		Weighted
	}
}
