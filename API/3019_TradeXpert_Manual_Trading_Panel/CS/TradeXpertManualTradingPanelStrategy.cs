using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Manual trading assistant that replicates TradeXpert panel actions through strategy parameters.
/// </summary>
public class TradeXpertManualTradingPanelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TradeXpertEntryAction> _entryAction;
	private readonly StrategyParam<TradeXpertPendingAction> _pendingAction;
	private readonly StrategyParam<decimal> _pendingPrice;
	private readonly StrategyParam<decimal> _pendingOffset;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPrice;
	private readonly StrategyParam<decimal> _stopLossOffset;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPrice;
	private readonly StrategyParam<decimal> _takeProfitOffset;
	private readonly StrategyParam<bool> _closePositionRequest;
	private readonly StrategyParam<bool> _reversePositionRequest;
	private readonly StrategyParam<decimal> _reverseVolume;

	private bool _marketActionHandled;
	private TradeXpertEntryAction _lastEntryAction;
	private bool _pendingActionHandled;
	private TradeXpertPendingAction _lastPendingAction;
	private decimal _entryPrice;
	private bool _stopTriggered;
	private bool _takeProfitTriggered;
	private decimal _lastPosition;

	/// <summary>
	/// Available market actions initiated by the user.
	/// </summary>
	public enum TradeXpertEntryAction
	{
		None,
		BuyMarket,
		SellMarket
	}

	/// <summary>
	/// Available pending order actions initiated by the user.
	/// </summary>
	public enum TradeXpertPendingAction
	{
		None,
		BuyLimit,
		BuyStop,
		SellLimit,
		SellStop
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TradeXpertManualTradingPanelStrategy"/> class.
	/// </summary>
	public TradeXpertManualTradingPanelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles used to monitor price for requests", "Market Data");

		_entryAction = Param(nameof(EntryAction), TradeXpertEntryAction.None)
		.SetDisplay("Entry Action", "Requested market order action. The value resets after execution.", "Manual Actions");

		_pendingAction = Param(nameof(PendingAction), TradeXpertPendingAction.None)
		.SetDisplay("Pending Action", "Requested pending order action. The value resets after the order is sent.", "Manual Actions");

		_pendingPrice = Param(nameof(PendingPrice), 0m)
		.SetRange(0m, 100000000m)
		.SetDisplay("Pending Price", "Absolute price for the pending order. Leave zero to use offset.", "Manual Actions");

		_pendingOffset = Param(nameof(PendingOffset), 0m)
		.SetRange(0m, 100000000m)
		.SetDisplay("Pending Offset", "Offset from the latest candle close used when price is not provided.", "Manual Actions");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Volume used for every market and pending order.", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 10m, 0.1m);

		_useStopLoss = Param(nameof(UseStopLoss), false)
		.SetDisplay("Use Stop Loss", "Enable stop loss management.", "Risk Management");

		_stopLossPrice = Param(nameof(StopLossPrice), 0m)
		.SetRange(0m, 100000000m)
		.SetDisplay("Stop Loss Price", "Absolute stop loss level. Leave zero to rely on offset.", "Risk Management");

		_stopLossOffset = Param(nameof(StopLossOffset), 0m)
		.SetRange(0m, 100000000m)
		.SetDisplay("Stop Loss Offset", "Distance from entry price used when absolute level is missing.", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 50m, 0.5m);

		_useTakeProfit = Param(nameof(UseTakeProfit), false)
		.SetDisplay("Use Take Profit", "Enable take profit management.", "Risk Management");

		_takeProfitPrice = Param(nameof(TakeProfitPrice), 0m)
		.SetRange(0m, 100000000m)
		.SetDisplay("Take Profit Price", "Absolute take profit level. Leave zero to rely on offset.", "Risk Management");

		_takeProfitOffset = Param(nameof(TakeProfitOffset), 0m)
		.SetRange(0m, 100000000m)
		.SetDisplay("Take Profit Offset", "Distance from entry price used when absolute level is missing.", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 50m, 0.5m);

		_closePositionRequest = Param(nameof(ClosePositionRequest), false)
		.SetDisplay("Close Position Request", "Set true to close the current position. The value resets automatically.", "Manual Actions");

		_reversePositionRequest = Param(nameof(ReversePositionRequest), false)
		.SetDisplay("Reverse Position Request", "Set true to reverse the current position. The value resets automatically.", "Manual Actions");

		_reverseVolume = Param(nameof(ReverseVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Reverse Volume", "Volume opened after reversing the current position.", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 10m, 0.1m);
	}

	/// <summary>
	/// Candle type used to observe prices and evaluate offsets.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Market action requested by the user.
	/// </summary>
	public TradeXpertEntryAction EntryAction
	{
		get => _entryAction.Value;
		set => _entryAction.Value = value;
	}

	/// <summary>
	/// Pending order action requested by the user.
	/// </summary>
	public TradeXpertPendingAction PendingAction
	{
		get => _pendingAction.Value;
		set => _pendingAction.Value = value;
	}

	/// <summary>
	/// Absolute price for the pending order.
	/// </summary>
	public decimal PendingPrice
	{
		get => _pendingPrice.Value;
		set => _pendingPrice.Value = value;
	}

	/// <summary>
	/// Offset from the latest candle close used for pending orders.
	/// </summary>
	public decimal PendingOffset
	{
		get => _pendingOffset.Value;
		set => _pendingOffset.Value = value;
	}

	/// <summary>
	/// Volume used for every order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Enables stop loss monitoring.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Absolute stop loss level.
	/// </summary>
	public decimal StopLossPrice
	{
		get => _stopLossPrice.Value;
		set => _stopLossPrice.Value = value;
	}

	/// <summary>
	/// Stop loss offset from the entry price.
	/// </summary>
	public decimal StopLossOffset
	{
		get => _stopLossOffset.Value;
		set => _stopLossOffset.Value = value;
	}

	/// <summary>
	/// Enables take profit monitoring.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Absolute take profit level.
	/// </summary>
	public decimal TakeProfitPrice
	{
		get => _takeProfitPrice.Value;
		set => _takeProfitPrice.Value = value;
	}

	/// <summary>
	/// Take profit offset from the entry price.
	/// </summary>
	public decimal TakeProfitOffset
	{
		get => _takeProfitOffset.Value;
		set => _takeProfitOffset.Value = value;
	}

	/// <summary>
	/// Request flag to close the current position.
	/// </summary>
	public bool ClosePositionRequest
	{
		get => _closePositionRequest.Value;
		set => _closePositionRequest.Value = value;
	}

	/// <summary>
	/// Request flag to reverse the current position.
	/// </summary>
	public bool ReversePositionRequest
	{
		get => _reversePositionRequest.Value;
		set => _reversePositionRequest.Value = value;
	}

	/// <summary>
	/// Volume used to open the new position after reversing.
	/// </summary>
	public decimal ReverseVolume
	{
		get => _reverseVolume.Value;
		set => _reverseVolume.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_marketActionHandled = EntryAction == TradeXpertEntryAction.None;
		_lastEntryAction = EntryAction;
		_pendingActionHandled = PendingAction == TradeXpertPendingAction.None;
		_lastPendingAction = PendingAction;
		_entryPrice = 0m;
		_stopTriggered = false;
		_takeProfitTriggered = false;
		_lastPosition = Position;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

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
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		Volume = TradeVolume;

		// Handle manual close and reverse requests even when indicators are still forming.
		HandleCloseRequest();
		HandleReverseRequest();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Execute user requested actions based on the latest candle information.
		HandleEntryAction(candle);
		HandlePendingAction(candle);

		// Manage stop loss and take profit logic using candle extremes.
		ManageRisk(candle);
	}

	private void HandleEntryAction(ICandleMessage candle)
	{
		if (_lastEntryAction != EntryAction)
		{
			_marketActionHandled = false;
			_lastEntryAction = EntryAction;
		}

		if (_marketActionHandled)
			return;

		var volume = TradeVolume;
		if (volume <= 0)
		{
			_marketActionHandled = true;
			EntryAction = TradeXpertEntryAction.None;
			_lastEntryAction = EntryAction;
			return;
		}

		switch (EntryAction)
		{
			case TradeXpertEntryAction.None:
				_marketActionHandled = true;
				break;

			case TradeXpertEntryAction.BuyMarket:
				// Execute a market buy and reset the request.
				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
				_stopTriggered = false;
				_takeProfitTriggered = false;
				_marketActionHandled = true;
				EntryAction = TradeXpertEntryAction.None;
				_lastEntryAction = EntryAction;
				_lastPosition = Position;
				break;

			case TradeXpertEntryAction.SellMarket:
				// Execute a market sell and reset the request.
				SellMarket(volume);
				_entryPrice = candle.ClosePrice;
				_stopTriggered = false;
				_takeProfitTriggered = false;
				_marketActionHandled = true;
				EntryAction = TradeXpertEntryAction.None;
				_lastEntryAction = EntryAction;
				_lastPosition = Position;
				break;
		}
	}

	private void HandlePendingAction(ICandleMessage candle)
	{
		if (_lastPendingAction != PendingAction)
		{
			_pendingActionHandled = false;
			_lastPendingAction = PendingAction;
		}

		if (_pendingActionHandled)
			return;

		switch (PendingAction)
		{
			case TradeXpertPendingAction.None:
				_pendingActionHandled = true;
				break;

			default:
				var volume = TradeVolume;
				if (volume <= 0)
				{
					_pendingActionHandled = true;
					PendingAction = TradeXpertPendingAction.None;
					_lastPendingAction = PendingAction;
					return;
				}

				var price = ResolvePendingPrice(candle);
				if (price <= 0)
					return;

				// Register the requested pending order and reset the action.
				switch (PendingAction)
				{
					case TradeXpertPendingAction.BuyLimit:
						BuyLimit(volume, price);
						break;

					case TradeXpertPendingAction.BuyStop:
						BuyStop(volume, price);
						break;

					case TradeXpertPendingAction.SellLimit:
						SellLimit(volume, price);
						break;

					case TradeXpertPendingAction.SellStop:
						SellStop(volume, price);
						break;
				}

				_pendingActionHandled = true;
				PendingAction = TradeXpertPendingAction.None;
				_lastPendingAction = PendingAction;
				break;
		}
	}

	private decimal ResolvePendingPrice(ICandleMessage candle)
	{
		var configuredPrice = PendingPrice;
		if (configuredPrice > 0)
			return configuredPrice;

		var offset = PendingOffset;
		if (offset <= 0)
			return 0m;

		var reference = candle.ClosePrice;
		return PendingAction switch
		{
			TradeXpertPendingAction.BuyLimit => Math.Max(reference - offset, 0m),
			TradeXpertPendingAction.BuyStop => reference + offset,
			TradeXpertPendingAction.SellLimit => reference + offset,
			TradeXpertPendingAction.SellStop => Math.Max(reference - offset, 0m),
			_ => 0m
		};
	}

	private void HandleCloseRequest()
	{
		if (!ClosePositionRequest)
			return;

		var volume = Math.Abs(Position);
		if (volume > 0)
		{
			// Close the current position using a market order.
			if (Position > 0)
				SellMarket(volume);
			else
				BuyMarket(volume);
		}

		ClosePositionRequest = false;
		_stopTriggered = false;
		_takeProfitTriggered = false;
	}

	private void HandleReverseRequest()
	{
		if (!ReversePositionRequest)
			return;

		var current = Math.Abs(Position);
		if (current > 0)
		{
			var reverseVolume = ReverseVolume;
			if (reverseVolume <= 0)
				reverseVolume = current;

			// Reverse by closing the current position and opening the opposite direction.
			if (Position > 0)
				SellMarket(current + reverseVolume);
			else
				BuyMarket(current + reverseVolume);
		}

		ReversePositionRequest = false;
		_stopTriggered = false;
		_takeProfitTriggered = false;
	}

	private void ManageRisk(ICandleMessage candle)
	{
		if (Position == 0)
		{
			if (_lastPosition != 0)
			{
				_lastPosition = 0m;
				_entryPrice = 0m;
				_stopTriggered = false;
				_takeProfitTriggered = false;
			}

			return;
		}

		if (Position != _lastPosition)
		{
			// Update entry information whenever the position size changes.
			_entryPrice = candle.ClosePrice;
			_stopTriggered = false;
			_takeProfitTriggered = false;
			_lastPosition = Position;
		}

		var isLong = Position > 0;

		var stopPrice = GetStopLossPrice(isLong);
		var takePrice = GetTakeProfitPrice(isLong);
		var volume = Math.Abs(Position);

		if (!_stopTriggered && UseStopLoss && stopPrice > 0)
		{
			// Trigger stop loss when the candle pierces the configured level.
			if ((isLong && candle.LowPrice <= stopPrice) || (!isLong && candle.HighPrice >= stopPrice))
			{
				if (isLong)
					SellMarket(volume);
				else
					BuyMarket(volume);

				_stopTriggered = true;
				_takeProfitTriggered = true;
				return;
			}
		}

		if (!_takeProfitTriggered && UseTakeProfit && takePrice > 0)
		{
			// Trigger take profit when the candle pierces the configured level.
			if ((isLong && candle.HighPrice >= takePrice) || (!isLong && candle.LowPrice <= takePrice))
			{
				if (isLong)
					SellMarket(volume);
				else
					BuyMarket(volume);

				_takeProfitTriggered = true;
				_stopTriggered = true;
			}
		}
	}

	private decimal GetStopLossPrice(bool isLong)
	{
		if (!UseStopLoss)
			return 0m;

		var price = StopLossPrice;
		if (price > 0)
			return price;

		var offset = StopLossOffset;
		if (offset <= 0 || _entryPrice <= 0)
			return 0m;

		return isLong ? _entryPrice - offset : _entryPrice + offset;
	}

	private decimal GetTakeProfitPrice(bool isLong)
	{
		if (!UseTakeProfit)
			return 0m;

		var price = TakeProfitPrice;
		if (price > 0)
			return price;

		var offset = TakeProfitOffset;
		if (offset <= 0 || _entryPrice <= 0)
			return 0m;

		return isLong ? _entryPrice + offset : _entryPrice - offset;
	}
}
