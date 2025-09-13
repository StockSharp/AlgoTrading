using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Line order strategy based on MQL LineOrder script.
/// Places an order when price touches the predefined entry line and
/// manages stop-loss, take-profit and optional trailing stop.
/// </summary>
public class LineOrderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _entryPrice;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private Sides? _currentSide;

	/// <summary>
	/// Price level that triggers the order.
	/// </summary>
	public decimal EntryPrice
	{
		get => _entryPrice.Value;
		set => _entryPrice.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
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
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public LineOrderStrategy()
	{
		_entryPrice = Param(nameof(EntryPrice), 0m)
			.SetDisplay("Entry Price", "Price level that triggers the order", "General");

		_stopLossPips = Param(nameof(StopLossPips), 20)
			.SetDisplay("Stop Loss (pips)", "Distance to initial stop loss", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_takeProfitPips = Param(nameof(TakeProfitPips), 30)
			.SetDisplay("Take Profit (pips)", "Distance to take profit", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 10);

		_trailingStopPips = Param(nameof(TrailingStopPips), 0)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0, 50, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
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
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
		_currentSide = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0)
		{
			TryEnter(candle.ClosePrice);
		}
		else
		{
			ManagePosition(candle.ClosePrice);
		}
	}

	private void TryEnter(decimal price)
	{
		if (EntryPrice <= 0)
			return;

		if (price >= EntryPrice)
		{
			BuyMarket();
			_currentSide = Sides.Buy;
		}
		else if (price <= EntryPrice)
		{
			SellMarket();
			_currentSide = Sides.Sell;
		}
		else
		{
			return;
		}

		var step = Security.PriceStep ?? 1m;

		_stopPrice = _currentSide == Sides.Buy
			? price - StopLossPips * step
			: price + StopLossPips * step;

		_takeProfitPrice = _currentSide == Sides.Buy
			? price + TakeProfitPips * step
			: price - TakeProfitPips * step;
	}

	private void ManagePosition(decimal price)
	{
		if (_currentSide == null)
			return;

		UpdateTrailingStop(price);

		if (_currentSide == Sides.Buy)
		{
			if (price <= _stopPrice || price >= _takeProfitPrice)
				SellMarket(Math.Abs(Position));
		}
		else
		{
			if (price >= _stopPrice || price <= _takeProfitPrice)
				BuyMarket(Math.Abs(Position));
		}
	}

	private void UpdateTrailingStop(decimal price)
	{
		if (TrailingStopPips <= 0 || _currentSide == null)
			return;

		var step = Security.PriceStep ?? 1m;
		var trail = TrailingStopPips * step;

		if (_currentSide == Sides.Buy)
		{
			var newStop = price - trail;
			if (newStop > _stopPrice)
				_stopPrice = newStop;
		}
		else
		{
			var newStop = price + trail;
			if (newStop < _stopPrice)
				_stopPrice = newStop;
		}
	}
}
