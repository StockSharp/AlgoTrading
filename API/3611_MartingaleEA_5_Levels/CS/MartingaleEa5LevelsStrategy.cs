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

using StockSharp.Algo.Candles;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Martingale averaging strategy converted from the MetaTrader expert "MartingaleEA-5 Levels".
/// </summary>
public class MartingaleEa5LevelsStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableMartingale;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _maxAdditions;
	private readonly StrategyParam<decimal> _level1DistancePips;
	private readonly StrategyParam<decimal> _level2DistancePips;
	private readonly StrategyParam<decimal> _level3DistancePips;
	private readonly StrategyParam<decimal> _level4DistancePips;
	private readonly StrategyParam<decimal> _level5DistancePips;
	private readonly StrategyParam<decimal> _takeProfitCurrency;
	private readonly StrategyParam<decimal> _stopLossCurrency;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<PositionEntry> _longEntries = new();
	private readonly List<PositionEntry> _shortEntries = new();

	private int _longAdditions;
	private int _shortAdditions;
	private decimal _longBaseVolume;
	private decimal _shortBaseVolume;
	private decimal _longLastVolume;
	private decimal _shortLastVolume;
	private decimal _pointSize;

	/// <summary>
	/// Initializes a new instance of <see cref="MartingaleEa5LevelsStrategy"/>.
	/// </summary>
	public MartingaleEa5LevelsStrategy()
	{
		_enableMartingale = Param(nameof(EnableMartingale), true)
			.SetDisplay("Enable Martingale", "Toggle averaging logic", "General");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Multiplier", "Multiplier applied to each additional order", "Money Management")
			.SetCanOptimize(true);

		_maxAdditions = Param(nameof(MaxAdditions), 4)
			.SetDisplay("Max Additions", "Maximum number of martingale additions", "Money Management")
			.SetRange(0, 5)
			.SetCanOptimize(true);

		_level1DistancePips = Param(nameof(Level1DistancePips), 300m)
			.SetNotNegative()
			.SetDisplay("Level 1 Distance", "Adverse movement (pips) triggering the first addition", "Distances")
			.SetCanOptimize(true);

		_level2DistancePips = Param(nameof(Level2DistancePips), 400m)
			.SetNotNegative()
			.SetDisplay("Level 2 Distance", "Extra movement (pips) before the second addition", "Distances")
			.SetCanOptimize(true);

		_level3DistancePips = Param(nameof(Level3DistancePips), 500m)
			.SetNotNegative()
			.SetDisplay("Level 3 Distance", "Extra movement (pips) before the third addition", "Distances")
			.SetCanOptimize(true);

		_level4DistancePips = Param(nameof(Level4DistancePips), 600m)
			.SetNotNegative()
			.SetDisplay("Level 4 Distance", "Extra movement (pips) before the fourth addition", "Distances")
			.SetCanOptimize(true);

		_level5DistancePips = Param(nameof(Level5DistancePips), 700m)
			.SetNotNegative()
			.SetDisplay("Level 5 Distance", "Extra movement (pips) before the fifth addition", "Distances")
			.SetCanOptimize(true);

		_takeProfitCurrency = Param(nameof(TakeProfitCurrency), 200m)
			.SetDisplay("Take Profit", "Floating profit required to liquidate a martingale group", "Risk")
			.SetCanOptimize(true);

		_stopLossCurrency = Param(nameof(StopLossCurrency), -500m)
			.SetDisplay("Stop Loss", "Floating loss threshold closing a martingale group", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Data series driving the martingale checks", "General");
	}

	/// <summary>
	/// Gets or sets whether the martingale logic is active.
	/// </summary>
	public bool EnableMartingale
	{
		get => _enableMartingale.Value;
		set => _enableMartingale.Value = value;
	}

	/// <summary>
	/// Gets or sets the multiplier applied to each new averaging order.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Gets or sets the maximum number of averaging additions per direction.
	/// </summary>
	public int MaxAdditions
	{
		get => _maxAdditions.Value;
		set => _maxAdditions.Value = value;
	}

	/// <summary>
	/// Gets or sets the first adverse distance expressed in pips.
	/// </summary>
	public decimal Level1DistancePips
	{
		get => _level1DistancePips.Value;
		set => _level1DistancePips.Value = value;
	}

	/// <summary>
	/// Gets or sets the incremental distance before the second addition (pips).
	/// </summary>
	public decimal Level2DistancePips
	{
		get => _level2DistancePips.Value;
		set => _level2DistancePips.Value = value;
	}

	/// <summary>
	/// Gets or sets the incremental distance before the third addition (pips).
	/// </summary>
	public decimal Level3DistancePips
	{
		get => _level3DistancePips.Value;
		set => _level3DistancePips.Value = value;
	}

	/// <summary>
	/// Gets or sets the incremental distance before the fourth addition (pips).
	/// </summary>
	public decimal Level4DistancePips
	{
		get => _level4DistancePips.Value;
		set => _level4DistancePips.Value = value;
	}

	/// <summary>
	/// Gets or sets the incremental distance before the fifth addition (pips).
	/// </summary>
	public decimal Level5DistancePips
	{
		get => _level5DistancePips.Value;
		set => _level5DistancePips.Value = value;
	}

	/// <summary>
	/// Gets or sets the floating profit in currency required to close all long or short entries.
	/// </summary>
	public decimal TakeProfitCurrency
	{
		get => _takeProfitCurrency.Value;
		set => _takeProfitCurrency.Value = value;
	}

	/// <summary>
	/// Gets or sets the floating loss threshold that forces an emergency close.
	/// </summary>
	public decimal StopLossCurrency
	{
		get => _stopLossCurrency.Value;
		set => _stopLossCurrency.Value = value;
	}

	/// <summary>
	/// Gets or sets the candle type powering the evaluation loop.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointSize = GetPointSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateEntryState();

		if (!EnableMartingale)
			return;

		var price = candle.ClosePrice;

		HandleMartingaleAdditions(price);
		HandleMartingaleClosures(price);
	}

	private void HandleMartingaleAdditions(decimal price)
	{
		if (_longEntries.Count > 0)
		{
			var floating = CalculateLongProfit(price);
			if (floating < 0m)
				TryOpenLongAdditions(price);
		}

		if (_shortEntries.Count > 0)
		{
			var floating = CalculateShortProfit(price);
			if (floating < 0m)
				TryOpenShortAdditions(price);
		}
	}

	private void HandleMartingaleClosures(decimal price)
	{
		var longProfit = CalculateLongProfit(price);
		if (_longEntries.Count > 0 && (longProfit >= TakeProfitCurrency || longProfit <= StopLossCurrency))
		{
			var volume = AdjustVolume(GetTotalVolume(_longEntries));
			if (volume > 0m)
			{
				SellMarket(volume);
			}
			ResetLongState();
		}

		var shortProfit = CalculateShortProfit(price);
		if (_shortEntries.Count > 0 && (shortProfit >= TakeProfitCurrency || shortProfit <= StopLossCurrency))
		{
			var volume = AdjustVolume(GetTotalVolume(_shortEntries));
			if (volume > 0m)
			{
				BuyMarket(volume);
			}
			ResetShortState();
		}
	}

	private void TryOpenLongAdditions(decimal price)
	{
		var thresholds = GetCumulativeThresholds();
		var maxLevels = Math.Min(MaxAdditions, thresholds.Length);

		if (maxLevels <= 0)
			return;

		var referencePrice = GetHighestEntryPrice(_longEntries);
		var adverseMove = referencePrice - price;

		while (_longAdditions < maxLevels)
		{
			var required = thresholds[_longAdditions];
			if (adverseMove < required)
				break;

			var nextVolume = CalculateNextLongVolume();
			if (nextVolume <= 0m)
				break;

			if (BuyMarket(nextVolume) != null)
			{
				_longAdditions++;
				_longLastVolume = nextVolume;
			}
			else
			{
				break;
			}
		}
	}

	private void TryOpenShortAdditions(decimal price)
	{
		var thresholds = GetCumulativeThresholds();
		var maxLevels = Math.Min(MaxAdditions, thresholds.Length);

		if (maxLevels <= 0)
			return;

		var referencePrice = GetLowestEntryPrice(_shortEntries);
		var adverseMove = price - referencePrice;

		while (_shortAdditions < maxLevels)
		{
			var required = thresholds[_shortAdditions];
			if (adverseMove < required)
				break;

			var nextVolume = CalculateNextShortVolume();
			if (nextVolume <= 0m)
				break;

			if (SellMarket(nextVolume) != null)
			{
				_shortAdditions++;
				_shortLastVolume = nextVolume;
			}
			else
			{
				break;
			}
		}
	}

	private decimal CalculateNextLongVolume()
	{
		if (_longLastVolume <= 0m)
		{
			_longLastVolume = _longBaseVolume;
		}

		var next = _longLastVolume * VolumeMultiplier;
		return AdjustVolume(next);
	}

	private decimal CalculateNextShortVolume()
	{
		if (_shortLastVolume <= 0m)
		{
			_shortLastVolume = _shortBaseVolume;
		}

		var next = _shortLastVolume * VolumeMultiplier;
		return AdjustVolume(next);
	}

	private decimal CalculateLongProfit(decimal price)
	{
		var profit = 0m;
		foreach (var entry in _longEntries)
		{
			profit += (price - entry.Price) * entry.Volume;
		}
		return profit;
	}

	private decimal CalculateShortProfit(decimal price)
	{
		var profit = 0m;
		foreach (var entry in _shortEntries)
		{
			profit += (entry.Price - price) * entry.Volume;
		}
		return profit;
	}

	private void UpdateEntryState()
	{
		if (_longEntries.Count == 0)
		{
			ResetLongState();
		}
		else if (_longAdditions == 0 && _longBaseVolume <= 0m)
		{
			_longBaseVolume = _longEntries[0].Volume;
			_longLastVolume = _longBaseVolume;
		}

		if (_shortEntries.Count == 0)
		{
			ResetShortState();
		}
		else if (_shortAdditions == 0 && _shortBaseVolume <= 0m)
		{
			_shortBaseVolume = _shortEntries[0].Volume;
			_shortLastVolume = _shortBaseVolume;
		}
	}

	private void ResetLongState()
	{
		_longEntries.Clear();
		_longAdditions = 0;
		_longBaseVolume = 0m;
		_longLastVolume = 0m;
	}

	private void ResetShortState()
	{
		_shortEntries.Clear();
		_shortAdditions = 0;
		_shortBaseVolume = 0m;
		_shortLastVolume = 0m;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade.Order;
		var info = trade.Trade;

		if (order == null || info == null)
			return;

		var volume = info.Volume;
		var price = info.Price;

		if (volume <= 0m)
			return;

		if (order.Direction == Sides.Buy)
			ProcessBuyTrade(volume, price);
		else
			ProcessSellTrade(volume, price);
	}

	private void ProcessBuyTrade(decimal volume, decimal price)
	{
		var remaining = volume;

		for (var i = 0; i < _shortEntries.Count && remaining > 0m;)
		{
			var entry = _shortEntries[i];
			var portion = Math.Min(entry.Volume, remaining);
			entry.Volume -= portion;
			remaining -= portion;

			if (entry.Volume <= 0m)
			{
				_shortEntries.RemoveAt(i);
				continue;
			}

			_shortEntries[i] = entry;
			break;
		}

		if (remaining > 0m)
			_longEntries.Add(new PositionEntry(price, remaining));
	}

	private void ProcessSellTrade(decimal volume, decimal price)
	{
		var remaining = volume;

		for (var i = 0; i < _longEntries.Count && remaining > 0m;)
		{
			var entry = _longEntries[i];
			var portion = Math.Min(entry.Volume, remaining);
			entry.Volume -= portion;
			remaining -= portion;

			if (entry.Volume <= 0m)
			{
				_longEntries.RemoveAt(i);
				continue;
			}

			_longEntries[i] = entry;
			break;
		}

		if (remaining > 0m)
			_shortEntries.Add(new PositionEntry(price, remaining));
	}

	private decimal[] GetCumulativeThresholds()
	{
		var distances = new[]
		{
			Level1DistancePips,
			Level2DistancePips,
			Level3DistancePips,
			Level4DistancePips,
			Level5DistancePips
		};

		var thresholds = new List<decimal>(distances.Length);
		var sum = 0m;
		var multiplier = _pointSize <= 0m ? 1m : _pointSize;

		foreach (var distance in distances)
		{
			if (distance > 0m)
			{
				sum += distance * multiplier;
			}

			thresholds.Add(sum);
		}

		return thresholds.ToArray();
	}

	private decimal GetPointSize()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		if (security.PriceStep is { } step && step > 0m)
			return step;

		if (security.MinPriceStep is { } minStep && minStep > 0m)
			return minStep;

		return 0.0001m;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;
		if (security == null)
			return volume;

		if (security.VolumeStep is { } step && step > 0m)
		{
			var steps = Math.Round(volume / step, MidpointRounding.AwayFromZero);
			if (steps < 1m)
				steps = 1m;

			volume = steps * step;
		}

		if (security.MinVolume is { } min && volume < min)
			volume = min;

		if (security.MaxVolume is { } max && volume > max)
			volume = max;

		return volume;
	}

	private static decimal GetTotalVolume(List<PositionEntry> entries)
	{
		var total = 0m;
		foreach (var entry in entries)
			total += entry.Volume;
		return total;
	}

	private static decimal GetHighestEntryPrice(List<PositionEntry> entries)
	{
		var price = 0m;
		foreach (var entry in entries)
		{
			if (entry.Price > price)
				price = entry.Price;
		}
		return price;
	}

	private static decimal GetLowestEntryPrice(List<PositionEntry> entries)
	{
		var price = 0m;
		var isInitialized = false;
		foreach (var entry in entries)
		{
			if (!isInitialized || entry.Price < price)
			{
				price = entry.Price;
				isInitialized = true;
			}
		}
		return price;
	}

	private sealed class PositionEntry
	{
		public PositionEntry(decimal price, decimal volume)
		{
			Price = price;
			Volume = volume;
		}

		public decimal Price { get; }
		public decimal Volume { get; set; }
	}
}

