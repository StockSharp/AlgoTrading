using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that mirrors the "Maximum Trade Volume" MetaTrader indicator.
/// Calculates the largest position size allowed by the current free funds for different order directions.
/// </summary>
public class MaximumTradeVolumeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _lastBuyVolume;
	private decimal? _lastSellVolume;
	private decimal? _lastPendingBuyVolume;
	private decimal? _lastPendingSellVolume;

	/// <summary>
	/// Candle source used to refresh the margin based volume calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Maximum market buy volume allowed by the available funds.
	/// </summary>
	public decimal MaxMarketBuyVolume { get; private set; }

	/// <summary>
	/// Maximum market sell volume allowed by the available funds.
	/// </summary>
	public decimal MaxMarketSellVolume { get; private set; }

	/// <summary>
	/// Maximum pending buy volume that can be reserved with the free margin.
	/// </summary>
	public decimal MaxPendingBuyVolume { get; private set; }

	/// <summary>
	/// Maximum pending sell volume that can be reserved with the free margin.
	/// </summary>
	public decimal MaxPendingSellVolume { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MaximumTradeVolumeStrategy"/> class.
	/// </summary>
	public MaximumTradeVolumeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used to trigger recalculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastBuyVolume = null;
		_lastSellVolume = null;
		_lastPendingBuyVolume = null;
		_lastPendingSellVolume = null;

		MaxMarketBuyVolume = 0m;
		MaxMarketSellVolume = 0m;
		MaxPendingBuyVolume = 0m;
		MaxPendingSellVolume = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var closePrice = candle.ClosePrice;

		var buyVolume = CalculateMaxVolume(Sides.Buy, closePrice);
		var sellVolume = CalculateMaxVolume(Sides.Sell, closePrice);
		var pendingBuyVolume = CalculateMaxPendingVolume(Sides.Buy, closePrice);
		var pendingSellVolume = CalculateMaxPendingVolume(Sides.Sell, closePrice);

		MaxMarketBuyVolume = buyVolume;
		MaxMarketSellVolume = sellVolume;
		MaxPendingBuyVolume = pendingBuyVolume;
		MaxPendingSellVolume = pendingSellVolume;

		LogIfChanged("Max lot for buy", buyVolume, ref _lastBuyVolume);
		LogIfChanged("Max lot for sell", sellVolume, ref _lastSellVolume);
		LogIfChanged("Max lot for pending buy", pendingBuyVolume, ref _lastPendingBuyVolume);
		LogIfChanged("Max lot for pending sell", pendingSellVolume, ref _lastPendingSellVolume);
	}

	private void LogIfChanged(string label, decimal value, ref decimal? cache)
	{
		if (cache is decimal previous && previous == value)
			return;

		cache = value;

		if (value > 0m)
		{
			LogInfo($"{label}: {value:0.####}");
		}
		else
		{
			LogWarn($"{label}: insufficient free funds");
		}
	}

	private decimal CalculateMaxPendingVolume(Sides side, decimal price)
	{
		// Pending orders in MetaTrader reserve margin the same way as market positions.
		// The StockSharp port follows the same rule and reuses the market calculation.
		return CalculateMaxVolume(side, price);
	}

	private decimal CalculateMaxVolume(Sides side, decimal price)
	{
		var security = Security;
		var portfolio = Portfolio;

		if (security == null || portfolio == null)
			return 0m;

		var freeFunds = GetFreeFunds(portfolio);
		if (freeFunds <= 0m)
			return 0m;

		var marginPerVolume = GetMarginPerVolume(security, side, price);
		if (marginPerVolume <= 0m)
			return 0m;

		var volumeStep = security.VolumeStep ?? 1m;
		if (volumeStep <= 0m)
			volumeStep = 1m;

		var minVolume = security.VolumeMin ?? volumeStep;
		var maxVolumeLimit = security.VolumeMax ?? decimal.MaxValue;

		var rawVolume = freeFunds / marginPerVolume;
		if (rawVolume <= 0m)
			return 0m;

		var steps = decimal.Floor(rawVolume / volumeStep);
		if (steps <= 0m)
			return 0m;

		var normalized = steps * volumeStep;

		if (normalized < minVolume)
			return 0m;

		if (normalized > maxVolumeLimit)
			normalized = maxVolumeLimit;

		return normalized;
	}

	private static decimal GetFreeFunds(Portfolio portfolio)
	{
		if (portfolio.CurrentBalance is decimal balance && balance > 0m)
			return balance;

		if (portfolio.CurrentValue is decimal value && value > 0m)
			return value;

		if (portfolio.BeginValue is decimal begin && begin > 0m)
			return begin;

		return 0m;
	}

	private static decimal GetMarginPerVolume(Security security, Sides side, decimal price)
	{
		var margin = side == Sides.Buy ? security.MarginBuy : security.MarginSell;
		if (margin is decimal directMargin && directMargin > 0m)
			return directMargin;

		var priceStep = security.PriceStep ?? 0m;
		var stepPrice = security.StepPrice ?? 0m;
		var volumeStep = security.VolumeStep ?? 1m;

		if (priceStep > 0m && stepPrice > 0m)
		{
			var estimated = stepPrice / priceStep * volumeStep;
			if (estimated > 0m)
				return estimated;
		}

		if (price > 0m)
		{
			var multiplier = volumeStep > 0m ? volumeStep : 1m;
			var fallback = price * multiplier;
			if (fallback > 0m)
				return fallback;
		}

		return 0m;
	}
}
