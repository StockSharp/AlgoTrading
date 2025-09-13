using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Places pending limit orders after RSI stays in extreme zones for several candles.
/// </summary>
public class AutoPendingByRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _pendingOffset;
	private readonly StrategyParam<int> _matchCount;
	private readonly StrategyParam<DataType> _candleType;

	private int _overboughtCount;
	private int _oversoldCount;
	private Order _buyOrder;
	private Order _sellOrder;

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// Offset for pending limit orders in price points.
	/// </summary>
	public decimal PendingOffset
	{
		get => _pendingOffset.Value;
		set => _pendingOffset.Value = value;
	}

	/// <summary>
	/// Number of consecutive candles before placing orders.
	/// </summary>
	public int MatchCount
	{
		get => _matchCount.Value;
		set => _matchCount.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public AutoPendingByRsiStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI calculation period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 7);

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "Overbought level", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(60m, 80m, 5m);

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "Oversold level", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20m, 40m, 5m);

		_pendingOffset = Param(nameof(PendingOffset), 10m)
			.SetDisplay("Pending Offset", "Offset for limit order in points", "General")
			.SetCanOptimize(true)
			.SetOptimize(5m, 30m, 5m);

		_matchCount = Param(nameof(MatchCount), 5)
			.SetDisplay("Match Count", "Consecutive candles before placing order", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for analysis", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new Rsi { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, Process)
			.Start();
	}

	private void Process(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (rsi < RsiOversold)
		{
			_oversoldCount++;
			_overboughtCount = 0;
		}
		else if (rsi > RsiOverbought)
		{
			_overboughtCount++;
			_oversoldCount = 0;
		}
		else
		{
			_overboughtCount = 0;
			_oversoldCount = 0;
		}

		if (_oversoldCount >= MatchCount && Position <= 0 && _buyOrder is null)
		{
			var price = candle.ClosePrice - PendingOffset * Security.PriceStep;
			_buyOrder = BuyLimit(price);
		}

		if (_overboughtCount >= MatchCount && Position >= 0 && _sellOrder is null)
		{
			var price = candle.ClosePrice + PendingOffset * Security.PriceStep;
			_sellOrder = SellLimit(price);
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order == _buyOrder)
			_buyOrder = null;
		else if (trade.Order == _sellOrder)
			_sellOrder = null;
	}

	/// <inheritdoc />
	protected override void OnOrderFailed(Order order, OrderFail fail)
	{
		base.OnOrderFailed(order, fail);

		if (order == _buyOrder)
			_buyOrder = null;
		else if (order == _sellOrder)
			_sellOrder = null;
	}
}
