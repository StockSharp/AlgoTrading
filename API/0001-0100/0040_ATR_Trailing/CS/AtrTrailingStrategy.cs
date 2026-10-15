using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Enters on price/SMA crossings; a crossing against the open position closes it and opens the crossing side.
/// A fill-anchored, finished-close/current-ATR ratchet exits at market on fresh executable quotes.
/// </summary>
public class AtrTrailingStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousClose;
	private decimal _previousMean;
	private decimal? _trailingStopLevel;
	private decimal _entryDistance;
	private decimal _entryVolume;
	private decimal _entryValue;
	private Sides _entrySide;
	private Order _pendingOrder;

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public int MAPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AtrTrailingStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14).SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
			.SetOptimize(7, 21, 7);
		_atrMultiplier = Param(nameof(AtrMultiplier), 3m).SetNotNegative()
			.SetDisplay("ATR Multiplier", "ATR multiplier for trailing stop; zero disables it", "Risk")
			.SetOptimize(2m, 4m, 0.5m);
		_maPeriod = Param(nameof(MAPeriod), 20).SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for Moving Average calculation for entry", "Indicators")
			.SetOptimize(10, 50, 5);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		OrderRegistering += order => _pendingOrder = order;
		Trades.TradeAdded += ObserveActualFill;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	private bool HasPendingOrder => _pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed);

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousClose = null;
		_previousMean = default;
		_trailingStopLevel = null;
		_entryDistance = _entryVolume = _entryValue = default;
		_entrySide = default;
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_previousClose = null;
		_trailingStopLevel = null;
		_entryVolume = _entryValue = 0m;
		_pendingOrder = null;
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ProcessQuote).Start();
		}
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var sma = new SimpleMovingAverage { Length = MAPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(atr, sma, ProcessCandle, false).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void Ratchet(decimal candidate)
	{
		_trailingStopLevel = _trailingStopLevel is decimal previous
			? (_entrySide == Sides.Buy ? Math.Max(previous, candidate) : Math.Min(previous, candidate))
			: candidate;
	}

	private void ObserveActualFill(MyTrade trade)
	{
		if (Position == 0m)
		{
			_trailingStopLevel = null;
			_entryVolume = _entryValue = 0m;
		}
		else if (trade.Order.Side == _entrySide && AtrMultiplier > 0m)
		{
			_entryVolume += trade.Trade.Volume;
			_entryValue += trade.Trade.Price * trade.Trade.Volume;
			var actualEntry = _entryValue / _entryVolume;
			Ratchet(_entrySide == Sides.Buy ? actualEntry - _entryDistance : actualEntry + _entryDistance);
		}
	}

	private void ProcessQuote(Level1ChangeMessage quote)
	{
		if (AtrMultiplier == 0m || Position == 0m || _trailingStopLevel is not decimal stop
			|| HasPendingOrder || !IsFormedAndOnlineAndAllowTrading())
			return;
		// Only a fresh executable-side price can activate; never reuse another side's stale quote.
		var field = Position > 0m ? Level1Fields.BestBidPrice : Level1Fields.BestAskPrice;
		if (!quote.Changes.TryGetValue(field, out var raw) || raw is not decimal price || price <= 0m)
			return;
		if (Position > 0m ? price <= stop : price >= stop)
			ClosePosition("ATR trailing stop");
	}

	private void ClosePosition(string comment)
	{
		RegisterOrder(new Order
		{
			Security = Security, Portfolio = Portfolio, Type = OrderTypes.Market,
			Side = Position > 0m ? Sides.Sell : Sides.Buy,
			Volume = Math.Abs(Position), Comment = comment,
		});
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished || !atrValue.Indicator.IsFormed || !smaValue.Indicator.IsFormed
			|| !IsFormedAndOnlineAndAllowTrading())
			return;
		var atr = atrValue.GetValue<decimal>();
		var mean = smaValue.GetValue<decimal>();
		var close = candle.ClosePrice;
		var downwardCross = _previousClose is decimal prevDown && prevDown >= _previousMean && close < mean;
		var upwardCross = _previousClose is decimal prevUp && prevUp <= _previousMean && close > mean;
		_previousClose = close;
		_previousMean = mean;
		// While a reversal is unfilled the old side is still held; its close must not seed the new side's stop.
		if (Position != 0m && (Position > 0m) == (_entrySide == Sides.Buy) && AtrMultiplier > 0m)
			Ratchet(Position > 0m ? close - atr * AtrMultiplier : close + atr * AtrMultiplier);
		if (HasPendingOrder)
			return;
		if (upwardCross || downwardCross)
		{
			var side = upwardCross ? Sides.Buy : Sides.Sell;
			// A crossing against the open position closes it and opens the crossing side in one order.
			if (Position == 0m || (Position > 0m) != (side == Sides.Buy))
			{
				_entrySide = side;
				_entryDistance = atr * AtrMultiplier;
				_entryVolume = _entryValue = 0m;
				_trailingStopLevel = null;
				RegisterOrder(new Order
				{
					Security = Security, Portfolio = Portfolio, Type = OrderTypes.Market,
					Side = side, Volume = Volume + Math.Abs(Position), Comment = "ATR trailing entry",
				});
			}
		}
		// A newly tightened level is eligible only for subsequent quote updates, not this bar's old wick.
	}
}
