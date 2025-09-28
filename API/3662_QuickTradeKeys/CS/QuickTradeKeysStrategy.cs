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
/// Manual trade helper that replicates the QuickTradeKeys123 MetaTrader script using StockSharp parameters.
/// </summary>
public class QuickTradeKeysStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _buyRequest;
	private readonly StrategyParam<bool> _sellRequest;
	private readonly StrategyParam<bool> _closeAllRequest;

	/// <summary>
	/// Initializes a new instance of the <see cref="QuickTradeKeysStrategy"/> class.
	/// </summary>
	public QuickTradeKeysStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume used for manual market orders.", "Manual Trading");

		_buyRequest = Param(nameof(BuyRequest), false)
			.SetDisplay("Buy Request", "Set to true to send a buy market order.", "Manual Trading")
			.SetCanOptimize(false);

		_sellRequest = Param(nameof(SellRequest), false)
			.SetDisplay("Sell Request", "Set to true to send a sell market order.", "Manual Trading")
			.SetCanOptimize(false);

		_closeAllRequest = Param(nameof(CloseAllRequest), false)
			.SetDisplay("Close All Request", "Set to true to close the current net position.", "Manual Trading")
			.SetCanOptimize(false);
	}

	/// <summary>
	/// Volume used for new manual market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Triggers a manual buy market order when set to true.
	/// </summary>
	public bool BuyRequest
	{
		get => _buyRequest.Value;
		set => _buyRequest.Value = value;
	}

	/// <summary>
	/// Triggers a manual sell market order when set to true.
	/// </summary>
	public bool SellRequest
	{
		get => _sellRequest.Value;
		set => _sellRequest.Value = value;
	}

	/// <summary>
	/// Triggers closing of the net position when set to true.
	/// </summary>
	public bool CloseAllRequest
	{
		get => _closeAllRequest.Value;
		set => _closeAllRequest.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		BuyRequest = false;
		SellRequest = false;
		CloseAllRequest = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		if (Portfolio == null)
			throw new InvalidOperationException("Portfolio is not specified.");

		Timer.Start(TimeSpan.FromMilliseconds(200), ProcessManualCommands);
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		Timer.Stop();

		base.OnStopped();
	}

	private void ProcessManualCommands()
	{
		if (!BuyRequest && !SellRequest && !CloseAllRequest)
			return;

		if (!IsOnline)
			return;

		if (Security == null || Portfolio == null)
			return;

		var buyRequested = BuyRequest;
		var sellRequested = SellRequest;
		var closeRequested = CloseAllRequest;

		if (buyRequested)
			BuyMarket(OrderVolume);

		if (sellRequested)
			SellMarket(OrderVolume);

		if (closeRequested)
			CloseNetPosition();

		if (buyRequested)
			BuyRequest = false;

		if (sellRequested)
			SellRequest = false;

		if (closeRequested)
			CloseAllRequest = false;
	}

	private void CloseNetPosition()
	{
		var netVolume = Position;

		if (netVolume > 0m)
		{
			SellMarket(netVolume);
		}
		else if (netVolume < 0m)
		{
			BuyMarket(-netVolume);
		}
	}
}

