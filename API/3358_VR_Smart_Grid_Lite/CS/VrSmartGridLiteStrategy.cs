namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Grid strategy inspired by VR Smart Grid Lite expert advisor.
/// </summary>
public class VrSmartGridLiteStrategy : Strategy
{
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<decimal> _startVolume;
	private readonly StrategyParam<decimal> _maximalVolume;
	private readonly StrategyParam<CloseModeOption> _closeMode;
	private readonly StrategyParam<int> _orderStepPips;
	private readonly StrategyParam<int> _minimalProfitPips;
	private readonly StrategyParam<int> _slippagePips;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<GridEntry> _entries = new();
	private ICandleMessage? _previousCandle;
	private decimal _pipSize;

	/// <summary>
	/// Defines how the grid closes positions.
	/// </summary>
	public enum CloseModeOption
	{
		/// <summary>
		/// Close opposite extremes at a weighted average price.
		/// </summary>
		Average,

		/// <summary>
		/// Partially close the most recent order and close the oldest order fully.
		/// </summary>
		PartClose
	}

	/// <summary>
	/// Take profit distance in pips for a single position.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Initial order volume.
	/// </summary>
	public decimal StartVolume
	{
		get => _startVolume.Value;
		set => _startVolume.Value = value;
	}

	/// <summary>
	/// Maximum allowed order volume.
	/// </summary>
	public decimal MaximalVolume
	{
		get => _maximalVolume.Value;
		set => _maximalVolume.Value = value;
	}

	/// <summary>
	/// Closing mode for managing grid exits.
	/// </summary>
	public CloseModeOption CloseMode
	{
		get => _closeMode.Value;
		set => _closeMode.Value = value;
	}

	/// <summary>
	/// Minimum distance between new orders in pips.
	/// </summary>
	public int OrderStepPips
	{
		get => _orderStepPips.Value;
		set => _orderStepPips.Value = value;
	}

	/// <summary>
	/// Minimal profit in pips when averaging several positions.
	/// </summary>
	public int MinimalProfitPips
	{
		get => _minimalProfitPips.Value;
		set => _minimalProfitPips.Value = value;
	}

	/// <summary>
	/// Slippage value in pips. Present for completeness of the original input list.
	/// </summary>
	public int SlippagePips
	{
		get => _slippagePips.Value;
		set => _slippagePips.Value = value;
	}

	/// <summary>
	/// Candle type used for synchronization.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="VrSmartGridLiteStrategy"/> class.
	/// </summary>
	public VrSmartGridLiteStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 300)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Target profit distance for single positions", "Trading").SetCanOptimize(true);

		_startVolume = Param(nameof(StartVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Start Volume", "Base volume for the first order", "Trading").SetCanOptimize(true);

		_maximalVolume = Param(nameof(MaximalVolume), 2.56m)
			.SetGreaterThanZero()
			.SetDisplay("Maximal Volume", "Upper limit for any single order", "Trading");

		_closeMode = Param(nameof(CloseMode), CloseModeOption.Average)
			.SetDisplay("Close Mode", "How the grid exits positions", "Exit");

		_orderStepPips = Param(nameof(OrderStepPips), 390)
			.SetGreaterThanZero()
			.SetDisplay("Order Step (pips)", "Required distance before adding a new order", "Grid").SetCanOptimize(true);

		_minimalProfitPips = Param(nameof(MinimalProfitPips), 70)
			.SetGreaterThanZero()
			.SetDisplay("Minimal Profit (pips)", "Extra profit added to weighted exits", "Exit").SetCanOptimize(true);

		_slippagePips = Param(nameof(SlippagePips), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slippage (pips)", "Reserved parameter from the original expert advisor", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candle subscription", "General");
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

		_entries.Clear();
		_previousCandle = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		BuildStatistics(out var buyStats, out var sellStats);

		var takeProfitDistance = TakeProfitPips > 0 ? TakeProfitPips * _pipSize : 0m;
		ApplyTakeProfit(candle, takeProfitDistance, buyStats, sellStats);

		BuildStatistics(out buyStats, out sellStats);

		var minimalProfitDistance = MinimalProfitPips > 0 ? MinimalProfitPips * _pipSize : 0m;
		ProcessCloseLogic(candle, minimalProfitDistance, buyStats, sellStats);

		BuildStatistics(out buyStats, out sellStats);

		if (_previousCandle != null)
		{
			TryOpenLong(candle, buyStats);
			TryOpenShort(candle, sellStats);
		}

		_previousCandle = candle;
	}

	/// <summary>
	/// Applies take profit logic when only one position is open.
	/// </summary>
	private void ApplyTakeProfit(ICandleMessage candle, decimal takeProfitDistance, SideStatistics buyStats, SideStatistics sellStats)
	{
		if (takeProfitDistance <= 0m)
			return;

		if (buyStats.Count == 1 && buyStats.MinEntry != null)
		{
			var entry = buyStats.MinEntry;
			if (candle.ClosePrice >= entry.EntryPrice + takeProfitDistance)
				CloseEntry(entry, entry.Volume);
		}

		if (sellStats.Count == 1 && sellStats.MaxEntry != null)
		{
			var entry = sellStats.MaxEntry;
			if (candle.ClosePrice <= entry.EntryPrice - takeProfitDistance)
				CloseEntry(entry, entry.Volume);
		}
	}

	/// <summary>
	/// Executes averaging or partial close exits.
	/// </summary>
	private void ProcessCloseLogic(ICandleMessage candle, decimal minimalProfitDistance, SideStatistics buyStats, SideStatistics sellStats)
	{
		if (CloseMode == CloseModeOption.Average)
		{
			if (buyStats.Count >= 2 && buyStats.MinEntry != null && buyStats.MaxEntry != null)
			{
				var target = CalculateAverageTarget(buyStats.MinEntry, buyStats.MaxEntry, minimalProfitDistance, true);
				if (target > 0m && candle.ClosePrice >= target)
				{
					var maxEntry = buyStats.MaxEntry;
					var minEntry = buyStats.MinEntry;

					if (maxEntry != null)
						CloseEntry(maxEntry, maxEntry.Volume);

					if (minEntry != null && _entries.Contains(minEntry))
						CloseEntry(minEntry, minEntry.Volume);
				}
			}

			if (sellStats.Count >= 2 && sellStats.MinEntry != null && sellStats.MaxEntry != null)
			{
				var target = CalculateAverageTarget(sellStats.MinEntry, sellStats.MaxEntry, minimalProfitDistance, false);
				if (target > 0m && candle.ClosePrice <= target)
				{
					var minEntry = sellStats.MinEntry;
					var maxEntry = sellStats.MaxEntry;

					if (minEntry != null)
						CloseEntry(minEntry, minEntry.Volume);

					if (maxEntry != null && _entries.Contains(maxEntry))
						CloseEntry(maxEntry, maxEntry.Volume);
				}
			}
		}
		else if (CloseMode == CloseModeOption.PartClose)
		{
			if (!TryPrepareOrderVolume(StartVolume, out var startVolume))
				return;

			if (buyStats.Count >= 2 && buyStats.MinEntry != null && buyStats.MaxEntry != null)
			{
				var target = CalculatePartCloseTarget(buyStats.MinEntry, buyStats.MaxEntry, minimalProfitDistance, true, startVolume);
				if (target > 0m && candle.ClosePrice >= target)
				{
					var maxEntry = buyStats.MaxEntry;
					var minEntry = buyStats.MinEntry;

					if (maxEntry != null)
						CloseEntry(maxEntry, Math.Min(maxEntry.Volume, startVolume));

					if (minEntry != null && _entries.Contains(minEntry))
						CloseEntry(minEntry, minEntry.Volume);
				}
			}

			if (sellStats.Count >= 2 && sellStats.MinEntry != null && sellStats.MaxEntry != null)
			{
				var target = CalculatePartCloseTarget(sellStats.MinEntry, sellStats.MaxEntry, minimalProfitDistance, false, startVolume);
				if (target > 0m && candle.ClosePrice <= target)
				{
					var minEntry = sellStats.MinEntry;
					var maxEntry = sellStats.MaxEntry;

					if (minEntry != null)
						CloseEntry(minEntry, Math.Min(minEntry.Volume, startVolume));

					if (maxEntry != null && _entries.Contains(maxEntry))
						CloseEntry(maxEntry, maxEntry.Volume);
				}
			}
		}
	}

	/// <summary>
	/// Attempts to open a long position according to the grid rules.
	/// </summary>
	private void TryOpenLong(ICandleMessage candle, SideStatistics buyStats)
	{
		if (_previousCandle == null || _previousCandle.ClosePrice <= _previousCandle.OpenPrice)
			return;

		if (!TryPrepareOrderVolume(buyStats.Count == 0 ? StartVolume : buyStats.MinEntry?.Volume * 2m ?? 0m, out var volume))
			return;

		var stepDistance = OrderStepPips > 0 ? OrderStepPips * _pipSize : 0m;
		if (buyStats.Count > 0 && buyStats.MinEntry != null && stepDistance > 0m)
		{
			var distance = buyStats.MinEntry.EntryPrice - candle.ClosePrice;
			if (distance <= stepDistance)
				return;
		}

		var order = BuyMarket(volume);
		if (order == null)
			return;

		_entries.Add(new GridEntry
		{
			IsLong = true,
			Volume = volume,
			EntryPrice = candle.ClosePrice
		});
	}

	/// <summary>
	/// Attempts to open a short position according to the grid rules.
	/// </summary>
	private void TryOpenShort(ICandleMessage candle, SideStatistics sellStats)
	{
		if (_previousCandle == null || _previousCandle.ClosePrice >= _previousCandle.OpenPrice)
			return;

		if (!TryPrepareOrderVolume(sellStats.Count == 0 ? StartVolume : sellStats.MaxEntry?.Volume * 2m ?? 0m, out var volume))
			return;

		var stepDistance = OrderStepPips > 0 ? OrderStepPips * _pipSize : 0m;
		if (sellStats.Count > 0 && sellStats.MaxEntry != null && stepDistance > 0m)
		{
			var distance = candle.ClosePrice - sellStats.MaxEntry.EntryPrice;
			if (distance <= stepDistance)
				return;
		}

		var order = SellMarket(volume);
		if (order == null)
			return;

		_entries.Add(new GridEntry
		{
			IsLong = false,
			Volume = volume,
			EntryPrice = candle.ClosePrice
		});
	}

	/// <summary>
	/// Closes part or all of an entry.
	/// </summary>
	private void CloseEntry(GridEntry entry, decimal volumeToClose)
	{
		if (entry.Volume <= 0m || volumeToClose <= 0m)
			return;

		if (!TryNormalizeVolume(Math.Min(entry.Volume, volumeToClose), out var volume))
			return;

		var order = entry.IsLong ? SellMarket(volume) : BuyMarket(volume);
		if (order == null)
			return;

		entry.Volume -= volume;

		if (entry.Volume <= 0m)
			_entries.Remove(entry);
	}

	/// <summary>
	/// Calculates weighted target for the average close mode.
	/// </summary>
	private static decimal CalculateAverageTarget(GridEntry minEntry, GridEntry maxEntry, decimal minimalProfitDistance, bool isLong)
	{
		var totalVolume = minEntry.Volume + maxEntry.Volume;
		if (totalVolume <= 0m)
			return 0m;

		var weighted = ((maxEntry.EntryPrice * maxEntry.Volume) + (minEntry.EntryPrice * minEntry.Volume)) / totalVolume;
		return isLong ? weighted + minimalProfitDistance : weighted - minimalProfitDistance;
	}

	/// <summary>
	/// Calculates weighted target for the partial close mode.
	/// </summary>
	private static decimal CalculatePartCloseTarget(GridEntry minEntry, GridEntry maxEntry, decimal minimalProfitDistance, bool isLong, decimal startVolume)
	{
		if (startVolume <= 0m)
			return 0m;

		decimal weighted;

		if (isLong)
		{
			weighted = ((maxEntry.EntryPrice * startVolume) + (minEntry.EntryPrice * minEntry.Volume)) / (startVolume + minEntry.Volume);
			return weighted + minimalProfitDistance;
		}

		weighted = ((maxEntry.EntryPrice * maxEntry.Volume) + (minEntry.EntryPrice * startVolume)) / (maxEntry.Volume + startVolume);
		return weighted - minimalProfitDistance;
	}

	/// <summary>
	/// Collects statistics about current entries on both sides.
	/// </summary>
	private void BuildStatistics(out SideStatistics buyStats, out SideStatistics sellStats)
	{
		GridEntry? buyMin = null;
		GridEntry? buyMax = null;
		GridEntry? sellMin = null;
		GridEntry? sellMax = null;
		var buyCount = 0;
		var sellCount = 0;

		for (var i = 0; i < _entries.Count; i++)
		{
			var entry = _entries[i];

			if (entry.IsLong)
			{
				buyCount++;

				if (buyMin == null || entry.EntryPrice < buyMin.EntryPrice)
					buyMin = entry;

				if (buyMax == null || entry.EntryPrice > buyMax.EntryPrice)
					buyMax = entry;
			}
			else
			{
				sellCount++;

				if (sellMin == null || entry.EntryPrice < sellMin.EntryPrice)
					sellMin = entry;

				if (sellMax == null || entry.EntryPrice > sellMax.EntryPrice)
					sellMax = entry;
			}
		}

		buyStats = new SideStatistics(buyCount, buyMin, buyMax);
		sellStats = new SideStatistics(sellCount, sellMin, sellMax);
	}

	/// <summary>
	/// Normalizes volume according to security constraints.
	/// </summary>
	private bool TryNormalizeVolume(decimal volume, out decimal normalized)
	{
		normalized = 0m;

		if (volume <= 0m)
			return false;

		if (Security == null)
		{
			normalized = volume;
			return true;
		}

		var step = Security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Floor((double)(volume / step));
			volume = step * (decimal)steps;
		}

		if (volume <= 0m)
			return false;

		var minVolume = Security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			return false;

		var maxVolume = Security.MaxVolume;
		if (maxVolume != null && volume > maxVolume.Value)
			volume = maxVolume.Value;

		normalized = volume;
		return normalized > 0m;
	}

	/// <summary>
	/// Prepares order volume with maximal volume limitation.
	/// </summary>
	private bool TryPrepareOrderVolume(decimal? rawVolume, out decimal preparedVolume)
	{
		preparedVolume = 0m;
		if (rawVolume == null)
			return false;

		var volume = rawVolume.Value;

		if (MaximalVolume > 0m && volume > MaximalVolume)
			volume = MaximalVolume;

		return TryNormalizeVolume(volume, out preparedVolume);
	}

	/// <summary>
	/// Calculates pip size using security metadata.
	/// </summary>
	private decimal CalculatePipSize()
	{
		if (Security == null)
			return 0.0001m;

		var step = Security.PriceStep ?? 0.0001m;
		var decimals = Security.Decimals ?? GetDecimalsFromStep(step);
		var factor = decimals == 3 || decimals == 5 ? 10m : 1m;

		return step * factor;
	}

	private static int GetDecimalsFromStep(decimal step)
	{
		if (step <= 0m)
			return 0;

		var value = Math.Abs(Math.Log10((double)step));
		return (int)Math.Round(value);
	}

	private sealed class GridEntry
	{
		public bool IsLong { get; set; }
		public decimal Volume { get; set; }
		public decimal EntryPrice { get; set; }
	}

	private readonly struct SideStatistics
	{
		public SideStatistics(int count, GridEntry? minEntry, GridEntry? maxEntry)
		{
			Count = count;
			MinEntry = minEntry;
			MaxEntry = maxEntry;
		}

		public int Count { get; }
		public GridEntry? MinEntry { get; }
		public GridEntry? MaxEntry { get; }
	}
}
