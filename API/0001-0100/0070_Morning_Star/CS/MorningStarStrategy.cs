using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-only three-candle Morning Star with middle-candle-low stop and confirmation-high target.
/// </summary>
public class MorningStarStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly List<ICandleMessage> _recent = new();
	private decimal? _stopPrice;
	private decimal? _targetPrice;
	private Order _entryOrder;
	private Order _exitOrder;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	public MorningStarStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Morning Star pattern timeframe", "General");
		_stopLossPercent = Param(nameof(StopLossPercent), 1m).SetRange(0m, 99m)
			.SetDisplay("Stop below middle low (%)", "Buffer below the middle candle's low; zero places the stop at that low", "Protection");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ClearState();
	}

	private void ClearState()
	{
		_recent.Clear();
		_stopPrice = _targetPrice = null;
		_entryOrder = _exitOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ClearState();
		var bids = new Subscription(DataType.Level1, Security);
		bids.MarketData.BuildField = Level1Fields.BestBidPrice;
		SubscribeLevel1(bids).Bind(ProcessBid).Start();
		var candles = SubscribeCandles(CandleType);
		candles.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candles);
			DrawOwnTrades(area);
		}
	}

	private static bool IsPending(Order order)
		=> order is not null && order.State is not (OrderStates.Done or OrderStates.Failed);

	private void ProcessBid(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid && bid > 0m)
			CheckExit(bid, bid);
	}

	private bool CheckExit(decimal low, decimal high)
	{
		if (Position <= 0m || IsPending(_exitOrder))
			return false;
		// With only a completed bar's range, prefer the adverse stop when both levels touched.
		if (_stopPrice is decimal stop && low <= stop ||
			_targetPrice is decimal target && high > target)
		{
			_exitOrder = SellMarket(Position);
			_recent.Clear();
			_stopPrice = _targetPrice = null;
			return true;
		}
		return false;
	}

	private void Append(ICandleMessage candle)
	{
		_recent.Add(candle);
		if (_recent.Count > 2)
			_recent.RemoveAt(0);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		if (Position > 0m && CheckExit(candle.LowPrice, candle.HighPrice))
			return;
		if (IsPending(_entryOrder) || IsPending(_exitOrder))
		{
			Append(candle);
			return;
		}
		if (Position == 0m && _recent.Count == 2 && IsFormedAndOnlineAndAllowTrading())
		{
			var first = _recent[0];
			var middle = _recent[1];
			var firstBody = first.OpenPrice - first.ClosePrice;
			var middleBody = Math.Abs(middle.ClosePrice - middle.OpenPrice);
			var thirdBody = candle.ClosePrice - candle.OpenPrice;
			var firstMidpoint = (first.HighPrice + first.LowPrice) / 2m;
			if (firstBody > 0m && middleBody < firstBody / 2m &&
				thirdBody >= firstBody / 2m && candle.ClosePrice > firstMidpoint)
			{
				_stopPrice = middle.LowPrice * (1m - StopLossPercent / 100m);
				_targetPrice = candle.HighPrice;
				_entryOrder = BuyMarket(Volume);
			}
		}
		Append(candle);
	}
}
