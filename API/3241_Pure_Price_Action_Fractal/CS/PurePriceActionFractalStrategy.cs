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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pure price action strategy converted from the MetaTrader expert advisor "Pure Price Action" (id 24291).
/// Combines fractal retests with weighted moving averages, multi-timeframe momentum and MACD filters.
/// </summary>
public class PurePriceActionFractalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _momentumCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _breakEvenTriggerPoints;
	private readonly StrategyParam<decimal> _breakEvenOffsetPoints;
	private readonly StrategyParam<decimal> _maxPosition;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<bool> _useTakeProfit;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private Momentum _momentumIndicator = null!;
	private MovingAverageConvergenceDivergenceSignal _macdIndicator = null!;

	private decimal _priceStep;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _trailingDistance;
	private decimal _breakEvenTrigger;
	private decimal _breakEvenOffset;

	private readonly Queue<decimal> _bodyDifferences = new();
	private decimal _h1;
	private decimal _h2;
	private decimal _h3;
	private decimal _h4;
	private decimal _h5;
	private decimal _l1;
	private decimal _l2;
	private decimal _l3;
	private decimal _l4;
	private decimal _l5;
	private int _fractalCount;
	private decimal? _lastUpFractal;
	private decimal? _lastDownFractal;
	private bool _touchedUpFractal;
	private bool _touchedDownFractal;

	private readonly Queue<decimal> _momentumDistances = new();
	private bool _momentumReady;

	private bool _macdReady;
	private decimal _macdMain;
	private decimal _macdSignal;

	private decimal? _entryPrice;
	private decimal _highestSinceEntry;
	private decimal _lowestSinceEntry;
	private decimal _lastPosition;
	private bool _breakEvenActivated;
	private decimal _breakEvenLevel;

	/// <summary>
	/// Initializes a new instance of <see cref="PurePriceActionFractalStrategy"/>.
	/// </summary>
	public PurePriceActionFractalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Signal Candles", "Primary candle type used for entries.", "General");

		_momentumCandleType = Param(nameof(MomentumCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Momentum Candles", "Higher timeframe used by the momentum filter.", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("MACD Candles", "Higher timeframe used by the MACD trend filter.", "General");

		_fastPeriod = Param(nameof(FastPeriod), 6)
			.SetDisplay("Fast LWMA", "Length of the fast weighted moving average.", "Indicators")
			.SetCanOptimize(true);

		_slowPeriod = Param(nameof(SlowPeriod), 85)
			.SetDisplay("Slow LWMA", "Length of the slow weighted moving average.", "Indicators")
			.SetCanOptimize(true);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetDisplay("Momentum Period", "Lookback for the momentum confirmation.", "Indicators")
			.SetCanOptimize(true);

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
			.SetDisplay("Momentum Threshold", "Minimal |Momentum-100| deviation required.", "Filters")
			.SetCanOptimize(true);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetDisplay("MACD Fast", "Fast EMA length for MACD.", "Indicators")
			.SetCanOptimize(true);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetDisplay("MACD Slow", "Slow EMA length for MACD.", "Indicators")
			.SetCanOptimize(true);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetDisplay("MACD Signal", "Signal EMA length for MACD.", "Indicators")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 20m)
			.SetDisplay("Stop-Loss (pts)", "Stop-loss distance in price steps.", "Risk")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetDisplay("Take-Profit (pts)", "Take-profit distance in price steps.", "Risk")
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 40m)
			.SetDisplay("Trailing Stop (pts)", "Trailing distance in price steps.", "Risk")
			.SetCanOptimize(true);

		_breakEvenTriggerPoints = Param(nameof(BreakEvenTriggerPoints), 30m)
			.SetDisplay("Break-Even Trigger", "Profit distance required before securing the trade.", "Risk")
			.SetCanOptimize(true);

		_breakEvenOffsetPoints = Param(nameof(BreakEvenOffsetPoints), 30m)
			.SetDisplay("Break-Even Offset", "Additional profit locked after the trigger.", "Risk")
			.SetCanOptimize(true);

		_maxPosition = Param(nameof(MaxPosition), 1m)
			.SetDisplay("Max Position", "Maximum absolute position size handled by the strategy.", "Risk")
			.SetCanOptimize(true);

		_enableTrailing = Param(nameof(EnableTrailing), true)
			.SetDisplay("Use Trailing", "Enable trailing stop management.", "Risk");

		_enableBreakEven = Param(nameof(EnableBreakEven), true)
			.SetDisplay("Use Break-Even", "Enable break-even stop management.", "Risk");

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop-Loss", "Enable fixed stop-loss protection.", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take-Profit", "Enable fixed take-profit targets.", "Risk");
	}

	/// <summary>
	/// Main signal candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Timeframe used to evaluate the momentum filter.
	/// </summary>
	public DataType MomentumCandleType
	{
		get => _momentumCandleType.Value;
		set => _momentumCandleType.Value = value;
	}

	/// <summary>
	/// Timeframe used to evaluate the MACD trend filter.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <summary>
	/// Length of the fast weighted moving average.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Length of the slow weighted moving average.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Momentum indicator period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Required absolute deviation of momentum from 100.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Fast EMA length for MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length for MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA length for MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Profit trigger before activating break-even protection.
	/// </summary>
	public decimal BreakEvenTriggerPoints
	{
		get => _breakEvenTriggerPoints.Value;
		set => _breakEvenTriggerPoints.Value = value;
	}

	/// <summary>
	/// Additional offset once the break-even trigger is reached.
	/// </summary>
	public decimal BreakEvenOffsetPoints
	{
		get => _breakEvenOffsetPoints.Value;
		set => _breakEvenOffsetPoints.Value = value;
	}

	/// <summary>
	/// Maximum absolute position size handled by the strategy.
	/// </summary>
	public decimal MaxPosition
	{
		get => _maxPosition.Value;
		set => _maxPosition.Value = value;
	}

	/// <summary>
	/// Enable trailing stop management.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Enable break-even logic.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	/// <summary>
	/// Enable fixed stop-loss protection.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Enable fixed take-profit targets.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReset()
	{
		base.OnReset();

		_bodyDifferences.Clear();
		_momentumDistances.Clear();
		_fractalCount = 0;
		_h1 = _h2 = _h3 = _h4 = _h5 = 0m;
		_l1 = _l2 = _l3 = _l4 = _l5 = 0m;
		_lastUpFractal = null;
		_lastDownFractal = null;
		_touchedUpFractal = false;
		_touchedDownFractal = false;
		_momentumReady = false;
		_macdReady = false;
		_macdMain = 0m;
		_macdSignal = 0m;
		_entryPrice = null;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
		_lastPosition = 0m;
		_breakEvenActivated = false;
		_breakEvenLevel = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new WeightedMovingAverage { Length = FastPeriod };
		_slowMa = new WeightedMovingAverage { Length = SlowPeriod };
		_momentumIndicator = new Momentum { Length = MomentumPeriod };
		_macdIndicator = new MovingAverageConvergenceDivergenceSignal
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
		{
			_priceStep = 1m;
		}

		_stopLossDistance = StopLossPoints * _priceStep;
		_takeProfitDistance = TakeProfitPoints * _priceStep;
		_trailingDistance = TrailingStopPoints * _priceStep;
		_breakEvenTrigger = BreakEvenTriggerPoints * _priceStep;
		_breakEvenOffset = BreakEvenOffsetPoints * _priceStep;

		StartProtection();

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
			.BindEx(_fastMa, _slowMa, ProcessMainCandle)
			.Start();

		var momentumSubscription = SubscribeCandles(MomentumCandleType);
		momentumSubscription
			.BindEx(_momentumIndicator, ProcessMomentum)
			.Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription
			.BindEx(_macdIndicator, ProcessMacd)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var currentPosition = Position;

		if (_lastPosition == 0m && currentPosition != 0m)
		{
			// A fresh position has been established.
			_entryPrice = trade.Trade.Price;
			_highestSinceEntry = _entryPrice ?? 0m;
			_lowestSinceEntry = _entryPrice ?? 0m;
			_breakEvenActivated = false;
			_breakEvenLevel = 0m;
		}
		else if (currentPosition == 0m)
		{
			// Position fully closed.
			_entryPrice = null;
			_highestSinceEntry = 0m;
			_lowestSinceEntry = 0m;
			_breakEvenActivated = false;
			_breakEvenLevel = 0m;
		}
		else if (Math.Sign((double)_lastPosition) != Math.Sign((double)currentPosition))
		{
			// Reversal detected, treat as a new entry.
			_entryPrice = trade.Trade.Price;
			_highestSinceEntry = _entryPrice ?? 0m;
			_lowestSinceEntry = _entryPrice ?? 0m;
			_breakEvenActivated = false;
			_breakEvenLevel = 0m;
		}
		else if (currentPosition > 0m && trade.Order.Side == Sides.Buy)
		{
			// Adding to a long position, update the weighted entry price.
			var tradeVolume = trade.Trade.Volume ?? trade.Order.Volume ?? 0m;
			if (tradeVolume > 0m && _entryPrice is decimal currentEntry && _lastPosition > 0m)
			{
				var totalVolume = currentPosition;
				var previousVolume = _lastPosition;
				_entryPrice = ((currentEntry * previousVolume) + (trade.Trade.Price * tradeVolume)) / totalVolume;
			}
			else if (_entryPrice is null)
			{
				_entryPrice = trade.Trade.Price;
			}

			_highestSinceEntry = Math.Max(_highestSinceEntry, trade.Trade.Price);
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, trade.Trade.Price);
		}
		else if (currentPosition < 0m && trade.Order.Side == Sides.Sell)
		{
			// Adding to a short position, update the weighted entry price.
			var tradeVolume = trade.Trade.Volume ?? trade.Order.Volume ?? 0m;
			if (tradeVolume > 0m && _entryPrice is decimal currentEntry && _lastPosition < 0m)
			{
				var totalVolume = Math.Abs(currentPosition);
				var previousVolume = Math.Abs(_lastPosition);
				_entryPrice = ((currentEntry * previousVolume) + (trade.Trade.Price * tradeVolume)) / totalVolume;
			}
			else if (_entryPrice is null)
			{
				_entryPrice = trade.Trade.Price;
			}

			_highestSinceEntry = Math.Max(_highestSinceEntry, trade.Trade.Price);
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, trade.Trade.Price);
		}

		_lastPosition = currentPosition;
	}

	private void ProcessMomentum(ICandleMessage candle, IIndicatorValue momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!momentumValue.IsFinal)
			return;

		var momentum = momentumValue.ToDecimal();

		var distance = Math.Abs(momentum - 100m);
		_momentumDistances.Enqueue(distance);
		while (_momentumDistances.Count > 3)
		{
			_momentumDistances.Dequeue();
		}

		_momentumReady = _momentumDistances.Count > 0;
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal)
			return;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdData)
			return;

		if (macdData.Macd is not decimal macd || macdData.Signal is not decimal signal)
			return;

		_macdMain = macd;
		_macdSignal = signal;
		_macdReady = true;
	}

	private void ProcessMainCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_bodyDifferences.Enqueue(candle.OpenPrice - candle.ClosePrice);
		while (_bodyDifferences.Count > 4)
		{
			_bodyDifferences.Dequeue();
		}

		UpdateFractals(candle);
		ManagePosition(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!fastValue.IsFinal || !slowValue.IsFinal)
			return;

		if (!_momentumReady || !_macdReady)
			return;

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();

		var hasMomentum = false;
		foreach (var distance in _momentumDistances)
		{
			if (distance >= MomentumThreshold)
			{
				hasMomentum = true;
				break;
			}
		}

		if (!hasMomentum)
			return;

		var macdBullish = _macdMain > _macdSignal;
		var macdBearish = _macdMain < _macdSignal;

		var bullishTrend = fast > slow;
		var bearishTrend = fast < slow;

		var bodyPattern = CheckBodyPattern();

		var canBuy = bodyPattern && bullishTrend && macdBullish && _touchedDownFractal;
		var canSell = bodyPattern && bearishTrend && macdBearish && _touchedUpFractal;

		if (canBuy)
		{
			ExecuteEntry(Sides.Buy);
		}
		else if (canSell)
		{
			ExecuteEntry(Sides.Sell);
		}
	}

	private bool CheckBodyPattern()
	{
		if (_bodyDifferences.Count < 3)
			return false;

		var items = _bodyDifferences.ToArray();
		var last = Math.Abs(items[^1]);
		var previous = Math.Abs(items[^2]);
		var older = Math.Abs(items[^3]);

		return last < previous && previous > older;
	}

	private void ExecuteEntry(Sides direction)
	{
		var currentPosition = Position;
		var maxPosition = MaxPosition;

		if (maxPosition <= 0m)
			return;

		if (Math.Abs(currentPosition) >= maxPosition)
			return;

		var plannedVolume = Volume;
		if (plannedVolume <= 0m)
		{
			plannedVolume = 1m;
		}

		var remainingCapacity = maxPosition - Math.Abs(currentPosition);
		if (plannedVolume > remainingCapacity)
		{
			plannedVolume = remainingCapacity;
		}

		if (plannedVolume <= 0m)
			return;

		if (direction == Sides.Buy)
		{
			BuyMarket(plannedVolume);
		}
		else
		{
			SellMarket(plannedVolume);
		}
	}

	private void ManagePosition(ICandleMessage candle)
	{
		var position = Position;
		if (position == 0m || _entryPrice is null)
			return;

		var entry = _entryPrice.Value;

		_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);
		_lowestSinceEntry = Math.Min(_lowestSinceEntry, candle.LowPrice);

		if (position > 0m)
		{
			if (UseStopLoss && _stopLossDistance > 0m && candle.LowPrice <= entry - _stopLossDistance)
			{
				SellMarket(position);
				return;
			}

			if (UseTakeProfit && _takeProfitDistance > 0m && candle.HighPrice >= entry + _takeProfitDistance)
			{
				SellMarket(position);
				return;
			}

			if (EnableTrailing && _trailingDistance > 0m)
			{
				var trailLevel = _highestSinceEntry - _trailingDistance;
				if (trailLevel > entry && candle.LowPrice <= trailLevel)
				{
					SellMarket(position);
					return;
				}
			}

			if (EnableBreakEven && !_breakEvenActivated && _breakEvenTrigger > 0m)
			{
				if (_highestSinceEntry - entry >= _breakEvenTrigger)
				{
					_breakEvenLevel = entry + _breakEvenOffset;
					_breakEvenActivated = true;
				}
			}

			if (_breakEvenActivated && candle.LowPrice <= _breakEvenLevel)
			{
				SellMarket(position);
			}
		}
		else if (position < 0m)
		{
			var absPosition = Math.Abs(position);

			if (UseStopLoss && _stopLossDistance > 0m && candle.HighPrice >= entry + _stopLossDistance)
			{
				BuyMarket(absPosition);
				return;
			}

			if (UseTakeProfit && _takeProfitDistance > 0m && candle.LowPrice <= entry - _takeProfitDistance)
			{
				BuyMarket(absPosition);
				return;
			}

			if (EnableTrailing && _trailingDistance > 0m)
			{
				var trailLevel = _lowestSinceEntry + _trailingDistance;
				if (trailLevel < entry && candle.HighPrice >= trailLevel)
				{
					BuyMarket(absPosition);
					return;
				}
			}

			if (EnableBreakEven && !_breakEvenActivated && _breakEvenTrigger > 0m)
			{
				if (entry - _lowestSinceEntry >= _breakEvenTrigger)
				{
					_breakEvenLevel = entry - _breakEvenOffset;
					_breakEvenActivated = true;
				}
			}

			if (_breakEvenActivated && candle.HighPrice >= _breakEvenLevel)
			{
				BuyMarket(absPosition);
			}
		}
	}

	private void UpdateFractals(ICandleMessage candle)
	{
		_h1 = _h2;
		_h2 = _h3;
		_h3 = _h4;
		_h4 = _h5;
		_h5 = candle.HighPrice;

		_l1 = _l2;
		_l2 = _l3;
		_l3 = _l4;
		_l4 = _l5;
		_l5 = candle.LowPrice;

		if (_fractalCount < 5)
		{
			_fractalCount++;
		}

		if (_fractalCount >= 5)
		{
			var upCandidate = _h3;
			if (upCandidate > _h1 && upCandidate > _h2 && upCandidate > _h4 && upCandidate > _h5)
			{
				_lastUpFractal = upCandidate;
			}

			var downCandidate = _l3;
			if (downCandidate < _l1 && downCandidate < _l2 && downCandidate < _l4 && downCandidate < _l5)
			{
				_lastDownFractal = downCandidate;
			}
		}

		_touchedUpFractal = false;
		_touchedDownFractal = false;

		if (_lastUpFractal is decimal upValue)
		{
			_touchedUpFractal = Math.Abs(candle.ClosePrice - upValue) <= _priceStep;
		}

		if (_lastDownFractal is decimal downValue)
		{
			_touchedDownFractal = Math.Abs(candle.ClosePrice - downValue) <= _priceStep;
		}
	}
}

