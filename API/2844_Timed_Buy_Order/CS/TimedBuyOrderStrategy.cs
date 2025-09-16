using System;

using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that submits a sequence of market buy orders at one-second intervals.
/// </summary>
public class TimedBuyOrderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _ordersToPlace;
	private readonly StrategyParam<TimeSpan> _interval;

	private int _ordersPlaced;
	private int _expectedSecond;
	private bool _isCompleted;

	/// <summary>
	/// Initializes a new instance of the <see cref="TimedBuyOrderStrategy"/> class.
	/// </summary>
	public TimedBuyOrderStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume for each market buy order", "Trading")
			.SetCanOptimize(true);

		_ordersToPlace = Param(nameof(OrdersToPlace), 60)
			.SetGreaterThanZero()
			.SetDisplay("Orders To Place", "Number of sequential buy orders before stopping", "Trading")
			.SetCanOptimize(true);

		_interval = Param(nameof(Interval), TimeSpan.FromSeconds(1))
			.SetDisplay("Timer Interval", "Delay between timer ticks that trigger order placement", "Timing");
	}

	/// <summary>
	/// Volume for each submitted market order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Total number of buy orders to submit before the strategy stops.
	/// </summary>
	public int OrdersToPlace
	{
		get => _ordersToPlace.Value;
		set => _ordersToPlace.Value = value;
	}

	/// <summary>
	/// Interval between timer callbacks.
	/// </summary>
	public TimeSpan Interval
	{
		get => _interval.Value;
		set => _interval.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ordersPlaced = 0;
		_expectedSecond = 0;
		_isCompleted = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_ordersPlaced = 0;
		_expectedSecond = 0;
		_isCompleted = false;

		Timer.Start(Interval, OnTimer);
	}

	private void OnTimer()
	{
		if (_isCompleted)
			return;

		if (_ordersPlaced >= OrdersToPlace)
		{
			CompleteStrategy();
			return;
		}

		var currentTime = CurrentTime;

		// Synchronize order submission to the expected second within the current minute.
		if (currentTime.Second != _expectedSecond)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = OrderVolume;

		if (volume <= 0)
		{
			LogWarning("Order volume must be positive to send trades.");
			CompleteStrategy();
			return;
		}

		BuyMarket(volume);

		_ordersPlaced++;
		_expectedSecond = (_expectedSecond + 1) % 60;

		LogInfo($"Submitted buy order {_ordersPlaced} of {OrdersToPlace} at {currentTime:HH:mm:ss}.");

		if (_ordersPlaced >= OrdersToPlace)
			CompleteStrategy();
	}

	private void CompleteStrategy()
	{
		if (_isCompleted)
			return;

		_isCompleted = true;
		Stop();
	}
}
