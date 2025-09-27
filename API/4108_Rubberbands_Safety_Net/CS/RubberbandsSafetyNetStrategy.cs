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
/// Converted version of the RUBBERBANDS MetaTrader expert using the StockSharp high level API.
/// Implements alternating long/short cycles with directional safety averaging and session controls.
/// </summary>
public class RubberbandsSafetyNetStrategy : Strategy
{
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _targetProfitPerLot;
	private readonly StrategyParam<bool> _useSessionTakeProfit;
	private readonly StrategyParam<decimal> _sessionTakeProfitPerLot;
	private readonly StrategyParam<bool> _useSessionStopLoss;
	private readonly StrategyParam<decimal> _sessionStopLossPerLot;
	private readonly StrategyParam<bool> _quiesceNow;
	private readonly StrategyParam<bool> _doNow;
	private readonly StrategyParam<bool> _stopNow;
	private readonly StrategyParam<bool> _closeNow;
	private readonly StrategyParam<bool> _useSafetyMode;
	private readonly StrategyParam<decimal> _safetyStartPerLot;
	private readonly StrategyParam<decimal> _safetyVolume;
	private readonly StrategyParam<decimal> _safetyStepPerLot;
	private readonly StrategyParam<decimal> _safetyProfitPerLot;
	private readonly StrategyParam<decimal> _safetyModeTakeProfitPerLot;
	private readonly StrategyParam<bool> _useInitialState;
	private readonly StrategyParam<decimal> _initialProfitSoFar;
	private readonly StrategyParam<bool> _initialSafetyMode;
	private readonly StrategyParam<bool> _initialSafetyToBuy;
	private readonly StrategyParam<int> _initialUsedSafetyCount;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _openVolume;
	private decimal _averagePrice;
	private decimal _realizedProfit;
	private bool _safetyMode;
	private int _safetyOrdersCount;
	private bool _initialStateApplied;
	private bool _closeRequested;
	private CloseReason _pendingCloseReason;
	private Sides _nextDirection;
	private Sides _lastDirection;

	private enum CloseReason
	{
		None,
		Target,
		Manual,
		Session,
		Safety
	}

	/// <summary>
	/// Initializes <see cref="RubberbandsSafetyNetStrategy"/> parameters.
	/// </summary>
	public RubberbandsSafetyNetStrategy()
	{
		_baseVolume = Param(nameof(BaseVolume), 0.05m)
		.SetGreaterThanZero()
		.SetDisplay("Base Volume", "Starting order volume", "Position")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 1m, 0.01m);

		_targetProfitPerLot = Param(nameof(TargetProfitPerLot), 1000m)
		.SetGreaterThanZero()
		.SetDisplay("Target Profit / Lot", "Realized profit target for the base trade", "Profit")
		.SetCanOptimize(true)
		.SetOptimize(100m, 2000m, 100m);

		_useSessionTakeProfit = Param(nameof(UseSessionTakeProfit), true)
		.SetDisplay("Use Session Take Profit", "Enable daily session profit target", "Session");

		_sessionTakeProfitPerLot = Param(nameof(SessionTakeProfitPerLot), 1300m)
		.SetGreaterThanZero()
		.SetDisplay("Session TP / Lot", "Session-wide profit target per lot", "Session")
		.SetCanOptimize(true)
		.SetOptimize(200m, 3000m, 100m);

		_useSessionStopLoss = Param(nameof(UseSessionStopLoss), false)
		.SetDisplay("Use Session Stop Loss", "Enable daily session stop loss", "Session");

		_sessionStopLossPerLot = Param(nameof(SessionStopLossPerLot), 300m)
		.SetGreaterThanZero()
		.SetDisplay("Session SL / Lot", "Session-wide loss limit per lot", "Session")
		.SetCanOptimize(true)
		.SetOptimize(100m, 2000m, 100m);

		_quiesceNow = Param(nameof(QuiesceNow), false)
		.SetDisplay("Quiesce", "Pause new entries once flat", "Manual");

		_doNow = Param(nameof(DoNow), false)
		.SetDisplay("Enter Now", "Force an immediate entry", "Manual");

		_stopNow = Param(nameof(StopNow), false)
		.SetDisplay("Stop Trading", "Halt trading loop without closing", "Manual");

		_closeNow = Param(nameof(CloseNow), false)
		.SetDisplay("Close Now", "Close all positions as soon as possible", "Manual");

		_useSafetyMode = Param(nameof(UseSafetyMode), true)
		.SetDisplay("Use Safety Mode", "Enable directional safety averaging", "Safety");

		_safetyStartPerLot = Param(nameof(SafetyStartPerLot), 2000m)
		.SetGreaterThanZero()
		.SetDisplay("Safety Start / Lot", "Drawdown per lot to activate safety mode", "Safety")
		.SetCanOptimize(true)
		.SetOptimize(200m, 4000m, 200m);

		_safetyVolume = Param(nameof(SafetyVolume), 0.05m)
		.SetGreaterThanZero()
		.SetDisplay("Safety Volume", "Volume of each safety averaging order", "Safety")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 1m, 0.01m);

		_safetyStepPerLot = Param(nameof(SafetyStepPerLot), 3000m)
		.SetGreaterThanZero()
		.SetDisplay("Safety Step / Lot", "Additional drawdown per safety order", "Safety")
		.SetCanOptimize(true)
		.SetOptimize(200m, 6000m, 200m);

		_safetyProfitPerLot = Param(nameof(SafetyProfitPerLot), 1300m)
		.SetGreaterThanZero()
		.SetDisplay("Safety Profit / Lot", "Net profit target while in safety mode", "Safety")
		.SetCanOptimize(true)
		.SetOptimize(200m, 3000m, 100m);

		_safetyModeTakeProfitPerLot = Param(nameof(SafetyModeTakeProfitPerLot), 500m)
		.SetGreaterThanZero()
		.SetDisplay("Safety Session TP", "Session profit target while in safety", "Safety")
		.SetCanOptimize(true)
		.SetOptimize(100m, 2000m, 100m);

		_useInitialState = Param(nameof(UseInitialState), false)
		.SetDisplay("Use Initial State", "Restore state variables on restart", "Initial");

		_initialProfitSoFar = Param(nameof(InitialProfitSoFar), 0m)
		.SetDisplay("Initial Profit", "Realized profit carried from previous run", "Initial");

		_initialSafetyMode = Param(nameof(InitialSafetyMode), false)
		.SetDisplay("Initial Safety Mode", "Was the safety mode active on restart", "Initial");

		_initialSafetyToBuy = Param(nameof(InitialSafetyToBuy), false)
		.SetDisplay("Initial Safety Buys", "If true, safety trades resume as buys", "Initial");

		_initialUsedSafetyCount = Param(nameof(InitialUsedSafetyCount), 0)
		.SetDisplay("Initial Safety Count", "Number of safety trades already placed", "Initial");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series", "Data");

		_nextDirection = Sides.Buy;
		_lastDirection = Sides.Buy;
	}

	/// <summary>
	/// Base market order volume.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Profit target per lot for the base position.
	/// </summary>
	public decimal TargetProfitPerLot
	{
		get => _targetProfitPerLot.Value;
		set => _targetProfitPerLot.Value = value;
	}

	/// <summary>
	/// Enable session take profit logic.
	/// </summary>
	public bool UseSessionTakeProfit
	{
		get => _useSessionTakeProfit.Value;
		set => _useSessionTakeProfit.Value = value;
	}

	/// <summary>
	/// Session take profit threshold per lot.
	/// </summary>
	public decimal SessionTakeProfitPerLot
	{
		get => _sessionTakeProfitPerLot.Value;
		set => _sessionTakeProfitPerLot.Value = value;
	}

	/// <summary>
	/// Enable session stop loss logic.
	/// </summary>
	public bool UseSessionStopLoss
	{
		get => _useSessionStopLoss.Value;
		set => _useSessionStopLoss.Value = value;
	}

	/// <summary>
	/// Session stop loss threshold per lot.
	/// </summary>
	public decimal SessionStopLossPerLot
	{
		get => _sessionStopLossPerLot.Value;
		set => _sessionStopLossPerLot.Value = value;
	}

	/// <summary>
	/// Pause new entries once the strategy is flat.
	/// </summary>
	public bool QuiesceNow
	{
		get => _quiesceNow.Value;
		set => _quiesceNow.Value = value;
	}

	/// <summary>
	/// Force an immediate market entry.
	/// </summary>
	public bool DoNow
	{
		get => _doNow.Value;
		set => _doNow.Value = value;
	}

	/// <summary>
	/// Halt trading decisions without closing exposure.
	/// </summary>
	public bool StopNow
	{
		get => _stopNow.Value;
		set => _stopNow.Value = value;
	}

	/// <summary>
	/// Close all positions as soon as practical.
	/// </summary>
	public bool CloseNow
	{
		get => _closeNow.Value;
		set => _closeNow.Value = value;
	}

	/// <summary>
	/// Enable the directional safety averaging logic.
	/// </summary>
	public bool UseSafetyMode
	{
		get => _useSafetyMode.Value;
		set => _useSafetyMode.Value = value;
	}

	/// <summary>
	/// Drawdown per lot that activates safety mode.
	/// </summary>
	public decimal SafetyStartPerLot
	{
		get => _safetyStartPerLot.Value;
		set => _safetyStartPerLot.Value = value;
	}

	/// <summary>
	/// Volume of each safety averaging order.
	/// </summary>
	public decimal SafetyVolume
	{
		get => _safetyVolume.Value;
		set => _safetyVolume.Value = value;
	}

	/// <summary>
	/// Additional drawdown per lot required for each extra safety order.
	/// </summary>
	public decimal SafetyStepPerLot
	{
		get => _safetyStepPerLot.Value;
		set => _safetyStepPerLot.Value = value;
	}

	/// <summary>
	/// Net profit target per lot while the strategy is in safety mode.
	/// </summary>
	public decimal SafetyProfitPerLot
	{
		get => _safetyProfitPerLot.Value;
		set => _safetyProfitPerLot.Value = value;
	}

	/// <summary>
	/// Session profit target per lot used while the safety mode is active.
	/// </summary>
	public decimal SafetyModeTakeProfitPerLot
	{
		get => _safetyModeTakeProfitPerLot.Value;
		set => _safetyModeTakeProfitPerLot.Value = value;
	}

	/// <summary>
	/// Enable restoration of state variables from a previous run.
	/// </summary>
	public bool UseInitialState
	{
		get => _useInitialState.Value;
		set => _useInitialState.Value = value;
	}

	/// <summary>
	/// Realized profit accumulated before the restart.
	/// </summary>
	public decimal InitialProfitSoFar
	{
		get => _initialProfitSoFar.Value;
		set => _initialProfitSoFar.Value = value;
	}

	/// <summary>
	/// Indicates whether safety mode was active on restart.
	/// </summary>
	public bool InitialSafetyMode
	{
		get => _initialSafetyMode.Value;
		set => _initialSafetyMode.Value = value;
	}

	/// <summary>
	/// Indicates the direction of safety trades on restart (true = buys).
	/// </summary>
	public bool InitialSafetyToBuy
	{
		get => _initialSafetyToBuy.Value;
		set => _initialSafetyToBuy.Value = value;
	}

	/// <summary>
	/// Number of safety orders that were already filled on restart.
	/// </summary>
	public int InitialUsedSafetyCount
	{
		get => _initialUsedSafetyCount.Value;
		set => _initialUsedSafetyCount.Value = value;
	}

	/// <summary>
	/// Candle type that drives the trading loop.
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

		_openVolume = 0m;
		_averagePrice = 0m;
		_realizedProfit = 0m;
		_safetyMode = false;
		_safetyOrdersCount = 0;
		_initialStateApplied = false;
		_closeRequested = false;
		_pendingCloseReason = CloseReason.None;
		_nextDirection = Sides.Buy;
		_lastDirection = Sides.Buy;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_initialStateApplied)
		{
			// Restore external state values only once after the start.
			ApplyInitialState();
			_initialStateApplied = true;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (StopNow)
		// Manual halt prevents any further decisions until the flag is cleared.
		return;

		if (CloseNow)
		{
			if (Position == 0m)
			{
				CloseNow = false;
				_realizedProfit = 0m;
			}
			else
			{
				// Defer liquidation to the standard closing pipeline.
				RequestClose(CloseReason.Manual);
			}
		}

		if (_closeRequested)
		{
			if (HandleCloseRequest())
			return;
		}

		var price = candle.ClosePrice;
		var unrealized = GetUnrealizedPnL(price);
		var totalProfit = _realizedProfit + unrealized;

		if (Position == 0m)
		{
			_safetyMode = false;
			_safetyOrdersCount = 0;

			if (HandleCloseRequest())
			return;

			if (QuiesceNow)
			return;

			if (DoNow)
			{
				DoNow = false;
				// Manual trigger ignores the minute boundary and enters immediately.
				TryOpenBaseOrder();
				return;
			}

			if (candle.CloseTime.Second == 0)
			{
				// Normal behaviour: open a new cycle on every full minute.
				TryOpenBaseOrder();
			}

			return;
		}

		var direction = Position > 0m ? Sides.Buy : Sides.Sell;
		_lastDirection = direction;

		if (UseSessionTakeProfit && totalProfit >= SessionTakeProfitPerLot * BaseVolume)
		{
			// Session profit reached, liquidate and reset profit counters.
			RequestClose(CloseReason.Session);
			if (HandleCloseRequest())
			return;
		}

		if (UseSessionStopLoss && !_safetyMode && totalProfit <= -SessionStopLossPerLot * BaseVolume)
		{
			// Session loss reached outside of safety mode.
			RequestClose(CloseReason.Session);
			if (HandleCloseRequest())
			return;
		}

		var baseTarget = TargetProfitPerLot * BaseVolume;
		if (!_safetyMode && unrealized >= baseTarget)
		{
			// Base cycle finished with profit.
			RequestClose(CloseReason.Target);
			if (HandleCloseRequest())
			return;
		}

		if (!UseSafetyMode)
		return;

		if (!_safetyMode)
		{
			if (unrealized <= -SafetyStartPerLot * BaseVolume)
			{
				_safetyMode = true;
				_safetyOrdersCount = 0;
				// First averaging order appears when the drawdown exceeds the trigger.
				if (TryAddSafetyOrder(direction))
				_safetyOrdersCount = 1;
			}

			return;
		}

		var totalVolume = Math.Abs(Position);
		var safetyProfitTarget = SafetyProfitPerLot * totalVolume;
		if (SafetyProfitPerLot > 0m && unrealized >= safetyProfitTarget)
		{
			// Profit target while in safety mode reached.
			RequestClose(CloseReason.Safety);
			if (HandleCloseRequest())
			return;
		}

		var nextThreshold = -(SafetyStartPerLot + _safetyOrdersCount * SafetyStepPerLot) * SafetyVolume;
		if (unrealized <= nextThreshold)
		{
			// Cascading drawdown adds another averaging order.
			if (TryAddSafetyOrder(direction))
			_safetyOrdersCount++;
		}

		if (SafetyModeTakeProfitPerLot > 0m && totalProfit >= SafetyModeTakeProfitPerLot * BaseVolume)
		{
			// Session level profit achieved during safety mode.
			RequestClose(CloseReason.Safety);
			HandleCloseRequest();
		}
	}

	private void ApplyInitialState()
	{
		if (!UseInitialState)
		return;

		_realizedProfit = InitialProfitSoFar;
		_safetyMode = InitialSafetyMode;
		_safetyOrdersCount = Math.Max(0, InitialUsedSafetyCount);
		_lastDirection = InitialSafetyToBuy ? Sides.Sell : Sides.Buy;
	}

	private void RequestClose(CloseReason reason)
	{
		if (_closeRequested)
		return;

		_closeRequested = true;
		_pendingCloseReason = reason;
		_lastDirection = Position > 0m ? Sides.Buy : Sides.Sell;
	}

	private bool HandleCloseRequest()
	{
		if (!_closeRequested)
		return false;

		if (!FlattenPosition())
		return true;

		OnAfterClose();
		return false;
	}

	private bool FlattenPosition()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
			return false;
		}

		if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
			return false;
		}

		return true;
	}

	private void OnAfterClose()
	{
		if (_pendingCloseReason == CloseReason.Manual || _pendingCloseReason == CloseReason.Session)
		_realizedProfit = 0m;

		_safetyMode = false;
		_safetyOrdersCount = 0;
		_closeRequested = false;
		_pendingCloseReason = CloseReason.None;
		CloseNow = false;
		DoNow = false;

		_nextDirection = _lastDirection == Sides.Buy ? Sides.Sell : Sides.Buy;
	}

	private bool TryOpenBaseOrder()
	{
		var volume = AlignVolume(BaseVolume);
		if (volume <= 0m)
		return false;

		if (_nextDirection == Sides.Buy)
		{
			BuyMarket(volume);
			_lastDirection = Sides.Buy;
		}
		else
		{
			SellMarket(volume);
			_lastDirection = Sides.Sell;
		}

		_nextDirection = _lastDirection == Sides.Buy ? Sides.Sell : Sides.Buy;
		return true;
	}

	private bool TryAddSafetyOrder(Sides direction)
	{
		var volume = AlignVolume(SafetyVolume);
		if (volume <= 0m)
		return false;

		if (direction == Sides.Buy)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}

		return true;
	}

	private decimal AlignVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var security = Security;
		if (security == null)
		return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = decimal.Floor(volume / step);
			volume = steps * step;
			if (volume == 0m)
			volume = step;
		}

		var min = security.VolumeMin ?? 0m;
		if (min > 0m && volume < min)
		volume = min;

		var max = security.VolumeMax ?? 0m;
		if (max > 0m && volume > max)
		volume = max;

		return volume;
	}

	private decimal GetUnrealizedPnL(decimal price)
	{
		if (_openVolume == 0m)
		return 0m;

		var diff = price - _averagePrice;
		return diff * _openVolume;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
		return;

		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;

		if (trade.Order.Side == Sides.Buy)
		{
			if (_openVolume >= 0m)
			{
				var total = _openVolume + volume;
				_averagePrice = total == 0m ? 0m : (_averagePrice * _openVolume + price * volume) / total;
				_openVolume = total;
			}
			else
			{
				var closing = Math.Min(volume, Math.Abs(_openVolume));
				_realizedProfit += (_averagePrice - price) * closing;
				_openVolume += volume;
				if (_openVolume >= 0m)
				_averagePrice = _openVolume == 0m ? 0m : price;
			}
		}
		else
		{
			if (_openVolume <= 0m)
			{
				var total = Math.Abs(_openVolume) + volume;
				_averagePrice = total == 0m ? 0m : (_averagePrice * Math.Abs(_openVolume) + price * volume) / total;
				_openVolume -= volume;
			}
			else
			{
				var closing = Math.Min(volume, _openVolume);
				_realizedProfit += (price - _averagePrice) * closing;
				_openVolume -= volume;
				if (_openVolume <= 0m)
				_averagePrice = _openVolume == 0m ? 0m : price;
			}
		}
	}
}
