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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Closes all open positions once unrealized profit exceeds a balance-based threshold
/// and cuts losses when the floating loss breaches the configured drawdown limit.
/// </summary>
public class MoneyManagerStrategy : Strategy
{
	private readonly StrategyParam<bool> _profitDealEnabled;
	private readonly StrategyParam<bool> _lossDealEnabled;
	private readonly StrategyParam<decimal> _profitPercent;
	private readonly StrategyParam<decimal> _lossPercent;
	private readonly StrategyParam<decimal> _lotCommission;
	private readonly StrategyParam<decimal> _lotSize;

	private decimal _bestBid;
	private decimal _bestAsk;

	/// <summary>
	/// Enables the profit-based exit rule.
	/// </summary>
	public bool ProfitDealEnabled
	{
		get => _profitDealEnabled.Value;
		set => _profitDealEnabled.Value = value;
	}

	/// <summary>
	/// Enables the loss-based exit rule.
	/// </summary>
	public bool LossDealEnabled
	{
		get => _lossDealEnabled.Value;
		set => _lossDealEnabled.Value = value;
	}

	/// <summary>
	/// Percentage of the current balance required to secure profits.
	/// </summary>
	public decimal ProfitPercent
	{
		get => _profitPercent.Value;
		set => _profitPercent.Value = value;
	}

	/// <summary>
	/// Percentage of the current balance tolerated as a loss.
	/// </summary>
	public decimal LossPercent
	{
		get => _lossPercent.Value;
		set => _lossPercent.Value = value;
	}

	/// <summary>
	/// Commission per lot included in the thresholds.
	/// </summary>
	public decimal LotCommission
	{
		get => _lotCommission.Value;
		set => _lotCommission.Value = value;
	}

	/// <summary>
	/// Reference lot size used to evaluate commission and spread costs.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// Initializes the strategy parameters.
	/// </summary>
	public MoneyManagerStrategy()
	{
		_profitDealEnabled = Param(nameof(ProfitDealEnabled), true)
			.SetDisplay("Enable Profit Deal", "Toggle profit-based auto-closing", "Risk Management");

		_lossDealEnabled = Param(nameof(LossDealEnabled), true)
			.SetDisplay("Enable Loss Deal", "Toggle loss-based auto-closing", "Risk Management");

		_profitPercent = Param(nameof(ProfitPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Profit %", "Balance percentage added to the profit threshold", "Risk Management")
			.SetCanOptimize(true);

		_lossPercent = Param(nameof(LossPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Loss %", "Balance percentage tolerated before closing", "Risk Management")
			.SetCanOptimize(true);

		_lotCommission = Param(nameof(LotCommission), 7m)
			.SetGreaterThanOrEqualsZero()
			.SetDisplay("Lot Commission", "Commission per lot added to thresholds", "Costs");

		_lotSize = Param(nameof(LotSize), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Size", "Reference lot size used in cost estimations", "Costs");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
			return [];

		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bestBid = 0m;
		_bestAsk = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
			throw new InvalidOperationException("Security must be assigned before starting the strategy.");

		if (Portfolio == null)
			throw new InvalidOperationException("Portfolio must be assigned before starting the strategy.");

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid) && bid is decimal bidPrice)
			_bestBid = bidPrice;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask) && ask is decimal askPrice)
			_bestAsk = askPrice;

		if (!ProfitDealEnabled && !LossDealEnabled)
			return;

		CheckPositionThresholds();
	}

	private void CheckPositionThresholds()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = Position;
		if (volume == 0m)
			return;

		var entryPrice = PositionPrice;
		if (entryPrice is not decimal avgPrice || avgPrice <= 0m)
			return;

		var closePrice = GetClosePrice();
		if (closePrice <= 0m)
			return;

		var balance = Portfolio?.CurrentValue ?? 0m;
		if (balance <= 0m)
			return;

		var spread = Math.Abs(_bestAsk - _bestBid);
		var spreadCost = spread > 0m ? spread * LotSize : 0m;
		var commissionCost = LotCommission * LotSize;

		var profit = (closePrice - avgPrice) * volume;

		if (ProfitDealEnabled)
		{
			var target = (ProfitPercent / 100m) * balance + commissionCost + spreadCost;
			if (profit >= target)
			{
				ClosePosition();
				return;
			}
		}

		if (!LossDealEnabled)
			return;

		var lossLimit = -((LossPercent / 100m) * balance + commissionCost + spreadCost);
		if (profit <= lossLimit)
			ClosePosition();
	}

	private decimal GetClosePrice()
	{
		var volume = Position;

		if (volume > 0m)
		{
			if (_bestBid > 0m)
				return _bestBid;
		}
		else if (volume < 0m)
		{
			if (_bestAsk > 0m)
				return _bestAsk;
		}

		return Security?.LastTrade?.Price ?? 0m;
	}
}

