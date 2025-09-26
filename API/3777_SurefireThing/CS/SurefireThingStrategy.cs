using System;
using System.Collections.Generic;

using StockSharp;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert advisor "Surefirething".
/// Places daily limit orders calculated from the previous session range and closes exposure at the end of each trading day.
/// </summary>
public class SurefireThingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _rangeMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _lastPreparedDay;
	private Order _buyLimitOrder;
	private Order _sellLimitOrder;
	private ICandleMessage _previousCandle;

	/// <summary>
	/// Initializes a new instance of the <see cref="SurefireThingStrategy"/> class.
	/// </summary>
	public SurefireThingStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume submitted with each pending order.", "General")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 10m)
			.SetNotNegative()
			.SetDisplay("Take-Profit Points", "Distance in price steps used for take-profit orders.", "Risk Management")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 15m)
			.SetNotNegative()
			.SetDisplay("Stop-Loss Points", "Distance in price steps used for stop-loss orders.", "Risk Management")
			.SetCanOptimize(true);

		_rangeMultiplier = Param(nameof(RangeMultiplier), 1.1m)
			.SetGreaterThanZero()
			.SetDisplay("Range Multiplier", "Multiplier applied to the previous candle range (1.1 in the original EA).", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe processed by the strategy.", "General");
	}

	/// <summary>
	/// Volume submitted with each pending order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Distance in price steps used for take-profit orders.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Distance in price steps used for stop-loss orders.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the previous candle range.
	/// </summary>
	public decimal RangeMultiplier
	{
		get => _rangeMultiplier.Value;
		set => _rangeMultiplier.Value = value;
	}

	/// <summary>
	/// Type of candles used to monitor the market.
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

		_lastPreparedDay = null;
		_buyLimitOrder = null;
		_sellLimitOrder = null;
		_previousCandle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		ConfigureProtection();

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

	private void ConfigureProtection()
	{
		var step = Security?.Step ?? 0m;

		Unit stopLossUnit = null;
		Unit takeProfitUnit = null;

		if (StopLossPoints > 0m && step > 0m)
			stopLossUnit = new Unit(StopLossPoints * step, UnitTypes.Absolute);

		if (TakeProfitPoints > 0m && step > 0m)
			takeProfitUnit = new Unit(TakeProfitPoints * step, UnitTypes.Absolute);

		StartProtection(
			takeProfit: takeProfitUnit,
			stopLoss: stopLossUnit,
			useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var currentDay = candle.OpenTime.UtcDateTime.Date;

		if (_previousCandle != null)
		{
			var previousDay = _previousCandle.OpenTime.UtcDateTime.Date;

			if (currentDay > previousDay)
			{
				CloseForNewDay();

				if (IsFormedAndOnlineAndAllowTrading())
					PlaceDailyOrders(currentDay, _previousCandle);
			}
		}

		_previousCandle = candle;
	}

	private void CloseForNewDay()
	{
		if (Position != 0)
			ClosePosition();

		CancelPendingOrder(ref _buyLimitOrder);
		CancelPendingOrder(ref _sellLimitOrder);

		_lastPreparedDay = null;
	}

	private void PlaceDailyOrders(DateTime newDay, ICandleMessage referenceCandle)
	{
		if (_lastPreparedDay == newDay)
			return;

		var range = referenceCandle.HighPrice - referenceCandle.LowPrice;
		if (range <= 0m)
			return;

		var adjustedRange = range * RangeMultiplier;
		if (adjustedRange <= 0m)
			return;

		var halfRange = adjustedRange / 2m;

		var sellPrice = NormalizePrice(referenceCandle.ClosePrice + halfRange);
		var buyPrice = NormalizePrice(referenceCandle.ClosePrice - halfRange);

		if (sellPrice <= 0m || buyPrice <= 0m)
			return;

		var volume = OrderVolume > 0m ? OrderVolume : Volume;
		if (volume <= 0m)
			return;

		_buyLimitOrder = BuyLimit(volume, buyPrice);
		_sellLimitOrder = SellLimit(volume, sellPrice);

		_lastPreparedDay = newDay;

		LogInfo($"New day detected. BuyLimit={buyPrice:0.#####}, SellLimit={sellPrice:0.#####}, Range={range:0.#####}.");
	}

	private decimal NormalizePrice(decimal price)
	{
		return Security?.ShrinkPrice(price) ?? price;
	}

	private void CancelPendingOrder(ref Order order)
	{
		if (order == null)
			return;

		if (order.State is OrderStates.Pending or OrderStates.Active)
			CancelOrder(order);

		order = null;
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (_buyLimitOrder != null && order == _buyLimitOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
			_buyLimitOrder = null;

		if (_sellLimitOrder != null && order == _sellLimitOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
			_sellLimitOrder = null;
	}
}
