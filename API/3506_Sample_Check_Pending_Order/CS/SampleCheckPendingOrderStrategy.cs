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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Replica of the SampleCheckPendingOrder MT5 expert that keeps a pair of stop orders around the market.
/// Validates volume, performs a margin pre-check, and restarts expired pending orders.
/// </summary>
public class SampleCheckPendingOrderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volumeTolerance;

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _expirationMinutes;
	private readonly StrategyParam<decimal> _accountLeverage;

	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal _priceStep;
	private decimal _instrumentMultiplier;
	private decimal _stopLossOffset;
	private decimal _takeProfitOffset;

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private DateTimeOffset? _buyOrderExpiry;
	private DateTimeOffset? _sellOrderExpiry;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public SampleCheckPendingOrderStrategy()
	{
		_volumeTolerance = Param(nameof(VolumeTolerance), 0.0000001m)
			.SetGreaterOrEqualThanZero()
			.SetDisplay("Volume tolerance", "Allowed difference when validating portfolio volume and margin.", "Risk");

		_orderVolume = Param(nameof(OrderVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Order volume", "Lot size submitted with each pending stop order", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 300)
		.SetGreaterThanZero()
		.SetDisplay("Stop loss (points)", "Distance in points used for the protective stop", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 900)
		.SetGreaterThanZero()
		.SetDisplay("Take profit (points)", "Distance in points used for the profit target", "Risk");

		_expirationMinutes = Param(nameof(ExpirationMinutes), 1440)
		.SetGreaterThanZero()
		.SetDisplay("Expiration (minutes)", "Lifetime of pending orders before cancellation", "Orders");

		_accountLeverage = Param(nameof(AccountLeverage), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Account leverage", "Estimated leverage used to approximate margin requirements", "Risk");
	}

	/// <summary>
	/// Allowed difference when comparing volumes and margin requirements.
	/// </summary>
	public decimal VolumeTolerance
	{
		get => _volumeTolerance.Value;
		set => _volumeTolerance.Value = value;
	}

	/// <summary>
	/// Order volume used for both stop orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance in points converted into absolute price units.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in points converted into absolute price units.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Pending order lifetime in minutes.
	/// </summary>
	public int ExpirationMinutes
	{
		get => _expirationMinutes.Value;
		set => _expirationMinutes.Value = value;
	}

	/// <summary>
	/// Estimated account leverage used when checking margin availability.
	/// </summary>
	public decimal AccountLeverage
	{
		get => _accountLeverage.Value;
		set => _accountLeverage.Value = value;
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
		_priceStep = 0m;
		_instrumentMultiplier = 1m;
		_stopLossOffset = 0m;
		_takeProfitOffset = 0m;
		_buyStopOrder = null;
		_sellStopOrder = null;
		_buyOrderExpiry = null;
		_sellOrderExpiry = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
		{
			_priceStep = 1m;
			LogWarning("Price step is not available. Falling back to 1.");
		}

		_instrumentMultiplier = Security?.Multiplier ?? 0m;
		if (_instrumentMultiplier <= 0m)
		{
			_instrumentMultiplier = 1m;
			LogWarning("Multiplier is not available. Falling back to 1.");
		}

		_stopLossOffset = StopLossPoints * _priceStep;
		_takeProfitOffset = TakeProfitPoints * _priceStep;

		StartProtection(
		stopLoss: _stopLossOffset > 0m ? new Unit(_stopLossOffset, UnitTypes.Absolute) : null,
		takeProfit: _takeProfitOffset > 0m ? new Unit(_takeProfitOffset, UnitTypes.Absolute) : null,
		useMarketOrders: true);

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue) && bidValue is decimal bidPrice)
		_bestBid = bidPrice;

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue) && askValue is decimal askPrice)
		_bestAsk = askPrice;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_bestBid is null || _bestAsk is null)
		return;

		CleanupOrder(ref _buyStopOrder);
		CleanupOrder(ref _sellStopOrder);

		if (!IsOrderActive(_buyStopOrder))
		_buyOrderExpiry = null;

		if (!IsOrderActive(_sellStopOrder))
		_sellOrderExpiry = null;

		var now = message.ServerTime != default ? message.ServerTime : CurrentTime;

		if (IsOrderActive(_buyStopOrder) && _buyOrderExpiry is DateTimeOffset buyExpiry && now >= buyExpiry)
		{
			CancelOrder(_buyStopOrder!);
			_buyOrderExpiry = null;
		}

		if (IsOrderActive(_sellStopOrder) && _sellOrderExpiry is DateTimeOffset sellExpiry && now >= sellExpiry)
		{
			CancelOrder(_sellStopOrder!);
			_sellOrderExpiry = null;
		}

		var volume = OrderVolume;
		if (!TryValidateVolume(volume, out var normalizedVolume))
		return;

		if (normalizedVolume <= 0m)
		return;

		var hasSellMargin = HasSufficientMargin(_bestBid.Value, normalizedVolume);
		var hasBuyMargin = HasSufficientMargin(_bestAsk.Value, normalizedVolume);

		if (!hasSellMargin || !hasBuyMargin)
		return;

		if (!IsOrderActive(_buyStopOrder))
		{
			var price = RoundPrice(_bestAsk.Value);
			if (price > 0m)
			{
				_buyStopOrder = BuyStop(normalizedVolume, price);
				_buyOrderExpiry = ExpirationMinutes > 0 ? now + TimeSpan.FromMinutes(ExpirationMinutes) : null;
			}
		}

		if (!IsOrderActive(_sellStopOrder))
		{
			var price = RoundPrice(_bestBid.Value);
			if (price > 0m)
			{
				_sellStopOrder = SellStop(normalizedVolume, price);
				_sellOrderExpiry = ExpirationMinutes > 0 ? now + TimeSpan.FromMinutes(ExpirationMinutes) : null;
			}
		}
	}

	private bool TryValidateVolume(decimal volume, out decimal normalizedVolume)
	{
		normalizedVolume = volume;

		var security = Security;
		if (security is null)
		{
			LogWarning("Security information is unavailable. Volume validation failed.");
			return false;
		}

		var minVolume = security.MinVolume ?? 0m;
		var maxVolume = security.MaxVolume ?? decimal.MaxValue;
		var step = security.VolumeStep ?? 0m;

		var tolerance = VolumeTolerance;
		if (step > 0m)
		{
			var stepTolerance = step / 100000m;
			if (stepTolerance > tolerance)
			tolerance = stepTolerance;
		}

		if (minVolume > 0m && volume + tolerance < minVolume)
		{
			LogWarning($"Volume {volume:0.######} is below the minimum allowed {minVolume:0.######}.");
			return false;
		}

		if (maxVolume < decimal.MaxValue && volume - tolerance > maxVolume)
		{
			LogWarning($"Volume {volume:0.######} exceeds the maximum allowed {maxVolume:0.######}.");
			return false;
		}

		if (step > 0m)
		{
			var remainder = decimal.Remainder(volume, step);
			remainder = Math.Abs(remainder);
			if (remainder > tolerance && Math.Abs(remainder - step) > tolerance)
			{
				LogWarning($"Volume {volume:0.######} is not aligned with the step {step:0.######}.");
				return false;
			}
		}

		return true;
	}

	private bool HasSufficientMargin(decimal price, decimal volume)
	{
		if (price <= 0m || volume <= 0m)
		return false;

		var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue;
		if (portfolioValue is null || portfolioValue.Value <= 0m)
		return true;

		var leverage = AccountLeverage > 0m ? AccountLeverage : 1m;
		var requiredMargin = price * volume * _instrumentMultiplier / leverage;

		if (portfolioValue.Value + VolumeTolerance < requiredMargin)
		{
			LogWarning($"Insufficient margin. Required: {requiredMargin:0.##}, available: {portfolioValue.Value:0.##}.");
			return false;
		}

		return true;
	}

	private decimal RoundPrice(decimal price)
	{
		if (_priceStep <= 0m)
		return price;

		return Math.Round(price / _priceStep, MidpointRounding.AwayFromZero) * _priceStep;
	}

	private static void CleanupOrder(ref Order order)
	{
		if (order is null)
		return;

		switch (order.State)
		{
			case OrderStates.Done:
			case OrderStates.Failed:
			case OrderStates.Canceled:
			order = null;
			break;
		}
	}

	private static bool IsOrderActive(Order order)
	{
		if (order is null)
		return false;

		return order.State switch
		{
			OrderStates.Active => true,
			OrderStates.Pending => true,
			OrderStates.None => true,
			_ => false,
		};
	}
}

