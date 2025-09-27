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
/// Stochastic based martingale averaging strategy translated from the MetaTrader expert "rmkp_9yj4qp1gn8fucubyqnvb".
/// Adds averaging orders when price moves against the latest entry and manages each leg with trailing stops and individual take profits.
/// </summary>
public class StochasticMartingaleGridStrategy : Strategy
{
	private sealed class Entry
	{
		public decimal Price { get; set; }
		public decimal Volume { get; set; }
		public decimal? TrailingPrice { get; set; }
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<decimal> _stepPips;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<decimal> _zoneBuy;
	private readonly StrategyParam<decimal> _zoneSell;

	private readonly List<Entry> _entries = new();

	private StochasticOscillator _stochastic = null!;
	private decimal? _previousMain;
	private decimal? _previousSignal;
	private decimal _pipSize;
	private decimal _lastEntryVolume;
	private decimal _lastEntryPrice;
	private Sides? _currentSide;

	/// <summary>
	/// Initializes a new instance of the <see cref="StochasticMartingaleGridStrategy"/> class.
	/// </summary>
	public StochasticMartingaleGridStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to evaluate stochastic values", "General");

		_baseVolume = Param(nameof(BaseVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Initial order volume", "Trading")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Distance to the take profit target for each entry", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance applied per entry", "Risk")
			.SetCanOptimize(true);

		_maxOrders = Param(nameof(MaxOrders), 7)
			.SetGreaterThanZero()
			.SetDisplay("Max Orders", "Maximum number of simultaneous averaging entries", "Martingale");

		_stepPips = Param(nameof(StepPips), 7m)
			.SetGreaterThanZero()
			.SetDisplay("Step (pips)", "Adverse move required before adding a new entry", "Martingale")
			.SetCanOptimize(true);

		_kPeriod = Param(nameof(KPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("%K Period", "Stochastic %K lookback length", "Indicators")
			.SetCanOptimize(true);

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("%D Period", "Stochastic %D smoothing length", "Indicators")
			.SetCanOptimize(true);

		_slowing = Param(nameof(Slowing), 3)
			.SetGreaterThanZero()
			.SetDisplay("Slowing", "Additional smoothing applied to %K", "Indicators")
			.SetCanOptimize(true);

		_zoneBuy = Param(nameof(ZoneBuy), 30m)
			.SetDisplay("Buy Zone", "Upper limit that allows long setups when %K is above %D", "Indicators")
			.SetCanOptimize(true);

		_zoneSell = Param(nameof(ZoneSell), 70m)
			.SetDisplay("Sell Zone", "Lower limit that allows short setups when %K is below %D", "Indicators")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initial trade volume.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips applied to every entry.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips applied to every entry.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Maximum number of averaging entries.
	/// </summary>
	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	/// <summary>
	/// Step in pips required to trigger a new averaging entry.
	/// </summary>
	public decimal StepPips
	{
		get => _stepPips.Value;
		set => _stepPips.Value = value;
	}

	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D period.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic slowing period.
	/// </summary>
	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}

	/// <summary>
	/// Maximum signal level that allows long entries.
	/// </summary>
	public decimal ZoneBuy
	{
		get => _zoneBuy.Value;
		set => _zoneBuy.Value = value;
	}

	/// <summary>
	/// Minimum signal level that allows short entries.
	/// </summary>
	public decimal ZoneSell
	{
		get => _zoneSell.Value;
		set => _zoneSell.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entries.Clear();
		_previousMain = null;
		_previousSignal = null;
		_pipSize = 0m;
		_lastEntryPrice = 0m;
		_lastEntryVolume = 0m;
		_currentSide = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		_stochastic = new StochasticOscillator
		{
			Length = KPeriod,
			K = { Length = Slowing },
			D = { Length = DPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (stochasticValue is not StochasticOscillatorValue stoch)
		return;

		if (stoch.K is not decimal currentMain || stoch.D is not decimal currentSignal)
		return;

		if (!_stochastic.IsFormed)
		{
		// Store the preliminary stochastic readings until the indicator is fully formed.
		_previousMain = currentMain;
		_previousSignal = currentSignal;
		return;
		}

		var tradingAllowed = IsFormedAndOnlineAndAllowTrading();

		if (_currentSide == Sides.Buy)
		{
		// Manage existing long-side averaging orders.
		ManageLongEntries(candle, tradingAllowed);
		}
		else if (_currentSide == Sides.Sell)
		{
		// Manage existing short-side averaging orders.
		ManageShortEntries(candle, tradingAllowed);
		}

		if (!tradingAllowed)
		{
		_previousMain = currentMain;
		_previousSignal = currentSignal;
		return;
		}

		if (_entries.Count == 0 && Position == 0m && _previousMain is decimal prevMain && _previousSignal is decimal prevSignal)
		{
		if (prevMain > prevSignal && prevSignal < ZoneBuy)
		{
		// Signal line exited the oversold zone while the main line is above it: open long cluster.
		OpenLong(candle.ClosePrice);
		}
		else if (prevMain < prevSignal && prevSignal > ZoneSell)
		{
		// Signal line exited the overbought zone while the main line is below it: open short cluster.
		OpenShort(candle.ClosePrice);
		}
		}

		_previousMain = currentMain;
		_previousSignal = currentSignal;
	}

	private void ManageLongEntries(ICandleMessage candle, bool tradingAllowed)
	{
		if (_entries.Count == 0)
		{
		ResetState();
		return;
		}

		var takeProfitDistance = ConvertPipsToPrice(TakeProfitPips);
		var trailingDistance = ConvertPipsToPrice(TrailingStopPips);
		var stepDistance = ConvertPipsToPrice(StepPips);

		if (tradingAllowed && stepDistance > 0m && _entries.Count < MaxOrders && _lastEntryVolume > 0m)
		{
		var triggerPrice = _lastEntryPrice - stepDistance;
		if (candle.LowPrice <= triggerPrice)
		{
		var desiredVolume = _lastEntryVolume * 2m;
		var nextVolume = PrepareVolume(desiredVolume);
		if (nextVolume > 0m)
		{
		// Double the volume and add a new averaging order below the latest long entry.
		var executionPrice = Math.Min(triggerPrice, candle.LowPrice);
		BuyMarket(nextVolume);

		var entry = new Entry
		{
		Price = executionPrice,
		Volume = nextVolume
		};

		_entries.Add(entry);
		_lastEntryPrice = entry.Price;
		_lastEntryVolume = entry.Volume;
		}
		}
		}

		foreach (var entry in _entries)
		{
		if (entry.Volume <= 0m)
		continue;

		if (takeProfitDistance > 0m)
		{
		var target = entry.Price + takeProfitDistance;
		if (candle.HighPrice >= target)
		{
		// Exit the current leg once its individual take profit level is reached.
		SellMarket(entry.Volume);
		entry.Volume = 0m;
		entry.TrailingPrice = null;
		continue;
		}
		}

		if (trailingDistance > 0m)
		{
		var profit = candle.ClosePrice - entry.Price;
		if (profit >= trailingDistance)
		{
		// Update the trailing stop anchor when the profit exceeds the threshold.
		var candidate = candle.ClosePrice - trailingDistance;
		entry.TrailingPrice = entry.TrailingPrice.HasValue
		? Math.Max(entry.TrailingPrice.Value, candidate)
		: candidate;
		}

		if (entry.TrailingPrice is decimal trailing && candle.LowPrice <= trailing)
		{
		// Price retraced back to the trailing stop level: exit the leg to secure profits.
		SellMarket(entry.Volume);
		entry.Volume = 0m;
		entry.TrailingPrice = null;
		}
		}
		}

		_entries.RemoveAll(e => e.Volume <= 0m);

		UpdateLastEntry();

		if (_entries.Count == 0)
		ResetState();
	}

	private void ManageShortEntries(ICandleMessage candle, bool tradingAllowed)
	{
		if (_entries.Count == 0)
		{
		ResetState();
		return;
		}

		var takeProfitDistance = ConvertPipsToPrice(TakeProfitPips);
		var trailingDistance = ConvertPipsToPrice(TrailingStopPips);
		var stepDistance = ConvertPipsToPrice(StepPips);

		if (tradingAllowed && stepDistance > 0m && _entries.Count < MaxOrders && _lastEntryVolume > 0m)
		{
		var triggerPrice = _lastEntryPrice + stepDistance;
		if (candle.HighPrice >= triggerPrice)
		{
		var desiredVolume = _lastEntryVolume * 2m;
		var nextVolume = PrepareVolume(desiredVolume);
		if (nextVolume > 0m)
		{
		// Double the volume and add a new averaging order above the latest short entry.
		var executionPrice = Math.Max(triggerPrice, candle.HighPrice);
		SellMarket(nextVolume);

		var entry = new Entry
		{
		Price = executionPrice,
		Volume = nextVolume
		};

		_entries.Add(entry);
		_lastEntryPrice = entry.Price;
		_lastEntryVolume = entry.Volume;
		}
		}
		}

		foreach (var entry in _entries)
		{
		if (entry.Volume <= 0m)
		continue;

		if (takeProfitDistance > 0m)
		{
		var target = entry.Price - takeProfitDistance;
		if (candle.LowPrice <= target)
		{
		// Exit the current leg once its individual take profit level is reached.
		BuyMarket(entry.Volume);
		entry.Volume = 0m;
		entry.TrailingPrice = null;
		continue;
		}
		}

		if (trailingDistance > 0m)
		{
		var profit = entry.Price - candle.ClosePrice;
		if (profit >= trailingDistance)
		{
		// Update the trailing stop anchor when the profit exceeds the threshold.
		var candidate = candle.ClosePrice + trailingDistance;
		entry.TrailingPrice = entry.TrailingPrice.HasValue
		? Math.Min(entry.TrailingPrice.Value, candidate)
		: candidate;
		}

		if (entry.TrailingPrice is decimal trailing && candle.HighPrice >= trailing)
		{
		// Price retraced back to the trailing stop level: exit the leg to secure profits.
		BuyMarket(entry.Volume);
		entry.Volume = 0m;
		entry.TrailingPrice = null;
		}
		}
		}

		_entries.RemoveAll(e => e.Volume <= 0m);

		UpdateLastEntry();

		if (_entries.Count == 0)
		ResetState();
	}

	private void OpenLong(decimal price)
	{
		var volume = PrepareVolume(BaseVolume);
		if (volume <= 0m)
		return;

		BuyMarket(volume);

		_entries.Clear();
		_entries.Add(new Entry
		{
		Price = price,
		Volume = volume
		});

		_lastEntryPrice = price;
		_lastEntryVolume = volume;
		_currentSide = Sides.Buy;
	}

	private void OpenShort(decimal price)
	{
		var volume = PrepareVolume(BaseVolume);
		if (volume <= 0m)
		return;

		SellMarket(volume);

		_entries.Clear();
		_entries.Add(new Entry
		{
		Price = price,
		Volume = volume
		});

		_lastEntryPrice = price;
		_lastEntryVolume = volume;
		_currentSide = Sides.Sell;
	}

	private void ResetState()
	{
		_entries.Clear();
		_lastEntryPrice = 0m;
		_lastEntryVolume = 0m;
		_currentSide = null;
	}

	private void UpdateLastEntry()
	{
		if (_entries.Count == 0)
		{
		_lastEntryPrice = 0m;
		_lastEntryVolume = 0m;
		return;
		}

		var last = _entries[^1];
		_lastEntryPrice = last.Price;
		_lastEntryVolume = last.Volume;
	}

	private decimal PrepareVolume(decimal requestedVolume)
	{
		if (requestedVolume <= 0m)
		return 0m;

		var normalized = NormalizeVolume(requestedVolume);
		if (normalized <= 0m)
		return 0m;

		var maxVolume = GetMaxVolumeLimit();
		if (normalized > maxVolume)
		normalized = NormalizeVolume(maxVolume);

		return normalized;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
		return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		volume = step * Math.Round(volume / step, MidpointRounding.AwayFromZero);

		var min = security.VolumeMin ?? 0m;
		if (min > 0m && volume < min)
		return 0m;

		var max = security.VolumeMax ?? decimal.MaxValue;
		if (volume > max)
		volume = max;

		return volume;
	}

	private decimal GetMaxVolumeLimit()
	{
		var security = Security;
		if (security?.VolumeMax is decimal max && max > 0m)
		return max;

		return decimal.MaxValue;
	}

	private decimal ConvertPipsToPrice(decimal pips)
	{
		if (pips <= 0m || _pipSize <= 0m)
		return 0m;

		return pips * _pipSize;
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
}

