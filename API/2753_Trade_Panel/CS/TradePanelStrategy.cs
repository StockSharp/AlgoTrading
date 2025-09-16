using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum TradePanelCloseMode
{
	CloseAll,
	CloseLast,
	CloseProfit,
	CloseLoss,
	ClosePartial
}

/// <summary>
/// Manual trade panel strategy that reproduces the TradePanel expert advisor logic.
/// </summary>
public class TradePanelStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _partialCloseVolume;
	private readonly StrategyParam<TradePanelCloseMode> _closeMode;
	private readonly StrategyParam<bool> _buyRequest;
	private readonly StrategyParam<bool> _sellRequest;
	private readonly StrategyParam<bool> _closeRequest;

	private decimal? _lastTradePrice;
	private decimal? _bestBidPrice;
	private decimal? _bestAskPrice;
	private bool _protectionOrderActive;

	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public decimal PartialCloseVolume
	{
		get => _partialCloseVolume.Value;
		set => _partialCloseVolume.Value = value;
	}

	public TradePanelCloseMode CloseMode
	{
		get => _closeMode.Value;
		set => _closeMode.Value = value;
	}

	public bool BuyRequest
	{
		get => _buyRequest.Value;
		set => _buyRequest.Value = value;
	}

	public bool SellRequest
	{
		get => _sellRequest.Value;
		set => _sellRequest.Value = value;
	}

	public bool CloseRequest
	{
		get => _closeRequest.Value;
		set => _closeRequest.Value = value;
	}

	public TradePanelStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume for new market orders.", "Manual Controls");

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetDisplay("Stop Loss Points", "Protective stop distance in points. Zero disables the stop loss.", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
			.SetDisplay("Take Profit Points", "Protective take-profit distance in points. Zero disables the take profit.", "Risk");

		_partialCloseVolume = Param(nameof(PartialCloseVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Partial Close Volume", "Volume to close when using partial or last close modes.", "Manual Controls");

		_closeMode = Param(nameof(CloseMode), TradePanelCloseMode.CloseAll)
			.SetDisplay("Close Mode", "Defines how manual close requests are processed.", "Manual Controls");

		_buyRequest = Param(nameof(BuyRequest), false)
			.SetDisplay("Buy Request", "Set to true to send a market buy order.", "Manual Controls")
			.SetCanOptimize(false);

		_sellRequest = Param(nameof(SellRequest), false)
			.SetDisplay("Sell Request", "Set to true to send a market sell order.", "Manual Controls")
			.SetCanOptimize(false);

		_closeRequest = Param(nameof(CloseRequest), false)
			.SetDisplay("Close Request", "Set to true to trigger the selected close mode.", "Manual Controls")
			.SetCanOptimize(false);
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastTradePrice = null;
		_bestBidPrice = null;
		_bestAskPrice = null;
		_protectionOrderActive = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		if (Portfolio == null)
			throw new InvalidOperationException("Portfolio is not specified.");

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.LastTradePrice, out var last))
			_lastTradePrice = (decimal)last;

		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_bestBidPrice = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_bestAskPrice = (decimal)ask;

		ProcessManualCommands();
		ManageProtection();
	}

	private void ProcessManualCommands()
	{
		if (!BuyRequest && !SellRequest && !CloseRequest)
			return;

		if (!IsOnline)
			return;

		if (Security == null || Portfolio == null)
			return;

		var buyRequested = BuyRequest;
		var sellRequested = SellRequest;
		var closeRequested = CloseRequest;

		if (buyRequested)
			BuyMarket(OrderVolume);

		if (sellRequested)
			SellMarket(OrderVolume);

		if (closeRequested)
			ExecuteCloseRequest();

		if (buyRequested)
			BuyRequest = false;

		if (sellRequested)
			SellRequest = false;

		if (closeRequested)
			CloseRequest = false;
	}

	private void ExecuteCloseRequest()
	{
		if (Position == 0)
			return;

		switch (CloseMode)
		{
			case TradePanelCloseMode.CloseAll:
				CloseEntirePosition();
				break;
			case TradePanelCloseMode.CloseLast:
				CloseVolume(OrderVolume);
				break;
			case TradePanelCloseMode.CloseProfit:
				if (IsPositionProfitable())
					CloseEntirePosition();
				break;
			case TradePanelCloseMode.CloseLoss:
				if (IsPositionLosing())
					CloseEntirePosition();
				break;
			case TradePanelCloseMode.ClosePartial:
				CloseVolume(PartialCloseVolume);
				break;
		}
	}

	private void CloseEntirePosition()
	{
		CloseVolume(Math.Abs(Position));
	}

	private void CloseVolume(decimal requestedVolume)
	{
		if (requestedVolume <= 0)
			return;

		if (Position > 0)
		{
			var volume = Math.Min(requestedVolume, Position);
			if (volume > 0)
				SellMarket(volume);
		}
		else if (Position < 0)
		{
			var volume = Math.Min(requestedVolume, Math.Abs(Position));
			if (volume > 0)
				BuyMarket(volume);
		}
	}

	private bool IsPositionProfitable()
	{
		var marketPrice = GetMarketPrice();
		if (marketPrice == null)
			return false;

		if (Position > 0)
			return marketPrice >= PositionPrice;

		if (Position < 0)
			return marketPrice <= PositionPrice;

		return false;
	}

	private bool IsPositionLosing()
	{
		var marketPrice = GetMarketPrice();
		if (marketPrice == null)
			return false;

		if (Position > 0)
			return marketPrice < PositionPrice;

		if (Position < 0)
			return marketPrice > PositionPrice;

		return false;
	}

	private void ManageProtection()
	{
		if (_protectionOrderActive)
			return;

		if (Position == 0)
			return;

		var marketPrice = GetMarketPrice();
		if (marketPrice == null)
			return;

		var step = Security.PriceStep ?? 1m;

		if (Position > 0)
		{
			if (StopLossPoints > 0m)
			{
				var stopPrice = PositionPrice - StopLossPoints * step;
				if (marketPrice <= stopPrice)
				{
					SellMarket(Math.Abs(Position));
					_protectionOrderActive = true;
					return;
				}
			}

			if (TakeProfitPoints > 0m)
			{
				var takePrice = PositionPrice + TakeProfitPoints * step;
				if (marketPrice >= takePrice)
				{
					SellMarket(Math.Abs(Position));
					_protectionOrderActive = true;
				}
			}
		}
		else
		{
			if (StopLossPoints > 0m)
			{
				var stopPrice = PositionPrice + StopLossPoints * step;
				if (marketPrice >= stopPrice)
				{
					BuyMarket(Math.Abs(Position));
					_protectionOrderActive = true;
					return;
				}
			}

			if (TakeProfitPoints > 0m)
			{
				var takePrice = PositionPrice - TakeProfitPoints * step;
				if (marketPrice <= takePrice)
				{
					BuyMarket(Math.Abs(Position));
					_protectionOrderActive = true;
				}
			}
		}
	}

	private decimal? GetMarketPrice()
	{
		if (_lastTradePrice != null)
			return _lastTradePrice;

		if (_bestBidPrice != null && _bestAskPrice != null)
			return (_bestBidPrice.Value + _bestAskPrice.Value) / 2m;

		return _bestBidPrice ?? _bestAskPrice;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		_protectionOrderActive = false;
	}
}
