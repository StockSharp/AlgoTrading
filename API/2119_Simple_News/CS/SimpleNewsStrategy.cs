using System;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Places symmetric pending orders around scheduled news time and manages trailing stop.
/// </summary>
public class SimpleNewsStrategy : Strategy
{
	private readonly StrategyParam<DateTimeOffset> _newsTime;
	private readonly StrategyParam<int> _deals;
	private readonly StrategyParam<decimal> _delta;
	private readonly StrategyParam<decimal> _distance;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trail;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _volume;

	private bool _ordersPlaced;
	private DateTimeOffset _cancelTime;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _highest;
	private decimal? _lowest;

	/// <summary>
	/// Scheduled news time.
	/// </summary>
	public DateTimeOffset NewsTime { get => _newsTime.Value; set => _newsTime.Value = value; }

	/// <summary>
	/// Number of buy/sell stop pairs.
	/// </summary>
	public int Deals { get => _deals.Value; set => _deals.Value = value; }

	/// <summary>
	/// Step between orders in pips.
	/// </summary>
	public decimal Delta { get => _delta.Value; set => _delta.Value = value; }

	/// <summary>
	/// Distance from current price for the first pair in pips.
	/// </summary>
	public decimal Distance { get => _distance.Value; set => _distance.Value = value; }

	/// <summary>
	/// Initial stop loss in pips.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Trailing stop in pips.
	/// </summary>
	public decimal Trail { get => _trail.Value; set => _trail.Value = value; }

	/// <summary>
	/// Take profit in pips.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }

	/// <summary>
	/// Initialize <see cref="SimpleNewsStrategy"/>.
	/// </summary>
	public SimpleNewsStrategy()
	{
		_newsTime = Param(nameof(NewsTime), DateTimeOffset.Now).SetDisplay("News Time", "Release time of news", "General");
		_deals = Param(nameof(Deals), 3).SetDisplay("Deals", "Number of order pairs", "Orders");
		_delta = Param(nameof(Delta), 50m).SetDisplay("Delta", "Step between orders (pips)", "Orders");
		_distance = Param(nameof(Distance), 300m).SetDisplay("Distance", "Distance from price (pips)", "Orders");
		_stopLoss = Param(nameof(StopLoss), 150m).SetDisplay("Stop Loss", "Initial stop loss (pips)", "Risk");
		_trail = Param(nameof(Trail), 200m).SetDisplay("Trail", "Trailing stop (pips)", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 900m).SetDisplay("Take Profit", "Take profit (pips)", "Risk");
		_volume = Param(nameof(Volume), 0.01m).SetDisplay("Volume", "Order volume", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(
			takeProfit: new Unit(TakeProfit * Security.PriceStep, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss * Security.PriceStep, UnitTypes.Absolute));

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_bestBid = (decimal)bid;
		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_bestAsk = (decimal)ask;

		var now = CurrentTime;
		if (!_ordersPlaced && now >= NewsTime - TimeSpan.FromMinutes(5) && now < NewsTime &&
			_bestBid is not null && _bestAsk is not null)
		{
			PlacePendingOrders();
			_cancelTime = NewsTime + TimeSpan.FromMinutes(10);
			_ordersPlaced = true;
		}

		if (_ordersPlaced && now > _cancelTime)
		{
			CancelActiveOrders();
			_ordersPlaced = false;
		}

		ManagePosition();
	}

	private void PlacePendingOrders()
	{
		var step = Delta * Security.PriceStep;
		for (var i = 0; i < Deals; i++)
		{
			var buyPrice = _bestAsk.Value + Distance * Security.PriceStep + step * i;
			var sellPrice = _bestBid.Value - Distance * Security.PriceStep - step * i;

			BuyStop(Volume, buyPrice);
			SellStop(Volume, sellPrice);
		}
	}

	private void ManagePosition()
	{
		var step = Security.PriceStep;
		if (Position > 0 && _bestBid is decimal bid && _longEntryPrice is decimal entry)
		{
			_highest = _highest is decimal h ? Math.Max(h, bid) : bid;
			var trailStop = _highest.Value - Trail * step;
			if (bid <= entry - StopLoss * step || bid >= entry + TakeProfit * step || bid <= trailStop)
				ClosePosition();
		}
		else if (Position < 0 && _bestAsk is decimal ask && _shortEntryPrice is decimal entry2)
		{
			_lowest = _lowest is decimal l ? Math.Min(l, ask) : ask;
			var trailStop = _lowest.Value + Trail * step;
			if (ask >= entry2 + StopLoss * step || ask <= entry2 - TakeProfit * step || ask >= trailStop)
				ClosePosition();
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0 && delta > 0)
		{
			_longEntryPrice = _bestAsk;
			_highest = _bestBid;
			_shortEntryPrice = null;
			_lowest = null;
		}
		else if (Position < 0 && delta < 0)
		{
			_shortEntryPrice = _bestBid;
			_lowest = _bestAsk;
			_longEntryPrice = null;
			_highest = null;
		}
		else if (Position == 0)
		{
			_longEntryPrice = null;
			_shortEntryPrice = null;
			_highest = null;
			_lowest = null;
		}
	}
}