namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Closes existing positions according to user defined rules.
/// </summary>
public class JimsClosePositionsStrategy : Strategy
{
	private readonly StrategyParam<bool> _closeAll;
	private readonly StrategyParam<bool> _closeAllProfit;
	private readonly StrategyParam<bool> _closeAllLoss;

	private ISubscription? _tradeSubscription;
	private bool _isClosing;

	/// <summary>
	/// Close every open position as soon as possible.
	/// </summary>
	public bool CloseAll
	{
		get => _closeAll.Value;
		set => _closeAll.Value = value;
	}

	/// <summary>
	/// Close positions only when they are in profit.
	/// </summary>
	public bool CloseAllProfit
	{
		get => _closeAllProfit.Value;
		set => _closeAllProfit.Value = value;
	}

	/// <summary>
	/// Close positions only when they are in loss.
	/// </summary>
	public bool CloseAllLoss
	{
		get => _closeAllLoss.Value;
		set => _closeAllLoss.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public JimsClosePositionsStrategy()
	{
		_closeAll = Param(nameof(CloseAll), true)
			.SetDisplay("Close All", "Close all open positions immediately", "General");

		_closeAllProfit = Param(nameof(CloseAllProfit), false)
			.SetDisplay("Close Profits", "Close only profitable positions", "General");

		_closeAllLoss = Param(nameof(CloseAllLoss), false)
			.SetDisplay("Close Losses", "Close only losing positions", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Ticks)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_tradeSubscription = null;
		_isClosing = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (!ValidateMode())
		{
			Stop();
			return;
		}

		// Process the current market price if it is already known.
		if (Security.LastPrice is decimal lastPrice)
			ProcessPrice(lastPrice);

		// Subscribe to trades to react on every new tick.
		_tradeSubscription = SubscribeTrades();
		_tradeSubscription.Bind(ProcessTrade).Start();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		// Reset the closing flag once the position becomes flat.
		if (Position == 0)
			_isClosing = false;
	}

	private bool ValidateMode()
	{
		var enabled = 0;

		if (CloseAll)
			enabled++;
		if (CloseAllProfit)
			enabled++;
		if (CloseAllLoss)
			enabled++;

		if (enabled <= 1)
			return true;

		LogError("Only one closing mode can be enabled at a time.");
		return false;
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		if (trade.TradePrice is not decimal tradePrice)
			return;

		ProcessPrice(tradePrice);
	}

	private void ProcessPrice(decimal marketPrice)
	{
		// No open position to handle.
		if (Position == 0)
			return;

		// Avoid sending multiple exit orders at the same time.
		if (_isClosing)
			return;

		if (CloseAll)
		{
			CloseActivePosition("Closing all positions on demand.");
			return;
		}

		var profit = CalculateProfit(marketPrice);

		if (CloseAllProfit && profit > 0m)
		{
			CloseActivePosition("Closing profitable position.");
		}
		else if (CloseAllLoss && profit < 0m)
		{
			CloseActivePosition("Closing losing position.");
		}
	}

	private decimal CalculateProfit(decimal marketPrice)
	{
		var averagePrice = Position.AveragePrice;

		if (averagePrice is null)
			return 0m;

		var signedVolume = (decimal)Position;
		var priceDiff = marketPrice - averagePrice.Value;

		// Positive value means profit, negative value means loss.
		return priceDiff * signedVolume;
	}

	private void CloseActivePosition(string reason)
	{
		var volume = (decimal)Position;

		if (volume == 0m)
			return;

		_isClosing = true;

		LogInfo(reason);
		CancelActiveOrders();

		if (volume > 0m)
		{
			// Long position: sell to exit.
			SellMarket(volume);
		}
		else
		{
			// Short position: buy to exit.
			BuyMarket(-volume);
		}
	}
}
