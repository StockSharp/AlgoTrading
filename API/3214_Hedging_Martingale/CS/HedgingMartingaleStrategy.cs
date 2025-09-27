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
/// Hedging martingale strategy translated from the MetaTrader expert "Hedging Martingale".
/// Opens both long and short positions on every new bar and adds martingale steps when price moves against a side.
/// </summary>
public class HedgingMartingaleStrategy : Strategy
{
	private sealed class PositionRecord
	{
		public decimal Volume { get; set; }
		public decimal EntryPrice { get; set; }
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _useTakeProfitInMoney;
	private readonly StrategyParam<decimal> _takeProfitInMoney;
	private readonly StrategyParam<bool> _useTakeProfitInPercent;
	private readonly StrategyParam<decimal> _takeProfitInPercent;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingTakeProfitMoney;
	private readonly StrategyParam<decimal> _trailingStopLossMoney;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _pipStepPips;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<bool> _closeMaxOrders;

	private readonly List<PositionRecord> _longPositions = new();
	private readonly List<PositionRecord> _shortPositions = new();

	private decimal _initialPortfolioValue;
	private decimal _maxFloatingProfit;
	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="HedgingMartingaleStrategy"/> class.
	/// </summary>
	public HedgingMartingaleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used to evaluate new bars", "General");

		_useTakeProfitInMoney = Param(nameof(UseTakeProfitInMoney), false)
		.SetDisplay("Use Money TP", "Close all trades when floating profit reaches the money target", "Risk");

		_takeProfitInMoney = Param(nameof(TakeProfitInMoney), 40m)
		.SetDisplay("Money Take Profit", "Floating profit target in account currency", "Risk")
		.SetCanOptimize(true);

		_useTakeProfitInPercent = Param(nameof(UseTakeProfitInPercent), false)
		.SetDisplay("Use Percent TP", "Close all trades when floating profit reaches the percent target", "Risk");

		_takeProfitInPercent = Param(nameof(TakeProfitInPercent), 10m)
		.SetDisplay("Percent Take Profit", "Floating profit target expressed as percent of starting equity", "Risk")
		.SetCanOptimize(true);

		_enableTrailing = Param(nameof(EnableTrailing), true)
		.SetDisplay("Enable Trailing", "Enable money based trailing stop for the trade basket", "Risk");

		_trailingTakeProfitMoney = Param(nameof(TrailingTakeProfitMoney), 40m)
		.SetDisplay("Trailing Start", "Floating profit that activates the trailing stop", "Risk")
		.SetCanOptimize(true);

		_trailingStopLossMoney = Param(nameof(TrailingStopLossMoney), 10m)
		.SetDisplay("Trailing Step", "Allowed profit retracement before closing the basket", "Risk")
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 30m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (pips)", "Take profit distance for each trade", "Trading")
		.SetCanOptimize(true);

		_pipStepPips = Param(nameof(PipStepPips), 30m)
		.SetGreaterThanZero()
		.SetDisplay("Pip Step", "Adverse movement required before adding a martingale order", "Trading")
		.SetCanOptimize(true);

		_baseVolume = Param(nameof(BaseVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Base Volume", "Initial volume for both hedge legs", "Money Management")
		.SetCanOptimize(true);

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Volume Multiplier", "Multiplier applied when adding martingale orders", "Money Management")
		.SetCanOptimize(true);

		_maxTrades = Param(nameof(MaxTrades), 4)
		.SetGreaterThanZero()
		.SetDisplay("Max Trades", "Maximum number of simultaneous open trades", "Risk")
		.SetCanOptimize(true);

		_closeMaxOrders = Param(nameof(CloseMaxOrders), true)
		.SetDisplay("Close On Max", "Close all positions when the maximum trade count is exceeded", "Risk");
	}

	/// <summary>
	/// Candle type that drives the strategy logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Enable money based take profit for the entire basket.
	/// </summary>
	public bool UseTakeProfitInMoney
	{
		get => _useTakeProfitInMoney.Value;
		set => _useTakeProfitInMoney.Value = value;
	}

	/// <summary>
	/// Floating profit target in currency units.
	/// </summary>
	public decimal TakeProfitInMoney
	{
		get => _takeProfitInMoney.Value;
		set => _takeProfitInMoney.Value = value;
	}

	/// <summary>
	/// Enable percent based take profit relative to starting equity.
	/// </summary>
	public bool UseTakeProfitInPercent
	{
		get => _useTakeProfitInPercent.Value;
		set => _useTakeProfitInPercent.Value = value;
	}

	/// <summary>
	/// Floating profit target expressed as percent of the starting portfolio value.
	/// </summary>
	public decimal TakeProfitInPercent
	{
		get => _takeProfitInPercent.Value;
		set => _takeProfitInPercent.Value = value;
	}

	/// <summary>
	/// Enable trailing of the basket profit using money targets.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Profit required before the trailing logic becomes active.
	/// </summary>
	public decimal TrailingTakeProfitMoney
	{
		get => _trailingTakeProfitMoney.Value;
		set => _trailingTakeProfitMoney.Value = value;
	}

	/// <summary>
	/// Allowed profit drawdown before the basket is closed.
	/// </summary>
	public decimal TrailingStopLossMoney
	{
		get => _trailingStopLossMoney.Value;
		set => _trailingStopLossMoney.Value = value;
	}

	/// <summary>
	/// Take profit distance for each entry expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Price movement in pips that triggers a new martingale order.
	/// </summary>
	public decimal PipStepPips
	{
		get => _pipStepPips.Value;
		set => _pipStepPips.Value = value;
	}

	/// <summary>
	/// Base volume for the initial hedge entries.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the largest position volume when averaging.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneous trades across both sides.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Close all trades once the maximum trade count is exceeded.
	/// </summary>
	public bool CloseMaxOrders
	{
		get => _closeMaxOrders.Value;
		set => _closeMaxOrders.Value = value;
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

		_longPositions.Clear();
		_shortPositions.Clear();
		_maxFloatingProfit = 0m;
		_initialPortfolioValue = 0m;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_longPositions.Clear();
		_shortPositions.Clear();
		_maxFloatingProfit = 0m;
		_initialPortfolioValue = Portfolio?.CurrentValue ?? 0m;
		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var closePrice = candle.ClosePrice;

		HandleBasketTargets(closePrice);

		if (IsFlat())
		{
			OpenHedgePair(closePrice);
			return;
		}

		if (GetTotalTradeCount() > MaxTrades && CloseMaxOrders)
		{
			CloseAllPositions(closePrice);
			return;
		}

		ApplyMartingaleLogic(closePrice);
	}

	private void HandleBasketTargets(decimal closePrice)
	{
		if (IsFlat())
		{
			_maxFloatingProfit = 0m;
			return;
		}

		var totalProfit = CalculateOpenProfit(closePrice);

		if (UseTakeProfitInMoney && TakeProfitInMoney > 0m && totalProfit >= TakeProfitInMoney)
		{
			CloseAllPositions(closePrice);
			return;
		}

		if (UseTakeProfitInPercent && TakeProfitInPercent > 0m && _initialPortfolioValue > 0m)
		{
			var target = _initialPortfolioValue * TakeProfitInPercent / 100m;
			if (totalProfit >= target)
			{
				CloseAllPositions(closePrice);
				return;
			}
		}

		if (!EnableTrailing || TrailingTakeProfitMoney <= 0m || TrailingStopLossMoney <= 0m)
		return;

		if (totalProfit >= TrailingTakeProfitMoney)
		{
			if (totalProfit > _maxFloatingProfit)
			_maxFloatingProfit = totalProfit;

			if (_maxFloatingProfit - totalProfit >= TrailingStopLossMoney)
			{
				CloseAllPositions(closePrice);
			}
		}
		else if (totalProfit < 0m)
		{
			_maxFloatingProfit = 0m;
		}
	}

	private void OpenHedgePair(decimal price)
	{
		var volume = AdjustVolume(BaseVolume);
		if (volume <= 0m)
		return;

		ExecuteOrder(Sides.Buy, volume, price);
		ExecuteOrder(Sides.Sell, volume, price);
	}

	private void ApplyMartingaleLogic(decimal price)
	{
		if (_pipSize <= 0m)
		return;

		var pipStep = PipStepPips * _pipSize;
		var takeProfitDistance = TakeProfitPips * _pipSize;

		if (pipStep <= 0m)
		return;

		TryAddMartingaleOrder(Sides.Buy, price, pipStep);
		TryAddMartingaleOrder(Sides.Sell, price, pipStep);

		ApplyIndividualTakeProfits(Sides.Buy, price, takeProfitDistance);
		ApplyIndividualTakeProfits(Sides.Sell, price, takeProfitDistance);
	}

	private void TryAddMartingaleOrder(Sides side, decimal price, decimal pipStep)
	{
		var (referencePrice, maxVolume, count) = GetDirectionStats(side);
		if (maxVolume <= 0m || count == 0)
		return;

		if (count >= MaxTrades)
		{
			if (CloseMaxOrders)
				CloseDirection(side, price);
			return;
		}

		var threshold = side == Sides.Buy ? referencePrice - pipStep : referencePrice + pipStep;
		var shouldAdd = side == Sides.Buy ? price <= threshold : price >= threshold;

		if (!shouldAdd)
		return;

		if (GetTotalTradeCount() >= MaxTrades)
		{
			if (CloseMaxOrders)
				CloseAllPositions(price);
			return;
		}

		var volume = AdjustVolume(maxVolume * VolumeMultiplier);
		if (volume <= 0m)
		return;

		ExecuteOrder(side, volume, price);
	}

	private void ApplyIndividualTakeProfits(Sides side, decimal price, decimal takeProfitDistance)
	{
		if (takeProfitDistance <= 0m)
		return;

		var positions = side == Sides.Buy ? _longPositions : _shortPositions;
		var snapshot = positions.ToArray();

		foreach (var position in snapshot)
		{
			var distance = side == Sides.Buy ? price - position.EntryPrice : position.EntryPrice - price;
			if (distance >= takeProfitDistance)
			{
				CloseDirection(side == Sides.Buy ? Sides.Buy : Sides.Sell, price, position.Volume);
			}
		}
	}

	private void CloseDirection(Sides side, decimal price, decimal? volumeOverride = null)
	{
		decimal volume;
		if (volumeOverride.HasValue)
		{
			volume = volumeOverride.Value;
		}
		else
		{
			volume = side == Sides.Buy ? GetTotalVolume(_longPositions) : GetTotalVolume(_shortPositions);
		}

		if (volume <= 0m)
		return;

		if (side == Sides.Buy)
		ExecuteOrder(Sides.Sell, volume, price);
		else
		ExecuteOrder(Sides.Buy, volume, price);
	}

	private void CloseAllPositions(decimal price)
	{
		var longVolume = GetTotalVolume(_longPositions);
		if (longVolume > 0m)
		ExecuteOrder(Sides.Sell, longVolume, price);

		var shortVolume = GetTotalVolume(_shortPositions);
		if (shortVolume > 0m)
		ExecuteOrder(Sides.Buy, shortVolume, price);

		_maxFloatingProfit = 0m;
	}

	private void ExecuteOrder(Sides side, decimal volume, decimal price)
	{
		if (volume <= 0m)
		return;

		if (side == Sides.Buy)
		BuyMarket(volume);
		else
		SellMarket(volume);

		UpdatePositions(side, volume, price);
	}

	private void UpdatePositions(Sides side, decimal volume, decimal price)
	{
		if (volume <= 0m)
		return;

		if (side == Sides.Buy)
		{
			var remaining = volume;
			var index = 0;
			while (remaining > 0m && index < _shortPositions.Count)
			{
				var position = _shortPositions[index];
				var qty = Math.Min(position.Volume, remaining);
				position.Volume -= qty;
				remaining -= qty;
				if (position.Volume <= 0m)
				{
					_shortPositions.RemoveAt(index);
					continue;
				}

				index++;
			}

			if (remaining > 0m)
			{
				_longPositions.Add(new PositionRecord
				{
					Volume = remaining,
					EntryPrice = price
				});
			}
		}
		else
		{
			var remaining = volume;
			var index = 0;
			while (remaining > 0m && index < _longPositions.Count)
			{
				var position = _longPositions[index];
				var qty = Math.Min(position.Volume, remaining);
				position.Volume -= qty;
				remaining -= qty;
				if (position.Volume <= 0m)
				{
					_longPositions.RemoveAt(index);
					continue;
				}

				index++;
			}

			if (remaining > 0m)
			{
				_shortPositions.Add(new PositionRecord
				{
					Volume = remaining,
					EntryPrice = price
				});
			}
		}
	}

	private (decimal referencePrice, decimal maxVolume, int count) GetDirectionStats(Sides side)
	{
		var positions = side == Sides.Buy ? _longPositions : _shortPositions;
		if (positions.Count == 0)
		return (0m, 0m, 0);

		decimal referencePrice = side == Sides.Buy ? decimal.MaxValue : decimal.MinValue;
		decimal maxVolume = 0m;

		foreach (var position in positions)
		{
			if (side == Sides.Buy)
				referencePrice = Math.Min(referencePrice, position.EntryPrice);
			else
				referencePrice = Math.Max(referencePrice, position.EntryPrice);

			if (position.Volume > maxVolume)
				maxVolume = position.Volume;
		}

		return (referencePrice, maxVolume, positions.Count);
	}

	private decimal CalculateOpenProfit(decimal currentPrice)
	{
		var profit = 0m;

		foreach (var position in _longPositions)
		{
			var diff = currentPrice - position.EntryPrice;
			profit += ConvertPriceToMoney(diff, position.Volume);
		}

		foreach (var position in _shortPositions)
		{
			var diff = position.EntryPrice - currentPrice;
			profit += ConvertPriceToMoney(diff, position.Volume);
		}

		return profit;
	}

	private decimal ConvertPriceToMoney(decimal priceDifference, decimal volume)
	{
		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;

		if (priceStep <= 0m || stepPrice <= 0m)
		return priceDifference * volume;

		var steps = priceDifference / priceStep;
		return steps * stepPrice * volume;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var minVolume = Security?.MinVolume ?? 0m;
		var maxVolume = Security?.MaxVolume ?? 0m;
		var step = Security?.VolumeStep ?? 0m;

		if (step > 0m)
		{
			var ratio = Math.Floor(volume / step);
			volume = ratio * step;
		}

		if (minVolume > 0m && volume < minVolume)
		return 0m;

		if (maxVolume > 0m && volume > maxVolume)
		volume = maxVolume;

		return volume;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return 0m;

		var step = priceStep;
		var digits = 0;
		while (step < 1m && digits < 10)
		{
			step *= 10m;
			digits++;
		}

		if (digits == 3 || digits == 5)
		return priceStep * 10m;

		return priceStep;
	}

	private static decimal GetTotalVolume(List<PositionRecord> positions)
	{
		var total = 0m;
		foreach (var position in positions)
			total += position.Volume;
		return total;
	}

	private bool IsFlat()
	{
		return _longPositions.Count == 0 && _shortPositions.Count == 0;
	}

	private int GetTotalTradeCount()
	{
		return _longPositions.Count + _shortPositions.Count;
	}
}

