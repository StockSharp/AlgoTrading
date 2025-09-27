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
/// Replicates the MetaTrader script "Virtual_SL_TP_Pending_with_SL_Trailing" (MQL ID 49146).
/// The strategy keeps virtual stop-loss / take-profit levels based on the initial spread and optionally
/// deploys a pending buy stop order once price reaches the configured trigger.
/// </summary>
public class VirtualSlTpV1Strategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _spreadThreshold;
	private readonly StrategyParam<int> _trailingStopPoints;
	private readonly StrategyParam<bool> _enableTrailing;

	private decimal _pointSize;
	private int _priceDecimals;
	private decimal? _initialSpread;
	private decimal? _virtualStopLoss;
	private decimal? _virtualTakeProfit;
	private decimal? _pendingOrderPrice;
	private decimal? _lastBid;
	private decimal? _lastAsk;
	private Order _pendingOrder;
	private bool _closeRequested;

	/// <summary>
	/// Initializes a new instance of the <see cref="VirtualSlTpV1Strategy"/> class.
	/// </summary>
	public VirtualSlTpV1Strategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 20)
			.SetNotNegative()
			.SetDisplay("Stop Loss (points)", "Virtual stop-loss distance expressed in MetaTrader points.", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(5, 200, 5);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 40)
			.SetNotNegative()
			.SetDisplay("Take Profit (points)", "Virtual take-profit distance expressed in MetaTrader points.", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10, 400, 10);

		_spreadThreshold = Param(nameof(SpreadThreshold), 2m)
			.SetNotNegative()
			.SetDisplay("Spread Threshold (points)", "Additional spread (in MetaTrader points) that forces virtual levels to shift.", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 10m, 0.5m);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 10)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (points)", "Distance from the trigger price used to place the pending buy stop order.", "Orders")
			.SetCanOptimize(true)
			.SetOptimize(5, 200, 5);

		_enableTrailing = Param(nameof(EnableTrailing), false)
			.SetDisplay("Enable Trailing", "Place a pending buy stop order once the trigger price is reached.", "Orders");
	}

	/// <summary>
	/// Virtual stop-loss distance in MetaTrader points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Virtual take-profit distance in MetaTrader points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Extra spread (in MetaTrader points) required before the virtual levels shift.
	/// </summary>
	public decimal SpreadThreshold
	{
		get => _spreadThreshold.Value;
		set => _spreadThreshold.Value = value;
	}

	/// <summary>
	/// Distance in MetaTrader points between the trigger price and the pending buy stop.
	/// </summary>
	public int TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Enable or disable the trailing buy stop functionality.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pointSize = 0m;
		_priceDecimals = 0;
		_initialSpread = null;
		_virtualStopLoss = null;
		_virtualTakeProfit = null;
		_pendingOrderPrice = null;
		_lastBid = null;
		_lastAsk = null;
		_closeRequested = false;

		CancelPendingOrder();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointSize = CalculatePointSize();
		_priceDecimals = Security?.Decimals ?? 0;
		_closeRequested = false;

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		CancelPendingOrder();
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (_pendingOrder != null && ReferenceEquals(order, _pendingOrder) && order.State != OrderStates.Active)
		{
			_pendingOrder = null;
		}
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
		{
			_lastBid = (decimal)bid;
		}

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
		{
			_lastAsk = (decimal)ask;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		if (_pointSize <= 0m || _lastBid is null || _lastAsk is null)
		{
			return;
		}

		var bidValue = _lastBid.Value;
		var askValue = _lastAsk.Value;
		if (bidValue <= 0m || askValue <= 0m)
		{
			return;
		}

		var currentSpread = (askValue - bidValue) / _pointSize;
		if (_initialSpread is null)
		{
			InitializeVirtualLevels(askValue, currentSpread);
			return;
		}

		AdjustVirtualLevels(currentSpread);
		CheckVirtualStops(bidValue);
		ProcessTrailing(askValue);
	}

	private void InitializeVirtualLevels(decimal askPrice, decimal currentSpread)
	{
		_initialSpread = currentSpread;
		_virtualStopLoss = NormalizePrice(askPrice - StopLossPoints * _pointSize);
		_virtualTakeProfit = NormalizePrice(askPrice + TakeProfitPoints * _pointSize);
		_pendingOrderPrice = NormalizePrice(askPrice + TrailingStopPoints * _pointSize);
		_closeRequested = false;
	}

	private void AdjustVirtualLevels(decimal currentSpread)
	{
		if (_initialSpread is null || SpreadThreshold <= 0m)
		{
			return;
		}

		var requiredSpread = _initialSpread.Value + SpreadThreshold;
		if (currentSpread <= requiredSpread)
		{
			return;
		}

		var deltaPoints = currentSpread - _initialSpread.Value;
		var adjustment = deltaPoints * _pointSize;

		if (_virtualStopLoss.HasValue)
		{
			_virtualStopLoss = NormalizePrice(_virtualStopLoss.Value + adjustment);
		}

		if (_virtualTakeProfit.HasValue)
		{
			_virtualTakeProfit = NormalizePrice(_virtualTakeProfit.Value + adjustment);
		}

		if (_pendingOrderPrice.HasValue)
		{
			_pendingOrderPrice = NormalizePrice(_pendingOrderPrice.Value + adjustment);
		}
	}

	private void CheckVirtualStops(decimal bidValue)
	{
		if (Position == 0m)
		{
			_closeRequested = false;
			return;
		}

		if (_closeRequested)
		{
			return;
		}

		if (_virtualStopLoss.HasValue && bidValue <= _virtualStopLoss.Value)
		{
			ClosePosition();
			_closeRequested = true;
			return;
		}

		if (_virtualTakeProfit.HasValue && bidValue >= _virtualTakeProfit.Value)
		{
			ClosePosition();
			_closeRequested = true;
		}
	}

	private void ProcessTrailing(decimal askValue)
	{
		if (!EnableTrailing)
		{
			CancelPendingOrder();
			return;
		}

		if (_pendingOrderPrice is null || askValue < _pendingOrderPrice.Value)
		{
			return;
		}

		if (Volume <= 0m)
		{
			return;
		}

		var desiredPrice = _pendingOrderPrice.Value;
		if (IsOrderActive(_pendingOrder))
		{
			if (Math.Abs(_pendingOrder!.Price - desiredPrice) <= _pointSize)
			{
				return;
			}

			CancelPendingOrder();
		}

		_pendingOrder = BuyStop(Volume, desiredPrice);
	}

	private void CancelPendingOrder()
	{
		if (_pendingOrder is { State: OrderStates.Active })
		{
			CancelOrder(_pendingOrder);
		}

		_pendingOrder = null;
	}

	private decimal CalculatePointSize()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 1m;
	}

	private decimal NormalizePrice(decimal price)
	{
		if (_priceDecimals <= 0)
		{
			return price;
		}

		return Math.Round(price, _priceDecimals, MidpointRounding.AwayFromZero);
	}

	private static bool IsOrderActive(Order order)
	{
		return order is { State: OrderStates.Active };
	}
}

