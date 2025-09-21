using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Recreates the "Breakeven v3" trade manager from MetaTrader.
/// The strategy does not open positions and instead adjusts protective exit orders
/// for the already opened long and short trades on the selected instrument.
/// </summary>
public class BreakevenV3Strategy : Strategy
{
	private readonly StrategyParam<int> _deltaPoints;
	private readonly StrategyParam<bool> _enableLogging;

	private readonly List<PositionLot> _openLots = new();

	private Order? _longExitOrder;
	private Order? _shortExitOrder;
	private decimal _pointValue;
	private decimal? _lastBid;
	private decimal? _lastAsk;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public BreakevenV3Strategy()
	{
		_deltaPoints = Param(nameof(DeltaPoints), 100)
		.SetNotNegative()
		.SetDisplay("Delta (points)", "Offset in MetaTrader points applied around the break-even price.", "General")
		.SetCanOptimize(true)
		.SetOptimize(10, 300, 10);

		_enableLogging = Param(nameof(EnableLogging), true)
		.SetDisplay("Enable Logging", "Write diagnostic messages whenever protective orders change.", "Diagnostics");
	}

	/// <summary>
	/// Extra offset from the break-even price expressed in MetaTrader points.
	/// </summary>
	public int DeltaPoints
	{
		get => _deltaPoints.Value;
		set => _deltaPoints.Value = value;
	}

	/// <summary>
	/// Enable verbose logging about recalculated break-even levels.
	/// </summary>
	public bool EnableLogging
	{
		get => _enableLogging.Value;
		set => _enableLogging.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_openLots.Clear();
		CancelExitOrders();
		_pointValue = 0m;
		_lastBid = null;
		_lastAsk = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = CalculatePointSize();

		LoadExistingPositions();

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		UpdateProtectionOrders();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade?.Order?.Security != Security)
		{
			return;
		}

		var tradeInfo = trade.Trade;
		if (tradeInfo == null)
		{
			return;
		}

		var price = tradeInfo.Price ?? 0m;
		var volume = tradeInfo.Volume;
		if (price <= 0m || volume <= 0m)
		{
			return;
		}

		var commission = trade.Commission ?? 0m;

		ProcessExecution(trade.Order.Direction, volume, price, commission);

		UpdateProtectionOrders();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
		{
			_lastBid = (decimal)bid;
		}

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
		{
			_lastAsk = (decimal)ask;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		UpdateProtectionOrders();
	}

	private void UpdateProtectionOrders()
	{
		if (!TryBuildSummary(out var summary))
		{
			CancelExitOrders();
			return;
		}

		if (summary.NetVolume == 0m)
		{
			CancelExitOrders();
			return;
		}

		var breakEven = summary.GetBreakEvenPrice();
		if (breakEven <= 0m || _pointValue <= 0m)
		{
			CancelExitOrders();
			return;
		}

		var offset = DeltaPoints * _pointValue;
		var targetPrice = summary.NetVolume > 0m ? breakEven + offset : breakEven - offset;

		if (summary.LongVolume > 0m)
		{
			if (summary.NetVolume > 0m)
			{
				UpdateOrRegisterOrder(ref _longExitOrder, Sides.Sell, OrderTypes.Limit, targetPrice, summary.LongVolume);
			}
			else if (summary.NetVolume < 0m)
			{
				UpdateOrRegisterOrder(ref _longExitOrder, Sides.Sell, OrderTypes.Stop, targetPrice, summary.LongVolume);
			}
			else
			{
				CancelOrder(ref _longExitOrder);
			}
		}
		else
		{
			CancelOrder(ref _longExitOrder);
		}

		if (summary.ShortVolume > 0m)
		{
			if (summary.NetVolume > 0m)
			{
				UpdateOrRegisterOrder(ref _shortExitOrder, Sides.Buy, OrderTypes.Stop, targetPrice, summary.ShortVolume);
			}
			else if (summary.NetVolume < 0m)
			{
				UpdateOrRegisterOrder(ref _shortExitOrder, Sides.Buy, OrderTypes.Limit, targetPrice, summary.ShortVolume);
			}
			else
			{
				CancelOrder(ref _shortExitOrder);
			}
		}
		else
		{
			CancelOrder(ref _shortExitOrder);
		}

		if (EnableLogging)
		{
			LogSummary(summary, breakEven, targetPrice);
		}
	}

	private void LogSummary(PositionSummary summary, decimal breakEven, decimal targetPrice)
	{
		var bid = _lastBid ?? 0m;
		var ask = _lastAsk ?? bid;

		var distancePoints = _pointValue > 0m
		? Math.Abs(targetPrice - (summary.NetVolume > 0m ? bid : ask)) / _pointValue
		: 0m;

		var floatingProfit = CalculateFloatingProfit(summary, bid, ask);

		LogInfo(
		$"Break-even {breakEven:0.#####}, target {targetPrice:0.#####}, distance {distancePoints:0.##} pts, net volume {summary.NetVolume:0.###}, floating profit {floatingProfit:0.##}");
	}

	private decimal CalculateFloatingProfit(PositionSummary summary, decimal bid, decimal ask)
	{
		decimal profit = 0m;

		if (summary.LongVolume > 0m && bid > 0m)
		{
			profit += (bid - summary.LongAveragePrice) * summary.LongVolume + summary.LongCharges;
		}
		else
		{
			profit += summary.LongCharges;
		}

		if (summary.ShortVolume > 0m && ask > 0m)
		{
			profit += (summary.ShortAveragePrice - ask) * summary.ShortVolume + summary.ShortCharges;
		}
		else
		{
			profit += summary.ShortCharges;
		}

		return profit;
	}

	private bool TryBuildSummary(out PositionSummary summary)
	{
		decimal longVolume = 0m;
		decimal shortVolume = 0m;
		decimal longCost = 0m;
		decimal shortCost = 0m;
		decimal longCharges = 0m;
		decimal shortCharges = 0m;

		foreach (var lot in _openLots)
		{
			var volume = lot.Volume;
			if (volume <= 0m)
			{
				continue;
			}

			if (lot.Side == Sides.Buy)
			{
				longVolume += volume;
				longCost += lot.Price * volume;
				longCharges += lot.Charges;
			}
			else
			{
				shortVolume += volume;
				shortCost += lot.Price * volume;
				shortCharges += lot.Charges;
			}
		}

		var longAverage = longVolume > 0m ? longCost / longVolume : 0m;
		var shortAverage = shortVolume > 0m ? shortCost / shortVolume : 0m;

		summary = new PositionSummary(longVolume, shortVolume, longAverage, shortAverage, longCharges, shortCharges);
		return summary.HasPositions;
	}

	private void ProcessExecution(Sides side, decimal volume, decimal price, decimal commission)
	{
		if (volume <= 0m)
		{
			return;
		}

		var opposite = side == Sides.Buy ? Sides.Sell : Sides.Buy;
		var remaining = volume;

		for (var i = 0; i < _openLots.Count && remaining > 0m; i++)
		{
			var lot = _openLots[i];
			if (lot.Side != opposite)
			{
				continue;
			}

			var closable = Math.Min(lot.Volume, remaining);
			if (closable <= 0m)
			{
				continue;
			}

			lot.Volume -= closable;
			remaining -= closable;

			if (lot.Volume <= 0m)
			{
				_openLots.RemoveAt(i);
				i--;
			}
		}

		if (remaining <= 0m)
		{
			return;
		}

		var chargesPerUnit = volume > 0m ? commission / volume : 0m;
		_openLots.Add(new PositionLot(side, remaining, price, chargesPerUnit));
	}

	private void LoadExistingPositions()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
		{
			return;
		}

		foreach (var position in portfolio.Positions)
		{
			if (position.Security != Security)
			{
				continue;
			}

			var currentValue = position.CurrentValue ?? 0m;
			if (currentValue == 0m)
			{
				continue;
			}

			var volume = Math.Abs(currentValue);
			var side = currentValue > 0m ? Sides.Buy : Sides.Sell;
			var price = position.AveragePrice ?? Security?.LastPrice ?? 0m;
			if (price <= 0m)
			{
				continue;
			}

			_openLots.Add(new PositionLot(side, volume, price, 0m));
		}
	}

	private decimal CalculatePointSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		{
			return 1m;
		}

		return step;
	}

	private void CancelExitOrders()
	{
		CancelOrder(ref _longExitOrder);
		CancelOrder(ref _shortExitOrder);
	}

	private void CancelOrder(ref Order? order)
	{
		if (order != null && order.State == OrderStates.Active)
		{
			CancelOrder(order);
		}

		order = null;
	}

	private void UpdateOrRegisterOrder(ref Order? order, Sides side, OrderTypes type, decimal price, decimal volume)
	{
		if (price <= 0m || volume <= 0m)
		{
			CancelOrder(ref order);
			return;
		}

		if (order != null && order.State == OrderStates.Active && order.Price == price && order.Volume == volume && order.Type == type && order.Direction == side)
		{
			return;
		}

		CancelOrder(ref order);

		order = type switch
		{
			OrderTypes.Limit when side == Sides.Sell => SellLimit(volume, price),
			OrderTypes.Limit when side == Sides.Buy => BuyLimit(volume, price),
			OrderTypes.Stop when side == Sides.Sell => SellStop(volume, price),
			OrderTypes.Stop when side == Sides.Buy => BuyStop(volume, price),
			_ => throw new InvalidOperationException("Unsupported order configuration.")
		};
	}

	private sealed class PositionLot
	{
		public PositionLot(Sides side, decimal volume, decimal price, decimal chargesPerUnit)
		{
			Side = side;
			Volume = volume;
			Price = price;
			ChargesPerUnit = chargesPerUnit;
		}

		public Sides Side { get; }

		public decimal Volume { get; set; }

		public decimal Price { get; }

		public decimal ChargesPerUnit { get; }

		public decimal Charges => ChargesPerUnit * Volume;
	}

	private readonly struct PositionSummary
	{
		public PositionSummary(decimal longVolume, decimal shortVolume, decimal longAveragePrice, decimal shortAveragePrice, decimal longCharges, decimal shortCharges)
		{
			LongVolume = longVolume;
			ShortVolume = shortVolume;
			LongAveragePrice = longAveragePrice;
			ShortAveragePrice = shortAveragePrice;
			LongCharges = longCharges;
			ShortCharges = shortCharges;
		}

		public decimal LongVolume { get; }

		public decimal ShortVolume { get; }

		public decimal LongAveragePrice { get; }

		public decimal ShortAveragePrice { get; }

		public decimal LongCharges { get; }

		public decimal ShortCharges { get; }

		public decimal NetVolume => LongVolume - ShortVolume;

		public bool HasPositions => LongVolume > 0m || ShortVolume > 0m;

		public decimal TotalCharges => LongCharges + ShortCharges;

		public decimal GetBreakEvenPrice()
		{
			var denominator = NetVolume;
			if (denominator == 0m)
			{
				return 0m;
			}

			var numerator = LongAveragePrice * LongVolume - ShortAveragePrice * ShortVolume - TotalCharges;
			return numerator / denominator;
		}
	}
}
