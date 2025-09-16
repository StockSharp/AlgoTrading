using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hans123 breakout strategy converted from MQL5.
/// Collects an intraday range and trades pending stop orders within a trading window.
/// Applies configurable stop-loss, take-profit, and trailing protection.
/// </summary>
public class Hans123TraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _rangeLength;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private Order? _buyStopOrder;
	private Order? _sellStopOrder;
	private Order? _protectionStopOrder;
	private Order? _protectionTakeOrder;
	private decimal? _entryPrice;
	private decimal _pipSize;

	/// <summary>
	/// Volume used for breakout orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Number of candles that form the breakout range.
	/// </summary>
	public int RangeLength
	{
		get => _rangeLength.Value;
		set => _rangeLength.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Extra move (in pips) before trailing activates again.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Start hour (inclusive) of the trading window.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// End hour (exclusive) of the trading window.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Hans123TraderStrategy"/> class.
	/// </summary>
	public Hans123TraderStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetDisplay("Order Volume", "Breakout order volume", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 2m, 0.1m);

		_rangeLength = Param(nameof(RangeLength), 80)
			.SetGreaterThanZero()
			.SetDisplay("Range Length", "Candles in breakout range", "General")
			.SetCanOptimize(true)
			.SetOptimize(40, 120, 10);

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0, 150, 10);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0, 200, 10);

		_trailingStopPips = Param(nameof(TrailingStopPips), 10)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0, 100, 5);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step (pips)", "Extra pips before trailing updates", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0, 50, 5);

		_startHour = Param(nameof(StartHour), 6)
			.SetDisplay("Start Hour", "Hour (UTC) when orders can be placed", "Schedule")
			.SetCanOptimize(true)
			.SetOptimize(0, 23, 1);

		_endHour = Param(nameof(EndHour), 10)
			.SetDisplay("End Hour", "Hour (UTC) when orders stop", "Schedule")
			.SetCanOptimize(true)
			.SetOptimize(1, 24, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
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

		_buyStopOrder = null;
		_sellStopOrder = null;
		_protectionStopOrder = null;
		_protectionTakeOrder = null;
		_entryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (StartHour < 0 || StartHour > 23)
			throw new ArgumentOutOfRangeException(nameof(StartHour), "Start hour must be between 0 and 23.");
		if (EndHour < 0 || EndHour > 24)
			throw new ArgumentOutOfRangeException(nameof(EndHour), "End hour must be between 0 and 24.");
		if (StartHour >= EndHour)
			throw new ArgumentException("Start hour must be strictly less than end hour.");
		if (TrailingStopPips > 0 && TrailingStepPips <= 0)
			throw new ArgumentException("Trailing step must be positive when trailing stop is enabled.");

		if (Security == null)
			throw new InvalidOperationException("Security must be set before starting the strategy.");

		_pipSize = CalculatePipSize();

		_highest = new Highest { Length = RangeLength };
		_lowest = new Lowest { Length = RangeLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_highest, _lowest, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			// Visualize candles, indicator bands, and executed trades for easier debugging.
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Trailing is evaluated on every completed candle.
		ManageTrailing(candle);

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var inWindow = IsWithinTradingWindow(candle.OpenTime);
		if (!inWindow)
		{
			// Do not keep pending orders outside the trading window.
			CancelEntryOrders();
			return;
		}

		if (OrderVolume <= 0m)
			return;

		if (highest <= lowest)
		{
			CancelEntryOrders();
			return;
		}

		// Refresh breakout orders each bar to follow the current range extremes.
		CancelEntryOrders();

		_buyStopOrder = BuyStop(OrderVolume, highest);
		_sellStopOrder = SellStop(OrderVolume, lowest);
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order?.Security != Security)
			return;

		if (Position > 0)
		{
			// Long position opened or increased: update protection orders.
			SetupProtection(isLong: true);
			CancelEntryOrders();
		}
		else if (Position < 0)
		{
			// Short position opened or increased: update protection orders.
			SetupProtection(isLong: false);
			CancelEntryOrders();
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			_entryPrice = null;
			CancelProtectionOrders();
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0.0001m;
		var decimals = Security?.Decimals;

		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		return time.Hour >= StartHour && time.Hour < EndHour;
	}

	private void SetupProtection(bool isLong)
	{
		_entryPrice = PositionPrice;

		CancelProtectionOrders();

		var volume = Math.Abs(Position);
		if (volume <= 0m || _entryPrice == null)
			return;

		var stopOffset = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;
		var takeOffset = TakeProfitPips > 0 ? TakeProfitPips * _pipSize : 0m;

		if (stopOffset > 0m)
		{
			var stopPrice = isLong
				? _entryPrice.Value - stopOffset
				: _entryPrice.Value + stopOffset;

			if (stopPrice > 0m)
			{
				_protectionStopOrder = isLong
					? SellStop(volume, stopPrice)
					: BuyStop(volume, stopPrice);
			}
		}

		if (takeOffset > 0m)
		{
			var takePrice = isLong
				? _entryPrice.Value + takeOffset
				: _entryPrice.Value - takeOffset;

			_protectionTakeOrder = isLong
				? SellLimit(volume, takePrice)
				: BuyLimit(volume, takePrice);
		}
	}

	private void ManageTrailing(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0 || TrailingStepPips <= 0 || Position == 0 || _entryPrice == null)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var activation = (TrailingStopPips + TrailingStepPips) * _pipSize;
		var tick = Security?.PriceStep ?? 0.0001m;

		if (Position > 0)
		{
			var move = candle.ClosePrice - _entryPrice.Value;
			if (move <= activation)
				return;

			var newStop = candle.ClosePrice - trailingDistance;
			if (_protectionStopOrder == null || newStop > _protectionStopOrder.Price + tick / 2m)
				UpdateTrailingStop(newStop);
		}
		else if (Position < 0)
		{
			var move = _entryPrice.Value - candle.ClosePrice;
			if (move <= activation)
				return;

			var newStop = candle.ClosePrice + trailingDistance;
			if (_protectionStopOrder == null || newStop < _protectionStopOrder.Price - tick / 2m)
				UpdateTrailingStop(newStop);
		}
	}

	private void UpdateTrailingStop(decimal price)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m || price <= 0m)
			return;

		if (_protectionStopOrder != null && _protectionStopOrder.State == OrderStates.Active)
			CancelOrder(_protectionStopOrder);

		// Register a refreshed stop order reflecting the trailing level.
		_protectionStopOrder = Position > 0
			? SellStop(volume, price)
			: BuyStop(volume, price);
	}

	private void CancelEntryOrders()
	{
		if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active)
			CancelOrder(_buyStopOrder);
		if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active)
			CancelOrder(_sellStopOrder);

		_buyStopOrder = null;
		_sellStopOrder = null;
	}

	private void CancelProtectionOrders()
	{
		if (_protectionStopOrder != null && _protectionStopOrder.State == OrderStates.Active)
			CancelOrder(_protectionStopOrder);
		if (_protectionTakeOrder != null && _protectionTakeOrder.State == OrderStates.Active)
			CancelOrder(_protectionTakeOrder);

		_protectionStopOrder = null;
		_protectionTakeOrder = null;
	}
}
