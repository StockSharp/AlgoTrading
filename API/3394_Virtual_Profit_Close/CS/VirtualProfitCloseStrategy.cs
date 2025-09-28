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

/// <summary>
/// Strategy that virtually closes positions once profit targets are met and optionally applies a trailing stop.
/// </summary>
public class VirtualProfitCloseStrategy : Strategy
{
	public enum DemoDirections
	{
		Sell,
		Buy
	}

	private readonly StrategyParam<decimal> _profitPips;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingOffsetPips;
	private readonly StrategyParam<decimal> _trailingActivationPips;
	private readonly StrategyParam<bool> _enableDemoMode;
	private readonly StrategyParam<DemoDirections> _demoDirection;
	private readonly StrategyParam<decimal> _demoVolume;
	private readonly StrategyParam<decimal> _demoStopPips;

	private decimal _pipSize;
	private decimal _profitDistance;
	private decimal _trailingOffset;
	private decimal _trailingActivation;
	private decimal _demoStopDistance;

	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	private bool _demoOrderActive;
	private ISubscriptionHandler<ITickTradeMessage> _tradeSubscription;

	/// <summary>
	/// Initializes a new instance of the <see cref="VirtualProfitCloseStrategy"/> class.
	/// </summary>
	public VirtualProfitCloseStrategy()
	{
		_profitPips = Param(nameof(ProfitPips), 30m)
			.SetDisplay("Profit (pips)", "Virtual take-profit expressed in MetaTrader pips", "General")
			.SetCanOptimize(true)
			.SetOptimize(5m, 100m, 5m);

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use Trailing", "Enable trailing stop management", "Risk");

		_trailingOffsetPips = Param(nameof(TrailingOffsetPips), 5m)
			.SetDisplay("Trailing Offset", "Distance between price and trailing stop in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 15m, 1m);

		_trailingActivationPips = Param(nameof(TrailingActivationPips), 2m)
			.SetDisplay("Trailing Activation", "Minimum profit in pips before trailing starts", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_enableDemoMode = Param(nameof(EnableDemoMode), false)
			.SetDisplay("Demo Mode", "Automatically open showcase positions", "Testing");

		_demoDirection = Param(nameof(DemoOrderDirection), DemoDirections.Sell)
			.SetDisplay("Demo Direction", "Order side used in demo mode", "Testing");

		_demoVolume = Param(nameof(DemoOrderVolume), 1m)
			.SetDisplay("Demo Volume", "Order volume submitted in demo mode", "Testing")
			.SetGreaterThanZero();

		_demoStopPips = Param(nameof(DemoStopPips), 20m)
			.SetDisplay("Demo Stop", "Protective stop distance for demo mode (pips)", "Testing")
			.SetGreaterThanZero();
	}

	/// <summary>
	/// Virtual profit target expressed in MetaTrader pips.
	/// </summary>
	public decimal ProfitPips
	{
		get => _profitPips.Value;
		set => _profitPips.Value = value;
	}

	/// <summary>
	/// Enable trailing stop behaviour.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Distance between market price and trailing stop once it is activated (pips).
	/// </summary>
	public decimal TrailingOffsetPips
	{
		get => _trailingOffsetPips.Value;
		set => _trailingOffsetPips.Value = value;
	}

	/// <summary>
	/// Minimum profit in pips before the trailing stop is engaged.
	/// </summary>
	public decimal TrailingActivationPips
	{
		get => _trailingActivationPips.Value;
		set => _trailingActivationPips.Value = value;
	}

	/// <summary>
	/// Enable automatic orders for demonstration purposes.
	/// </summary>
	public bool EnableDemoMode
	{
		get => _enableDemoMode.Value;
		set => _enableDemoMode.Value = value;
	}

	/// <summary>
	/// Direction used for demo orders.
	/// </summary>
	public DemoDirections DemoOrderDirection
	{
		get => _demoDirection.Value;
		set => _demoDirection.Value = value;
	}

	/// <summary>
	/// Volume used for demo orders.
	/// </summary>
	public decimal DemoOrderVolume
	{
		get => _demoVolume.Value;
		set => _demoVolume.Value = value;
	}

	/// <summary>
	/// Stop distance applied to demo orders (pips).
	/// </summary>
	public decimal DemoStopPips
	{
		get => _demoStopPips.Value;
		set => _demoStopPips.Value = value;
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

		_pipSize = 0m;
		_profitDistance = 0m;
		_trailingOffset = 0m;
		_trailingActivation = 0m;
		_demoStopDistance = 0m;

		_longTrailingStop = null;
		_shortTrailingStop = null;

		_demoOrderActive = false;
		_tradeSubscription = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();
		_profitDistance = ProfitPips > 0m ? ProfitPips * _pipSize : 0m;
		_trailingOffset = TrailingOffsetPips > 0m ? TrailingOffsetPips * _pipSize : 0m;
		_trailingActivation = TrailingActivationPips > 0m ? TrailingActivationPips * _pipSize : 0m;
		_demoStopDistance = DemoStopPips > 0m ? DemoStopPips * _pipSize : 0m;

		_tradeSubscription = SubscribeTicks();
		_tradeSubscription.Bind(ProcessTrade).Start();

		TryOpenDemoOrder();
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			ResetTrailing();
			_demoOrderActive = false;
		}
		else
		{
			_demoOrderActive = true;
		}
	}

	private void ProcessTrade(ITickTradeMessage trade)
	{
		var lastPrice = trade.Price;

		var bid = Security.BestBid?.Price ?? lastPrice;
		var ask = Security.BestAsk?.Price ?? lastPrice;

		if (Position > 0m)
		{
			HandleLongPosition(bid);
		}
		else if (Position < 0m)
		{
			HandleShortPosition(ask);
		}
		else
		{
			TryOpenDemoOrder();
		}
	}

	private void HandleLongPosition(decimal bid)
	{
		var position = Position;
		if (position <= 0m)
		return;

		var entryPrice = Position.AveragePrice;
		if (entryPrice is null)
		return;

		var profit = bid - entryPrice.Value;
		if (_profitDistance > 0m && profit >= _profitDistance)
		{
		LogInfo($"Virtual profit target reached for long position at {bid:0.#####}.");
		SellMarket(position);
		ResetTrailing();
		return;
		}

		if (!UseTrailingStop || _trailingOffset <= 0m)
		return;

		if (bid - entryPrice.Value < _trailingActivation)
		return;

		var candidate = bid - _trailingOffset;
		if (_longTrailingStop is null || candidate > _longTrailingStop.Value)
		_longTrailingStop = candidate;

		if (_longTrailingStop is decimal stop && bid <= stop)
		{
		LogInfo($"Trailing stop triggered for long position at {bid:0.#####}.");
		SellMarket(position);
		ResetTrailing();
		}
	}

	private void HandleShortPosition(decimal ask)
	{
		var position = Position;
		if (position >= 0m)
		return;

		var entryPrice = Position.AveragePrice;
		if (entryPrice is null)
		return;

		var profit = entryPrice.Value - ask;
		if (_profitDistance > 0m && profit >= _profitDistance)
		{
		LogInfo($"Virtual profit target reached for short position at {ask:0.#####}.");
		BuyMarket(Math.Abs(position));
		ResetTrailing();
		return;
		}

		if (!UseTrailingStop || _trailingOffset <= 0m)
		return;

		if (entryPrice.Value - ask < _trailingActivation)
		return;

		var candidate = ask + _trailingOffset;
		if (_shortTrailingStop is null || candidate < _shortTrailingStop.Value)
		_shortTrailingStop = candidate;

		if (_shortTrailingStop is decimal stop && ask >= stop)
		{
		LogInfo($"Trailing stop triggered for short position at {ask:0.#####}.");
		BuyMarket(Math.Abs(position));
		ResetTrailing();
		}
	}

	private void TryOpenDemoOrder()
	{
		if (!EnableDemoMode || _demoOrderActive)
		return;

		var volume = DemoOrderVolume;
		if (volume <= 0m)
		return;

		switch (DemoOrderDirection)
		{
			case DemoDirections.Buy:
			{
			BuyMarket(volume);
			ApplyDemoStop(true);
			break;
			}
			case DemoDirections.Sell:
			{
			SellMarket(volume);
			ApplyDemoStop(false);
			break;
			}
		}

		_demoOrderActive = true;
	}

	private void ApplyDemoStop(bool isLong)
	{
		if (_demoStopDistance <= 0m)
		return;

		var last = Security.LastPrice;
		if (last is null)
		return;

		var stop = isLong ? last.Value - _demoStopDistance : last.Value + _demoStopDistance;
		if (stop <= 0m)
		return;

		SetStopLoss(stop);
	}

	private void ResetTrailing()
	{
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	private decimal CalculatePipSize()
	{
		var security = Security ?? throw new InvalidOperationException("Security is not configured.");
		var priceStep = security.PriceStep ?? 1m;
		var decimals = security.Decimals;

		if (decimals == 3 || decimals == 5)
		return priceStep * 10m;

		return priceStep;
	}
}

