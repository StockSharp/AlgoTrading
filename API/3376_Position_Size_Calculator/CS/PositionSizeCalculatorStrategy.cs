using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Replicates the MetaTrader "Position Size Calculator" utility in StockSharp.
/// The strategy does not submit orders; it only computes the recommended volume
/// together with risk and margin metrics based on the current portfolio and quotes.
/// </summary>
public class PositionSizeCalculatorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<bool> _useEquity;
	private readonly StrategyParam<bool> _useRiskMoney;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _riskMoney;
	private readonly StrategyParam<decimal> _commissionPerLot;
	private readonly StrategyParam<Sides> _tradeDirection;

	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal? _lastTradePrice;
	private decimal? _lastVolume;
	private decimal? _lastRiskMoney;
	private decimal? _lastRiskPercent;
	private decimal? _lastMargin;
	private decimal? _lastEntryPrice;
	private decimal? _lastStopPrice;

	/// <summary>
	/// Stop-loss distance expressed in price points (price steps).
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// When <c>true</c> the calculation uses the account equity (preferring <see cref="Portfolio.CurrentValue"/>).
	/// When <c>false</c> the initial balance is used instead.
	/// </summary>
	public bool UseEquity
	{
		get => _useEquity.Value;
		set => _useEquity.Value = value;
	}

	/// <summary>
	/// Choose between risk percent and absolute currency risk input.
	/// </summary>
	public bool UseRiskMoney
	{
		get => _useRiskMoney.Value;
		set => _useRiskMoney.Value = value;
	}

	/// <summary>
	/// Risk tolerance expressed as percent of the selected portfolio value.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Risk tolerance expressed as account currency amount.
	/// </summary>
	public decimal RiskMoney
	{
		get => _riskMoney.Value;
		set => _riskMoney.Value = value;
	}

	/// <summary>
	/// One-way commission charged per lot. The calculation adds both sides of commission (entry and exit).
	/// </summary>
	public decimal CommissionPerLot
	{
		get => _commissionPerLot.Value;
		set => _commissionPerLot.Value = value;
	}

	/// <summary>
	/// Preferred direction for the calculation (affects entry and stop prices).
	/// </summary>
	public Sides TradeDirection
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
	}

	/// <summary>
	/// Latest recommended trading volume.
	/// </summary>
	public decimal? RecommendedVolume => _lastVolume;

	/// <summary>
	/// Latest risk estimate expressed in account currency (commissions included).
	/// </summary>
	public decimal? CalculatedRiskMoney => _lastRiskMoney;

	/// <summary>
	/// Latest risk estimate expressed as percentage of the evaluated portfolio value.
	/// </summary>
	public decimal? CalculatedRiskPercent => _lastRiskPercent;

	/// <summary>
	/// Margin required to open the position with the recommended volume.
	/// </summary>
	public decimal? CalculatedMargin => _lastMargin;

	/// <summary>
	/// Entry price used in the latest calculation.
	/// </summary>
	public decimal? LastEntryPrice => _lastEntryPrice;

	/// <summary>
	/// Stop price used in the latest calculation.
	/// </summary>
	public decimal? LastStopPrice => _lastStopPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="PositionSizeCalculatorStrategy"/> class.
	/// </summary>
	public PositionSizeCalculatorStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (points)", "Stop-loss distance expressed in price points", "Risk");

		_useEquity = Param(nameof(UseEquity), true)
			.SetDisplay("Use Equity", "Toggle between equity and balance based capital", "Risk");

		_useRiskMoney = Param(nameof(UseRiskMoney), false)
			.SetDisplay("Use Risk Money", "Switch between percent risk and currency risk", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 1m)
			.SetRange(0m, 100m)
			.SetDisplay("Risk Percent", "Percent of capital exposed on the trade", "Risk");

		_riskMoney = Param(nameof(RiskMoney), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Money", "Absolute risk tolerance in account currency", "Risk");

		_commissionPerLot = Param(nameof(CommissionPerLot), 0m)
			.SetMinValue(0m)
			.SetDisplay("Commission per Lot", "One-way commission charged per traded lot", "Costs");

		_tradeDirection = Param(nameof(TradeDirection), Sides.Buy)
			.SetDisplay("Trade Direction", "Direction used when estimating entry and stop", "General");
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

		_bestBid = null;
		_bestAsk = null;
		_lastTradePrice = null;
		_lastVolume = null;
		_lastRiskMoney = null;
		_lastRiskPercent = null;
		_lastMargin = null;
		_lastEntryPrice = null;
		_lastStopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid) && bid is decimal bidPrice)
			_bestBid = bidPrice;

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask) && ask is decimal askPrice)
			_bestAsk = askPrice;

		if (message.Changes.TryGetValue(Level1Fields.LastTradePrice, out var last) && last is decimal lastPrice)
			_lastTradePrice = lastPrice;

		RecalculatePositionSize();
	}

	private void RecalculatePositionSize()
	{
		var security = Security;
		var portfolio = Portfolio;

		if (security == null || portfolio == null)
			return;

		var entryPrice = GetEntryPrice();
		if (entryPrice <= 0m)
			return;

		var stopDistance = CalculateStopDistance(security);
		if (stopDistance <= 0m)
			return;

		var stopPrice = TradeDirection == Sides.Buy
			? entryPrice - stopDistance
			: entryPrice + stopDistance;

		if (stopPrice <= 0m)
			return;

		var portfolioValue = GetPortfolioValue(portfolio);
		if (portfolioValue <= 0m)
			return;

		var riskInputs = CalculateRiskInputs(portfolioValue);
		if (riskInputs.RiskMoney <= 0m)
			return;

		var riskPerVolume = CalculateRiskPerVolume(security, stopDistance);
		if (riskPerVolume <= 0m)
			return;

		var volume = NormalizeVolume(riskInputs.RiskMoney / riskPerVolume, security);
		if (volume <= 0m)
			return;

		var commissionCost = volume * CommissionPerLot * 2m;
		var actualRiskMoney = volume * riskPerVolume + commissionCost;
		var actualRiskPercent = portfolioValue > 0m ? actualRiskMoney / portfolioValue * 100m : 0m;

		var marginRequirement = CalculateMarginRequirement(security, volume, entryPrice);

		if (_lastVolume == volume &&
			_lastRiskMoney == actualRiskMoney &&
			_lastRiskPercent == actualRiskPercent &&
			_lastMargin == marginRequirement &&
			_lastEntryPrice == entryPrice &&
			_lastStopPrice == stopPrice)
		{
			return;
		}

		_lastVolume = volume;
		_lastRiskMoney = actualRiskMoney;
		_lastRiskPercent = actualRiskPercent;
		_lastMargin = marginRequirement;
		_lastEntryPrice = entryPrice;
		_lastStopPrice = stopPrice;

		Volume = volume;

		AddInfoLog(
			"Position size updated. Dir={0}, Entry={1:0.#####}, Stop={2:0.#####}, Volume={3:0.####}, Risk={4:0.##}, Risk%={5:0.##}, Margin={6:0.##}",
			TradeDirection,
			entryPrice,
			stopPrice,
			volume,
			actualRiskMoney,
			actualRiskPercent,
			marginRequirement);
	}

	private decimal GetEntryPrice()
	{
		return TradeDirection switch
		{
			Sides.Buy => _bestAsk ?? _lastTradePrice ?? _bestBid ?? 0m,
			Sides.Sell => _bestBid ?? _lastTradePrice ?? _bestAsk ?? 0m,
			_ => 0m,
		};
	}

	private decimal CalculateStopDistance(Security security)
	{
		var priceStep = security.PriceStep ?? 0m;
		if (priceStep <= 0m)
			priceStep = 1m;

		return priceStep * StopLossPoints;
	}

	private (decimal RiskMoney, decimal RiskPercent) CalculateRiskInputs(decimal portfolioValue)
	{
		if (UseRiskMoney)
		{
			var percent = portfolioValue > 0m ? RiskMoney / portfolioValue * 100m : 0m;
			return (RiskMoney, percent);
		}

		var money = portfolioValue * RiskPercent / 100m;
		return (money, RiskPercent);
	}

	private static decimal CalculateRiskPerVolume(Security security, decimal stopDistance)
	{
		var priceStep = security.PriceStep ?? 0m;
		if (priceStep <= 0m)
			priceStep = 1m;

		var stepPrice = security.StepPrice ?? 0m;
		if (stepPrice <= 0m)
			stepPrice = priceStep;

		var steps = stopDistance / priceStep;
		if (steps <= 0m)
			return 0m;

		return steps * stepPrice;
	}

	private static decimal NormalizeVolume(decimal rawVolume, Security security)
	{
		if (rawVolume <= 0m)
			return 0m;

		var volumeStep = security.VolumeStep ?? 1m;
		if (volumeStep <= 0m)
			volumeStep = 1m;

		var minVolume = security.MinVolume ?? volumeStep;
		if (minVolume <= 0m)
			minVolume = volumeStep;

		var maxVolume = security.MaxVolume ?? decimal.MaxValue;

		var steps = decimal.Floor(rawVolume / volumeStep);
		if (steps <= 0m)
			return 0m;

		var normalized = steps * volumeStep;

		if (normalized < minVolume)
			return 0m;

		if (normalized > maxVolume)
			normalized = maxVolume;

		return normalized;
	}

	private static decimal CalculateMarginRequirement(Security security, decimal volume, decimal entryPrice)
	{
		if (volume <= 0m)
			return 0m;

		var margin = security.MarginBuy;
		if (margin is null or <= 0m)
			margin = security.MarginSell;

		if (margin is decimal direct && direct > 0m)
			return direct * volume;

		var volumeStep = security.VolumeStep ?? 1m;
		if (volumeStep <= 0m)
			volumeStep = 1m;

		if (entryPrice <= 0m)
			return 0m;

		var marginPerStep = entryPrice * volumeStep;
		if (marginPerStep <= 0m)
			return 0m;

		return marginPerStep * (volume / volumeStep);
	}

	private decimal GetPortfolioValue(Portfolio portfolio)
	{
		if (UseEquity)
		{
			var equity = portfolio.CurrentValue ?? portfolio.CurrentBalance ?? portfolio.BeginValue ?? 0m;
			return equity;
		}

		var balance = portfolio.BeginBalance;
		if (balance > 0m)
			return balance;

		balance = portfolio.CurrentBalance ?? portfolio.BeginValue ?? portfolio.CurrentValue ?? 0m;
		return balance;
	}
}
