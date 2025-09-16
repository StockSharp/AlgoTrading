using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Logging;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the Parabolic SAR pending order logic with Fibonacci targets.
/// </summary>
public class ParabolicSarFiboLimitsStrategy : Strategy
{
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _stopHour;
	private readonly StrategyParam<decimal> _fastStep;
	private readonly StrategyParam<decimal> _fastMaxStep;
	private readonly StrategyParam<decimal> _slowStep;
	private readonly StrategyParam<decimal> _slowMaxStep;
	private readonly StrategyParam<int> _barSearch;
	private readonly StrategyParam<decimal> _offsetPoints;
	private readonly StrategyParam<decimal> _breakEvenPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<decimal> _firstFiboPercent;
	private readonly StrategyParam<decimal> _secondFiboPercent;
	private readonly StrategyParam<DataType> _candleType;

	private ParabolicSar? _slowSar;
	private ParabolicSar? _fastSar;
	private Highest? _highestHigh;
	private Lowest? _lowestLow;

	private decimal _priceStep;

	private bool _pendingLongSetup;
	private bool _pendingShortSetup;

	private decimal? _previousHigh;
	private decimal? _previousLow;

	private Order? _buyLimitOrder;
	private Order? _sellLimitOrder;

	private decimal? _pendingLongStop;
	private decimal? _pendingLongTake;
	private decimal? _pendingShortStop;
	private decimal? _pendingShortTake;

	private decimal? _longStopPrice;
	private decimal? _longTakeProfit;
	private decimal? _shortStopPrice;
	private decimal? _shortTakeProfit;

	/// <summary>
	/// Initializes a new instance of the <see cref="ParabolicSarFiboLimitsStrategy"/> class.
	/// </summary>
	public ParabolicSarFiboLimitsStrategy()
	{
		_useTimeFilter = Param(nameof(UseTimeFilter), false)
			.SetDisplay("Use Time Filter", "Enable trading only inside the configured session", "General");

		_startHour = Param(nameof(StartHour), 7)
			.SetDisplay("Start Hour", "Session start hour in exchange time", "General");

		_stopHour = Param(nameof(StopHour), 17)
			.SetDisplay("Stop Hour", "Session stop hour in exchange time", "General");

		_fastStep = Param(nameof(FastAcceleration), 0.02m)
			.SetDisplay("Fast SAR Step", "Acceleration factor for the fast SAR", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.05m, 0.01m);

		_fastMaxStep = Param(nameof(FastAccelerationMax), 0.2m)
			.SetDisplay("Fast SAR Max", "Maximum acceleration factor for the fast SAR", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.5m, 0.1m);

		_slowStep = Param(nameof(SlowAcceleration), 0.005m)
			.SetDisplay("Slow SAR Step", "Acceleration factor for the slow SAR", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.001m, 0.02m, 0.001m);

		_slowMaxStep = Param(nameof(SlowAccelerationMax), 0.05m)
			.SetDisplay("Slow SAR Max", "Maximum acceleration factor for the slow SAR", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.02m, 0.2m, 0.02m);

		_barSearch = Param(nameof(BarSearch), 3)
			.SetGreaterThanZero()
			.SetDisplay("Bar Search", "Depth in bars used to locate swing points", "Signals");

		_offsetPoints = Param(nameof(OffsetPoints), 100m)
			.SetDisplay("Offset (points)", "Extra distance in points for protective stops", "Risk");

		_breakEvenPoints = Param(nameof(BreakEvenPoints), 0m)
			.SetDisplay("Break Even (points)", "Profit in points required before moving the stop to break even", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 0m)
			.SetDisplay("Trailing Stop (points)", "Distance in points for the trailing stop", "Risk");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 5m)
			.SetDisplay("Trailing Step (points)", "Minimum profit increment before the trailing stop is moved", "Risk");

		_firstFiboPercent = Param(nameof(FirstFiboPercent), 50m)
			.SetDisplay("Entry Fibonacci %", "Fibonacci percentage used for the limit entry", "Signals");

		_secondFiboPercent = Param(nameof(SecondFiboPercent), 161m)
			.SetDisplay("Target Fibonacci %", "Fibonacci percentage used for the take profit", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for indicator calculations", "General");

		Volume = 0.1m;
	}

	/// <summary>
	/// Gets or sets a value indicating whether the strategy trades only inside the configured session.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Gets or sets the trading session start hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Gets or sets the trading session stop hour.
	/// </summary>
	public int StopHour
	{
		get => _stopHour.Value;
		set => _stopHour.Value = value;
	}

	/// <summary>
	/// Gets or sets the fast Parabolic SAR acceleration.
	/// </summary>
	public decimal FastAcceleration
	{
		get => _fastStep.Value;
		set => _fastStep.Value = value;
	}

	/// <summary>
	/// Gets or sets the maximum fast Parabolic SAR acceleration.
	/// </summary>
	public decimal FastAccelerationMax
	{
		get => _fastMaxStep.Value;
		set => _fastMaxStep.Value = value;
	}

	/// <summary>
	/// Gets or sets the slow Parabolic SAR acceleration.
	/// </summary>
	public decimal SlowAcceleration
	{
		get => _slowStep.Value;
		set => _slowStep.Value = value;
	}

	/// <summary>
	/// Gets or sets the maximum slow Parabolic SAR acceleration.
	/// </summary>
	public decimal SlowAccelerationMax
	{
		get => _slowMaxStep.Value;
		set => _slowMaxStep.Value = value;
	}

	/// <summary>
	/// Gets or sets the number of bars used to locate swing points.
	/// </summary>
	public int BarSearch
	{
		get => _barSearch.Value;
		set => _barSearch.Value = value;
	}

	/// <summary>
	/// Gets or sets the protective stop offset expressed in points.
	/// </summary>
	public decimal OffsetPoints
	{
		get => _offsetPoints.Value;
		set => _offsetPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets the break even trigger distance expressed in points.
	/// </summary>
	public decimal BreakEvenPoints
	{
		get => _breakEvenPoints.Value;
		set => _breakEvenPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets the trailing stop distance expressed in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets the trailing stop step expressed in points.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets the Fibonacci percentage used for pending orders.
	/// </summary>
	public decimal FirstFiboPercent
	{
		get => _firstFiboPercent.Value;
		set => _firstFiboPercent.Value = value;
	}

	/// <summary>
	/// Gets or sets the Fibonacci percentage used for take profit calculation.
	/// </summary>
	public decimal SecondFiboPercent
	{
		get => _secondFiboPercent.Value;
		set => _secondFiboPercent.Value = value;
	}

	/// <summary>
	/// Gets or sets the candle type used for calculations.
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

		_slowSar = null;
		_fastSar = null;
		_highestHigh = null;
		_lowestLow = null;

		_pendingLongSetup = false;
		_pendingShortSetup = false;

		_previousHigh = null;
		_previousLow = null;

		_buyLimitOrder = null;
		_sellLimitOrder = null;

		_pendingLongStop = null;
		_pendingLongTake = null;
		_pendingShortStop = null;
		_pendingShortTake = null;

		_longStopPrice = null;
		_longTakeProfit = null;
		_shortStopPrice = null;
		_shortTakeProfit = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security.PriceStep ?? 0m;
		if (_priceStep <= 0m)
		{
			_priceStep = 0.0001m;
		}

		_slowSar = new ParabolicSar
		{
			Acceleration = SlowAcceleration,
			AccelerationMax = SlowAccelerationMax
		};

		_fastSar = new ParabolicSar
		{
			Acceleration = FastAcceleration,
			AccelerationMax = FastAccelerationMax
		};

		_highestHigh = new Highest
		{
			Length = BarSearch,
			CandlePrice = CandlePrice.High
		};

		_lowestLow = new Lowest
		{
			Length = BarSearch,
			CandlePrice = CandlePrice.Low
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_slowSar, _fastSar, _highestHigh, _lowestLow, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _slowSar);
			DrawIndicator(area, _fastSar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal slowSar, decimal fastSar, decimal highestHigh, decimal lowestLow)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		UpdateProtection(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			return;
		}

		if (UseTimeFilter && !IsWithinTradingWindow(candle.CloseTime))
		{
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			return;
		}

		var price = candle.ClosePrice;

		if (fastSar > price && slowSar < price)
		{
			_pendingLongSetup = true;
		}

		if (slowSar > price)
		{
			_pendingLongSetup = false;
		}

		if (fastSar < price && slowSar > price)
		{
			_pendingShortSetup = true;
		}

		if (slowSar < price)
		{
			_pendingShortSetup = false;
		}

		if (fastSar > price)
		{
			CancelBuyLimit();
		}
		else if (fastSar < price)
		{
			CancelSellLimit();
		}

		if (_slowSar?.IsFormed != true || _fastSar?.IsFormed != true || _highestHigh?.IsFormed != true || _lowestLow?.IsFormed != true)
		{
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			return;
		}

		if (slowSar < price && fastSar < price && _pendingLongSetup)
		{
			TryPlaceBuyLimit(candle, lowestLow);
			_pendingLongSetup = false;
		}

		if (slowSar > price && fastSar > price && _pendingShortSetup)
		{
			TryPlaceSellLimit(candle, highestHigh);
			_pendingShortSetup = false;
		}

		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
	}

	private void TryPlaceBuyLimit(ICandleMessage candle, decimal swingLow)
	{
		if (Volume <= 0m || _previousHigh is null)
		{
			return;
		}

		var min = swingLow;
		var max = _previousHigh.Value;
		if (max <= min)
		{
			return;
		}

		var entry = CalculateFiboLevel(max, min, FirstFiboPercent / 100m);
		var stop = min - GetOffset(OffsetPoints);
		var take = CalculateFiboLevel(max, min, SecondFiboPercent / 100m);

		var price = candle.ClosePrice;
		var minDistance = GetOffset(5m);
		if (minDistance <= 0m)
		{
			minDistance = _priceStep;
		}

		if (price - entry < minDistance || price - stop < minDistance || take - price < minDistance)
		{
			return;
		}

		entry = NormalizePrice(entry);
		stop = NormalizePrice(stop);
		take = NormalizePrice(take);

		CancelBuyLimit();
		_buyLimitOrder = BuyLimit(Volume, entry);
		_pendingLongStop = stop;
		_pendingLongTake = take;
		this.LogInfo($"Submitted buy limit {entry} with stop {stop} and target {take}.");
	}

	private void TryPlaceSellLimit(ICandleMessage candle, decimal swingHigh)
	{
		if (Volume <= 0m || _previousLow is null)
		{
			return;
		}

		var max = swingHigh;
		var min = _previousLow.Value;
		if (max <= min)
		{
			return;
		}

		var entry = CalculateFiboLevel(min, max, FirstFiboPercent / 100m);
		var stop = max + GetOffset(OffsetPoints);
		var take = CalculateFiboLevel(min, max, SecondFiboPercent / 100m);

		var price = candle.ClosePrice;
		var minDistance = GetOffset(5m);
		if (minDistance <= 0m)
		{
			minDistance = _priceStep;
		}

		if (entry - price < minDistance || stop - price < minDistance || price - take < minDistance)
		{
			return;
		}

		entry = NormalizePrice(entry);
		stop = NormalizePrice(stop);
		take = NormalizePrice(take);

		CancelSellLimit();
		_sellLimitOrder = SellLimit(Volume, entry);
		_pendingShortStop = stop;
		_pendingShortTake = take;
		this.LogInfo($"Submitted sell limit {entry} with stop {stop} and target {take}.");
	}

	private void UpdateProtection(ICandleMessage candle)
	{
		var price = candle.ClosePrice;

		if (Position > 0m)
		{
			var entry = PositionPrice;
			if (entry == 0m && _pendingLongStop.HasValue)
			{
				entry = candle.ClosePrice;
			}

			if (_longStopPrice is null && _pendingLongStop is decimal stop)
			{
				_longStopPrice = stop;
			}

			if (_longTakeProfit is null && _pendingLongTake is decimal take)
			{
				_longTakeProfit = take;
			}

			ApplyTrailingForLong(price, entry);
			ApplyBreakEvenForLong(price, entry);

			if (_longTakeProfit is decimal longTake && price >= longTake)
			{
				SellMarket(Position);
				ResetLongProtection();
				return;
			}

			if (_longStopPrice is decimal longStop && price <= longStop)
			{
				SellMarket(Position);
				ResetLongProtection();
			}
		}
		else if (Position < 0m)
		{
			var entry = PositionPrice;
			if (entry == 0m && _pendingShortStop.HasValue)
			{
				entry = candle.ClosePrice;
			}

			if (_shortStopPrice is null && _pendingShortStop is decimal stop)
			{
				_shortStopPrice = stop;
			}

			if (_shortTakeProfit is null && _pendingShortTake is decimal take)
			{
				_shortTakeProfit = take;
			}

			ApplyTrailingForShort(price, entry);
			ApplyBreakEvenForShort(price, entry);

			if (_shortTakeProfit is decimal shortTake && price <= shortTake)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortProtection();
				return;
			}

			if (_shortStopPrice is decimal shortStop && price >= shortStop)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortProtection();
			}
		}
		else
		{
			ResetLongProtection();
			ResetShortProtection();
		}
	}

	private void ApplyTrailingForLong(decimal price, decimal entry)
	{
		if (TrailingStopPoints <= 0m || entry == 0m)
		{
			return;
		}

		var trail = GetOffset(TrailingStopPoints);
		var step = GetOffset(TrailingStepPoints);
		if (price - entry < trail)
		{
			return;
		}

		var newStop = price - trail;
		if (_longStopPrice is null || newStop - _longStopPrice >= step)
		{
			_longStopPrice = newStop;
		}
	}

	private void ApplyTrailingForShort(decimal price, decimal entry)
	{
		if (TrailingStopPoints <= 0m || entry == 0m)
		{
			return;
		}

		var trail = GetOffset(TrailingStopPoints);
		var step = GetOffset(TrailingStepPoints);
		if (entry - price < trail)
		{
			return;
		}

		var newStop = price + trail;
		if (_shortStopPrice is null || _shortStopPrice - newStop >= step)
		{
			_shortStopPrice = newStop;
		}
	}

	private void ApplyBreakEvenForLong(decimal price, decimal entry)
	{
		if (BreakEvenPoints <= 0m || entry == 0m)
		{
			return;
		}

		var threshold = entry + GetOffset(BreakEvenPoints);
		var desiredStop = entry + _priceStep;
		if (price >= threshold && (_longStopPrice is null || _longStopPrice < desiredStop))
		{
			_longStopPrice = desiredStop;
		}
	}

	private void ApplyBreakEvenForShort(decimal price, decimal entry)
	{
		if (BreakEvenPoints <= 0m || entry == 0m)
		{
			return;
		}

		var threshold = entry - GetOffset(BreakEvenPoints);
		var desiredStop = entry - _priceStep;
		if (price <= threshold && (_shortStopPrice is null || _shortStopPrice > desiredStop))
		{
			_shortStopPrice = desiredStop;
		}
	}

	private void ResetLongProtection()
	{
		_pendingLongStop = null;
		_pendingLongTake = null;
		_longStopPrice = null;
		_longTakeProfit = null;
	}

	private void ResetShortProtection()
	{
		_pendingShortStop = null;
		_pendingShortTake = null;
		_shortStopPrice = null;
		_shortTakeProfit = null;
	}

	private decimal CalculateFiboLevel(decimal high, decimal low, decimal factor)
	{
		return low + (high - low) * factor;
	}

	private void CancelBuyLimit()
	{
		if (_buyLimitOrder != null && _buyLimitOrder.State == OrderStates.Active)
		{
			CancelOrder(_buyLimitOrder);
		}
		_buyLimitOrder = null;
	}

	private void CancelSellLimit()
	{
		if (_sellLimitOrder != null && _sellLimitOrder.State == OrderStates.Active)
		{
			CancelOrder(_sellLimitOrder);
		}
		_sellLimitOrder = null;
	}

	private decimal GetOffset(decimal points)
	{
		return points * _priceStep;
	}

	private decimal NormalizePrice(decimal price)
	{
		return Security?.ShrinkPrice(price) ?? price;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var hour = time.Hour;
		return hour >= StartHour && hour <= StopHour;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade.Order;
		if (order == null)
		{
			return;
		}

		if (_buyLimitOrder != null && order == _buyLimitOrder)
		{
			_buyLimitOrder = null;
			_longStopPrice = _pendingLongStop;
			_longTakeProfit = _pendingLongTake;
			_pendingLongStop = null;
			_pendingLongTake = null;
			CancelSellLimit();
		}
		else if (_sellLimitOrder != null && order == _sellLimitOrder)
		{
			_sellLimitOrder = null;
			_shortStopPrice = _pendingShortStop;
			_shortTakeProfit = _pendingShortTake;
			_pendingShortStop = null;
			_pendingShortTake = null;
			CancelBuyLimit();
		}
	}

	/// <inheritdoc />
	protected override void OnOrderStateChanged(Order order)
	{
		base.OnOrderStateChanged(order);

		if (_buyLimitOrder != null && order == _buyLimitOrder && order.State != OrderStates.Active)
		{
			_buyLimitOrder = null;
		}

		if (_sellLimitOrder != null && order == _sellLimitOrder && order.State != OrderStates.Active)
		{
			_sellLimitOrder = null;
		}
	}
}
