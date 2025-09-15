using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on fast and slow Parabolic SAR combined with Fibonacci levels.
/// It places pending limit orders at 50% retracement and exits at predefined stop or target.
/// </summary>
public class FiboIsarStrategy : Strategy
{
	// parameters
	private readonly StrategyParam<decimal> _stepFast;
	private readonly StrategyParam<decimal> _maxFast;
	private readonly StrategyParam<decimal> _stepSlow;
	private readonly StrategyParam<decimal> _maxSlow;
	private readonly StrategyParam<int> _countBarSearch;
	private readonly StrategyParam<int> _indentStopLoss;
	private readonly StrategyParam<decimal> _fiboEntranceLevel;
	private readonly StrategyParam<decimal> _fiboProfitLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _stopHour;

	// indicators
	private ParabolicSar _fastSar = null!;
	private ParabolicSar _slowSar = null!;
	private Highest _highest = null!;
	private Lowest _lowest = null!;

	// state
	private Order? _pendingOrder;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	/// <summary>
	/// Step for the fast SAR indicator.
	/// </summary>
	public decimal StepFast { get => _stepFast.Value; set => _stepFast.Value = value; }

	/// <summary>
	/// Maximum acceleration for the fast SAR indicator.
	/// </summary>
	public decimal MaximumFast { get => _maxFast.Value; set => _maxFast.Value = value; }

	/// <summary>
	/// Step for the slow SAR indicator.
	/// </summary>
	public decimal StepSlow { get => _stepSlow.Value; set => _stepSlow.Value = value; }

	/// <summary>
	/// Maximum acceleration for the slow SAR indicator.
	/// </summary>
	public decimal MaximumSlow { get => _maxSlow.Value; set => _maxSlow.Value = value; }

	/// <summary>
	/// Lookback period for searching extremes.
	/// </summary>
	public int CountBarSearch { get => _countBarSearch.Value; set => _countBarSearch.Value = value; }

	/// <summary>
	/// Stop loss offset in pips.
	/// </summary>
	public int IndentStopLoss { get => _indentStopLoss.Value; set => _indentStopLoss.Value = value; }

	/// <summary>
	/// Fibonacci level for placing limit orders (percent).
	/// </summary>
	public decimal FiboEntranceLevel { get => _fiboEntranceLevel.Value; set => _fiboEntranceLevel.Value = value; }

	/// <summary>
	/// Fibonacci level for profit taking (percent).
	/// </summary>
	public decimal FiboProfitLevel { get => _fiboProfitLevel.Value; set => _fiboProfitLevel.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Enable trading hours filter.
	/// </summary>
	public bool UseTimeFilter { get => _useTimeFilter.Value; set => _useTimeFilter.Value = value; }

	/// <summary>
	/// Start hour for trading (local time).
	/// </summary>
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }

	/// <summary>
	/// Stop hour for trading (local time).
	/// </summary>
	public int StopHour { get => _stopHour.Value; set => _stopHour.Value = value; }
	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public FiboIsarStrategy()
	{
		_stepFast = Param(nameof(StepFast), 0.02m)
			.SetDisplay("Fast SAR Step", "Acceleration step for fast SAR", "Indicators")
			.SetCanOptimize(true);

		_maxFast = Param(nameof(MaximumFast), 0.2m)
			.SetDisplay("Fast SAR Max", "Maximum acceleration for fast SAR", "Indicators")
			.SetCanOptimize(true);

		_stepSlow = Param(nameof(StepSlow), 0.01m)
			.SetDisplay("Slow SAR Step", "Acceleration step for slow SAR", "Indicators")
			.SetCanOptimize(true);

		_maxSlow = Param(nameof(MaximumSlow), 0.1m)
			.SetDisplay("Slow SAR Max", "Maximum acceleration for slow SAR", "Indicators")
			.SetCanOptimize(true);

		_countBarSearch = Param(nameof(CountBarSearch), 3)
			.SetGreaterThanZero()
			.SetDisplay("Count Bar Search", "Lookback for extremes", "General")
			.SetCanOptimize(true);

		_indentStopLoss = Param(nameof(IndentStopLoss), 30)
			.SetDisplay("Indent Stop Loss", "Stop loss offset in pips", "General")
			.SetCanOptimize(true);

		_fiboEntranceLevel = Param(nameof(FiboEntranceLevel), 50m)
			.SetDisplay("Fibo Entry", "Fibonacci entry level percentage", "Fibonacci")
			.SetCanOptimize(true);

		_fiboProfitLevel = Param(nameof(FiboProfitLevel), 161m)
			.SetDisplay("Fibo Profit", "Fibonacci profit level percentage", "Fibonacci")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_useTimeFilter = Param(nameof(UseTimeFilter), false)
			.SetDisplay("Use Time Filter", "Enable trading hours filter", "General");

		_startHour = Param(nameof(StartHour), 7)
			.SetDisplay("Start Hour", "Trading start hour", "General");

		_stopHour = Param(nameof(StopHour), 17)
			.SetDisplay("Stop Hour", "Trading stop hour", "General");
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
		_pendingOrder = null;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_fastSar = new ParabolicSar { Acceleration = StepFast, AccelerationMax = MaximumFast };
		_slowSar = new ParabolicSar { Acceleration = StepSlow, AccelerationMax = MaximumSlow };
		_highest = new Highest { Length = CountBarSearch };
		_lowest = new Lowest { Length = CountBarSearch };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_fastSar, _slowSar, _highest, _lowest, ProcessCandle).Start();
	}
	private void ProcessCandle(ICandleMessage candle, decimal fastSar, decimal slowSar, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hour = candle.CloseTime.LocalDateTime.Hour;
		if (UseTimeFilter && (hour < StartHour || hour > StopHour))
		{
			CancelPendingOrder();
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;

		// Manage open position exits
		if (Position > 0)
		{
			if (price <= _stopPrice || price >= _takeProfitPrice)
				ClosePosition();
		}
		else if (Position < 0)
		{
			if (price >= _stopPrice || price <= _takeProfitPrice)
				ClosePosition();
		}

		// Remove pending order if opposite trend detected
		if (_pendingOrder != null)
		{
			var side = _pendingOrder.Side;
			if ((side == Sides.Buy && (slowSar > fastSar || fastSar >= price)) ||
				(side == Sides.Sell && (slowSar < fastSar || fastSar <= price)))
			{
				CancelPendingOrder();
			}
		}

		if (_pendingOrder != null || Position != 0)
			return;

		var rangeHigh = highest;
		var rangeLow = lowest;
		var range = rangeHigh - rangeLow;

		var volume = Volume + Math.Abs(Position);

		if (slowSar < fastSar && fastSar < price)
		{
			var entry = rangeLow + range * (FiboEntranceLevel / 100m);
			var profit = rangeLow + range * (FiboProfitLevel / 100m);
			var stop = rangeLow - IndentStopLoss * Security.Step;

			_stopPrice = stop;
			_takeProfitPrice = profit;
			_pendingOrder = BuyLimit(volume, entry);
		}
		else if (slowSar > fastSar && fastSar > price)
		{
			var entry = rangeHigh - range * (FiboEntranceLevel / 100m);
			var profit = rangeHigh - range * (FiboProfitLevel / 100m);
			var stop = rangeHigh + IndentStopLoss * Security.Step;

			_stopPrice = stop;
			_takeProfitPrice = profit;
			_pendingOrder = SellLimit(volume, entry);
		}
	}
	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (_pendingOrder != null && order == _pendingOrder && order.State != OrderStates.Active)
			_pendingOrder = null;
	}

	private void CancelPendingOrder()
	{
		if (_pendingOrder != null)
		{
			CancelOrder(_pendingOrder);
			_pendingOrder = null;
		}
	}
}
