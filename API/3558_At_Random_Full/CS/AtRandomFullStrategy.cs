using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "At random Full" MetaTrader 5 expert advisor that alternates between random buy and sell entries.
/// The strategy exposes the same risk control toggles such as trade direction filters, time windows and grid spacing.
/// </summary>
public class AtRandomFullStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<int> _minStepPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _trailingActivatePoints;
	private readonly StrategyParam<int> _trailingStopPoints;
	private readonly StrategyParam<int> _trailingStepPoints;
	private readonly StrategyParam<bool> _onlyOnePosition;
	private readonly StrategyParam<bool> _closeOpposite;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _useTimeControl;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<TradeMode> _tradeMode;
	private readonly StrategyParam<int> _randomSeed;

	private Random? _random;
	private decimal _priceStep;
	private decimal _normalizedVolume;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _minStepDistance;
	private decimal _trailingActivateDistance;

	private DateTimeOffset? _lastLongSignalBar;
	private DateTimeOffset? _lastShortSignalBar;
	private decimal? _lastLongEntryPrice;
	private decimal? _lastShortEntryPrice;
	private int _longEntries;
	private int _shortEntries;

	/// <summary>
	/// Defines which side of the market is allowed to trade.
	/// </summary>
	public enum TradeMode
	{
		/// <summary>
		/// Long and short positions are permitted.
		/// </summary>
		Both,

		/// <summary>
		/// Only long positions can be opened.
		/// </summary>
		BuyOnly,

		/// <summary>
		/// Only short positions can be opened.
		/// </summary>
		SellOnly
	}

	/// <summary>
	/// Candle type used to schedule signal evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base market order volume expressed in lots.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Maximum number of averaged entries allowed per direction (0 disables the check).
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Minimal distance in points required between the current price and the previous entry.
	/// </summary>
	public int MinStepPoints
	{
		get => _minStepPoints.Value;
		set => _minStepPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in MetaTrader points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in MetaTrader points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Profit threshold that activates the trailing stop (in points).
	/// </summary>
	public int TrailingActivatePoints
	{
		get => _trailingActivatePoints.Value;
		set => _trailingActivatePoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance once activated (in points).
	/// </summary>
	public int TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Minimum price improvement required before the trailing stop is moved (in points).
	/// </summary>
	public int TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// When true, the strategy maintains at most one open position at a time.
	/// </summary>
	public bool OnlyOnePosition
	{
		get => _onlyOnePosition.Value;
		set => _onlyOnePosition.Value = value;
	}

	/// <summary>
	/// When enabled, opposite positions are closed before opening a new trade.
	/// </summary>
	public bool CloseOpposite
	{
		get => _closeOpposite.Value;
		set => _closeOpposite.Value = value;
	}

	/// <summary>
	/// Inverts random signals so that buys become sells and vice versa.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Enables the trading session time filter.
	/// </summary>
	public bool UseTimeControl
	{
		get => _useTimeControl.Value;
		set => _useTimeControl.Value = value;
	}

	/// <summary>
	/// Session start time (inclusive) when the time filter is active.
	/// </summary>
	public TimeSpan SessionStart
	{
		get => _sessionStart.Value;
		set => _sessionStart.Value = value;
	}

	/// <summary>
	/// Session end time (inclusive) when the time filter is active.
	/// </summary>
	public TimeSpan SessionEnd
	{
		get => _sessionEnd.Value;
		set => _sessionEnd.Value = value;
	}

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
	public TradeMode Mode
	{
		get => _tradeMode.Value;
		set => _tradeMode.Value = value;
	}

	/// <summary>
	/// Optional fixed seed for the random generator (0 keeps the environment tick count).
	/// </summary>
	public int RandomSeed
	{
		get => _randomSeed.Value;
		set => _randomSeed.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public AtRandomFullStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used to evaluate signals", "General");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Base market order volume in lots", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 1m, 0.01m);

		_maxPositions = Param(nameof(MaxPositions), 5)
			.SetNotNegative()
			.SetDisplay("Max Positions", "Maximum number of averaged entries per direction (0 = unlimited)", "Risk")
			.SetCanOptimize(true)
			.SetRange(0, 10);

		_minStepPoints = Param(nameof(MinStepPoints), 150)
			.SetNotNegative()
			.SetDisplay("Min Step (pts)", "Minimal spacing between consecutive entries", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 300, 10);

		_stopLossPoints = Param(nameof(StopLossPoints), 150)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pts)", "Protective stop distance in points", "Protection")
			.SetCanOptimize(true)
			.SetOptimize(0, 600, 10);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 460)
			.SetNotNegative()
			.SetDisplay("Take Profit (pts)", "Profit target distance in points", "Protection")
			.SetCanOptimize(true)
			.SetOptimize(0, 800, 10);

		_trailingActivatePoints = Param(nameof(TrailingActivatePoints), 70)
			.SetNotNegative()
			.SetDisplay("Trailing Activate (pts)", "Profit needed before the trailing stop engages", "Protection");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 250)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pts)", "Distance maintained by the trailing stop", "Protection");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 50)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pts)", "Minimal price improvement before trailing adjusts", "Protection");

		_onlyOnePosition = Param(nameof(OnlyOnePosition), false)
			.SetDisplay("Only One", "Restrict the strategy to a single open position", "Execution");

		_closeOpposite = Param(nameof(CloseOpposite), false)
			.SetDisplay("Close Opposite", "Close opposing positions before opening a new one", "Execution");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse", "Invert random buy/sell decisions", "Execution");

		_useTimeControl = Param(nameof(UseTimeControl), false)
			.SetDisplay("Use Time Control", "Limit trading to a configurable time window", "Timing");

		_sessionStart = Param(nameof(SessionStart), new TimeSpan(10, 1, 0))
			.SetDisplay("Session Start", "Trading window start time", "Timing");

		_sessionEnd = Param(nameof(SessionEnd), new TimeSpan(15, 2, 0))
			.SetDisplay("Session End", "Trading window end time", "Timing");

		_tradeMode = Param(nameof(Mode), TradeMode.Both)
			.SetDisplay("Trade Mode", "Allowed trade direction", "Execution");

		_randomSeed = Param(nameof(RandomSeed), 0)
			.SetDisplay("Random Seed", "Fixed seed for deterministic simulations (0 = auto)", "Execution");
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

		_random = null;
		_priceStep = 0m;
		_normalizedVolume = 0m;
		_stopLossDistance = 0m;
		_takeProfitDistance = 0m;
		_minStepDistance = 0m;
		_trailingActivateDistance = 0m;
		_lastLongSignalBar = null;
		_lastShortSignalBar = null;
		_lastLongEntryPrice = null;
		_lastShortEntryPrice = null;
		_longEntries = 0;
		_shortEntries = 0;

		Volume = OrderVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var seed = RandomSeed;
		_random = seed == 0 ? new Random(Environment.TickCount) : new Random(seed);

		_priceStep = ResolvePriceStep();
		_normalizedVolume = NormalizeVolume(OrderVolume);
		Volume = _normalizedVolume;

		_stopLossDistance = StopLossPoints * _priceStep;
		_takeProfitDistance = TakeProfitPoints * _priceStep;
		_minStepDistance = MinStepPoints * _priceStep;
		_trailingActivateDistance = TrailingActivatePoints * _priceStep;
		var trailingStopDistance = TrailingStopPoints * _priceStep;
		var trailingStepDistance = TrailingStepPoints * _priceStep;

		var takeUnit = _takeProfitDistance > 0m ? new Unit(_takeProfitDistance, UnitTypes.Price) : new Unit();
		var stopUnit = _stopLossDistance > 0m ? new Unit(_stopLossDistance, UnitTypes.Price) : new Unit();

		LogInfo($"Trailing parameters: activate {_trailingActivateDistance}, stop {trailingStopDistance}, step {trailingStepDistance}.");
		StartProtection(takeProfit: takeUnit, stopLoss: stopUnit, isStopTrailing: trailingStopDistance > 0m);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (UseTimeControl && !IsWithinTradingWindow(candle.OpenTime))
			return;

		if (_random == null)
			return;

		if (OnlyOnePosition && Position != 0m)
			return;

		var direction = _random.NextDouble() < 0.5 ? Sides.Buy : Sides.Sell;
		direction = ReverseSignals ? Invert(direction) : direction;

		if (!IsDirectionAllowed(direction))
			return;

		var barTime = candle.OpenTime;
		if (!RegisterBar(direction, barTime))
			return;

		if (!CanOpen(direction))
			return;

		var entryPrice = candle.ClosePrice;
		if (entryPrice <= 0m)
			return;

		if (!IsStepLargeEnough(direction, entryPrice))
			return;

		if (!EnsureOppositeClosed(direction))
			return;

		var volume = _normalizedVolume;
		if (volume <= 0m)
			return;

		if (direction == Sides.Buy)
		{
			LogInfo($"Opening random long at {entryPrice} with volume {volume}.");
			BuyMarket(volume);
		}
		else
		{
			LogInfo($"Opening random short at {entryPrice} with volume {volume}.");
			SellMarket(volume);
		}
	}

	private Sides Invert(Sides side) => side == Sides.Buy ? Sides.Sell : Sides.Buy;

	private bool IsDirectionAllowed(Sides side)
	{
		return Mode switch
		{
			TradeMode.Both => true,
			TradeMode.BuyOnly => side == Sides.Buy,
			TradeMode.SellOnly => side == Sides.Sell,
			_ => true
		};
	}

	private bool RegisterBar(Sides direction, DateTimeOffset barTime)
	{
		if (direction == Sides.Buy)
		{
			if (_lastLongSignalBar == barTime)
				return false;

			_lastLongSignalBar = barTime;
			return true;
		}

		if (_lastShortSignalBar == barTime)
			return false;

		_lastShortSignalBar = barTime;
		return true;
	}

	private bool CanOpen(Sides direction)
	{
		if (MaxPositions <= 0)
			return true;

		if (direction == Sides.Buy)
		{
			if (Position < 0m)
				return true;

			return _longEntries < MaxPositions;
		}

		if (Position > 0m)
			return true;

		return _shortEntries < MaxPositions;
	}

	private bool IsStepLargeEnough(Sides direction, decimal price)
	{
		if (_minStepDistance <= 0m)
			return true;

		var reference = direction == Sides.Buy ? _lastLongEntryPrice : _lastShortEntryPrice;
		if (reference is null)
			return true;

		var distance = Math.Abs(price - reference.Value);
		return distance >= _minStepDistance;
	}

	private bool EnsureOppositeClosed(Sides direction)
	{
		if (!CloseOpposite)
			return true;

		if (direction == Sides.Buy && Position < 0m)
		{
			ClosePosition();
			return false;
		}

		if (direction == Sides.Sell && Position > 0m)
		{
			ClosePosition();
			return false;
		}

		return true;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var start = SessionStart;
		var end = SessionEnd;
		var current = time.TimeOfDay;

		if (start <= end)
			return current >= start && current <= end;

		return current >= start || current <= end;
	}

	private decimal ResolvePriceStep()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		if (security.PriceStep is decimal step && step > 0m)
			return step;

		if (security.MinStep is decimal minStep && minStep > 0m)
			return minStep;

		return 0.0001m;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;
		if (security?.VolumeStep is decimal step && step > 0m)
			volume = Math.Floor(volume / step) * step;

		if (security?.MinVolume is decimal min && min > 0m && volume < min)
			volume = min;

		if (security?.MaxVolume is decimal max && max > 0m && volume > max)
			volume = max;

		return volume;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order.Security != Security)
			return;

		if (Position == 0m)
		{
			_lastLongEntryPrice = null;
			_lastShortEntryPrice = null;
			_longEntries = 0;
			_shortEntries = 0;
			return;
		}

		if (Position > 0m && trade.OrderDirection == Sides.Buy)
		{
			_lastLongEntryPrice = trade.Price;
			_longEntries++;
			_shortEntries = 0;
			_lastShortEntryPrice = null;
		}
		else if (Position < 0m && trade.OrderDirection == Sides.Sell)
		{
			_lastShortEntryPrice = trade.Price;
			_shortEntries++;
			_longEntries = 0;
			_lastLongEntryPrice = null;
		}
	}
}

