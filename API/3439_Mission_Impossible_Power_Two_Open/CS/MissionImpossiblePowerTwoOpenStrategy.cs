using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "Mission Impossible Power Two Open" MetaTrader strategy.
/// The strategy opens a position in the direction of the previous candle
/// and builds an averaging grid when price moves against the initial trade.
/// </summary>
public class MissionImpossiblePowerTwoOpenStrategy : Strategy
{
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _maxVolumeParam;
	private readonly StrategyParam<decimal> _power;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitFirstPips;
	private readonly StrategyParam<int> _takeProfitNextPips;
	private readonly StrategyParam<int> _gridStepPips;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<GridEntry> _longEntries = new();
	private readonly List<GridEntry> _shortEntries = new();

	private decimal _priceStep;
	private decimal _pipValue;
	private decimal _volumeStep;
	private decimal _minVolume;
	private decimal _maxVolume;

	private decimal? _previousOpen;
	private decimal? _previousClose;

	private Sides? _pendingEntrySide;
	private decimal _pendingEntryVolume;
	private Sides? _pendingCloseSide;
	private decimal _pendingCloseVolume;

	private decimal? _longTakeProfit;
	private decimal? _longStopLoss;
	private decimal? _shortTakeProfit;
	private decimal? _shortStopLoss;

	private decimal? _lastLongEntryPrice;
	private decimal? _lastShortEntryPrice;

	private decimal? _longAveragePrice;
	private decimal? _shortAveragePrice;

	/// <summary>
	/// Container for individual grid entries.
	/// </summary>
	private struct GridEntry
	{
		public decimal EntryPrice;
		public decimal Volume;
	}

	/// <summary>
	/// Base market order volume.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Maximum allowed market order volume.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolumeParam.Value;
		set => _maxVolumeParam.Value = value;
	}

	/// <summary>
	/// Power coefficient used to scale the averaging volume.
	/// </summary>
	public decimal Power
	{
		get => _power.Value;
		set => _power.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance for the first position.
	/// </summary>
	public int TakeProfitFirstPips
	{
		get => _takeProfitFirstPips.Value;
		set => _takeProfitFirstPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance for the averaged basket.
	/// </summary>
	public int TakeProfitNextPips
	{
		get => _takeProfitNextPips.Value;
		set => _takeProfitNextPips.Value = value;
	}

	/// <summary>
	/// Minimum price displacement before adding a new grid entry.
	/// </summary>
	public int GridStepPips
	{
		get => _gridStepPips.Value;
		set => _gridStepPips.Value = value;
	}

	/// <summary>
	/// Maximum number of grid entries per direction.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Candle type used for signal detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes default parameters.
	/// </summary>
	public MissionImpossiblePowerTwoOpenStrategy()
	{
		_baseVolume = Param(nameof(BaseVolume), 0.01m)
		.SetNotNegative()
		.SetDisplay("Base Volume", "Initial market order size", "Trading")
		.SetCanOptimize(true);

		_maxVolumeParam = Param(nameof(MaxVolume), 2m)
		.SetNotNegative()
		.SetDisplay("Max Volume", "Upper cap for any market order", "Trading");

		_power = Param(nameof(Power), 13m)
		.SetNotNegative()
		.SetDisplay("Power", "Multiplier applied to unrealized loss when scaling volume", "Risk")
		.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 400)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Stop distance in price steps", "Protection")
		.SetCanOptimize(true);

		_takeProfitFirstPips = Param(nameof(TakeProfitFirstPips), 15)
		.SetNotNegative()
		.SetDisplay("First Take Profit", "Take-profit for the first grid entry", "Protection")
		.SetCanOptimize(true);

		_takeProfitNextPips = Param(nameof(TakeProfitNextPips), 7)
		.SetNotNegative()
		.SetDisplay("Grid Take Profit", "Take-profit applied to the averaged basket", "Protection")
		.SetCanOptimize(true);

		_gridStepPips = Param(nameof(GridStepPips), 21)
		.SetNotNegative()
		.SetDisplay("Grid Step", "Minimum adverse move before adding a new entry", "Trading")
		.SetCanOptimize(true);

		_maxTrades = Param(nameof(MaxTrades), 16)
		.SetNotNegative()
		.SetDisplay("Max Trades", "Maximum number of averaging entries", "Risk")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for signals", "General");
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

		_longEntries.Clear();
		_shortEntries.Clear();

		_previousOpen = null;
		_previousClose = null;

		_pendingEntrySide = null;
		_pendingEntryVolume = 0m;
		_pendingCloseSide = null;
		_pendingCloseVolume = 0m;

		_longTakeProfit = null;
		_longStopLoss = null;
		_shortTakeProfit = null;
		_shortStopLoss = null;

		_lastLongEntryPrice = null;
		_lastShortEntryPrice = null;
		_longAveragePrice = null;
		_shortAveragePrice = null;

		Volume = BaseVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Cache trading instrument parameters once trading starts.
		_priceStep = Security?.PriceStep ?? 0.0001m;
		_volumeStep = Security?.VolumeStep ?? 0.01m;
		_minVolume = Security?.MinVolume ?? BaseVolume;
		_maxVolume = Security?.MaxVolume ?? 0m;
		_pipValue = Security?.StepPrice ?? 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Always manage existing positions before evaluating new signals.
		ManageDirectionClosures(candle.ClosePrice);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			StorePreviousCandle(candle);
			return;
		}

		var signal = DetermineSignal();

		if (signal == Sides.Buy || _longEntries.Count > 0)
		EvaluateLongEntries(signal, candle.ClosePrice);

		if (signal == Sides.Sell || _shortEntries.Count > 0)
		EvaluateShortEntries(signal, candle.ClosePrice);

		StorePreviousCandle(candle);
	}

	private void StorePreviousCandle(ICandleMessage candle)
	{
		_previousOpen = candle.OpenPrice;
		_previousClose = candle.ClosePrice;
	}

	private Sides? DetermineSignal()
	{
		if (_previousOpen is not decimal prevOpen || _previousClose is not decimal prevClose)
		return null;

		if (prevClose > prevOpen)
		return Sides.Buy;

		if (prevClose < prevOpen)
		return Sides.Sell;

		return null;
	}

	private void EvaluateLongEntries(Sides? signal, decimal price)
	{
		var existingCount = _longEntries.Count;
		if (existingCount == 0)
		{
			if (signal == Sides.Buy)
			TryOpenLong(price, existingCount);
			return;
		}

		if (existingCount >= MaxTrades)
		return;

		if (!ShouldAddLong(price))
		return;

		TryOpenLong(price, existingCount);
	}

	private void EvaluateShortEntries(Sides? signal, decimal price)
	{
		var existingCount = _shortEntries.Count;
		if (existingCount == 0)
		{
			if (signal == Sides.Sell)
			TryOpenShort(price, existingCount);
			return;
		}

		if (existingCount >= MaxTrades)
		return;

		if (!ShouldAddShort(price))
		return;

		TryOpenShort(price, existingCount);
	}

	private void TryOpenLong(decimal price, int existingCount)
	{
		var volume = CalculateOrderVolume(_longEntries, price, Sides.Buy);
		if (volume <= 0m)
		return;

		_pendingEntrySide = Sides.Buy;
		_pendingEntryVolume = volume;

		LogInfo($"Opening long grid entry #{existingCount + 1} with volume {volume} at price {price}");
		BuyMarket(volume);
	}

	private void TryOpenShort(decimal price, int existingCount)
	{
		var volume = CalculateOrderVolume(_shortEntries, price, Sides.Sell);
		if (volume <= 0m)
		return;

		_pendingEntrySide = Sides.Sell;
		_pendingEntryVolume = volume;

		LogInfo($"Opening short grid entry #{existingCount + 1} with volume {volume} at price {price}");
		SellMarket(volume);
	}

	private bool ShouldAddLong(decimal price)
	{
		if (_lastLongEntryPrice is not decimal lastPrice)
		return false;

		var step = GridStepPips * _priceStep;
		if (step <= 0m)
		return true;

		return lastPrice - price >= step;
	}

	private bool ShouldAddShort(decimal price)
	{
		if (_lastShortEntryPrice is not decimal lastPrice)
		return false;

		var step = GridStepPips * _priceStep;
		if (step <= 0m)
		return true;

		return price - lastPrice >= step;
	}

	private decimal CalculateOrderVolume(List<GridEntry> entries, decimal price, Sides side)
	{
		var volume = BaseVolume;

		if (entries.Count > 0)
		{
			var multiplier = CalculateMultiplier(entries, price, side);
			var addition = Math.Abs(multiplier * Power);
			volume += addition;
		}

		var cap = MaxVolume;
		if (_maxVolume > 0m)
		cap = Math.Min(cap, _maxVolume);

		volume = Math.Min(volume, cap);
		volume = AdjustVolume(volume);
		return volume;
	}

	private decimal CalculateMultiplier(List<GridEntry> entries, decimal price, Sides side)
	{
		if (entries.Count == 0 || price <= 0m)
		return 0m;

		decimal sum = 0m;
		foreach (var entry in entries)
		{
			var profit = CalculateUnrealizedProfit(entry, price, side);
			sum += entry.EntryPrice * profit;
		}

		return (sum / price) * 0.0001m;
	}

	private decimal CalculateUnrealizedProfit(GridEntry entry, decimal price, Sides side)
	{
		if (_priceStep <= 0m || _pipValue <= 0m)
		return 0m;

		var direction = side == Sides.Buy ? 1m : -1m;
		var priceDiff = (price - entry.EntryPrice) * direction;
		var steps = priceDiff / _priceStep;
		return steps * _pipValue * entry.Volume;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		if (_volumeStep > 0m)
		{
			var ratio = Math.Round(volume / _volumeStep, MidpointRounding.AwayFromZero);
			volume = ratio * _volumeStep;
		}

		if (volume < _minVolume)
		volume = _minVolume;

		if (_maxVolume > 0m && volume > _maxVolume)
		volume = _maxVolume;

		return volume;
	}

	private void ManageDirectionClosures(decimal price)
	{
		if (_longEntries.Count > 0)
		{
			if (_longTakeProfit is decimal tp && price >= tp)
			CloseLong();
			else if (_longStopLoss is decimal sl && price <= sl)
			CloseLong();
		}

		if (_shortEntries.Count > 0)
		{
			if (_shortTakeProfit is decimal tp && price <= tp)
			CloseShort();
			else if (_shortStopLoss is decimal sl && price >= sl)
			CloseShort();
		}
	}

	private void CloseLong()
	{
		if (_pendingCloseSide == Sides.Buy)
		return;

		var volume = GetTotalVolume(_longEntries);
		if (volume <= 0m)
		return;

		_pendingCloseSide = Sides.Buy;
		_pendingCloseVolume = volume;

		LogInfo($"Closing long basket with volume {volume}");
		SellMarket(volume);
	}

	private void CloseShort()
	{
		if (_pendingCloseSide == Sides.Sell)
		return;

		var volume = GetTotalVolume(_shortEntries);
		if (volume <= 0m)
		return;

		_pendingCloseSide = Sides.Sell;
		_pendingCloseVolume = volume;

		LogInfo($"Closing short basket with volume {volume}");
		BuyMarket(volume);
	}

	private decimal GetTotalVolume(List<GridEntry> entries)
	{
		decimal total = 0m;
		foreach (var entry in entries)
		total += entry.Volume;
		return total;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var order = trade.Order;
		if (order == null)
		return;

		var side = order.Side;
		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;

		if (volume <= 0m)
		return;

		// Handle pending closures first to keep the grid state consistent.
		if (_pendingCloseSide == Sides.Buy && side == Sides.Sell)
		{
			ApplyClose(_longEntries, volume);
			_pendingCloseVolume -= volume;
			if (_pendingCloseVolume <= 0m)
			_pendingCloseSide = null;

			UpdateLongState();
			return;
		}

		if (_pendingCloseSide == Sides.Sell && side == Sides.Buy)
		{
			ApplyClose(_shortEntries, volume);
			_pendingCloseVolume -= volume;
			if (_pendingCloseVolume <= 0m)
			_pendingCloseSide = null;

			UpdateShortState();
			return;
		}

		// Then process pending entries.
		if (_pendingEntrySide == Sides.Buy && side == Sides.Buy)
		{
			AddEntry(_longEntries, price, volume);
			_pendingEntryVolume -= volume;
			if (_pendingEntryVolume <= 0m)
			_pendingEntrySide = null;

			UpdateLongState();
			return;
		}

		if (_pendingEntrySide == Sides.Sell && side == Sides.Sell)
		{
			AddEntry(_shortEntries, price, volume);
			_pendingEntryVolume -= volume;
			if (_pendingEntryVolume <= 0m)
			_pendingEntrySide = null;

			UpdateShortState();
			return;
		}

		// Fallback: classify trade by comparing to current exposure.
		if (side == Sides.Buy)
		{
			if (_shortEntries.Count > 0)
			{
				ApplyClose(_shortEntries, volume);
				UpdateShortState();
			}
			else
			{
				AddEntry(_longEntries, price, volume);
				UpdateLongState();
			}
		}
		else
		{
			if (_longEntries.Count > 0)
			{
				ApplyClose(_longEntries, volume);
				UpdateLongState();
			}
			else
			{
				AddEntry(_shortEntries, price, volume);
				UpdateShortState();
			}
		}
	}

	private void AddEntry(List<GridEntry> entries, decimal price, decimal volume)
	{
		var normalizedVolume = AdjustVolume(volume);
		if (normalizedVolume <= 0m)
		return;

		entries.Add(new GridEntry
		{
			EntryPrice = price,
			Volume = normalizedVolume,
		});
	}

	private void ApplyClose(List<GridEntry> entries, decimal volume)
	{
		var remaining = volume;
		for (var i = entries.Count - 1; i >= 0 && remaining > 0m; i--)
		{
			var entry = entries[i];
			var reduce = Math.Min(entry.Volume, remaining);
			entry.Volume -= reduce;
			remaining -= reduce;

			if (entry.Volume <= 0m)
			{
				entries.RemoveAt(i);
			}
			else
			{
				entries[i] = entry;
			}
		}
	}

	private void UpdateLongState()
	{
		UpdateDirectionState(_longEntries, true);
	}

	private void UpdateShortState()
	{
		UpdateDirectionState(_shortEntries, false);
	}

	private void UpdateDirectionState(List<GridEntry> entries, bool isLong)
	{
		if (entries.Count == 0)
		{
			if (isLong)
			{
				_longAveragePrice = null;
				_lastLongEntryPrice = null;
				_longTakeProfit = null;
				_longStopLoss = null;
			}
			else
			{
				_shortAveragePrice = null;
				_lastShortEntryPrice = null;
				_shortTakeProfit = null;
				_shortStopLoss = null;
			}
			return;
		}

		var totalVolume = GetTotalVolume(entries);
		if (totalVolume <= 0m)
		{
			entries.Clear();
			if (isLong)
			{
				_longAveragePrice = null;
				_lastLongEntryPrice = null;
				_longTakeProfit = null;
				_longStopLoss = null;
			}
			else
			{
				_shortAveragePrice = null;
				_lastShortEntryPrice = null;
				_shortTakeProfit = null;
				_shortStopLoss = null;
			}
			return;
		}

		decimal weightedPrice = 0m;
		foreach (var entry in entries)
		weightedPrice += entry.EntryPrice * entry.Volume;

		var average = weightedPrice / totalVolume;
		var lastPrice = entries[^1].EntryPrice;

		var stopDistance = StopLossPips * _priceStep;
		var firstTakeProfit = TakeProfitFirstPips * _priceStep;
		var gridTakeProfit = TakeProfitNextPips * _priceStep;

		if (isLong)
		{
			_longAveragePrice = average;
			_lastLongEntryPrice = lastPrice;

			if (entries.Count == 1)
			{
				_longTakeProfit = NormalizePrice(lastPrice + firstTakeProfit);
				_longStopLoss = NormalizePrice(lastPrice - stopDistance);
			}
			else
			{
				_longTakeProfit = NormalizePrice(average + gridTakeProfit);
				_longStopLoss = NormalizePrice(average - stopDistance);
			}
		}
		else
		{
			_shortAveragePrice = average;
			_lastShortEntryPrice = lastPrice;

			if (entries.Count == 1)
			{
				_shortTakeProfit = NormalizePrice(lastPrice - firstTakeProfit);
				_shortStopLoss = NormalizePrice(lastPrice + stopDistance);
			}
			else
			{
				_shortTakeProfit = NormalizePrice(average - gridTakeProfit);
				_shortStopLoss = NormalizePrice(average + stopDistance);
			}
		}
	}

	private decimal NormalizePrice(decimal price)
	{
		if (_priceStep <= 0m)
		return price;

		var ratio = Math.Round(price / _priceStep, MidpointRounding.AwayFromZero);
		return ratio * _priceStep;
	}
}
