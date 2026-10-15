using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Enters against the preceding two-close move on a finished doji.
/// Exits at the opposite doji extreme or native actual-fill percent protection.
/// </summary>
public class DojiReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _dojiThreshold;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private ICandleMessage _older;
	private ICandleMessage _previous;
	private decimal? _targetHigh;
	private decimal? _targetLow;
	private Order _pendingOrder;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal DojiThreshold { get => _dojiThreshold.Value; set => _dojiThreshold.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	public DojiReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Doji and prior-close timeframe", "General");
		_dojiThreshold = Param(nameof(DojiThreshold), 0.1m).SetRange(0m, 1m)
			.SetDisplay("Doji Threshold", "Strict upper bound for absolute body divided by high-low range", "Pattern");
		_stopLossPercent = Param(nameof(StopLossPercent), 1m).SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it", "Protection");
		OrderRegistering += order =>
		{
			_pendingOrder = order;
			if (Position > 0m && order.Side == Sides.Sell ||
				Position < 0m && order.Side == Sides.Buy)
				_targetHigh = _targetLow = null;
		};
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
		_older = _previous = null;
		_targetHigh = _targetLow = null;
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ClearState();
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ProcessQuote).Start();
		}
		var candles = SubscribeCandles(CandleType);
		candles.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candles);
			DrawOwnTrades(area);
		}
	}

	private bool Pending => _pendingOrder is not null &&
		_pendingOrder.State is not (OrderStates.Done or OrderStates.Failed);

	private void ProcessQuote(Level1ChangeMessage message)
	{
		if (Pending)
			return;
		if (Position > 0m && _targetHigh is decimal high &&
			message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid &&
			bid > high)
			SellMarket(Position);
		else if (Position < 0m && _targetLow is decimal low &&
			message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask &&
			ask > 0m && ask < low)
			BuyMarket(Math.Abs(Position));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		if (!Pending && Position > 0m && _targetHigh is decimal high && candle.HighPrice > high)
			SellMarket(Position);
		else if (!Pending && Position < 0m && _targetLow is decimal low && candle.LowPrice < low)
			BuyMarket(Math.Abs(Position));
		else if (!Pending && Position == 0m && _older is not null && _previous is not null &&
			IsDoji(candle) && IsFormedAndOnlineAndAllowTrading())
		{
			if (_previous.ClosePrice < _older.ClosePrice)
			{
				_targetHigh = candle.HighPrice;
				_targetLow = null;
				BuyMarket(Volume);
			}
			else if (_previous.ClosePrice > _older.ClosePrice)
			{
				_targetLow = candle.LowPrice;
				_targetHigh = null;
				SellMarket(Volume);
			}
		}
		_older = _previous;
		_previous = candle;
	}

	private bool IsDoji(ICandleMessage candle)
	{
		var range = candle.HighPrice - candle.LowPrice;
		return range > 0m && Math.Abs(candle.ClosePrice - candle.OpenPrice) / range < DojiThreshold;
	}
}
