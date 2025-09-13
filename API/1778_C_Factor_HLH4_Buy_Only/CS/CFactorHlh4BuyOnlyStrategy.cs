using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// C Factor HLH4 buy-only strategy.
/// Buys when price closes above previous candle high.
/// Exits based on previous candle levels.
/// </summary>
public class CFactorHlh4BuyOnlyStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private decimal _prevLow;

	/// <summary>
	/// Initializes a new instance of <see cref="CFactorHlh4BuyOnlyStrategy"/>.
	/// </summary>
	public CFactorHlh4BuyOnlyStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 100)
			.SetDisplay("Stop Loss", "Stop loss distance in ticks", "Risk Management");

		_takeProfit = Param(nameof(TakeProfit), 200)
			.SetDisplay("Take Profit", "Take profit distance in ticks", "Risk Management");

		_riskPercent = Param(nameof(RiskPercent), 5m)
			.SetDisplay("Risk %", "Percent of equity risked per trade", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <summary>
	/// Stop loss distance in ticks.
	/// </summary>
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take profit distance in ticks.
	/// </summary>
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Percent of equity risked per trade.
	/// </summary>
	public decimal RiskPercent { get => _riskPercent.Value; set => _riskPercent.Value = value; }

	/// <summary>
	/// Candle type and timeframe.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHigh = 0m;
		_prevLow = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var step = Security.PriceStep ?? 1m;
		StartProtection(
			stopLoss: new Unit(StopLoss * step, UnitTypes.Absolute),
			takeProfit: new Unit(TakeProfit * step, UnitTypes.Absolute),
			isStopTrailing: false,
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var step = Security.PriceStep ?? 1m;

		if (Position > 0)
		{
			if (candle.ClosePrice >= _prevLow + 100m * step ||
				candle.ClosePrice <= _prevHigh - 20m * step)
			{
				SellMarket(Position);
				return;
			}
		}
		else if (Position == 0 && candle.ClosePrice >= _prevHigh)
		{
			var qty = CalculateQty();
			if (qty > 0)
				BuyMarket(qty);
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}

	private decimal CalculateQty()
	{
		var equity = Portfolio?.CurrentValue ?? 0m;
		var step = Security.PriceStep ?? 1m;
		var riskValue = equity * RiskPercent / 100m;
		var stopDist = StopLoss * step;
		return stopDist > 0m ? riskValue / stopDist : 0m;
	}
}
