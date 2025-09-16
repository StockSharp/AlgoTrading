using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Stochastic extremes.
/// Places a market order and layered limit orders when the oscillator reaches extreme levels.
/// Closes positions on profit using Bollinger Bands and trend confirmation.
/// </summary>
public class ElliottTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _order2;
	private readonly StrategyParam<decimal> _order3;
	private readonly StrategyParam<decimal> _order4;
	private readonly StrategyParam<decimal> _order5;
	private readonly StrategyParam<decimal> _order6;
	private readonly StrategyParam<decimal> _order7;
	private readonly StrategyParam<decimal> _order8;
	private readonly StrategyParam<decimal> _order9;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<Order> _buyLimits = new();
	private readonly List<Order> _sellLimits = new();
	private decimal _step;

	/// <summary>
	/// Stochastic %K length.
	/// </summary>
	public int StochLength { get => _stochLength.Value; set => _stochLength.Value = value; }

	/// <summary>
	/// Overbought level for Stochastic.
	/// </summary>
	public decimal OverboughtLevel { get => _overbought.Value; set => _overbought.Value = value; }

	/// <summary>
	/// Oversold level for Stochastic.
	/// </summary>
	public decimal OversoldLevel { get => _oversold.Value; set => _oversold.Value = value; }

	/// <summary>
	/// Profit target in currency.
	/// </summary>
	public decimal ProfitTarget { get => _profitTarget.Value; set => _profitTarget.Value = value; }

	/// <summary>
	/// Distance in pips for second pending order.
	/// </summary>
	public decimal Order2Offset { get => _order2.Value; set => _order2.Value = value; }

	/// <summary>
	/// Distance in pips for third pending order.
	/// </summary>
	public decimal Order3Offset { get => _order3.Value; set => _order3.Value = value; }

	/// <summary>
	/// Distance in pips for fourth pending order.
	/// </summary>
	public decimal Order4Offset { get => _order4.Value; set => _order4.Value = value; }

	/// <summary>
	/// Distance in pips for fifth pending order.
	/// </summary>
	public decimal Order5Offset { get => _order5.Value; set => _order5.Value = value; }

	/// <summary>
	/// Distance in pips for sixth pending order.
	/// </summary>
	public decimal Order6Offset { get => _order6.Value; set => _order6.Value = value; }

	/// <summary>
	/// Distance in pips for seventh pending order.
	/// </summary>
	public decimal Order7Offset { get => _order7.Value; set => _order7.Value = value; }

	/// <summary>
	/// Distance in pips for eighth pending order.
	/// </summary>
	public decimal Order8Offset { get => _order8.Value; set => _order8.Value = value; }

	/// <summary>
	/// Distance in pips for ninth pending order.
	/// </summary>
	public decimal Order9Offset { get => _order9.Value; set => _order9.Value = value; }

	/// <summary>
	/// Candle type for indicator calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="ElliottTraderStrategy"/>.
	/// </summary>
	public ElliottTraderStrategy()
	{
		_stochLength = Param(nameof(StochLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Length", "Length for %K", "Indicator")
			.SetCanOptimize(true);

		_overbought = Param(nameof(OverboughtLevel), 80m)
			.SetDisplay("Overbought", "Level to start selling", "Indicator")
			.SetCanOptimize(true);

		_oversold = Param(nameof(OversoldLevel), 20m)
			.SetDisplay("Oversold", "Level to start buying", "Indicator")
			.SetCanOptimize(true);

		_profitTarget = Param(nameof(ProfitTarget), 100m)
			.SetDisplay("Profit Target", "Close when PnL reaches value", "Risk")
			.SetCanOptimize(true);

		_order2 = Param(nameof(Order2Offset), 55m)
			.SetDisplay("Order2 Offset", "Distance for second order", "Orders");
		_order3 = Param(nameof(Order3Offset), 89m)
			.SetDisplay("Order3 Offset", "Distance for third order", "Orders");
		_order4 = Param(nameof(Order4Offset), 144m)
			.SetDisplay("Order4 Offset", "Distance for fourth order", "Orders");
		_order5 = Param(nameof(Order5Offset), 210m)
			.SetDisplay("Order5 Offset", "Distance for fifth order", "Orders");
		_order6 = Param(nameof(Order6Offset), 360m)
			.SetDisplay("Order6 Offset", "Distance for sixth order", "Orders");
		_order7 = Param(nameof(Order7Offset), 450m)
			.SetDisplay("Order7 Offset", "Distance for seventh order", "Orders");
		_order8 = Param(nameof(Order8Offset), 520m)
			.SetDisplay("Order8 Offset", "Distance for eighth order", "Orders");
		_order9 = Param(nameof(Order9Offset), 630m)
			.SetDisplay("Order9 Offset", "Distance for ninth order", "Orders");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles for calculation", "General");
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
		_buyLimits.Clear();
		_sellLimits.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_step = Security?.PriceStep ?? 1m;

		var stochastic = new StochasticOscillator
		{
			Length = StochLength,
			K = { Length = 3 },
			D = { Length = 3 }
		};

		var bollinger = new BollingerBands
		{
			Length = 20,
			Width = 2m
		};

		var maSlow = new SMA { Length = 200 };
		var maFast = new SMA { Length = 55 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, bollinger, maSlow, maFast, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, maSlow);
			DrawIndicator(area, maFast);
			var stochArea = CreateChartArea();
			DrawIndicator(stochArea, stochastic);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue, IIndicatorValue bbValue, IIndicatorValue maSlowValue, IIndicatorValue maFastValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal k)
			return;

		var bb = (BollingerBandsValue)bbValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		var maSlow = maSlowValue.ToDecimal();
		var maFast = maFastValue.ToDecimal();
		var price = candle.ClosePrice;

		if (k >= 90m && maSlow <= maFast)
			CancelOrders(_buyLimits);
		else if (k <= 10m && maSlow >= maFast)
			CancelOrders(_sellLimits);

		if (Position > 0 && PnL >= ProfitTarget && price >= lower && maSlow >= maFast)
		{
			SellMarket(Position);
			CancelOrders(_buyLimits);
			return;
		}
		else if (Position < 0 && PnL >= ProfitTarget && price <= upper && maSlow <= maFast)
		{
			BuyMarket(Math.Abs(Position));
			CancelOrders(_sellLimits);
			return;
		}

		if (Position == 0 && _buyLimits.Count == 0 && _sellLimits.Count == 0)
		{
			if (k >= OverboughtLevel)
			{
				SellMarket(Volume);
				PlaceSellLimits(price);
			}
			else if (k <= OversoldLevel)
			{
				BuyMarket(Volume);
				PlaceBuyLimits(price);
			}
		}
	}

	private void PlaceSellLimits(decimal basePrice)
	{
		var offsets = new[] { Order2Offset, Order3Offset, Order4Offset, Order5Offset, Order6Offset, Order7Offset, Order8Offset, Order9Offset };
		foreach (var off in offsets)
		{
			var order = SellLimit(basePrice + off * _step, Volume);
			_sellLimits.Add(order);
		}
	}

	private void PlaceBuyLimits(decimal basePrice)
	{
		var offsets = new[] { Order2Offset, Order3Offset, Order4Offset, Order5Offset, Order6Offset, Order7Offset, Order8Offset, Order9Offset };
		foreach (var off in offsets)
		{
			var order = BuyLimit(basePrice - off * _step, Volume);
			_buyLimits.Add(order);
		}
	}

	private void CancelOrders(List<Order> orders)
	{
		foreach (var order in orders)
		{
			if (order?.State == OrderStates.Active)
				CancelOrder(order);
		}
		orders.Clear();
	}
}
