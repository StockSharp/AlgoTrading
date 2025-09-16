using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trendline breakout strategy based on user defined price levels.
/// Opens long or short positions when price crosses the specified lines.
/// Supports optional absolute exit lines and trailing stop.
/// </summary>
public class BreakThroughStrategy : Strategy
{
	private readonly StrategyParam<decimal> _buyLinePrice;
	private readonly StrategyParam<decimal> _sellLinePrice;
	private readonly StrategyParam<decimal> _buyTakeProfitLine;
	private readonly StrategyParam<decimal> _buyStopLossLine;
	private readonly StrategyParam<decimal> _sellTakeProfitLine;
	private readonly StrategyParam<decimal> _sellStopLossLine;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<DataType> _candleType;

	private bool _buyLineAbove;
	private bool _sellLineAbove;
	private bool _initialized;

	private decimal _buyStopPrice;
	private decimal _buyTakePrice;
	private decimal _sellStopPrice;
	private decimal _sellTakePrice;
	private decimal _trailingStopPrice;

	/// <summary>
	/// Price level for buy entry.
	/// </summary>
	public decimal BuyLinePrice
	{
		get => _buyLinePrice.Value;
		set => _buyLinePrice.Value = value;
	}

	/// <summary>
	/// Price level for sell entry.
	/// </summary>
	public decimal SellLinePrice
	{
		get => _sellLinePrice.Value;
		set => _sellLinePrice.Value = value;
	}

	/// <summary>
	/// Optional line for closing long position.
	/// </summary>
	public decimal BuyTakeProfitLine
	{
		get => _buyTakeProfitLine.Value;
		set => _buyTakeProfitLine.Value = value;
	}

	/// <summary>
	/// Optional line for closing long position with loss.
	/// </summary>
	public decimal BuyStopLossLine
	{
		get => _buyStopLossLine.Value;
		set => _buyStopLossLine.Value = value;
	}

	/// <summary>
	/// Optional line for closing short position.
	/// </summary>
	public decimal SellTakeProfitLine
	{
		get => _sellTakeProfitLine.Value;
		set => _sellTakeProfitLine.Value = value;
	}

	/// <summary>
	/// Optional line for closing short position with loss.
	/// </summary>
	public decimal SellStopLossLine
	{
		get => _sellStopLossLine.Value;
		set => _sellStopLossLine.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Candle type used for price evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BreakThroughStrategy()
	{
		_buyLinePrice = Param(nameof(BuyLinePrice), 0m)
		.SetDisplay("Buy Line", "Price level for buy entry", "Lines");

		_sellLinePrice = Param(nameof(SellLinePrice), 0m)
		.SetDisplay("Sell Line", "Price level for sell entry", "Lines");

		_buyTakeProfitLine = Param(nameof(BuyTakeProfitLine), 0m)
		.SetDisplay("Buy TP Line", "Price to close long position", "Lines");

		_buyStopLossLine = Param(nameof(BuyStopLossLine), 0m)
		.SetDisplay("Buy SL Line", "Price to stop long position", "Lines");

		_sellTakeProfitLine = Param(nameof(SellTakeProfitLine), 0m)
		.SetDisplay("Sell TP Line", "Price to close short position", "Lines");

		_sellStopLossLine = Param(nameof(SellStopLossLine), 0m)
		.SetDisplay("Sell SL Line", "Price to stop short position", "Lines");

		_takeProfitPips = Param(nameof(TakeProfitPips), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (pips)", "Profit target distance", "Risk")
		.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 30m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (pips)", "Loss limit distance", "Risk")
		.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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
		_buyLineAbove = false;
		_sellLineAbove = false;
		_initialized = false;
		_buyStopPrice = 0m;
		_buyTakePrice = 0m;
		_sellStopPrice = 0m;
		_sellTakePrice = 0m;
		_trailingStopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var price = candle.ClosePrice;

		if (!_initialized)
		{
		_buyLineAbove = BuyLinePrice > price;
		_sellLineAbove = SellLinePrice > price;
		_initialized = true;
		}

		var volume = Volume + Math.Abs(Position);

		// Entry logic
		if (BuyLinePrice > 0m && Position <= 0)
		{
		if ((!_buyLineAbove && price < BuyLinePrice) || (_buyLineAbove && price > BuyLinePrice))
		{
		BuyMarket(volume);
		var stopOffset = PipsToPrice(StopLossPips);
		var takeOffset = PipsToPrice(TakeProfitPips);
		_buyStopPrice = price - stopOffset;
		_buyTakePrice = price + takeOffset;
		_trailingStopPrice = _buyStopPrice;
		return;
		}
		}

		if (SellLinePrice > 0m && Position >= 0)
		{
		if ((!_sellLineAbove && price > SellLinePrice) || (_sellLineAbove && price < SellLinePrice))
		{
		SellMarket(volume);
		var stopOffset = PipsToPrice(StopLossPips);
		var takeOffset = PipsToPrice(TakeProfitPips);
		_sellStopPrice = price + stopOffset;
		_sellTakePrice = price - takeOffset;
		_trailingStopPrice = _sellStopPrice;
		return;
		}
		}

		// Exit logic for long position
		if (Position > 0)
		{
		if (BuyTakeProfitLine > 0m && price >= BuyTakeProfitLine)
		{
		SellMarket(Math.Abs(Position));
		return;
		}

		if (BuyStopLossLine > 0m && price <= BuyStopLossLine)
		{
		SellMarket(Math.Abs(Position));
		return;
		}

		if (price <= _buyStopPrice)
		{
		SellMarket(Math.Abs(Position));
		return;
		}

		if (price >= _buyTakePrice)
		{
		SellMarket(Math.Abs(Position));
		return;
		}

		if (TrailingStopPips > 0m)
		{
		var newStop = price - PipsToPrice(TrailingStopPips);
		if (newStop > _trailingStopPrice)
		_trailingStopPrice = newStop;

		if (price <= _trailingStopPrice)
		{
		SellMarket(Math.Abs(Position));
		return;
		}
		}
		}
		else if (Position < 0)
		{
		if (SellTakeProfitLine > 0m && price <= SellTakeProfitLine)
		{
		BuyMarket(Math.Abs(Position));
		return;
		}

		if (SellStopLossLine > 0m && price >= SellStopLossLine)
		{
		BuyMarket(Math.Abs(Position));
		return;
		}

		if (price >= _sellStopPrice)
		{
		BuyMarket(Math.Abs(Position));
		return;
		}

		if (price <= _sellTakePrice)
		{
		BuyMarket(Math.Abs(Position));
		return;
		}

		if (TrailingStopPips > 0m)
		{
		var newStop = price + PipsToPrice(TrailingStopPips);
		if (newStop < _trailingStopPrice)
		_trailingStopPrice = newStop;

		if (price >= _trailingStopPrice)
		{
		BuyMarket(Math.Abs(Position));
		return;
		}
		}
		}
	}

	private decimal PipsToPrice(decimal pips) => Security.PriceStep * pips;
}
