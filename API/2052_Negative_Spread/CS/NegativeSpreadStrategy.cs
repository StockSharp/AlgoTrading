using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that sells when the spread becomes negative and immediately closes the position.
/// </summary>
public class NegativeSpreadStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stopLossPips;

	private bool _isOpening;
	private bool _isClosing;

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public NegativeSpreadStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_takeProfitPips = Param(nameof(TakeProfitPips), 5000)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Protection");

		_stopLossPips = Param(nameof(StopLossPips), 5000)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Protection");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, default(DataType))];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_isOpening = false;
		_isClosing = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var tick = Security?.PriceStep ?? 1m;
		var tp = TakeProfitPips * tick;
		var sl = StopLossPips * tick;

		StartProtection(new Unit(tp, UnitTypes.Absolute), new Unit(sl, UnitTypes.Absolute));

		SubscribeOrderBook()
			.Bind(ProcessOrderBook)
			.Start();
	}

	private void ProcessOrderBook(IOrderBookMessage orderBook)
	{
		var bestBid = orderBook.Bids != null && orderBook.Bids.Length > 0 ? orderBook.Bids[0].Price : (decimal?)null;
		var bestAsk = orderBook.Asks != null && orderBook.Asks.Length > 0 ? orderBook.Asks[0].Price : (decimal?)null;

		if (bestBid is null || bestAsk is null)
			return;

		if (bestAsk < bestBid && Position == 0 && !_isOpening)
		{
			_isOpening = true;
			SellMarket(Volume);
			return;
		}

		if (Position < 0)
		{
			_isOpening = false;

			if (!_isClosing)
			{
				_isClosing = true;
				ClosePosition();
			}
		}
		else
		{
			_isClosing = false;
		}
	}
}
