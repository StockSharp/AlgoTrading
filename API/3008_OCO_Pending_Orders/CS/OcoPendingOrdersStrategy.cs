using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// OCO pending order strategy converted from the MetaTrader OCO_EA expert advisor.
/// Places market orders when price reaches configured limit or stop levels and optionally clears remaining orders.
/// </summary>
public class OcoPendingOrdersStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _buyLimitPrice;
	private readonly StrategyParam<decimal> _buyStopPrice;
	private readonly StrategyParam<decimal> _sellLimitPrice;
	private readonly StrategyParam<decimal> _sellStopPrice;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<bool> _useOcoLink;
	private readonly StrategyParam<bool> _isArmed;

	private decimal? _currentBid;
	private decimal? _currentAsk;
	private decimal _pipSize;
	private decimal _stopLossOffset;
	private decimal _takeProfitOffset;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public OcoPendingOrdersStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order volume", "Volume sent with each market order", "Trading");

		_buyLimitPrice = Param(nameof(BuyLimitPrice), 0m)
			.SetDisplay("Buy limit price", "Ask price threshold for long entries", "Orders");

		_buyStopPrice = Param(nameof(BuyStopPrice), 0m)
			.SetDisplay("Buy stop price", "Ask price threshold for momentum long entries", "Orders");

		_sellLimitPrice = Param(nameof(SellLimitPrice), 0m)
			.SetDisplay("Sell limit price", "Bid price threshold for short entries", "Orders");

		_sellStopPrice = Param(nameof(SellStopPrice), 0m)
			.SetDisplay("Sell stop price", "Bid price threshold for momentum short entries", "Orders");

		_stopLossPips = Param(nameof(StopLossPips), 300)
			.SetGreaterThanZero()
			.SetDisplay("Stop loss (pips)", "Distance in points used for protective stop", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 300)
			.SetGreaterThanZero()
			.SetDisplay("Take profit (pips)", "Distance in points used for profit target", "Risk");

		_useOcoLink = Param(nameof(UseOcoLink), true)
			.SetDisplay("Use OCO link", "When enabled, the first fill cancels other pending triggers", "Orders");

		_isArmed = Param(nameof(IsArmed), false)
			.SetDisplay("Armed", "Enable price monitoring and allow executions", "Trading");
	}

	/// <summary>
	/// Volume used when sending market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Ask price threshold for buy limit style entries.
	/// </summary>
	public decimal BuyLimitPrice
	{
		get => _buyLimitPrice.Value;
		set => _buyLimitPrice.Value = value;
	}

	/// <summary>
	/// Ask price threshold for buy stop style entries.
	/// </summary>
	public decimal BuyStopPrice
	{
		get => _buyStopPrice.Value;
		set => _buyStopPrice.Value = value;
	}

	/// <summary>
	/// Bid price threshold for sell limit style entries.
	/// </summary>
	public decimal SellLimitPrice
	{
		get => _sellLimitPrice.Value;
		set => _sellLimitPrice.Value = value;
	}

	/// <summary>
	/// Bid price threshold for sell stop style entries.
	/// </summary>
	public decimal SellStopPrice
	{
		get => _sellStopPrice.Value;
		set => _sellStopPrice.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Indicates whether the first trigger cancels the rest.
	/// </summary>
	public bool UseOcoLink
	{
		get => _useOcoLink.Value;
		set => _useOcoLink.Value = value;
	}

	/// <summary>
	/// Enables order execution when true.
	/// </summary>
	public bool IsArmed
	{
		get => _isArmed.Value;
		set => _isArmed.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentBid = null;
		_currentAsk = null;
		_pipSize = 0m;
		_stopLossOffset = 0m;
		_takeProfitOffset = 0m;
		IsArmed = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.PriceStep ?? 0m;
		if (_pipSize <= 0m)
		{
			_pipSize = 1m;
		}

		_stopLossOffset = StopLossPips * _pipSize;
		_takeProfitOffset = TakeProfitPips * _pipSize;

		StartProtection(
			stopLoss: new Unit(_stopLossOffset, UnitTypes.Absolute),
			takeProfit: new Unit(_takeProfitOffset, UnitTypes.Absolute),
			useMarketOrders: true);

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid) && bid is decimal bidPrice)
		{
			_currentBid = bidPrice;
		}

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask) && ask is decimal askPrice)
		{
			_currentAsk = askPrice;
		}

		TryExecuteOrders();
	}

	private void TryExecuteOrders()
	{
		if (!IsArmed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (OrderVolume <= 0m)
			return;

		if (_currentAsk is decimal ask)
		{
			if (BuyLimitPrice > 0m && ask <= BuyLimitPrice)
			{
				AddInfoLog($"Buy limit triggered at ask {ask} (threshold {BuyLimitPrice}).");
				BuyMarket(OrderVolume);
				BuyLimitPrice = 0m;

				if (UseOcoLink)
				{
					ResetAllOrders();
					AddInfoLog("OCO link cleared remaining pending orders after buy limit execution.");
					return;
				}
			}

			if (BuyStopPrice > 0m && ask >= BuyStopPrice)
			{
				AddInfoLog($"Buy stop triggered at ask {ask} (threshold {BuyStopPrice}).");
				BuyMarket(OrderVolume);
				BuyStopPrice = 0m;

				if (UseOcoLink)
				{
					ResetAllOrders();
					AddInfoLog("OCO link cleared remaining pending orders after buy stop execution.");
					return;
				}
			}
		}

		if (_currentBid is decimal bidPrice)
		{
			if (SellLimitPrice > 0m && bidPrice >= SellLimitPrice)
			{
				AddInfoLog($"Sell limit triggered at bid {bidPrice} (threshold {SellLimitPrice}).");
				SellMarket(OrderVolume);
				SellLimitPrice = 0m;

				if (UseOcoLink)
				{
					ResetAllOrders();
					AddInfoLog("OCO link cleared remaining pending orders after sell limit execution.");
					return;
				}
			}

			if (SellStopPrice > 0m && bidPrice <= SellStopPrice)
			{
				AddInfoLog($"Sell stop triggered at bid {bidPrice} (threshold {SellStopPrice}).");
				SellMarket(OrderVolume);
				SellStopPrice = 0m;

				if (UseOcoLink)
				{
					ResetAllOrders();
					AddInfoLog("OCO link cleared remaining pending orders after sell stop execution.");
					return;
				}
			}
		}

		UpdateArmingState();
	}

	private void ResetAllOrders()
	{
		BuyLimitPrice = 0m;
		BuyStopPrice = 0m;
		SellLimitPrice = 0m;
		SellStopPrice = 0m;
		IsArmed = false;
	}

	private void UpdateArmingState()
	{
		if (BuyLimitPrice <= 0m &&
			BuyStopPrice <= 0m &&
			SellLimitPrice <= 0m &&
			SellStopPrice <= 0m)
		{
			IsArmed = false;
		}
	}
}
