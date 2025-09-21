using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trailing stop manager converted from the "TP SL Trailing" MetaTrader 5 expert advisor.
/// The strategy does not open positions and focuses purely on stop-loss / take-profit maintenance.
/// </summary>
public class TpSlTrailingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<bool> _onlyZeroValues;

	private Order _stopOrder;
	private Order _takeProfitOrder;

	private decimal _pipSize;
	private decimal? _currentStopPrice;
	private decimal? _currentTakeProfitPrice;
	private Sides? _currentSide;
	private bool _protectionInitialized;

	/// <summary>
	/// Candle type that drives the trailing logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initial stop-loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Initial take-profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Additional trailing step in pips that must be exceeded before the stop is moved again.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Matches the original EA flag - place protection only when a position has no stop or take-profit.
	/// </summary>
	public bool OnlyZeroValues
	{
		get => _onlyZeroValues.Value;
		set => _onlyZeroValues.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TpSlTrailingStrategy"/> class.
	/// </summary>
	public TpSlTrailingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used to monitor price movement", "General");

		_stopLossPips = Param(nameof(StopLossPips), 50)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Initial stop-loss distance in pips", "Risk Management");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Initial take-profit distance in pips", "Risk Management");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk Management");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
		.SetNotNegative()
		.SetDisplay("Trailing Step (pips)", "Extra pips needed before the trailing stop moves", "Risk Management");

		_onlyZeroValues = Param(nameof(OnlyZeroValues), true)
		.SetDisplay("Only Zero Values", "Replicate MQL behavior: place protection only if missing", "Behavior");
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

		ResetProtectionOrders();
		_currentSide = null;
		_protectionInitialized = false;
		_currentStopPrice = null;
		_currentTakeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips != 0 && TrailingStepPips == 0)
			throw new InvalidOperationException("Trailing is not possible when the trailing step is zero.");

		_pipSize = GetPipSize();

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

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			ResetProtectionOrders();
			_currentSide = null;
			_protectionInitialized = false;
			_currentStopPrice = null;
			_currentTakeProfitPrice = null;
			return;
		}

		var side = Position > 0 ? Sides.Buy : Sides.Sell;
		if (_currentSide != side)
		{
			ResetProtectionOrders();
			_currentSide = side;
			_protectionInitialized = false;
			_currentStopPrice = null;
			_currentTakeProfitPrice = null;
		}
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 1m;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0)
			return;

		var isLong = Position > 0;
		var closePrice = candle.ClosePrice;

		if (_currentSide == null)
			_currentSide = isLong ? Sides.Buy : Sides.Sell;

		if (OnlyZeroValues && !_protectionInitialized)
		{
			if (TryInitializeProtection(isLong, closePrice))
				return;
		}

		ApplyTrailing(isLong, closePrice);
	}

	private bool TryInitializeProtection(bool isLong, decimal closePrice)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return false;

		decimal? stopPrice = null;
		if (StopLossPips > 0)
		{
			var distance = StopLossPips * _pipSize;
			stopPrice = isLong ? closePrice - distance : closePrice + distance;
		}

		decimal? takeProfitPrice = null;
		if (TakeProfitPips > 0)
		{
			var distance = TakeProfitPips * _pipSize;
			takeProfitPrice = isLong ? closePrice + distance : closePrice - distance;
		}

		if (stopPrice == null && takeProfitPrice == null)
		{
			_protectionInitialized = true;
			return false;
		}

		if (stopPrice != null)
			UpdateStopOrder(isLong, stopPrice.Value, volume);
		else
			ResetStopOrder();

		if (takeProfitPrice != null)
			UpdateTakeProfitOrder(isLong, takeProfitPrice.Value, volume);
		else
			ResetTakeProfitOrder();

		_protectionInitialized = true;
		return true;
	}

	private void ApplyTrailing(bool isLong, decimal closePrice)
	{
		if (TrailingStopPips <= 0)
			return;

		var entryPrice = PositionPrice;
		if (entryPrice == 0m)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		if (trailingDistance <= 0m)
			return;

		var stepDistance = TrailingStepPips * _pipSize;
		var minDistanceForMove = trailingDistance + stepDistance;
		if (minDistanceForMove <= 0m)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		if (isLong)
		{
			var profit = closePrice - entryPrice;
			if (profit <= minDistanceForMove)
				return;

			var thresholdPrice = closePrice - minDistanceForMove;
			if (_currentStopPrice.HasValue && _currentStopPrice.Value >= thresholdPrice && _currentStopPrice.Value != 0m)
				return;

			var newStopPrice = closePrice - trailingDistance;
			UpdateStopOrder(true, newStopPrice, volume);
			_protectionInitialized = true;
		}
		else
		{
			var profit = entryPrice - closePrice;
			if (profit <= minDistanceForMove)
				return;

			var thresholdPrice = closePrice + minDistanceForMove;
			if (_currentStopPrice.HasValue && _currentStopPrice.Value <= thresholdPrice && _currentStopPrice.Value != 0m)
				return;

			var newStopPrice = closePrice + trailingDistance;
			UpdateStopOrder(false, newStopPrice, volume);
			_protectionInitialized = true;
		}
	}

	private void UpdateStopOrder(bool isLong, decimal stopPrice, decimal volume)
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active && _stopOrder.Price == stopPrice && _stopOrder.Volume == volume)
			return;

		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		_stopOrder = isLong
		? SellStop(price: stopPrice, volume: volume)
		: BuyStop(price: stopPrice, volume: volume);

		_currentStopPrice = stopPrice;
	}

	private void UpdateTakeProfitOrder(bool isLong, decimal takeProfitPrice, decimal volume)
	{
		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active && _takeProfitOrder.Price == takeProfitPrice && _takeProfitOrder.Volume == volume)
			return;

		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
			CancelOrder(_takeProfitOrder);

		_takeProfitOrder = isLong
		? SellLimit(price: takeProfitPrice, volume: volume)
		: BuyLimit(price: takeProfitPrice, volume: volume);

		_currentTakeProfitPrice = takeProfitPrice;
	}

	private void ResetProtectionOrders()
	{
		ResetStopOrder();
		ResetTakeProfitOrder();
	}

	private void ResetStopOrder()
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		_stopOrder = null;
		_currentStopPrice = null;
	}

	private void ResetTakeProfitOrder()
	{
		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
			CancelOrder(_takeProfitOrder);

		_takeProfitOrder = null;
		_currentTakeProfitPrice = null;
	}
}
