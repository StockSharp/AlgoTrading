namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy that submits a sequence of market buy orders on each candle close.
/// After a configurable number of orders have been placed, it stops.
/// </summary>
public class TimedBuyOrderStrategy : Strategy
{
	private readonly StrategyParam<int> _ordersToPlace;
	private readonly StrategyParam<DataType> _candleType;

	private int _ordersPlaced;

	/// <summary>
	/// Initializes a new instance of the <see cref="TimedBuyOrderStrategy"/> class.
	/// </summary>
	public TimedBuyOrderStrategy()
	{
		_ordersToPlace = Param(nameof(OrdersToPlace), 60)
			.SetGreaterThanZero()
			.SetDisplay("Orders To Place", "Number of sequential buy orders before stopping", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");
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
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_ordersPlaced = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ordersPlaced = 0;

		var sma = new SMA { Length = 5 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, OnProcess)
			.Start();
	}

	private void OnProcess(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_ordersPlaced >= OrdersToPlace)
			return;

		BuyMarket();

		_ordersPlaced++;
	}
}
