namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Manual trade assistant converted from the MetaTrader backtesting panel.
/// Provides public helpers to adjust volume, risk parameters, and trigger market orders.
/// </summary>
public class BacktestingTradeAssistantPanelStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<int> _magicNumber;

	private decimal _pipSize;
	private decimal? _bestBid;
	private decimal? _bestAsk;

	/// <summary>
	/// Initializes a new instance of <see cref="BacktestingTradeAssistantPanelStrategy"/>.
	/// </summary>
	public BacktestingTradeAssistantPanelStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume used when manual entries are triggered.", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Distance to the stop loss expressed in MetaTrader points.", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 100m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Distance to the take profit expressed in MetaTrader points.", "Risk");

		_magicNumber = Param(nameof(MagicNumber), 99)
			.SetDisplay("Magic Number", "Identifier kept for compatibility with the MT4 version.", "General");
	}

	/// <summary>
	/// Trading volume sent with market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set
		{
			_orderVolume.Value = value;
			if (value > 0m)
				Volume = value;
		}
	}

	/// <summary>
	/// Stop loss distance measured in MetaTrader points.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance measured in MetaTrader points.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Preserved magic number that identifies manual trades.
	/// </summary>
	public int MagicNumber
	{
		get => _magicNumber.Value;
		set => _magicNumber.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Level1)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pipSize = 0m;
		_bestBid = null;
		_bestAsk = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;
		_pipSize = CalculatePipSize();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid)
			_bestBid = bid;

		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask)
			_bestAsk = ask;
	}

	/// <summary>
	/// Updates the trading volume and synchronizes it with the internal <see cref="Strategy.Volume"/> property.
	/// </summary>
	/// <param name="volume">Volume in lots.</param>
	public void SetOrderVolume(decimal volume)
	{
		OrderVolume = volume;
	}

	/// <summary>
	/// Applies a new stop-loss distance in MetaTrader points.
	/// </summary>
	/// <param name="pips">Distance measured in MetaTrader points.</param>
	public void SetStopLoss(decimal pips)
	{
		StopLossPips = pips;
	}

	/// <summary>
	/// Applies a new take-profit distance in MetaTrader points.
	/// </summary>
	/// <param name="pips">Distance measured in MetaTrader points.</param>
	public void SetTakeProfit(decimal pips)
	{
		TakeProfitPips = pips;
	}

	/// <summary>
	/// Opens a long position with the configured volume and attaches protective orders.
	/// </summary>
	public void ManualBuy()
	{
		var volume = NormalizeVolume(OrderVolume);
		if (volume <= 0m)
			return;

		var currentPosition = Position;
		var referencePrice = GetReferencePrice(isLong: true);

		BuyMarket(volume);

		if (referencePrice is decimal price)
		{
			var resultingPosition = currentPosition + volume;
			AttachProtection(price, resultingPosition);
		}
	}

	/// <summary>
	/// Opens a short position with the configured volume and attaches protective orders.
	/// </summary>
	public void ManualSell()
	{
		var volume = NormalizeVolume(OrderVolume);
		if (volume <= 0m)
			return;

		var currentPosition = Position;
		var referencePrice = GetReferencePrice(isLong: false);

		SellMarket(volume);

		if (referencePrice is decimal price)
		{
			var resultingPosition = currentPosition - volume;
			AttachProtection(price, resultingPosition);
		}
	}

	/// <summary>
	/// Closes any open position at market price.
	/// </summary>
	public void CloseAllPositions()
	{
		var position = Position;
		if (position > 0m)
		{
			SellMarket(position);
		}
		else if (position < 0m)
		{
			BuyMarket(-position);
		}
	}

	private void AttachProtection(decimal referencePrice, decimal resultingPosition)
	{
		var stopDistance = ConvertPipsToPrice(StopLossPips);
		if (stopDistance > 0m)
			SetStopLoss(stopDistance, referencePrice, resultingPosition);

		var takeDistance = ConvertPipsToPrice(TakeProfitPips);
		if (takeDistance > 0m)
			SetTakeProfit(takeDistance, referencePrice, resultingPosition);
	}

	private decimal? GetReferencePrice(bool isLong)
	{
		if (isLong)
		{
			if (_bestAsk is decimal ask)
				return ask;
		}
		else
		{
			if (_bestBid is decimal bid)
				return bid;
		}

		var security = Security;
		return security?.LastTradePrice;
	}

	private decimal ConvertPipsToPrice(decimal pips)
	{
		if (pips <= 0m)
			return 0m;

		var pipSize = _pipSize;
		if (pipSize <= 0m)
			pipSize = _pipSize = CalculatePipSize();

		return pips * pipSize;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		var priceStep = security.PriceStep ?? 0m;
		if (priceStep <= 0m)
			priceStep = 0.0001m;

		var decimals = security.Decimals ?? 0;
		var multiplier = decimals is 5 or 3 ? 10m : 1m;
		return priceStep * multiplier;
	}
}
