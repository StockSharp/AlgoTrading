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
/// Strategy that alternates trade direction after stop-loss and keeps direction after take-profit.
/// Recreates the behavior of the original MQL expert advisor using level1 price data.
/// </summary>
public class RomanDirectionFlipStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _takeProfitSteps;
	private readonly StrategyParam<int> _stopLossSteps;
	private readonly StrategyParam<bool> _startWithBuy;

	private decimal? _lastBid;
	private decimal? _lastAsk;
	private decimal? _entryPrice;
	private bool _nextTradeBuy;
	private bool _orderPending;

	/// <summary>
	/// Volume sent with each market order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Take-profit target measured in price steps.
	/// </summary>
	public int TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Stop-loss threshold measured in price steps.
	/// </summary>
	public int StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Defines whether the very first order starts from the long side.
	/// </summary>
	public bool StartWithBuy
	{
		get => _startWithBuy.Value;
		set => _startWithBuy.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="RomanDirectionFlipStrategy"/>.
	/// </summary>
	public RomanDirectionFlipStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order volume", "Quantity for every market order", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 46)
			.SetGreaterThanZero()
			.SetDisplay("Take profit (steps)", "Number of price steps required to take profit", "Risk management")
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 5);

		_stopLossSteps = Param(nameof(StopLossSteps), 31)
			.SetGreaterThanZero()
			.SetDisplay("Stop loss (steps)", "Number of price steps allowed against the position", "Risk management")
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 5);

		_startWithBuy = Param(nameof(StartWithBuy), true)
			.SetDisplay("Start with buy", "If true the very first entry opens a long position", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastBid = null;
		_lastAsk = null;
		_entryPrice = null;
		_nextTradeBuy = true;
		_orderPending = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_nextTradeBuy = StartWithBuy;
		_orderPending = false;
		_entryPrice = null;

		// Subscribe to Level1 data to track best bid and ask prices.
		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		// Draw executed orders on the chart if visualization is enabled.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawOwnTrades(area);
		}
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
			_lastBid = Convert.ToDecimal(bidValue);

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
			_lastAsk = Convert.ToDecimal(askValue);

		// Skip processing until strategy is ready to trade.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep <= 0m)
			priceStep = 1m;

		if (Position == 0)
		{
			// Do not send another order while there is a pending request.
			if (_orderPending)
				return;

			if (_nextTradeBuy)
			{
				if (_lastAsk is not decimal ask)
					return;

				// Enter long position using current ask price as reference.
				BuyMarket(OrderVolume);
				_entryPrice = ask;
				_orderPending = true;
			}
			else
			{
				if (_lastBid is not decimal bid)
					return;

				// Enter short position using current bid price as reference.
				SellMarket(OrderVolume);
				_entryPrice = bid;
				_orderPending = true;
			}

			return;
		}

		if (_orderPending)
			return;

		if (Position > 0)
		{
			if (_entryPrice is not decimal entry || _lastBid is not decimal bid)
				return;

			// Calculate distance in price steps between bid price and entry price.
			var profitSteps = (bid - entry) / priceStep;

			if (profitSteps >= TakeProfitSteps)
			{
				// Keep long direction after a profitable exit.
				_nextTradeBuy = true;
				RequestExit();
			}
			else if (profitSteps <= -StopLossSteps)
			{
				// Switch to short direction after a stop-loss.
				_nextTradeBuy = false;
				RequestExit();
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice is not decimal entry || _lastAsk is not decimal ask)
				return;

			// Calculate distance in price steps between entry price and ask price.
			var profitSteps = (entry - ask) / priceStep;

			if (profitSteps >= TakeProfitSteps)
			{
				// Keep short direction after a profitable exit.
				_nextTradeBuy = false;
				RequestExit();
			}
			else if (profitSteps <= -StopLossSteps)
			{
				// Switch to long direction after a stop-loss.
				_nextTradeBuy = true;
				RequestExit();
			}
		}
	}

	private void RequestExit()
	{
		if (_orderPending || Position == 0)
			return;

		// ClosePosition submits a market order that closes the current position direction.
		ClosePosition();
		_orderPending = true;
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		// Reset pending flag once a position update is observed.
		if (Position == 0)
		{
			_entryPrice = null;
			_orderPending = false;
		}
		else
		{
			_orderPending = false;
		}
	}
}

