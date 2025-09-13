using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moves stop-loss to break-even when price reaches the specified level.
/// Designed as a utility to protect an existing position.
/// </summary>
public class StopLossMoverStrategy : Strategy
{
	private readonly StrategyParam<decimal> _moveSlPrice;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private bool _isStopMoved;

	/// <summary>
	/// Price level at which stop-loss will be moved to the entry price.
	/// </summary>
	public decimal MoveSlPrice
	{
		get => _moveSlPrice.Value;
		set => _moveSlPrice.Value = value;
	}

	/// <summary>
	/// Candle type used to monitor price movement.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public StopLossMoverStrategy()
	{
		_moveSlPrice = Param(nameof(MoveSlPrice), 0m)
			.SetDisplay("Move SL Price", "Price level that triggers stop-loss move", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 100m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to monitor", "General");
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
		_entryPrice = 0m;
		_isStopMoved = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Subscribe to candles and start data flow
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		// Start protection mechanism
		StartProtection();

		// Open initial long position
		BuyMarket(Volume);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Stop-loss can be moved only if entry price is known
		if (_entryPrice == 0m)
			return;

		if (_isStopMoved)
			return;

		// For long positions, move stop when candle high crosses the level
		if (Position > 0 && candle.HighPrice > MoveSlPrice)
		{
			SellStopMarket(Position, _entryPrice);
			_isStopMoved = true;
			LogInfo("Stop-loss moved to {0}", _entryPrice);
		}
		// For short positions, move stop when candle low crosses the level
		else if (Position < 0 && candle.LowPrice < MoveSlPrice)
		{
			BuyStopMarket(Math.Abs(Position), _entryPrice);
			_isStopMoved = true;
			LogInfo("Stop-loss moved to {0}", _entryPrice);
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		// Capture entry price when first trade is executed
		if (_entryPrice == 0m)
			_entryPrice = trade.Trade.Price;
	}
}
