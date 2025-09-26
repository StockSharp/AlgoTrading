namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class MTrendLineStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _regressionLength;
	private readonly StrategyParam<decimal> _pointValue;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _minDistancePoints;

	private readonly StrategyParam<bool> _slot1Enabled;
	private readonly StrategyParam<PendingOrderType> _slot1Mode;
	private readonly StrategyParam<decimal> _slot1DistancePoints;
	private readonly StrategyParam<decimal> _slot1Volume;

	private readonly StrategyParam<bool> _slot2Enabled;
	private readonly StrategyParam<PendingOrderType> _slot2Mode;
	private readonly StrategyParam<decimal> _slot2DistancePoints;
	private readonly StrategyParam<decimal> _slot2Volume;

	private readonly StrategyParam<bool> _slot3Enabled;
	private readonly StrategyParam<PendingOrderType> _slot3Mode;
	private readonly StrategyParam<decimal> _slot3DistancePoints;
	private readonly StrategyParam<decimal> _slot3Volume;

	private LinearRegression _regression = null!;
	private decimal _pointSize;
	private decimal _stopLossOffset;
	private decimal _takeProfitOffset;
	private decimal _minDistance;
	private decimal _priceTolerance;
	private decimal? _bestBid;
	private decimal? _bestAsk;

	private readonly PendingOrderState[] _states;
	// Keeps runtime information for each slot so we can cancel or refresh orders deterministically.

	/// <summary>
	/// Supported pending order styles.
	/// </summary>
	public enum PendingOrderType
	{
		/// <summary>
		/// Buys below the current market with a limit order.
		/// </summary>
		BuyLimit,

		/// <summary>
		/// Buys above the market using a stop order.
		/// </summary>
		BuyStop,

		/// <summary>
		/// Sells above the market using a limit order.
		/// </summary>
		SellLimit,

		/// <summary>
		/// Sells below the market using a stop order.
		/// </summary>
		SellStop
	}

	private sealed class PendingOrderState
	{
		public Order ActiveOrder;
		public decimal? LastPrice;
	}

	public MTrendLineStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for the regression trend line.", "General");

		_regressionLength = Param(nameof(RegressionLength), 24)
			.SetGreaterThanZero()
			.SetDisplay("Regression Length", "Number of candles included in the linear regression.", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(12, 48, 12);

		_pointValue = Param(nameof(PointValue), 0m)
			.SetNotNegative()
			.SetDisplay("Point Value", "Monetary value of one MetaTrader point (0 = use security price step).", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Default volume used for each pending order.", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pts)", "Distance of the protective stop relative to the order price.", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pts)", "Distance of the profit target relative to the order price.", "Risk");

		_minDistancePoints = Param(nameof(MinDistancePoints), 0m)
			.SetNotNegative()
			.SetDisplay("Min Distance (pts)", "Minimal distance to best bid/ask before adjusting orders.", "Risk");

		_slot1Enabled = Param(nameof(PendingOrder1Enabled), true)
			.SetDisplay("Slot 1 Enabled", "Enable the first pending order slot.", "Pending Order #1");

		_slot1Mode = Param(nameof(PendingOrder1Mode), PendingOrderType.BuyLimit)
			.SetDisplay("Slot 1 Type", "Pending order style used by the first slot.", "Pending Order #1");

		_slot1DistancePoints = Param(nameof(PendingOrder1DistancePoints), 10m)
			.SetDisplay("Slot 1 Distance", "Offset from the regression line in MetaTrader points.", "Pending Order #1");

		_slot1Volume = Param(nameof(PendingOrder1Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Slot 1 Volume", "Order volume for the first slot (falls back to Trade Volume when zero).", "Pending Order #1");

		_slot2Enabled = Param(nameof(PendingOrder2Enabled), false)
			.SetDisplay("Slot 2 Enabled", "Enable the second pending order slot.", "Pending Order #2");

		_slot2Mode = Param(nameof(PendingOrder2Mode), PendingOrderType.SellLimit)
			.SetDisplay("Slot 2 Type", "Pending order style used by the second slot.", "Pending Order #2");

		_slot2DistancePoints = Param(nameof(PendingOrder2DistancePoints), 10m)
			.SetDisplay("Slot 2 Distance", "Offset from the regression line in MetaTrader points.", "Pending Order #2");

		_slot2Volume = Param(nameof(PendingOrder2Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Slot 2 Volume", "Order volume for the second slot (falls back to Trade Volume when zero).", "Pending Order #2");

		_slot3Enabled = Param(nameof(PendingOrder3Enabled), false)
			.SetDisplay("Slot 3 Enabled", "Enable the third pending order slot.", "Pending Order #3");

		_slot3Mode = Param(nameof(PendingOrder3Mode), PendingOrderType.BuyStop)
			.SetDisplay("Slot 3 Type", "Pending order style used by the third slot.", "Pending Order #3");

		_slot3DistancePoints = Param(nameof(PendingOrder3DistancePoints), 10m)
			.SetDisplay("Slot 3 Distance", "Offset from the regression line in MetaTrader points.", "Pending Order #3");

		_slot3Volume = Param(nameof(PendingOrder3Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Slot 3 Volume", "Order volume for the third slot (falls back to Trade Volume when zero).", "Pending Order #3");

		_states = new[]
		{
			new PendingOrderState(),
			new PendingOrderState(),
			new PendingOrderState()
		};
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RegressionLength
	{
		get => _regressionLength.Value;
		set => _regressionLength.Value = value;
	}

	public decimal PointValue
	{
		get => _pointValue.Value;
		set => _pointValue.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public decimal MinDistancePoints
	{
		get => _minDistancePoints.Value;
		set => _minDistancePoints.Value = value;
	}

	public bool PendingOrder1Enabled
	{
		get => _slot1Enabled.Value;
		set => _slot1Enabled.Value = value;
	}

	public PendingOrderType PendingOrder1Mode
	{
		get => _slot1Mode.Value;
		set => _slot1Mode.Value = value;
	}

	public decimal PendingOrder1DistancePoints
	{
		get => _slot1DistancePoints.Value;
		set => _slot1DistancePoints.Value = value;
	}

	public decimal PendingOrder1Volume
	{
		get => _slot1Volume.Value;
		set => _slot1Volume.Value = value;
	}

	public bool PendingOrder2Enabled
	{
		get => _slot2Enabled.Value;
		set => _slot2Enabled.Value = value;
	}

	public PendingOrderType PendingOrder2Mode
	{
		get => _slot2Mode.Value;
		set => _slot2Mode.Value = value;
	}

	public decimal PendingOrder2DistancePoints
	{
		get => _slot2DistancePoints.Value;
		set => _slot2DistancePoints.Value = value;
	}

	public decimal PendingOrder2Volume
	{
		get => _slot2Volume.Value;
		set => _slot2Volume.Value = value;
	}

	public bool PendingOrder3Enabled
	{
		get => _slot3Enabled.Value;
		set => _slot3Enabled.Value = value;
	}

	public PendingOrderType PendingOrder3Mode
	{
		get => _slot3Mode.Value;
		set => _slot3Mode.Value = value;
	}

	public decimal PendingOrder3DistancePoints
	{
		get => _slot3DistancePoints.Value;
		set => _slot3DistancePoints.Value = value;
	}

	public decimal PendingOrder3Volume
	{
		get => _slot3Volume.Value;
		set => _slot3Volume.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pointSize = 0m;
		_stopLossOffset = 0m;
		_takeProfitOffset = 0m;
		_minDistance = 0m;
		_priceTolerance = 0m;
		_bestBid = null;
		_bestAsk = null;

		foreach (var state in _states)
		{
			state.ActiveOrder = null;
			state.LastPrice = null;
		}
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;
	// Align helper methods (Buy/Sell helpers) with the configured trade volume.

		_pointSize = PointValue;
		if (_pointSize <= 0m)
		{
			_pointSize = Security?.PriceStep ?? 0m;
		}

		if (_pointSize <= 0m)
		{
			_pointSize = 0.0001m;
		}

		_stopLossOffset = StopLossPoints * _pointSize;
		_takeProfitOffset = TakeProfitPoints * _pointSize;
		_minDistance = MinDistancePoints * _pointSize;
		_priceTolerance = _pointSize / 10m;

		_regression = new LinearRegression
		{
			Length = RegressionLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_regression, ProcessCandle)
			.Start();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _regression);
			DrawOwnTrades(area);
		}
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		// Cache the latest best bid/ask to validate minimum distance constraints.
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid) && bid is decimal bidPrice)
		{
			_bestBid = bidPrice;
		}

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask) && ask is decimal askPrice)
		{
			_bestAsk = askPrice;
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue regressionValue)
	{
		// Work only with finished candles to mimic the original MetaTrader timing.
		if (candle.State != CandleStates.Finished)
			return;

		if (!_regression.IsFormed)
			return;

		var regression = (LinearRegressionValue)regressionValue;
		if (regression.LinearReg is not decimal basePrice)
			return;

		UpdateSlot(0, basePrice);
		UpdateSlot(1, basePrice);
		UpdateSlot(2, basePrice);
	}

	private void UpdateSlot(int index, decimal basePrice)
	{
		// Select the slot configuration and determine whether it should manage a pending order.
		var state = _states[index];
		var enabled = index switch
		{
			0 => PendingOrder1Enabled,
			1 => PendingOrder2Enabled,
			2 => PendingOrder3Enabled,
			_ => false
		};

		if (!enabled)
		{
			CancelSlot(state);
			return;
		}

		var mode = index switch
		{
			0 => PendingOrder1Mode,
			1 => PendingOrder2Mode,
			2 => PendingOrder3Mode,
			_ => PendingOrderType.BuyLimit
		};

		var distance = index switch
		{
			0 => PendingOrder1DistancePoints,
			1 => PendingOrder2DistancePoints,
			2 => PendingOrder3DistancePoints,
			_ => 0m
		};

		var volume = index switch
		{
			0 => PendingOrder1Volume,
			1 => PendingOrder2Volume,
			2 => PendingOrder3Volume,
			_ => 0m
		};

		var actualVolume = volume > 0m ? volume : TradeVolume;
		if (actualVolume <= 0m)
			return;

		var targetPrice = basePrice + distance * _pointSize;
		if (targetPrice <= 0m)
		{
			CancelSlot(state);
			return;
		}

		// Translate the slot mode into a StockSharp order helper and target order type.
		var (side, orderType) = mode switch
		{
			PendingOrderType.BuyLimit => (Sides.Buy, OrderTypes.Limit),
			PendingOrderType.BuyStop => (Sides.Buy, OrderTypes.Stop),
			PendingOrderType.SellLimit => (Sides.Sell, OrderTypes.Limit),
			PendingOrderType.SellStop => (Sides.Sell, OrderTypes.Stop),
			_ => (Sides.Buy, OrderTypes.Limit)
		};

		if (!CheckMinimalDistance(side, orderType, targetPrice))
			return;

		var stopLossPrice = CalculateStopLoss(side, targetPrice);
		var takeProfitPrice = CalculateTakeProfit(side, targetPrice);

		if (state.ActiveOrder is { } existing)
		{
			// Cancel the previous pending order if the price needs to move.
			if (existing.State.IsActive())
			{
				if (state.LastPrice is decimal lastPrice && Math.Abs(lastPrice - targetPrice) <= _priceTolerance)
					return;

				CancelOrder(existing);
				state.ActiveOrder = null;
				state.LastPrice = null;
				return;
			}

			state.ActiveOrder = null;
			state.LastPrice = null;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		Order newOrder = (side, orderType) switch
		{
			(Sides.Buy, OrderTypes.Limit) => BuyLimit(actualVolume, targetPrice, stopLoss: stopLossPrice, takeProfit: takeProfitPrice),
			(Sides.Buy, OrderTypes.Stop) => BuyStop(actualVolume, targetPrice, stopLoss: stopLossPrice, takeProfit: takeProfitPrice),
			(Sides.Sell, OrderTypes.Limit) => SellLimit(actualVolume, targetPrice, stopLoss: stopLossPrice, takeProfit: takeProfitPrice),
			(Sides.Sell, OrderTypes.Stop) => SellStop(actualVolume, targetPrice, stopLoss: stopLossPrice, takeProfit: takeProfitPrice),
			_ => null
		};

		if (newOrder == null)
			return;

		newOrder.Comment = $"MTrendLine slot {index + 1}";
		state.ActiveOrder = newOrder;
		state.LastPrice = targetPrice;
	}

	private void CancelSlot(PendingOrderState state)
	{
		// Helper that safely cancels the slot order and resets cached state.
		if (state.ActiveOrder is not { } order)
			return;

		if (order.State.IsActive())
			CancelOrder(order);

		state.ActiveOrder = null;
		state.LastPrice = null;
	}

	private bool CheckMinimalDistance(Sides side, OrderTypes orderType, decimal price)
	{
		if (_minDistance <= 0m)
			return true;

		return (side, orderType) switch
		{
			(Sides.Buy, OrderTypes.Limit) => _bestAsk is not decimal ask || ask - price >= _minDistance,
			(Sides.Buy, OrderTypes.Stop) => _bestAsk is not decimal ask || price - ask >= _minDistance,
			(Sides.Sell, OrderTypes.Limit) => _bestBid is not decimal bid || price - bid >= _minDistance,
			(Sides.Sell, OrderTypes.Stop) => _bestBid is not decimal bid || bid - price >= _minDistance,
			_ => true
		};
	}

	private decimal? CalculateStopLoss(Sides side, decimal price)
	{
		if (_stopLossOffset <= 0m)
			return null;

		return side == Sides.Buy ? price - _stopLossOffset : price + _stopLossOffset;
	}

	private decimal? CalculateTakeProfit(Sides side, decimal price)
	{
		if (_takeProfitOffset <= 0m)
			return null;

		return side == Sides.Buy ? price + _takeProfitOffset : price - _takeProfitOffset;
	}
}
