using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades once at 01:00 New York from the preceding calendar day's midnight H1 candle.
/// Local bid/ask SL/TP levels are anchored to the actual average entry fill and price step.
/// </summary>
public class SilverMidnightCandleColorStrategy : Strategy
{
	private static readonly TimeZoneInfo _newYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _takeProfitLongTicks, _takeProfitShortTicks, _stopLossTicks;
	private readonly Dictionary<DateTime, Sides> _midnight = new();
	private DateTime? _enteredDay;
	private Sides _entrySide;
	private decimal _entryValue, _entryVolume, _bid, _ask;
	private bool _exitPending;
	private Order _exitOrder;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int TakeProfitLongTicks { get => _takeProfitLongTicks.Value; set => _takeProfitLongTicks.Value = value; }
	public int TakeProfitShortTicks { get => _takeProfitShortTicks.Value; set => _takeProfitShortTicks.Value = value; }
	public int StopLossTicks { get => _stopLossTicks.Value; set => _stopLossTicks.Value = value; }

	public SilverMidnightCandleColorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame());
		_takeProfitLongTicks = Param(nameof(TakeProfitLongTicks), 57).SetNotNegative();
		_takeProfitShortTicks = Param(nameof(TakeProfitShortTicks), 48).SetNotNegative();
		_stopLossTicks = Param(nameof(StopLossTicks), 200).SetNotNegative();
		// TradeAdded is the deduplicated own-fill stream, including partial market fills.
		Trades.TradeAdded += ProcessTrade;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_midnight.Clear();
		_enteredDay = null;
		_entrySide = default;
		_entryValue = _entryVolume = _bid = _ask = 0m;
		_exitPending = false;
		_exitOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (CandleType != TimeSpan.FromHours(1).TimeFrame())
			throw new ArgumentException("The midnight colour is defined by a one-hour candle.", nameof(CandleType));
		if (Security.PriceStep is not decimal step || step <= 0m)
			throw new InvalidOperationException("A positive security price step is required for tick SL/TP.");
		ResetState();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		// Separate fields also deliver bid-only and ask-only deltas; callbacks cache both sides.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ProcessQuote).Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private static DateTime ToNewYork(DateTime utc)
		=> TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _newYork);

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		var local = ToNewYork(candle.OpenTime);
		if (local.TimeOfDay == TimeSpan.Zero)
			_midnight[local.Date] = candle.ClosePrice > candle.OpenPrice ? Sides.Buy : Sides.Sell;
		// Only the two latest calendar days can be used by a subsequent entry.
		foreach (var day in new List<DateTime>(_midnight.Keys))
			if (day < local.Date.AddDays(-1))
				_midnight.Remove(day);
	}

	private void ProcessTrade(MyTrade trade)
	{
		if (trade.Order.Side == _entrySide)
		{
			_entryValue += trade.Trade.Price * trade.Trade.Volume;
			_entryVolume += trade.Trade.Volume;
		}
		else if (_entryVolume > 0m)
		{
			var closed = Math.Min(trade.Trade.Volume, _entryVolume);
			_entryValue -= _entryValue / _entryVolume * closed;
			_entryVolume -= closed;
			if (_entryVolume == 0m)
				_entryValue = 0m;
		}
		if (Position == 0m)
			_exitPending = false;
	}

	private void ProcessQuote(Level1ChangeMessage quote)
	{
		if (quote.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid)
			_bid = bid;
		if (quote.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask)
			_ask = ask;
		if (Position != 0m)
		{
			CheckProtection();
			return;
		}

		var local = ToNewYork(quote.ServerTime);
		if (local.Hour != 1 || local.Minute != 0 || _enteredDay == local.Date
			|| !_midnight.TryGetValue(local.Date.AddDays(-1), out var side)
			|| !IsFormedAndOnlineAndAllowTrading())
			return;
		_enteredDay = local.Date;
		_entrySide = side;
		_entryValue = _entryVolume = 0m;
		_exitPending = false;
		if (side == Sides.Buy)
			BuyMarket();
		else
			SellMarket();
	}

	private void CheckProtection()
	{
		// A late entry fill can leave residual exposure after a completed exit.
		if (_exitOrder?.State is OrderStates.Done or OrderStates.Failed)
		{
			_exitPending = false;
			_exitOrder = null;
		}
		if (_exitPending || _entryVolume <= 0m)
			return;
		var price = Position > 0m ? _bid : _ask;
		if (price <= 0m)
			return;
		var entry = _entryValue / _entryVolume;
		var step = Security.PriceStep.Value;
		var targetTicks = Position > 0m ? TakeProfitLongTicks : TakeProfitShortTicks;
		var direction = Position > 0m ? 1m : -1m;
		var target = entry + direction * targetTicks * step;
		var stop = entry - direction * StopLossTicks * step;
		var takeHit = targetTicks > 0 && (Position > 0m ? price >= target : price <= target);
		var stopHit = StopLossTicks > 0 && (Position > 0m ? price <= stop : price >= stop);
		if (!takeHit && !stopHit)
			return;
		_exitPending = true;
		_exitOrder = Position > 0m ? SellMarket(Position) : BuyMarket(Math.Abs(Position));
	}
}
