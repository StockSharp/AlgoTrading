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
/// Grid strategy translated from the MetaTrader 5 expert advisor "TugbaGold".
/// Combines martingale position sizing with averaging based exits.
/// </summary>
public class TugbaGoldStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _startVolume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<CloseOrderModes> _closeMode;
	private readonly StrategyParam<decimal> _pointOrderStepPips;
	private readonly StrategyParam<decimal> _minimalProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<GridEntry> _buyEntries = new();
	private readonly List<GridEntry> _sellEntries = new();

	private decimal? _previousOpen;
	private decimal? _previousClose;

	/// <summary>
	/// Take profit distance in pips for a single position.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Initial order volume when starting a new grid.
	/// </summary>
	public decimal StartVolume
	{
		get => _startVolume.Value;
		set => _startVolume.Value = value;
	}

	/// <summary>
	/// Maximum allowed order volume. Set to zero to disable the cap.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Averaging exit mode.
	/// </summary>
	public CloseOrderModes CloseMode
	{
		get => _closeMode.Value;
		set => _closeMode.Value = value;
	}

	/// <summary>
	/// Grid spacing between averaging entries measured in pips.
	/// </summary>
	public decimal PointOrderStepPips
	{
		get => _pointOrderStepPips.Value;
		set => _pointOrderStepPips.Value = value;
	}

	/// <summary>
	/// Minimal profit in pips required before averaging positions are closed.
	/// </summary>
	public decimal MinimalProfitPips
	{
		get => _minimalProfitPips.Value;
		set => _minimalProfitPips.Value = value;
	}

	/// <summary>
	/// Candle type used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TugbaGoldStrategy"/> class.
	/// </summary>
	public TugbaGoldStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 300m)
			.SetDisplay("Take Profit (pips)", "Distance for single order take profit", "Risk")
			.SetGreaterThanOrEqualZero();

		_startVolume = Param(nameof(StartVolume), 0.01m)
			.SetDisplay("Start Volume", "Initial order volume for a new grid", "Trading")
			.SetGreaterThanZero();

		_maxVolume = Param(nameof(MaxVolume), 2.56m)
			.SetDisplay("Max Volume", "Upper cap for order volume (0 disables)", "Trading")
			.SetGreaterThanOrEqualZero();

		_closeMode = Param(nameof(CloseMode), CloseOrderModes.Average)
			.SetDisplay("Close Mode", "Averaging exit logic", "Trading");

		_pointOrderStepPips = Param(nameof(PointOrderStepPips), 390m)
			.SetDisplay("Grid Step (pips)", "Minimal distance between new entries", "Trading")
			.SetGreaterThanOrEqualZero();

		_minimalProfitPips = Param(nameof(MinimalProfitPips), 70m)
			.SetDisplay("Minimal Profit (pips)", "Profit requirement before closing the basket", "Trading")
			.SetGreaterThanOrEqualZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Source candles for signals", "General");
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

		_buyEntries.Clear();
		_sellEntries.Clear();
		_previousOpen = null;
		_previousClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var point = GetPointValue();
		if (point <= 0m)
			return;

		var bid = GetBidPrice(candle);
		var ask = GetAskPrice(candle);

		if (bid <= 0m || ask <= 0m)
			return;

		var takeProfitDistance = TakeProfitPips * point;
		var minimalProfit = MinimalProfitPips * point;
		var orderStep = PointOrderStepPips * point;

		var buyCount = _buyEntries.Count;
		var sellCount = _sellEntries.Count;

		var buyMinIndex = GetBuyExtremeIndex(true);
		var buyMaxIndex = GetBuyExtremeIndex(false);
		var sellMinIndex = GetSellExtremeIndex(true);
		var sellMaxIndex = GetSellExtremeIndex(false);

		var buyTarget = CalculateBuyTarget(minimalProfit, buyMinIndex, buyMaxIndex);
		var sellTarget = CalculateSellTarget(minimalProfit, sellMinIndex, sellMaxIndex);

		if (takeProfitDistance > 0m && buyCount == 1)
		{
			var entry = _buyEntries[0];
			if (bid >= entry.Price + takeProfitDistance)
			{
				CloseBuyEntry(0, entry.Volume);
				buyCount = _buyEntries.Count;
				buyMinIndex = GetBuyExtremeIndex(true);
				buyMaxIndex = GetBuyExtremeIndex(false);
				buyTarget = CalculateBuyTarget(minimalProfit, buyMinIndex, buyMaxIndex);
			}
		}

		if (takeProfitDistance > 0m && sellCount == 1)
		{
			var entry = _sellEntries[0];
			if (ask <= entry.Price - takeProfitDistance)
			{
				CloseSellEntry(0, entry.Volume);
				sellCount = _sellEntries.Count;
				sellMinIndex = GetSellExtremeIndex(true);
				sellMaxIndex = GetSellExtremeIndex(false);
				sellTarget = CalculateSellTarget(minimalProfit, sellMinIndex, sellMaxIndex);
			}
		}

		if (CloseMode == CloseOrderModes.Average)
		{
			if (buyTarget is decimal bt && buyCount >= 2 && bid >= bt)
			{
				if (buyMaxIndex >= 0)
				{
					var index = buyMaxIndex;
					var volume = _buyEntries[index].Volume;
					CloseBuyEntry(index, volume);
				}

				buyMinIndex = GetBuyExtremeIndex(true);
				if (buyMinIndex >= 0)
				{
					var index = buyMinIndex;
					var volume = _buyEntries[index].Volume;
					CloseBuyEntry(index, volume);
				}

				buyCount = _buyEntries.Count;
				buyMinIndex = GetBuyExtremeIndex(true);
				buyMaxIndex = GetBuyExtremeIndex(false);
				buyTarget = CalculateBuyTarget(minimalProfit, buyMinIndex, buyMaxIndex);
			}

			if (sellTarget is decimal st && sellCount >= 2 && ask <= st)
			{
				if (sellMinIndex >= 0)
				{
					var index = sellMinIndex;
					var volume = _sellEntries[index].Volume;
					CloseSellEntry(index, volume);
				}

				sellMaxIndex = GetSellExtremeIndex(false);
				if (sellMaxIndex >= 0)
				{
					var index = sellMaxIndex;
					var volume = _sellEntries[index].Volume;
					CloseSellEntry(index, volume);
				}

				sellCount = _sellEntries.Count;
				sellMinIndex = GetSellExtremeIndex(true);
				sellMaxIndex = GetSellExtremeIndex(false);
				sellTarget = CalculateSellTarget(minimalProfit, sellMinIndex, sellMaxIndex);
			}
		}
		else
		{
			if (buyTarget is decimal bt && buyCount >= 2 && bid >= bt)
			{
				if (buyMaxIndex >= 0)
				{
					var index = buyMaxIndex;
					var volume = Math.Min(StartVolume, _buyEntries[index].Volume);
					CloseBuyEntry(index, volume);
				}

				buyMinIndex = GetBuyExtremeIndex(true);
				if (buyMinIndex >= 0)
				{
					var index = buyMinIndex;
					var volume = _buyEntries[index].Volume;
					CloseBuyEntry(index, volume);
				}

				buyCount = _buyEntries.Count;
				buyMinIndex = GetBuyExtremeIndex(true);
				buyMaxIndex = GetBuyExtremeIndex(false);
				buyTarget = CalculateBuyTarget(minimalProfit, buyMinIndex, buyMaxIndex);
			}

			if (sellTarget is decimal st && sellCount >= 2 && ask <= st)
			{
				if (sellMinIndex >= 0)
				{
					var index = sellMinIndex;
					var volume = Math.Min(StartVolume, _sellEntries[index].Volume);
					CloseSellEntry(index, volume);
				}

				sellMaxIndex = GetSellExtremeIndex(false);
				if (sellMaxIndex >= 0)
				{
					var index = sellMaxIndex;
					var volume = _sellEntries[index].Volume;
					CloseSellEntry(index, volume);
				}

				sellCount = _sellEntries.Count;
				sellMinIndex = GetSellExtremeIndex(true);
				sellMaxIndex = GetSellExtremeIndex(false);
				sellTarget = CalculateSellTarget(minimalProfit, sellMinIndex, sellMaxIndex);
			}
		}

		if (_previousOpen is decimal prevOpen && _previousClose is decimal prevClose)
		{
			if (prevClose > prevOpen)
			{
				var shouldBuy = buyCount == 0;
				if (!shouldBuy && buyMinIndex >= 0)
				{
					var minPrice = _buyEntries[buyMinIndex].Price;
					shouldBuy = minPrice - ask > orderStep;
				}

				if (shouldBuy)
				{
					var volume = buyCount == 0 ? StartVolume : _buyEntries[buyMinIndex].Volume * 2m;
					OpenBuy(ask, volume);
				}
			}

			if (prevClose < prevOpen)
			{
				var shouldSell = sellCount == 0;
				if (!shouldSell && sellMaxIndex >= 0)
				{
					var maxPrice = _sellEntries[sellMaxIndex].Price;
					shouldSell = bid - maxPrice > orderStep;
				}

				if (shouldSell)
				{
					var volume = sellCount == 0 ? StartVolume : _sellEntries[sellMaxIndex].Volume * 2m;
					OpenSell(bid, volume);
				}
			}
		}

		_previousOpen = candle.OpenPrice;
		_previousClose = candle.ClosePrice;
	}

	private decimal? CalculateBuyTarget(decimal minimalProfit, int minIndex, int maxIndex)
	{
		if (minIndex < 0 || maxIndex < 0)
			return null;

		var minEntry = _buyEntries[minIndex];
		var maxEntry = _buyEntries[maxIndex];
		var denominator = maxEntry.Volume + minEntry.Volume;
		if (denominator <= 0m)
			return null;

		if (CloseMode == CloseOrderModes.Average)
		{
			return (maxEntry.Price * maxEntry.Volume + minEntry.Price * minEntry.Volume) / denominator + minimalProfit;
		}

		return (maxEntry.Price * StartVolume + minEntry.Price * minEntry.Volume) / (StartVolume + minEntry.Volume) + minimalProfit;
	}

	private decimal? CalculateSellTarget(decimal minimalProfit, int minIndex, int maxIndex)
	{
		if (minIndex < 0 || maxIndex < 0)
			return null;

		var minEntry = _sellEntries[minIndex];
		var maxEntry = _sellEntries[maxIndex];
		var denominator = maxEntry.Volume + minEntry.Volume;
		if (denominator <= 0m)
			return null;

		if (CloseMode == CloseOrderModes.Average)
		{
			return (maxEntry.Price * maxEntry.Volume + minEntry.Price * minEntry.Volume) / denominator - minimalProfit;
		}

		return (maxEntry.Price * maxEntry.Volume + minEntry.Price * StartVolume) / (maxEntry.Volume + StartVolume) - minimalProfit;
	}

	private void OpenBuy(decimal ask, decimal volume)
	{
		var adjusted = AdjustVolume(volume);
		if (adjusted <= 0m)
			return;

		var price = NormalizePrice(ask);

		BuyMarket(adjusted);
		_buyEntries.Add(new GridEntry(price, adjusted));
	}

	private void OpenSell(decimal bid, decimal volume)
	{
		var adjusted = AdjustVolume(volume);
		if (adjusted <= 0m)
			return;

		var price = NormalizePrice(bid);

		SellMarket(adjusted);
		_sellEntries.Add(new GridEntry(price, adjusted));
	}

	private void CloseBuyEntry(int index, decimal volume)
	{
		if (index < 0 || index >= _buyEntries.Count)
			return;

		var entry = _buyEntries[index];
		var amount = Math.Min(volume, entry.Volume);
		amount = AdjustVolume(amount);
		if (amount <= 0m)
			return;

		SellMarket(amount);

		var remaining = entry.Volume - amount;
		if (remaining <= 0m)
		{
			_buyEntries.RemoveAt(index);
		}
		else
		{
			_buyEntries[index] = new GridEntry(entry.Price, remaining);
		}
	}

	private void CloseSellEntry(int index, decimal volume)
	{
		if (index < 0 || index >= _sellEntries.Count)
			return;

		var entry = _sellEntries[index];
		var amount = Math.Min(volume, entry.Volume);
		amount = AdjustVolume(amount);
		if (amount <= 0m)
			return;

		BuyMarket(amount);

		var remaining = entry.Volume - amount;
		if (remaining <= 0m)
		{
			_sellEntries.RemoveAt(index);
		}
		else
		{
			_sellEntries[index] = new GridEntry(entry.Price, remaining);
		}
	}

	private decimal AdjustVolume(decimal volume)
	{
		var result = volume;

		if (MaxVolume > 0m && result > MaxVolume)
			result = MaxVolume;

		var security = Security;
		if (security == null)
			return result;

		var min = security.MinVolume ?? 0m;
		var max = security.MaxVolume;
		var step = security.VolumeStep ?? 0m;

		if (result < min)
			result = min;

		if (max.HasValue && result > max.Value)
			result = max.Value;

		if (step > 0m)
		{
			var steps = Math.Round(result / step, MidpointRounding.AwayFromZero);
			result = steps * step;
		}

		return result;
	}

	private decimal NormalizePrice(decimal price)
	{
		var security = Security;
		if (security == null)
			return price;

		var normalized = security.ShrinkPrice(price);
		return normalized > 0m ? normalized : price;
	}

	private decimal GetPointValue()
	{
		var security = Security;
		if (security?.PriceStep is decimal step && step > 0m)
			return step;

		return 0.0001m;
	}

	private decimal GetBidPrice(ICandleMessage candle)
	{
		if (Security?.BestBid?.Price is decimal bid && bid > 0m)
			return bid;

		if (Security?.LastTrade?.Price is decimal last && last > 0m)
			return last;

		return candle.ClosePrice;
	}

	private decimal GetAskPrice(ICandleMessage candle)
	{
		if (Security?.BestAsk?.Price is decimal ask && ask > 0m)
			return ask;

		if (Security?.LastTrade?.Price is decimal last && last > 0m)
			return last;

		return candle.ClosePrice;
	}

	private int GetBuyExtremeIndex(bool isMin)
	{
		if (_buyEntries.Count == 0)
			return -1;

		var index = 0;
		var extreme = _buyEntries[0].Price;

		for (var i = 1; i < _buyEntries.Count; i++)
		{
			var price = _buyEntries[i].Price;
			if (isMin)
			{
				if (price < extreme)
				{
					extreme = price;
					index = i;
				}
			}
			else if (price > extreme)
			{
					extreme = price;
					index = i;
			}
		}

		return index;
	}

	private int GetSellExtremeIndex(bool isMin)
	{
		if (_sellEntries.Count == 0)
			return -1;

		var index = 0;
		var extreme = _sellEntries[0].Price;

		for (var i = 1; i < _sellEntries.Count; i++)
		{
			var price = _sellEntries[i].Price;
			if (isMin)
			{
				if (price < extreme)
				{
					extreme = price;
					index = i;
				}
			}
			else if (price > extreme)
			{
					extreme = price;
					index = i;
			}
		}

		return index;
	}

	/// <summary>
	/// Exit mode for averaging positions.
	/// </summary>
	public enum CloseOrderModes
	{
		Average,
		Partial,
	}

	private readonly struct GridEntry
	{
		public GridEntry(decimal price, decimal volume)
		{
			Price = price;
			Volume = volume;
		}

		public decimal Price { get; }

		public decimal Volume { get; }
	}
}

