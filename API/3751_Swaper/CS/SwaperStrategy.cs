namespace StockSharp.Samples.Strategies;

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using StockSharp.Algo.Candles;

/// <summary>
/// Swap-based mean reversion strategy converted from the MetaTrader expert "Swaper 1.1".
/// Calculates a synthetic fair value using closed trades, adjusts the open position, and keeps the volume within the available margin.
/// </summary>
public class SwaperStrategy : Strategy
{
	private readonly StrategyParam<decimal> _experts;
	private readonly StrategyParam<decimal> _beginPrice;
	private readonly StrategyParam<int> _magicNumber;
	private readonly StrategyParam<decimal> _baseUnits;
	private readonly StrategyParam<decimal> _contractMultiplier;
	private readonly StrategyParam<decimal> _marginPerLot;
	private readonly StrategyParam<decimal> _fallbackSpreadSteps;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _initialCapital;
	private decimal _realizedPnL;
	private decimal _positionVolume;
	private decimal _averagePrice;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private ICandleMessage _previousCandle;

	/// <summary>
	/// Initializes a new instance of the <see cref="SwaperStrategy"/> class.
	/// </summary>
	public SwaperStrategy()
	{
		_experts = Param(nameof(Experts), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Experts", "Weighting coefficient applied to the synthetic fair value.", "General");

		_beginPrice = Param(nameof(BeginPrice), 1.8014m)
		.SetGreaterThanZero()
		.SetDisplay("Begin Price", "Initial price used to recreate the historical balance.", "General");

		_magicNumber = Param(nameof(MagicNumber), 777)
		.SetDisplay("Magic Number", "Identifier kept for compatibility with the MetaTrader expert.", "General");

		_baseUnits = Param(nameof(BaseUnits), 1000m)
		.SetGreaterThanZero()
		.SetDisplay("Base Units", "Synthetic account units used when calculating the fair value denominator.", "Money Management");

		_contractMultiplier = Param(nameof(ContractMultiplier), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Contract Multiplier", "Value multiplier applied to realized and unrealized profit.", "Money Management");

		_marginPerLot = Param(nameof(MarginPerLot), 1000m)
		.SetGreaterThanZero()
		.SetDisplay("Margin Per Lot", "Approximate capital required to keep one lot open.", "Money Management");

		_fallbackSpreadSteps = Param(nameof(FallbackSpreadSteps), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Fallback Spread (steps)", "Spread expressed in price steps when level-one data is unavailable.", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe that replaces the tick-based loop of the original expert.", "Data");
	}

	/// <summary>
	/// Weighting coefficient applied to the synthetic fair value.
	/// </summary>
	public decimal Experts
	{
		get => _experts.Value;
		set => _experts.Value = value;
	}

	/// <summary>
	/// Initial price used to recreate the historical balance.
	/// </summary>
	public decimal BeginPrice
	{
		get => _beginPrice.Value;
		set => _beginPrice.Value = value;
	}

	/// <summary>
	/// Identifier kept for compatibility with the MetaTrader expert.
	/// </summary>
	public int MagicNumber
	{
		get => _magicNumber.Value;
		set => _magicNumber.Value = value;
	}

	/// <summary>
	/// Synthetic account units used when calculating the fair value denominator.
	/// </summary>
	public decimal BaseUnits
	{
		get => _baseUnits.Value;
		set => _baseUnits.Value = value;
	}

	/// <summary>
	/// Value multiplier applied to realized and unrealized profit.
	/// </summary>
	public decimal ContractMultiplier
	{
		get => _contractMultiplier.Value;
		set => _contractMultiplier.Value = value;
	}

	/// <summary>
	/// Approximate capital required to keep one lot open.
	/// </summary>
	public decimal MarginPerLot
	{
		get => _marginPerLot.Value;
		set => _marginPerLot.Value = value;
	}

	/// <summary>
	/// Spread expressed in price steps when level-one data is unavailable.
	/// </summary>
	public decimal FallbackSpreadSteps
	{
		get => _fallbackSpreadSteps.Value;
		set => _fallbackSpreadSteps.Value = value;
	}

	/// <summary>
	/// Primary timeframe that replaces the tick-based loop of the original expert.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var security = Security;

		if (security != null)
		{
			yield return (security, CandleType);
			yield return (security, DataType.Level1);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_initialCapital = 0m;
		_realizedPnL = 0m;
		_positionVolume = 0m;
		_averagePrice = 0m;
		_bestBid = null;
		_bestAsk = null;
		_previousCandle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_initialCapital = BaseUnits * BeginPrice;
		_realizedPnL = 0m;
		_positionVolume = 0m;
		_averagePrice = 0m;
		_bestBid = null;
		_bestAsk = null;
		_previousCandle = null;

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription.Bind(ProcessCandle).Start();

		SubscribeLevel1().Bind(ProcessLevel1).Start();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid)
		_bestBid = bid;

		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask)
		_bestAsk = ask;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
		_previousCandle = candle;
		return;
		}

		if (_previousCandle == null)
		{
		_previousCandle = candle;
		return;
		}

		var security = Security;
		var priceStep = security?.PriceStep ?? 0.0001m;
		var spread = GetSpread(priceStep);
		var high = Math.Max(candle.HighPrice, _previousCandle.HighPrice);
		var low = Math.Min(candle.LowPrice, _previousCandle.LowPrice);

		if (high <= 0m || low <= 0m)
		{
		_previousCandle = candle;
		return;
		}

		var denominator = high + spread;
		if (denominator <= 0m)
		{
		_previousCandle = candle;
		return;
		}

		var com = CalculateDenominator();
		if (com == 0m)
		{
		_previousCandle = candle;
		return;
		}

		var money = CalculateSyntheticCapital(candle.ClosePrice);
		var expertsWeight = Experts;
		var dt = (money / denominator - com) * expertsWeight / (expertsWeight + 1m);

		if (dt < 0m)
		{
		var altDenominator = money / low;
		var dtAlt = (com - altDenominator) * expertsWeight / (expertsWeight + 1m);

		if (dtAlt < 1m)
		{
		ClosePositionIfExists();
		_previousCandle = candle;
		return;
		}

		var lots = Math.Floor((double)dtAlt) / 10m;
		AdjustShort(lots);
		}
		else
		{
		if (dt < 1m)
		{
		ClosePositionIfExists();
		_previousCandle = candle;
		return;
		}

		var lots = Math.Floor((double)dt) / 10m;
		AdjustLong(lots);
		}

		_previousCandle = candle;
	}

	private decimal CalculateSyntheticCapital(decimal currentPrice)
	{
		var multiplier = ContractMultiplier;
		var unrealized = _positionVolume * currentPrice * multiplier;
		return _initialCapital + _realizedPnL + unrealized;
	}

	private decimal CalculateDenominator()
	{
		return BaseUnits + ContractMultiplier * _positionVolume;
	}

	private decimal GetSpread(decimal priceStep)
	{
		if (_bestBid is decimal bid && _bestAsk is decimal ask && ask > bid)
		return ask - bid;

		var steps = FallbackSpreadSteps;
		return (steps <= 0m ? 1m : steps) * priceStep;
	}

	private void AdjustShort(decimal targetLots)
	{
		if (targetLots <= 0m)
		return;

		if (Position > 0m)
		{
		var reduce = Math.Min(Position, targetLots);
		if (reduce > 0m)
		SellMarket(reduce);
		return;
		}

		var currentShort = Position < 0m ? Math.Abs(Position) : 0m;
		if (currentShort >= targetLots)
		return;

		var additional = targetLots - currentShort;
		var tradable = GetTradableVolume(additional);
		if (tradable > 0m)
		SellMarket(tradable);
	}

	private void AdjustLong(decimal targetLots)
	{
		if (targetLots <= 0m)
		return;

		if (Position < 0m)
		{
		var reduce = Math.Min(Math.Abs(Position), targetLots);
		if (reduce > 0m)
		BuyMarket(reduce);
		return;
		}

		var currentLong = Position > 0m ? Position : 0m;
		if (currentLong >= targetLots)
		return;

		var additional = targetLots - currentLong;
		var tradable = GetTradableVolume(additional);
		if (tradable > 0m)
		BuyMarket(tradable);
	}

	private void ClosePositionIfExists()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		if (Position > 0m)
		SellMarket(volume);
		else
		BuyMarket(volume);
	}

	private decimal GetTradableVolume(decimal desiredLots)
	{
		if (desiredLots <= 0m)
		return 0m;

		var marginPerLot = MarginPerLot;
		var availableCapital = Portfolio?.CurrentValue ?? (_initialCapital + _realizedPnL);

		if (marginPerLot <= 0m || availableCapital <= 0m)
		return Math.Floor((double)(desiredLots * 10m)) / 10m;

		var maxLots = Math.Floor((double)((availableCapital / marginPerLot) * 10m)) / 10m;
		if (maxLots <= 0m)
		return 0m;

		return Math.Min(desiredLots, maxLots);
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var order = trade.Order;
		if (order == null || order.Security != Security)
		return;

		var tradeInfo = trade.Trade;
		var volume = tradeInfo.Volume;
		if (volume <= 0m)
		return;

		var signedVolume = order.Direction == Sides.Buy ? volume : -volume;
		var price = tradeInfo.Price;

		if (_positionVolume == 0m || Math.Sign(_positionVolume) == Math.Sign(signedVolume))
		{
		var totalVolume = _positionVolume + signedVolume;
		if (totalVolume == 0m)
		{
		_positionVolume = 0m;
		_averagePrice = 0m;
		}
		else
		{
		var weightedPrice = _averagePrice * _positionVolume + price * signedVolume;
		_positionVolume = totalVolume;
		_averagePrice = weightedPrice / totalVolume;
		}
		return;
		}

		var closingVolume = Math.Min(Math.Abs(signedVolume), Math.Abs(_positionVolume));
		var realized = (price - _averagePrice) * closingVolume * Math.Sign(_positionVolume) * ContractMultiplier;
		_realizedPnL += realized;

		var remainingVolume = _positionVolume + signedVolume;

		if (remainingVolume == 0m)
		{
		_positionVolume = 0m;
		_averagePrice = 0m;
		return;
		}

		if (Math.Sign(_positionVolume) == Math.Sign(remainingVolume))
		{
		_positionVolume = remainingVolume;
		return;
		}

		_positionVolume = remainingVolume;
		_averagePrice = price;
	}
}

