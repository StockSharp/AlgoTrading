namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Trades confirmed candle order-block zones with optional wick boundaries,
/// percent movement filtering and frozen zone-range exits from actual fills.
/// </summary>
public class PowerZoneStrategy : Strategy
{
	private readonly StrategyParam<int> _periods;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<bool> _isUseWicks;
	private readonly StrategyParam<decimal> _takeProfitFactor;
	private readonly StrategyParam<decimal> _stopLossFactor;
	private readonly StrategyParam<DataType> _candleType;
	private readonly List<(decimal open, decimal high, decimal low, decimal close)> _bars = [];
	private decimal? _bullHigh;
	private decimal? _bullLow;
	private decimal? _bearHigh;
	private decimal? _bearLow;
	private bool _isBullUsed;
	private bool _isBearUsed;
	private Order _pendingOrder;
	private decimal _filledPosition;
	private decimal _entryPrice;
	private decimal _stopDistance;
	private decimal _takeDistance;

	/// <summary>Initializes five-bar confirmations, body zones, zero movement threshold and range-based 1/1.5 exits.</summary>
	public PowerZoneStrategy()
	{
		_periods = Param(nameof(Periods), 5).SetGreaterThanZero()
			.SetDisplay("Periods", "Completed same-color confirmation candles after the opposite anchor", "Zones");
		_threshold = Param(nameof(Threshold), 0m).SetRange(0m, decimal.MaxValue)
			.SetDisplay("Threshold", "Minimum absolute percent move from anchor close to final confirmation close", "Zones");
		_isUseWicks = Param("UseWicks", false)
			.SetDisplay("Use Wicks", "Use anchor high/low instead of body boundaries", "Zones");
		_takeProfitFactor = Param(nameof(TakeProfitFactor), 1.5m).SetGreaterThanZero()
			.SetDisplay("Take Profit Factor", "Frozen entry-zone range multiple above/below actual fill basis", "Protection");
		_stopLossFactor = Param(nameof(StopLossFactor), 1m).SetGreaterThanZero()
			.SetDisplay("Stop Loss Factor", "Frozen entry-zone range multiple below/above actual fill basis", "Protection");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Finished zone confirmations and subsequent breakout decisions", "General");
		OrderRegistering += order => _pendingOrder = order;
		OwnTradeReceived += ObserveFill;
	}

	/// <summary>Number of strict same-color confirmation bars after the opposite anchor candle.</summary>
	public int Periods { get => _periods.Value; set => _periods.Value = value; }
	/// <summary>Inclusive minimum absolute anchor-to-confirmation close movement in percent.</summary>
	public decimal Threshold { get => _threshold.Value; set => _threshold.Value = value; }
	/// <summary>Use anchor wicks; the published strategy parameter remains named UseWicks.</summary>
	public bool IsUseWicks { get => _isUseWicks.Value; set => _isUseWicks.Value = value; }
	/// <summary>Positive take distance as a multiple of the accepted zone range.</summary>
	public decimal TakeProfitFactor { get => _takeProfitFactor.Value; set => _takeProfitFactor.Value = value; }
	/// <summary>Positive stop distance as a multiple of the accepted zone range.</summary>
	public decimal StopLossFactor { get => _stopLossFactor.Value; set => _stopLossFactor.Value = value; }
	/// <summary>Base candle type; five minutes by default.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	private void ResetState()
	{
		_bars.Clear();
		_bullHigh = _bullLow = _bearHigh = _bearLow = null;
		_isBullUsed = _isBearUsed = false;
		_pendingOrder = null;
		_filledPosition = _entryPrice = _stopDistance = _takeDistance = 0m;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (Periods <= 0 || Threshold < 0m || TakeProfitFactor <= 0m || StopLossFactor <= 0m)
			throw new ArgumentException("Periods and risk factors must be positive; movement threshold cannot be negative.");
		ResetState();
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveQuote).Start();
		}
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private bool IsPending => _pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed);

	private void Submit(Sides side, decimal volume, string comment)
		=> RegisterOrder(new Order { Security = Security, Portfolio = Portfolio, Type = OrderTypes.Market, Side = side, Volume = volume, Comment = comment });

	private void ObserveFill(Subscription subscription, MyTrade trade)
	{
		var previous = _filledPosition;
		var signed = trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
		_filledPosition += signed;
		if (_filledPosition == 0m)
			_entryPrice = 0m;
		else if (previous == 0m || Math.Sign(_filledPosition) != Math.Sign(previous))
			_entryPrice = trade.Trade.Price;
		else if (Math.Sign(signed) == Math.Sign(previous))
			_entryPrice = (_entryPrice * Math.Abs(previous) + trade.Trade.Price * Math.Abs(signed)) / Math.Abs(_filledPosition);
	}

	private bool TryRiskExit(decimal price)
	{
		if (Position == 0m || _entryPrice <= 0m || IsPending || !IsFormedAndOnlineAndAllowTrading())
			return false;
		var profit = (price - _entryPrice) * Math.Sign(Position);
		var comment = profit <= -_stopDistance ? "PowerZone stop loss" : profit >= _takeDistance ? "PowerZone take profit" : null;
		if (comment is null)
			return false;
		Submit(Position > 0m ? Sides.Sell : Sides.Buy, Math.Abs(Position), comment);
		return true;
	}

	private void ObserveQuote(Level1ChangeMessage quote)
	{
		var field = Position > 0m ? Level1Fields.BestBidPrice : Level1Fields.BestAskPrice;
		if (quote.TryGetDecimal(field) is decimal price && price > 0m)
			TryRiskExit(price);
	}

	private void ConfirmZones()
	{
		if (_bars.Count < checked(Periods + 2))
			return;
		var anchor = _bars[0];
		var bull = anchor.close < anchor.open;
		var bear = anchor.close > anchor.open;
		for (var i = 1; i < _bars.Count - 1; i++)
		{
			bull &= _bars[i].close > _bars[i].open;
			bear &= _bars[i].close < _bars[i].open;
		}
		if ((!bull && !bear) || anchor.close == 0m
			|| 100m * Math.Abs(_bars[^2].close - anchor.close) / Math.Abs(anchor.close) < Threshold)
			return;
		var high = IsUseWicks ? anchor.high : Math.Max(anchor.open, anchor.close);
		var low = IsUseWicks ? anchor.low : Math.Min(anchor.open, anchor.close);
		if (high <= low)
			return;
		if (bull)
		{
			_bullHigh = high;
			_bullLow = low;
			_isBullUsed = false;
		}
		else
		{
			_bearHigh = high;
			_bearLow = low;
			_isBearUsed = false;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		_bars.Add((candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice));
		if (_bars.Count > checked(Periods + 2))
			_bars.RemoveAt(0);
		ConfirmZones();
		if (TryRiskExit(candle.ClosePrice) || IsPending || !IsFormedAndOnlineAndAllowTrading())
			return;
		Sides? side = null;
		var width = 0m;
		if (!_isBullUsed && _bullHigh is decimal bullHigh && _bullLow is decimal bullLow
			&& candle.ClosePrice > bullHigh && Position <= 0m)
		{
			side = Sides.Buy;
			width = bullHigh - bullLow;
			_isBullUsed = true;
		}
		else if (!_isBearUsed && _bearHigh is decimal bearHigh && _bearLow is decimal bearLow
			&& candle.ClosePrice < bearLow && Position >= 0m)
		{
			side = Sides.Sell;
			width = bearHigh - bearLow;
			_isBearUsed = true;
		}
		if (side is Sides direction)
		{
			_stopDistance = width * StopLossFactor;
			_takeDistance = width * TakeProfitFactor;
			Submit(direction, Volume + Math.Abs(Position), "PowerZone breakout entry");
		}
	}
}
