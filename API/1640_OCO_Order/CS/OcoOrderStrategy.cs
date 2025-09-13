using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Executes one-cancels-the-other orders using predefined trigger prices.
/// Monitors best bid and ask to submit market orders when a trigger is hit.
/// Applies stop-loss and take-profit protection measured in pips.
/// </summary>
public class OcoOrderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _buyLimitPrice;
	private readonly StrategyParam<decimal> _sellLimitPrice;
	private readonly StrategyParam<decimal> _buyStopPrice;
	private readonly StrategyParam<decimal> _sellStopPrice;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _isOco;
	private readonly StrategyParam<bool> _confirmation;

	private decimal _bestBid;
	private decimal _bestAsk;

	/// <summary>
	/// Price that triggers a market buy when the ask drops to or below it.
	/// </summary>
	public decimal BuyLimitPrice
	{
		get => _buyLimitPrice.Value;
		set => _buyLimitPrice.Value = value;
	}

	/// <summary>
	/// Price that triggers a market sell when the bid rises to or above it.
	/// </summary>
	public decimal SellLimitPrice
	{
		get => _sellLimitPrice.Value;
		set => _sellLimitPrice.Value = value;
	}

	/// <summary>
	/// Price that triggers a market buy when the ask rises to or above it.
	/// </summary>
	public decimal BuyStopPrice
	{
		get => _buyStopPrice.Value;
		set => _buyStopPrice.Value = value;
	}

	/// <summary>
	/// Price that triggers a market sell when the bid falls to or below it.
	/// </summary>
	public decimal SellStopPrice
	{
		get => _sellStopPrice.Value;
		set => _sellStopPrice.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// If true, remaining triggers are cleared after the first order.
	/// </summary>
	public bool IsOco
	{
		get => _isOco.Value;
		set => _isOco.Value = value;
	}

	/// <summary>
	/// Enables order triggers when set to true.
	/// </summary>
	public bool Confirmation
	{
		get => _confirmation.Value;
		set => _confirmation.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public OcoOrderStrategy()
	{
		_buyLimitPrice = Param(nameof(BuyLimitPrice), 0m)
			.SetDisplay("Buy Limit Price", "Trigger price for limit buy", "General");

		_sellLimitPrice = Param(nameof(SellLimitPrice), 0m)
			.SetDisplay("Sell Limit Price", "Trigger price for limit sell", "General");

		_buyStopPrice = Param(nameof(BuyStopPrice), 0m)
			.SetDisplay("Buy Stop Price", "Trigger price for stop buy", "General");

		_sellStopPrice = Param(nameof(SellStopPrice), 0m)
			.SetDisplay("Sell Stop Price", "Trigger price for stop sell", "General");

		_stopLossPips = Param(nameof(StopLossPips), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Pips", "Stop-loss distance in pips", "Risk Management");

		_takeProfitPips = Param(nameof(TakeProfitPips), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Pips", "Take-profit distance in pips", "Risk Management");

		_isOco = Param(nameof(IsOco), true)
			.SetDisplay("OCO Mode", "Cancel other triggers after first order", "General");

		_confirmation = Param(nameof(Confirmation), false)
			.SetDisplay("Confirmation", "Enable order triggers", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(
			takeProfit: new Unit(TakeProfitPips * Security.PriceStep, UnitTypes.Absolute),
			stopLoss: new Unit(StopLossPips * Security.PriceStep, UnitTypes.Absolute)
		);

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

		CheckTriggers();
	}

	private void CheckTriggers()
	{
		if (!Confirmation)
			return;

		if (BuyLimitPrice > 0 && _bestAsk <= BuyLimitPrice)
		{
			BuyMarket(Volume);
			AfterOrderTriggered(_buyLimitPrice);
		}
		else if (SellLimitPrice > 0 && _bestBid >= SellLimitPrice)
		{
			SellMarket(Volume);
			AfterOrderTriggered(_sellLimitPrice);
		}
		else if (BuyStopPrice > 0 && _bestAsk >= BuyStopPrice)
		{
			BuyMarket(Volume);
			AfterOrderTriggered(_buyStopPrice);
		}
		else if (SellStopPrice > 0 && _bestBid <= SellStopPrice)
		{
			SellMarket(Volume);
			AfterOrderTriggered(_sellStopPrice);
		}

		if (BuyLimitPrice == 0 && SellLimitPrice == 0 && BuyStopPrice == 0 && SellStopPrice == 0)
			Confirmation = false;
	}

	private void AfterOrderTriggered(StrategyParam<decimal> levelParam)
	{
		levelParam.Value = 0;

		if (IsOco)
		{
			_buyLimitPrice.Value = 0;
			_sellLimitPrice.Value = 0;
			_buyStopPrice.Value = 0;
			_sellStopPrice.Value = 0;
			Confirmation = false;
		}
	}
}
