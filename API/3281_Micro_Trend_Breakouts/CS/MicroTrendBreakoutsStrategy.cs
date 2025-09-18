using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Micro Trend Breakouts strategy converted from MetaTrader.
/// </summary>
public class MicroTrendBreakoutsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<int> _takeProfitSteps;
	private readonly StrategyParam<int> _stopLossSteps;
	private readonly StrategyParam<bool> _useTrailing;
	private readonly StrategyParam<int> _trailingStartSteps;
	private readonly StrategyParam<int> _trailingStepSize;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<int> _breakEvenTriggerSteps;
	private readonly StrategyParam<int> _breakEvenPaddingSteps;
	private readonly StrategyParam<int> _macdShortPeriod;
	private readonly StrategyParam<int> _macdLongPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private LinearWeightedMovingAverage _fastMa = null!;
	private LinearWeightedMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergence _macd = null!;

	private ICandleMessage? _previousCandle;
	private ICandleMessage? _twoCandlesAgo;
	private decimal? _previousMomentum;
	private decimal? _twoMomentumsAgo;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal _highestSinceEntry;
	private decimal _lowestSinceEntry;
	private bool _breakEvenActivated;

	/// <summary>
	/// Initializes a new instance of the <see cref="MicroTrendBreakoutsStrategy"/> class.
	/// </summary>
	public MicroTrendBreakoutsStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Default volume for market orders", "General")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 5m, 0.1m);

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("Fast LWMA", "Fast linear weighted moving average period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
		.SetGreaterThanZero()
		.SetDisplay("Slow LWMA", "Slow linear weighted moving average period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(30, 150, 5);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Period", "Momentum averaging period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 2);

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Threshold", "Minimum absolute momentum to allow trades", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 2m, 0.1m);

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 50)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Take Profit", "Take profit distance in price steps", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10, 200, 10);

		_stopLossSteps = Param(nameof(StopLossSteps), 20)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Stop Loss", "Stop loss distance in price steps", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10, 150, 10);

		_useTrailing = Param(nameof(UseTrailing), true)
		.SetDisplay("Use Trailing", "Enable trailing stop logic", "Risk");

		_trailingStartSteps = Param(nameof(TrailingStartSteps), 40)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Trail Activation", "Profit in price steps before trailing starts", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10, 200, 10);

		_trailingStepSize = Param(nameof(TrailingStepSize), 40)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Trail Step", "Distance between current extreme and trailing stop in steps", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10, 200, 10);

		_useBreakEven = Param(nameof(UseBreakEven), true)
		.SetDisplay("Use Breakeven", "Move stop to breakeven when profit threshold is reached", "Risk");

		_breakEvenTriggerSteps = Param(nameof(BreakEvenTriggerSteps), 30)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Breakeven Trigger", "Profit in steps that activates breakeven", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10, 200, 10);

		_breakEvenPaddingSteps = Param(nameof(BreakEvenPaddingSteps), 30)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Breakeven Padding", "Extra steps added when moving stop to breakeven", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0, 100, 5);

		_macdShortPeriod = Param(nameof(MacdShortPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(6, 18, 1);

		_macdLongPeriod = Param(nameof(MacdLongPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA period for MACD", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 40, 1);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA period for MACD", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 15, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for signals", "General");
	}

	/// <summary>
	/// Default market order volume.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Fast linear weighted moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow linear weighted moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Momentum averaging period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum absolute momentum required for signals.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public int StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Enables trailing stop adjustments.
	/// </summary>
	public bool UseTrailing
	{
		get => _useTrailing.Value;
		set => _useTrailing.Value = value;
	}

	/// <summary>
	/// Profit in steps required before trailing is activated.
	/// </summary>
	public int TrailingStartSteps
	{
		get => _trailingStartSteps.Value;
		set => _trailingStartSteps.Value = value;
	}

	/// <summary>
	/// Distance between current extreme and trailing stop.
	/// </summary>
	public int TrailingStepSize
	{
		get => _trailingStepSize.Value;
		set => _trailingStepSize.Value = value;
	}

	/// <summary>
	/// Enables breakeven adjustment when profits appear.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Profit threshold for breakeven activation.
	/// </summary>
	public int BreakEvenTriggerSteps
	{
		get => _breakEvenTriggerSteps.Value;
		set => _breakEvenTriggerSteps.Value = value;
	}

	/// <summary>
	/// Extra distance added to the entry price when moving stop to breakeven.
	/// </summary>
	public int BreakEvenPaddingSteps
	{
		get => _breakEvenPaddingSteps.Value;
		set => _breakEvenPaddingSteps.Value = value;
	}

	/// <summary>
	/// MACD fast EMA period.
	/// </summary>
	public int MacdShortPeriod
	{
		get => _macdShortPeriod.Value;
		set => _macdShortPeriod.Value = value;
	}

	/// <summary>
	/// MACD slow EMA period.
	/// </summary>
	public int MacdLongPeriod
	{
		get => _macdLongPeriod.Value;
		set => _macdLongPeriod.Value = value;
	}

	/// <summary>
	/// MACD signal EMA period.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Primary candle data type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_previousCandle = null;
		_twoCandlesAgo = null;
		_previousMomentum = null;
		_twoMomentumsAgo = null;
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
		_breakEvenActivated = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_fastMa = new LinearWeightedMovingAverage { Length = FastMaPeriod };
		_slowMa = new LinearWeightedMovingAverage { Length = SlowMaPeriod };
		_momentum = new Momentum { Length = MomentumPeriod };
		_macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdShortPeriod,
			LongPeriod = MacdLongPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_fastMa, _slowMa, _momentum, _macd, ProcessCandle)
		.Start();

		StartProtection();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawIndicator(priceArea, _fastMa);
			DrawIndicator(priceArea, _slowMa);
			DrawOwnTrades(priceArea);
		}

		var oscillatorArea = CreateChartArea("Oscillators");
		if (oscillatorArea != null)
		{
			DrawIndicator(oscillatorArea, _momentum);
			DrawIndicator(oscillatorArea, _macd);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal momentumValue, decimal macdLine, decimal macdSignal, decimal macdHistogram)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (ManageOpenPosition(candle))
		{
			UpdateHistory(candle, momentumValue);
			return;
		}

		var hasMomentumHistory = _previousMomentum.HasValue && _twoMomentumsAgo.HasValue;
		var hasCandleHistory = _previousCandle != null && _twoCandlesAgo != null;

		if (Position <= 0m && hasMomentumHistory && hasCandleHistory)
		{
			var momentumPass = HasMomentumImpulse(momentumValue);
			var priceStructure = _twoCandlesAgo!.LowPrice < _previousCandle!.HighPrice;
			var macdFilter = (macdLine > 0m && macdLine > macdSignal) || (macdLine <= 0m && macdLine > macdSignal);

			if (fastValue > slowValue && priceStructure && momentumPass && macdFilter)
			{
				BuyMarket();
				InitializePositionLevels(candle, true);
				UpdateHistory(candle, momentumValue);
				return;
			}
		}

		if (Position >= 0m && hasMomentumHistory && hasCandleHistory)
		{
			var momentumPass = HasMomentumImpulse(momentumValue);
			var priceStructure = _previousCandle!.LowPrice < _twoCandlesAgo!.HighPrice;
			var macdFilter = (macdLine > 0m && macdLine < macdSignal) || (macdLine <= 0m && macdLine < macdSignal);

			if (fastValue < slowValue && priceStructure && momentumPass && macdFilter)
			{
				SellMarket();
				InitializePositionLevels(candle, false);
				UpdateHistory(candle, momentumValue);
				return;
			}
		}

		UpdateHistory(candle, momentumValue);
	}

	private bool HasMomentumImpulse(decimal currentMomentum)
	{
		var current = Math.Abs(currentMomentum);
		var prev = Math.Abs(_previousMomentum ?? 0m);
		var prev2 = Math.Abs(_twoMomentumsAgo ?? 0m);
		return current >= MomentumThreshold || prev >= MomentumThreshold || prev2 >= MomentumThreshold;
	}

	private bool ManageOpenPosition(ICandleMessage candle)
	{
		var priceStep = GetPriceStep();

		if (Position > 0m)
		{
			_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, candle.LowPrice);

			if (UseBreakEven && !_breakEvenActivated && _entryPrice is decimal longEntry)
			{
				var profit = candle.HighPrice - longEntry;
				if (BreakEvenTriggerSteps > 0 && profit >= BreakEvenTriggerSteps * priceStep)
				{
					var newStop = longEntry + BreakEvenPaddingSteps * priceStep;
					if (_stopPrice is null || newStop > _stopPrice)
					_stopPrice = newStop;
					_breakEvenActivated = true;
				}
			}

			if (UseTrailing && _entryPrice is decimal entryPrice)
			{
				var profitFromEntry = _highestSinceEntry - entryPrice;
				if (TrailingStartSteps > 0 && profitFromEntry >= TrailingStartSteps * priceStep)
				{
					var trailingStop = _highestSinceEntry - TrailingStepSize * priceStep;
					if (_stopPrice is null || trailingStop > _stopPrice)
					_stopPrice = trailingStop;
				}
			}

			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice is decimal target && candle.HighPrice >= target)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}
		}
		else if (Position < 0m)
		{
			_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, candle.LowPrice);

			if (UseBreakEven && !_breakEvenActivated && _entryPrice is decimal shortEntry)
			{
				var profit = shortEntry - candle.LowPrice;
				if (BreakEvenTriggerSteps > 0 && profit >= BreakEvenTriggerSteps * priceStep)
				{
					var newStop = shortEntry - BreakEvenPaddingSteps * priceStep;
					if (_stopPrice is null || newStop < _stopPrice)
					_stopPrice = newStop;
					_breakEvenActivated = true;
				}
			}

			if (UseTrailing && _entryPrice is decimal entryPrice)
			{
				var profitFromEntry = entryPrice - _lowestSinceEntry;
				if (TrailingStartSteps > 0 && profitFromEntry >= TrailingStartSteps * priceStep)
				{
					var trailingStop = _lowestSinceEntry + TrailingStepSize * priceStep;
					if (_stopPrice is null || trailingStop < _stopPrice)
					_stopPrice = trailingStop;
				}
			}

			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice is decimal target && candle.LowPrice <= target)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return true;
			}
		}
		else
		{
			ResetPositionState();
		}

		return false;
	}

	private void InitializePositionLevels(ICandleMessage candle, bool isLong)
	{
		var priceStep = GetPriceStep();
		_entryPrice = candle.ClosePrice;
		_highestSinceEntry = candle.HighPrice;
		_lowestSinceEntry = candle.LowPrice;
		_breakEvenActivated = false;

		if (StopLossSteps > 0)
		{
			_stopPrice = isLong
			? _entryPrice - StopLossSteps * priceStep
			: _entryPrice + StopLossSteps * priceStep;
		}
		else
		{
			_stopPrice = null;
		}

		if (TakeProfitSteps > 0)
		{
			_takeProfitPrice = isLong
			? _entryPrice + TakeProfitSteps * priceStep
			: _entryPrice - TakeProfitSteps * priceStep;
		}
		else
		{
			_takeProfitPrice = null;
		}
	}

	private void UpdateHistory(ICandleMessage candle, decimal momentumValue)
	{
		_twoCandlesAgo = _previousCandle;
		_previousCandle = candle;

		_twoMomentumsAgo = _previousMomentum;
		_previousMomentum = momentumValue;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
		_breakEvenActivated = false;
	}

	private decimal GetPriceStep()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		priceStep = 1m;
		return priceStep;
	}
}
