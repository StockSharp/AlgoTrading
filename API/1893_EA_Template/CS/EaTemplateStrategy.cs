namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Simple candle color strategy converted from a MetaTrader expert advisor.
/// Opens a long position after a bullish candle and a short position after a bearish one.
/// Optional reverse mode flips entry and exit signals.
/// </summary>
public class EaTemplateStrategy : Strategy
{
	private readonly StrategyParam<bool> _reverseTrade;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _lots;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _spreadLimit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	/// <summary>
	/// Invert entry and exit signals.
	/// </summary>
	public bool ReverseTrade
	{
		get => _reverseTrade.Value;
		set => _reverseTrade.Value = value;
	}

	/// <summary>
	/// Use equity based volume calculation.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Percent of equity to risk per trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Fixed order size.
	/// </summary>
	public decimal Lots
	{
		get => _lots.Value;
		set => _lots.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread in points.
	/// </summary>
	public int SpreadLimit
	{
		get => _spreadLimit.Value;
		set => _spreadLimit.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="EaTemplateStrategy"/>.
	/// </summary>
	public EaTemplateStrategy()
	{
		_reverseTrade = Param(nameof(ReverseTrade), false)
				.SetDisplay("Reverse Trade", "Invert entry and exit signals", "Trading");

		_useMoneyManagement = Param(nameof(UseMoneyManagement), false)
				.SetDisplay("Use Money Management", "Use risk based volume calculation", "Risk Management");

		_riskPercent = Param(nameof(RiskPercent), 30m)
				.SetRange(1m, 100m)
				.SetDisplay("Risk Percent", "Percent of equity to risk", "Risk Management");

		_lots = Param(nameof(Lots), 0.1m)
				.SetGreaterThanZero()
				.SetDisplay("Fixed Lot Size", "Fixed order size", "Risk Management");

		_stopLoss = Param(nameof(StopLoss), 50)
				.SetDisplay("Stop Loss", "Stop loss in points", "Risk Management");

		_takeProfit = Param(nameof(TakeProfit), 70)
				.SetDisplay("Take Profit", "Take profit in points", "Risk Management");

		_spreadLimit = Param(nameof(SpreadLimit), 10)
				.SetDisplay("Spread Limit", "Maximum spread in points", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles", "General");
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
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		var spread = Security.BestAsk - Security.BestBid;
		var maxSpread = SpreadLimit * Security.PriceStep;
		if (maxSpread > 0 && spread > maxSpread)
				return;

		var isBullish = candle.ClosePrice > candle.OpenPrice;
		var isBearish = candle.ClosePrice < candle.OpenPrice;

		if (Position == 0)
		{
				if ((isBullish && !ReverseTrade) || (isBearish && ReverseTrade))
				{
					var volume = GetOrderVolume(candle.ClosePrice);
					BuyMarket(volume);
					_entryPrice = candle.ClosePrice;
				}
				else if ((isBearish && !ReverseTrade) || (isBullish && ReverseTrade))
				{
					var volume = GetOrderVolume(candle.ClosePrice);
					SellMarket(volume);
					_entryPrice = candle.ClosePrice;
				}

				return;
		}

		var exitLong = (isBearish && !ReverseTrade) || (isBullish && ReverseTrade);
		var exitShort = (isBullish && !ReverseTrade) || (isBearish && ReverseTrade);

		if (Position > 0 && exitLong)
				SellMarket(Position);

		if (Position < 0 && exitShort)
				BuyMarket(-Position);

		if (Position > 0)
				CheckStopsForLong(candle.ClosePrice);
		else if (Position < 0)
				CheckStopsForShort(candle.ClosePrice);
	}

	private decimal GetOrderVolume(decimal price)
	{
		if (UseMoneyManagement)
		{
				var portfolioValue = Portfolio.CurrentValue ?? 0m;
				var size = portfolioValue * (RiskPercent / 100m) / price;
				return size > 0 ? size : Lots;
		}

		return Lots;
	}

	private void CheckStopsForLong(decimal price)
	{
		var stop = StopLoss * Security.PriceStep;
		if (stop > 0 && price <= _entryPrice - stop)
		{
				SellMarket(Position);
				return;
		}

		var profit = TakeProfit * Security.PriceStep;
		if (profit > 0 && price >= _entryPrice + profit)
				SellMarket(Position);
	}

	private void CheckStopsForShort(decimal price)
	{
		var stop = StopLoss * Security.PriceStep;
		if (stop > 0 && price >= _entryPrice + stop)
		{
				BuyMarket(-Position);
				return;
		}

		var profit = TakeProfit * Security.PriceStep;
		if (profit > 0 && price <= _entryPrice - profit)
				BuyMarket(-Position);
	}
}
