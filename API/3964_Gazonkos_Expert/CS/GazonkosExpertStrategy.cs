namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Momentum pullback strategy converted from the MetaTrader 4 "gazonkos expert" EA.
/// </summary>
public class GazonkosExpertStrategy : Strategy
{
	private enum TradeState
	{
		WaitingForSlot,
		WaitingForImpulse,
		MonitoringRetracement,
		AwaitingExecution,
	}

	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _retracementPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<int> _t1Shift;
	private readonly StrategyParam<int> _t2Shift;
	private readonly StrategyParam<decimal> _deltaPips;
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<int> _maxActiveTrades;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closeHistory = new();

	private TradeState _state = TradeState.WaitingForSlot;
	private Sides? _pendingDirection;
	private decimal _extremePrice;
	private int? _lastTradeHour;
	private int? _lastSignalHour;
	private decimal _pointValue;

	/// <summary>
	/// Initializes a new instance of <see cref="GazonkosExpertStrategy"/>.
	/// </summary>
	public GazonkosExpertStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 16m)
			.SetDisplay("Take Profit (pips)", "Distance between entry and the take profit level", "Risk")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_retracementPips = Param(nameof(RetracementPips), 16m)
			.SetDisplay("Retracement (pips)", "Pullback distance that confirms the entry", "Signals")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 40m)
			.SetDisplay("Stop Loss (pips)", "Distance between entry and the protective stop", "Risk")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_t1Shift = Param(nameof(T1Shift), 3)
			.SetDisplay("T1 Shift", "Index of the older reference close used for momentum detection", "Signals")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_t2Shift = Param(nameof(T2Shift), 2)
			.SetDisplay("T2 Shift", "Index of the newer reference close used for momentum detection", "Signals")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_deltaPips = Param(nameof(DeltaPips), 40m)
			.SetDisplay("Delta (pips)", "Minimum distance between the reference closes to trigger a signal", "Signals")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_lotSize = Param(nameof(LotSize), 0.1m)
			.SetDisplay("Lot Size", "Fixed volume used for each trade", "Orders")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_maxActiveTrades = Param(nameof(MaxActiveTrades), 1)
			.SetDisplay("Max Active Trades", "Maximum number of simultaneous trades allowed", "Risk")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to evaluate the momentum signal", "General");
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Pullback distance expressed in pips.
	/// </summary>
	public decimal RetracementPips
	{
		get => _retracementPips.Value;
		set => _retracementPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Index of the older candle used in the momentum calculation.
	/// </summary>
	public int T1Shift
	{
		get => _t1Shift.Value;
		set => _t1Shift.Value = value;
	}

	/// <summary>
	/// Index of the newer candle used in the momentum calculation.
	/// </summary>
	public int T2Shift
	{
		get => _t2Shift.Value;
		set => _t2Shift.Value = value;
	}

	/// <summary>
	/// Required momentum distance expressed in pips.
	/// </summary>
	public decimal DeltaPips
	{
		get => _deltaPips.Value;
		set => _deltaPips.Value = value;
	}

	/// <summary>
	/// Fixed lot size of every order.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneous trades allowed by the strategy.
	/// </summary>
	public int MaxActiveTrades
	{
		get => _maxActiveTrades.Value;
		set => _maxActiveTrades.Value = value;
	}

	/// <summary>
	/// Candle series type used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_closeHistory.Clear();
		_state = TradeState.WaitingForSlot;
		_pendingDirection = null;
		_extremePrice = 0m;
		_lastTradeHour = null;
		_lastSignalHour = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = Security?.PriceStep ?? 0m;
		if (_pointValue <= 0m)
			_pointValue = 0.0001m;

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		StoreClose(candle.ClosePrice);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!TryGetClose(T1Shift, out var t1Close) || !TryGetClose(T2Shift, out var t2Close))
			return;

		switch (_state)
		{
			case TradeState.WaitingForSlot:
				ProcessWaitingForSlot(candle);
				break;
			case TradeState.WaitingForImpulse:
				ProcessWaitingForImpulse(candle, t1Close, t2Close);
				break;
			case TradeState.MonitoringRetracement:
				ProcessMonitoringRetracement(candle);
				break;
			case TradeState.AwaitingExecution:
				ProcessAwaitingExecution(candle);
				break;
		}
	}

	private void ProcessWaitingForSlot(ICandleMessage candle)
	{
		if (CanStartNewCycle(candle.CloseTime))
		{
			_state = TradeState.WaitingForImpulse;
			LogInfo($"Slot available at {candle.CloseTime:u}.");
		}
	}

	private void ProcessWaitingForImpulse(ICandleMessage candle, decimal t1Close, decimal t2Close)
	{
		var deltaThreshold = DeltaPips * _pointValue;
		if (deltaThreshold <= 0m)
			return;

		var difference = t2Close - t1Close;

		if (difference > deltaThreshold)
		{
			_pendingDirection = Sides.Buy;
			_extremePrice = Math.Max(candle.HighPrice, candle.ClosePrice);
			_lastSignalHour = candle.CloseTime.Hour;
			_state = TradeState.MonitoringRetracement;
			LogInfo($"Bullish impulse detected at {candle.CloseTime:u} with diff {difference}.");
			return;
		}

		if (-difference > deltaThreshold)
		{
			_pendingDirection = Sides.Sell;
			_extremePrice = candle.LowPrice > 0m ? Math.Min(candle.LowPrice, candle.ClosePrice) : candle.ClosePrice;
			_lastSignalHour = candle.CloseTime.Hour;
			_state = TradeState.MonitoringRetracement;
			LogInfo($"Bearish impulse detected at {candle.CloseTime:u} with diff {difference}.");
		}
	}

	private void ProcessMonitoringRetracement(ICandleMessage candle)
	{
		if (_pendingDirection == null)
		{
			ResetState();
			return;
		}

		if (_lastSignalHour.HasValue && _lastSignalHour.Value != candle.CloseTime.Hour)
		{
			LogInfo("Signal expired because the hour changed.");
			ResetState();
			return;
		}

		var retracementDistance = RetracementPips * _pointValue;
		if (retracementDistance <= 0m)
		{
			ResetState();
			return;
		}

		if (_pendingDirection == Sides.Buy)
		{
			_extremePrice = Math.Max(_extremePrice, Math.Max(candle.HighPrice, candle.ClosePrice));
			var triggerPrice = _extremePrice - retracementDistance;
			if (candle.ClosePrice <= triggerPrice)
			{
				_state = TradeState.AwaitingExecution;
				LogInfo($"Bullish pullback confirmed at {candle.CloseTime:u}. Trigger price {triggerPrice}.");
			}
		}
		else if (_pendingDirection == Sides.Sell)
		{
			_extremePrice = _extremePrice <= 0m ? candle.LowPrice : Math.Min(_extremePrice, Math.Min(candle.LowPrice, candle.ClosePrice));
			var triggerPrice = _extremePrice + retracementDistance;
			if (candle.ClosePrice >= triggerPrice)
			{
				_state = TradeState.AwaitingExecution;
				LogInfo($"Bearish pullback confirmed at {candle.CloseTime:u}. Trigger price {triggerPrice}.");
			}
		}
	}

	private void ProcessAwaitingExecution(ICandleMessage candle)
	{
		if (_pendingDirection == null)
		{
			ResetState();
			return;
		}

		if (!CanStartNewCycle(candle.CloseTime))
		{
			LogInfo("Cannot execute because slot conditions are no longer satisfied.");
			ResetState();
			return;
		}

		var volume = LotSize;
		if (volume <= 0m)
		{
			ResetState();
			return;
		}

		var takeProfit = TakeProfitPips * _pointValue;
		var stopLoss = StopLossPips * _pointValue;

		if (_pendingDirection == Sides.Buy)
		{
			var resultingPosition = Position + volume;
			BuyMarket(volume);
			if (takeProfit > 0m)
				SetTakeProfit(takeProfit, candle.ClosePrice, resultingPosition);
			if (stopLoss > 0m)
				SetStopLoss(stopLoss, candle.ClosePrice, resultingPosition);
			_lastTradeHour = candle.CloseTime.Hour;
			LogInfo($"Opened long position at {candle.CloseTime:u} with volume {volume}.");
		}
		else if (_pendingDirection == Sides.Sell)
		{
			var resultingPosition = Position - volume;
			SellMarket(volume);
			if (takeProfit > 0m)
				SetTakeProfit(takeProfit, candle.ClosePrice, resultingPosition);
			if (stopLoss > 0m)
				SetStopLoss(stopLoss, candle.ClosePrice, resultingPosition);
			_lastTradeHour = candle.CloseTime.Hour;
			LogInfo($"Opened short position at {candle.CloseTime:u} with volume {volume}.");
		}

		ResetState();
	}

	private bool CanStartNewCycle(DateTimeOffset time)
	{
		if (_lastTradeHour.HasValue && _lastTradeHour.Value == time.Hour)
			return false;

		if (MaxActiveTrades <= 0)
			return false;

		if (LotSize <= 0m)
			return false;

		var currentTrades = LotSize > 0m ? Math.Abs(Position) / LotSize : 0m;
		return currentTrades < MaxActiveTrades;
	}

	private void ResetState()
	{
		_state = TradeState.WaitingForSlot;
		_pendingDirection = null;
		_extremePrice = 0m;
		_lastSignalHour = null;
	}

	private void StoreClose(decimal value)
	{
		_closeHistory.Add(value);

		var capacity = Math.Max(T1Shift, T2Shift) + 5;
		if (_closeHistory.Count > capacity)
			_closeHistory.RemoveAt(0);
	}

	private bool TryGetClose(int shift, out decimal value)
	{
		value = 0m;
		if (shift < 0)
			return false;

		var index = _closeHistory.Count - 1 - shift;
		if (index < 0 || index >= _closeHistory.Count)
			return false;

		value = _closeHistory[index];
		return true;
	}
}
