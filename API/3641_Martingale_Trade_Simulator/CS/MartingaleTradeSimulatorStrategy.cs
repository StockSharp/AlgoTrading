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
/// Manual martingale simulator that reproduces the "Martingale Trade Simulator" expert advisor.
/// Provides buy/sell buttons, optional martingale averaging and trailing stop automation.
/// </summary>
public class MartingaleTradeSimulatorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<bool> _enableMartingale;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<decimal> _martingaleStepPoints;
	private readonly StrategyParam<decimal> _martingaleTakeProfitOffset;
	private readonly StrategyParam<bool> _buyRequest;
	private readonly StrategyParam<bool> _sellRequest;
	private readonly StrategyParam<bool> _martingaleRequest;

	private decimal? _lastTradePrice;
	private decimal? _bestBidPrice;
	private decimal? _bestAskPrice;

	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	private decimal? _lowestLongPrice;
	private decimal? _highestShortPrice;
	private decimal? _longTakeProfit;
	private decimal? _shortTakeProfit;

	private int _longEntriesCount;
	private int _shortEntriesCount;
	private decimal _previousPosition;
	private bool _longMartingaleActive;
	private bool _shortMartingaleActive;

	/// <summary>
	/// Volume used for manual market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enables the trailing stop automation.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Distance from price to the trailing stop in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Minimal step required to move the trailing stop in points.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Enables martingale averaging logic.
	/// </summary>
	public bool EnableMartingale
	{
		get => _enableMartingale.Value;
		set => _enableMartingale.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the volume of each martingale order.
	/// </summary>
	public decimal MartingaleMultiplier
	{
		get => _martingaleMultiplier.Value;
		set => _martingaleMultiplier.Value = value;
	}

	/// <summary>
	/// Price step in points before a new martingale order can be placed.
	/// </summary>
	public decimal MartingaleStepPoints
	{
		get => _martingaleStepPoints.Value;
		set => _martingaleStepPoints.Value = value;
	}

	/// <summary>
	/// Offset in points added to the averaged take-profit price.
	/// </summary>
	public decimal MartingaleTakeProfitOffset
	{
		get => _martingaleTakeProfitOffset.Value;
		set => _martingaleTakeProfitOffset.Value = value;
	}

	/// <summary>
	/// Manual trigger for a market buy order.
	/// </summary>
	public bool BuyRequest
	{
		get => _buyRequest.Value;
		set => _buyRequest.Value = value;
	}

	/// <summary>
	/// Manual trigger for a market sell order.
	/// </summary>
	public bool SellRequest
	{
		get => _sellRequest.Value;
		set => _sellRequest.Value = value;
	}

	/// <summary>
	/// Manual trigger for martingale averaging.
	/// </summary>
	public bool MartingaleRequest
	{
		get => _martingaleRequest.Value;
		set => _martingaleRequest.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="MartingaleTradeSimulatorStrategy"/>.
	/// </summary>
	public MartingaleTradeSimulatorStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Base volume for manual market orders.", "Manual Controls");

		_stopLossPoints = Param(nameof(StopLossPoints), 500m)
		.SetGreaterThanOrEqualTo(0m)
		.SetDisplay("Stop Loss (points)", "Distance from entry to protective stop.", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 500m)
		.SetGreaterThanOrEqualTo(0m)
		.SetDisplay("Take Profit (points)", "Distance from entry to protective target.", "Risk");

		_enableTrailing = Param(nameof(EnableTrailing), true)
		.SetDisplay("Enable Trailing", "Turn the trailing stop automation on or off.", "Trailing")
		.SetCanOptimize(false);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 50m)
		.SetGreaterThanOrEqualTo(0m)
		.SetDisplay("Trailing Stop (points)", "Distance of the trailing stop from market price.", "Trailing");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 20m)
		.SetGreaterThanOrEqualTo(0m)
		.SetDisplay("Trailing Step (points)", "Minimal gain required to move the trailing stop.", "Trailing");

		_enableMartingale = Param(nameof(EnableMartingale), true)
		.SetDisplay("Enable Martingale", "Allow averaging orders using martingale sizing.", "Martingale")
		.SetCanOptimize(false);

		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 1.2m)
		.SetGreaterThan(0m)
		.SetDisplay("Martingale Multiplier", "Volume multiplier for each averaging order.", "Martingale");

		_martingaleStepPoints = Param(nameof(MartingaleStepPoints), 150m)
		.SetGreaterThanOrEqualTo(0m)
		.SetDisplay("Martingale Step (points)", "Minimal adverse move before adding a new order.", "Martingale");

		_martingaleTakeProfitOffset = Param(nameof(MartingaleTakeProfitOffset), 50m)
		.SetGreaterThanOrEqualTo(0m)
		.SetDisplay("Martingale TP Offset (points)", "Extra distance added to averaged take-profit.", "Martingale");

		_buyRequest = Param(nameof(BuyRequest), false)
		.SetDisplay("Buy", "Set to true to send a market buy order.", "Manual Controls")
		.SetCanOptimize(false);

		_sellRequest = Param(nameof(SellRequest), false)
		.SetDisplay("Sell", "Set to true to send a market sell order.", "Manual Controls")
		.SetCanOptimize(false);

		_martingaleRequest = Param(nameof(MartingaleRequest), false)
		.SetDisplay("Martingale", "Set to true to evaluate and place an averaging order.", "Manual Controls")
		.SetCanOptimize(false);
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

		_lastTradePrice = null;
		_bestBidPrice = null;
		_bestAskPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_lowestLongPrice = null;
		_highestShortPrice = null;
		_longTakeProfit = null;
		_shortTakeProfit = null;
		_longEntriesCount = 0;
		_shortEntriesCount = 0;
		_previousPosition = 0m;
		_longMartingaleActive = false;
		_shortMartingaleActive = false;
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
		ProcessMartingaleCommand();
		ManageRisk();
	}

	private void ProcessManualCommands()
	{
		if (!BuyRequest && !SellRequest)
		return;

		if (!IsOnline)
		return;

		if (Security == null || Portfolio == null)
		return;

		var buyRequested = BuyRequest;
		var sellRequested = SellRequest;

		if (buyRequested)
		BuyMarket(OrderVolume);

		if (sellRequested)
		SellMarket(OrderVolume);

		if (buyRequested)
		BuyRequest = false;

		if (sellRequested)
		SellRequest = false;
	}

	private void ProcessMartingaleCommand()
	{
		if (!MartingaleRequest)
		return;

		MartingaleRequest = false;

		if (!EnableMartingale)
		return;

		if (!IsOnline)
		return;

		if (Security == null || Portfolio == null)
		return;

		var step = GetPriceStep() * MartingaleStepPoints;
		if (step <= 0m)
		return;

		if (Position > 0)
		{
			var ask = GetAskPrice();
			if (ask == null)
			return;

			var referencePrice = _lowestLongPrice ?? PositionPrice;
			if (referencePrice == null)
			return;

			if (referencePrice.Value - ask.Value >= step)
			{
				var volume = CalculateNextVolume(true);
				if (volume > 0m)
				{
					BuyMarket(volume);
					_longMartingaleActive = true;
				}
			}
		}
		else if (Position < 0)
		{
			var bid = GetBidPrice();
			if (bid == null)
			return;

			var referencePrice = _highestShortPrice ?? PositionPrice;
			if (referencePrice == null)
			return;

			if (bid.Value - referencePrice.Value >= step)
			{
				var volume = CalculateNextVolume(false);
				if (volume > 0m)
				{
					SellMarket(volume);
					_shortMartingaleActive = true;
				}
			}
		}
	}

	private void ManageRisk()
	{
		if (Position == 0)
		{
			_longTrailingStop = null;
			_shortTrailingStop = null;
			return;
		}

		var marketPrice = GetMarketPrice();
		if (marketPrice == null)
		return;

		var step = GetPriceStep();
		var positionPrice = PositionPrice;
		if (positionPrice == null)
		return;

		if (Position > 0)
		{
			ApplyLongProtection(marketPrice.Value, positionPrice.Value, step);
		}
		else
		{
			ApplyShortProtection(marketPrice.Value, positionPrice.Value, step);
		}
	}

	private void ApplyLongProtection(decimal marketPrice, decimal positionPrice, decimal priceStep)
	{
		if (StopLossPoints > 0m)
		{
			var stopPrice = positionPrice - StopLossPoints * priceStep;
			if (marketPrice <= stopPrice)
			SellMarket(Math.Abs(Position));
		}

		var takePrice = _longMartingaleActive ? _longTakeProfit : (TakeProfitPoints > 0m ? positionPrice + TakeProfitPoints * priceStep : null);
		if (takePrice != null && marketPrice >= takePrice.Value)
		SellMarket(Math.Abs(Position));

		if (!EnableTrailing || TrailingStopPoints <= 0m)
		{
			_longTrailingStop = null;
			return;
		}

		var trailingDistance = TrailingStopPoints * priceStep;
		var trailingStep = TrailingStepPoints * priceStep;

		if (_longTrailingStop == null)
		{
			_longTrailingStop = marketPrice - trailingDistance;
		}
		else
		{
			var candidate = marketPrice - trailingDistance;
			if (candidate - _longTrailingStop.Value >= trailingStep)
			_longTrailingStop = candidate;
		}

		if (_longTrailingStop != null && marketPrice <= _longTrailingStop.Value)
		SellMarket(Math.Abs(Position));
	}

	private void ApplyShortProtection(decimal marketPrice, decimal positionPrice, decimal priceStep)
	{
		if (StopLossPoints > 0m)
		{
			var stopPrice = positionPrice + StopLossPoints * priceStep;
			if (marketPrice >= stopPrice)
			BuyMarket(Math.Abs(Position));
		}

		var takePrice = _shortMartingaleActive ? _shortTakeProfit : (TakeProfitPoints > 0m ? positionPrice - TakeProfitPoints * priceStep : null);
		if (takePrice != null && marketPrice <= takePrice.Value)
		BuyMarket(Math.Abs(Position));

		if (!EnableTrailing || TrailingStopPoints <= 0m)
		{
			_shortTrailingStop = null;
			return;
		}

		var trailingDistance = TrailingStopPoints * priceStep;
		var trailingStep = TrailingStepPoints * priceStep;

		if (_shortTrailingStop == null)
		{
			_shortTrailingStop = marketPrice + trailingDistance;
		}
		else
		{
			var candidate = marketPrice + trailingDistance;
			if (_shortTrailingStop.Value - candidate >= trailingStep)
			_shortTrailingStop = candidate;
		}

		if (_shortTrailingStop != null && marketPrice >= _shortTrailingStop.Value)
		BuyMarket(Math.Abs(Position));
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var price = trade.Trade?.Price;
		if (price is null)
		return;

		if (Position > 0)
		{
			_longMartingaleActive = _longMartingaleActive && Position > 0;
			_shortMartingaleActive = false;
			_shortTrailingStop = null;
			_shortTakeProfit = null;

			if (trade.Order.Direction == Sides.Buy)
			{
				_lowestLongPrice = _lowestLongPrice.HasValue ? Math.Min(_lowestLongPrice.Value, price.Value) : price.Value;
				UpdateLongTakeProfit();
			}
			else if (Position <= 0)
			{
				ResetLongState();
			}
		}
		else if (Position < 0)
		{
			_shortMartingaleActive = _shortMartingaleActive && Position < 0;
			_longMartingaleActive = false;
			_longTrailingStop = null;
			_longTakeProfit = null;

			if (trade.Order.Direction == Sides.Sell)
			{
				_highestShortPrice = _highestShortPrice.HasValue ? Math.Max(_highestShortPrice.Value, price.Value) : price.Value;
				UpdateShortTakeProfit();
			}
			else if (Position >= 0)
			{
				ResetShortState();
			}
		}
		else
		{
			ResetLongState();
			ResetShortState();
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0)
		{
			if (_previousPosition <= 0m)
			{
				_longEntriesCount = 1;
			}
			else if (delta > 0m)
			{
				_longEntriesCount++;
			}
			else if (delta < 0m)
			{
				_longEntriesCount = Math.Max(1, _longEntriesCount - 1);
			}

			_shortEntriesCount = 0;
		}
		else if (Position < 0)
		{
			if (_previousPosition >= 0m)
			{
				_shortEntriesCount = 1;
			}
			else if (delta < 0m)
			{
				_shortEntriesCount++;
			}
			else if (delta > 0m)
			{
				_shortEntriesCount = Math.Max(1, _shortEntriesCount - 1);
			}

			_longEntriesCount = 0;
		}
		else
		{
			_longEntriesCount = 0;
			_shortEntriesCount = 0;
		}

		if (Position == 0m)
		{
			ResetLongState();
			ResetShortState();
		}

		_previousPosition = Position;
	}

	private void UpdateLongTakeProfit()
	{
		if (!_longMartingaleActive)
		return;

		var positionPrice = PositionPrice;
		if (positionPrice == null)
		return;

		var offset = MartingaleTakeProfitOffset * GetPriceStep();
		_longTakeProfit = positionPrice.Value + offset;
	}

	private void UpdateShortTakeProfit()
	{
		if (!_shortMartingaleActive)
		return;

		var positionPrice = PositionPrice;
		if (positionPrice == null)
		return;

		var offset = MartingaleTakeProfitOffset * GetPriceStep();
		_shortTakeProfit = positionPrice.Value - offset;
	}

	private decimal? GetMarketPrice()
	{
		if (_lastTradePrice != null)
		return _lastTradePrice;

		if (_bestBidPrice != null && _bestAskPrice != null)
		return (_bestBidPrice.Value + _bestAskPrice.Value) / 2m;

		return _bestBidPrice ?? _bestAskPrice;
	}

	private decimal? GetBidPrice()
	{
		return _bestBidPrice ?? _lastTradePrice;
	}

	private decimal? GetAskPrice()
	{
		return _bestAskPrice ?? _lastTradePrice;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep;
		return step is null || step == 0m ? 1m : step.Value;
	}

	private decimal CalculateNextVolume(bool isLong)
	{
		var entries = isLong ? _longEntriesCount : _shortEntriesCount;
		var multiplier = MartingaleMultiplier;

		if (multiplier <= 0m)
		return 0m;

		var power = entries;
		var factor = (decimal)Math.Pow((double)multiplier, power);
		return OrderVolume * factor;
	}

	private void ResetLongState()
	{
		_longMartingaleActive = false;
		_longTrailingStop = null;
		_longTakeProfit = null;
		_lowestLongPrice = null;
	}

	private void ResetShortState()
	{
		_shortMartingaleActive = false;
		_shortTrailingStop = null;
		_shortTakeProfit = null;
		_highestShortPrice = null;
	}
}

