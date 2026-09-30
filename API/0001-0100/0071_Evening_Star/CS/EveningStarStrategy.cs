using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Short-only three-candle Evening Star with middle-candle-high stop and confirmation-low target.
/// </summary>
public class EveningStarStrategy : Strategy
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

	public EveningStarStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Evening Star pattern timeframe", "General");
		_stopLossPercent = Param(nameof(StopLossPercent), 1m).SetNotNegative()
			.SetDisplay("Stop above middle high (%)", "Buffer above the middle candle's high; zero places the stop at that high", "Protection");
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
		var asks = new Subscription(DataType.Level1, Security);
		asks.MarketData.BuildField = Level1Fields.BestAskPrice;
		SubscribeLevel1(asks).Bind(ProcessAsk).Start();
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

	private void ProcessAsk(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask && ask > 0m)
			CheckExit(ask, ask);
	}

	private bool CheckExit(decimal high, decimal low)
	{
		if (Position >= 0m || IsPending(_exitOrder))
			return false;
		// With only a completed bar's range, prefer the adverse stop when both levels touched.
		if (_stopPrice is decimal stop && high >= stop ||
			_targetPrice is decimal target && low < target)
		{
			_exitOrder = BuyMarket(Math.Abs(Position));
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
		if (Position < 0m && CheckExit(candle.HighPrice, candle.LowPrice))
		{
			Append(candle);
			return;
		}
		if (IsPending(_entryOrder) || IsPending(_exitOrder))
		{
			Append(candle);
			return;
		}
		if (Position == 0m && _recent.Count == 2 && IsFormedAndOnlineAndAllowTrading())
		{
			var first = _recent[0];
			var middle = _recent[1];
			var firstBody = first.ClosePrice - first.OpenPrice;
			var middleBody = Math.Abs(middle.ClosePrice - middle.OpenPrice);
			var thirdBody = candle.OpenPrice - candle.ClosePrice;
			var firstMidpoint = (first.HighPrice + first.LowPrice) / 2m;
			if (firstBody > 0m && middleBody < firstBody / 2m &&
				thirdBody > 0m && candle.ClosePrice < firstMidpoint)
			{
				_stopPrice = middle.HighPrice * (1m + StopLossPercent / 100m);
				_targetPrice = candle.LowPrice;
				_entryOrder = SellMarket(Volume);
			}
		}
		Append(candle);
	}
}
