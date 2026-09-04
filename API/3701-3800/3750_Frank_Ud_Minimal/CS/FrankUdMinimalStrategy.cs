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
/// Minimal port of the Frank Ud averaging expert from MetaTrader.
/// The strategy opens hedged martingale grids and liquidates both sides
/// once the newest position reaches the configured profit in pips.
/// </summary>
public class FrankUdMinimalStrategy : Strategy
{
	// Forex convention this expert came from: one pip is roughly a ten-thousandth of the quoted
	// price (0.0001 on EURUSD at 1.10, 0.01 on USDJPY at 150). Expressing it as a fraction of the
	// price keeps the same grid spacing on instruments quoted in five figures.
	private const decimal _pipFraction = 0.0001m;

	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _reEntryPips;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _minimumFreeMarginRatio;
	private readonly StrategyParam<decimal> _extraTakeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<PositionEntry> _longEntries = new();
	private readonly List<PositionEntry> _shortEntries = new();

	private decimal _pointValue;
	private decimal _takeProfitThreshold;
	private decimal _takeProfitDistance;
	private decimal _reEntryDistance;
	private decimal _baseVolume;
	private decimal _lastBid;
	private decimal _lastAsk;

	/// <summary>
	/// Creates a new instance of <see cref="FrankUdMinimalStrategy"/> with default parameters.
	/// </summary>
	public FrankUdMinimalStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 65m)
		.SetDisplay("Profit trigger (pips)", "Pip profit that forces an exit of all positions.", "Risk")
		.SetGreaterThanZero();

		_reEntryPips = Param(nameof(ReEntryPips), 41m)
		.SetDisplay("Re-entry distance (pips)", "Pip distance required before adding the next grid order.", "Grid")
		.SetGreaterThanZero();

		_initialVolume = Param(nameof(InitialVolume), 0.1m)
		.SetDisplay("Initial volume", "Base lot used for the very first order.", "Risk")
		.SetGreaterThanZero();

		_minimumFreeMarginRatio = Param(nameof(MinimumFreeMarginRatio), 0.5m)
		.SetDisplay("Free margin ratio", "Free margin must stay above Balance × Ratio before adding orders.", "Risk")
		.SetNotNegative();

		_extraTakeProfitPips = Param(nameof(ExtraTakeProfitPips), 25m)
		.SetDisplay("Buffer profit (pips)", "Additional pip distance applied when calculating buffered targets.", "Risk")
		.SetNotNegative();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle type", "Candle series the grid reacts to.", "General");
}

	/// <summary>
	/// Profit threshold expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Distance in pips between consecutive martingale entries.
	/// </summary>
	public decimal ReEntryPips
	{
		get => _reEntryPips.Value;
		set => _reEntryPips.Value = value;
	}

	/// <summary>
	/// Base lot volume for the very first order.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Minimal free margin ratio required to send new orders.
	/// </summary>
	public decimal MinimumFreeMarginRatio
{
		get => _minimumFreeMarginRatio.Value;
		set => _minimumFreeMarginRatio.Value = value;
}

	/// <summary>
	/// Additional pip buffer added to the take-profit distance.
	/// </summary>
	public decimal ExtraTakeProfitPips
	{
		get => _extraTakeProfitPips.Value;
		set => _extraTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Candle series the grid reacts to.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longEntries.Clear();
		_shortEntries.Clear();

		_pointValue = 0m;
		_takeProfitThreshold = 0m;
		_takeProfitDistance = 0m;
		_reEntryDistance = 0m;
		_baseVolume = 0m;
		_lastBid = 0m;
		_lastAsk = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// The pip and the distances derived from it need a quote, so they are set up on the first one.
		_takeProfitThreshold = TakeProfitPips;
		_baseVolume = AdjustVolume(InitialVolume);

		SubscribeCandles(CandleType)
		.Bind(ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// The bundled history carries no book, so the close stands for both sides of the quote.
		_lastBid = candle.ClosePrice;
		_lastAsk = candle.ClosePrice;

		if (_lastBid <= 0m || _lastAsk <= 0m)
		return;

		if (!TryInitializePip())
		return;

		if (ShouldCloseLong())
		CloseLongPositions();

		if (ShouldCloseShort())
		CloseShortPositions();

		if (ShouldOpenLong())
		OpenLongPosition();

		if (ShouldOpenShort())
		OpenShortPosition();
	}

	private bool TryInitializePip()
	{
		if (_pointValue > 0m)
		return true;

		var reference = (_lastBid + _lastAsk) / 2m;
		if (reference <= 0m)
		return false;

		// A missing or zero price step simply leaves the pip unfloored; the fraction alone already
		// keeps it positive.
		var floor = Security?.PriceStep is decimal step && step > 0m ? step : 0m;

		// Frozen for the rest of the run: a pip that followed the price would move the grid under itself.
		_pointValue = Math.Max(reference * _pipFraction, floor);
		_takeProfitDistance = (TakeProfitPips + ExtraTakeProfitPips) * _pointValue;
		_reEntryDistance = ReEntryPips * _pointValue;

		return true;
	}

	private bool ShouldCloseLong()
	{
		if (_longEntries.Count == 0)
		return false;

		var entry = GetMaxVolumeEntry(_longEntries);
		if (entry == null)
		return false;

		var profitPips = (_lastBid - entry.Price) / _pointValue;
		var bufferedTarget = entry.Price + _takeProfitDistance;
		var reachedBufferedTarget = _takeProfitDistance > 0m && _lastBid >= bufferedTarget;

		return profitPips > _takeProfitThreshold || reachedBufferedTarget;
	}

	private bool ShouldCloseShort()
	{
		if (_shortEntries.Count == 0)
		return false;

		var entry = GetMaxVolumeEntry(_shortEntries);
		if (entry == null)
		return false;

		var profitPips = (entry.Price - _lastAsk) / _pointValue;
		var bufferedTarget = entry.Price - _takeProfitDistance;
		var reachedBufferedTarget = _takeProfitDistance > 0m && _lastAsk <= bufferedTarget;

		return profitPips > _takeProfitThreshold || reachedBufferedTarget;
	}

	private bool ShouldOpenLong()
	{
		if (_baseVolume <= 0m)
		return false;

		if (!HasEnoughMargin())
		return false;

		if (_longEntries.Count == 0)
		return true;

		var lowestPrice = GetExtremePrice(_longEntries, true);
		return lowestPrice - _reEntryDistance > _lastAsk;
	}

	private bool ShouldOpenShort()
	{
		if (_baseVolume <= 0m)
		return false;

		if (!HasEnoughMargin())
		return false;

		if (_shortEntries.Count == 0)
		return true;

		var highestPrice = GetExtremePrice(_shortEntries, false);
		return highestPrice + _reEntryDistance < _lastBid;
	}

	private void OpenLongPosition()
	{
		var volume = DetermineNextVolume(_longEntries);
		if (volume <= 0m)
		return;

		BuyMarket(volume);
		AddEntry(_longEntries, _lastAsk, volume);
	}

	private void OpenShortPosition()
	{
		var volume = DetermineNextVolume(_shortEntries);
		if (volume <= 0m)
		return;

		SellMarket(volume);
		AddEntry(_shortEntries, _lastBid, volume);
	}

	private void CloseLongPositions()
	{
		var volume = GetTotalVolume(_longEntries);
		if (volume <= 0m)
		return;

		SellMarket(volume);
		_longEntries.Clear();
	}

	private void CloseShortPositions()
	{
		var volume = GetTotalVolume(_shortEntries);
		if (volume <= 0m)
		return;

		BuyMarket(volume);
		_shortEntries.Clear();
	}

	private decimal DetermineNextVolume(List<PositionEntry> entries)
	{
		if (_baseVolume <= 0m)
		return 0m;

		var volume = entries.Count == 0
		? _baseVolume
		: GetMaxVolume(entries) * 2m;

		return AdjustVolume(volume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var security = Security;

		if (security?.VolumeStep is decimal step && step > 0m)
		{
			var steps = Math.Floor(volume / step);
			volume = steps * step;
		}

		if (security?.MinVolume is decimal min && min > 0m && volume < min)
		volume = min;

		if (security?.MaxVolume is decimal max && max > 0m && volume > max)
		volume = max;

		return volume;
	}

	private bool HasEnoughMargin()
	{
		if (MinimumFreeMarginRatio <= 0m)
		return true;

		var portfolio = Portfolio;
		if (portfolio == null)
		return true;

		var balance = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
		if (balance <= 0m)
		return true;

		var blocked = portfolio.Commission ?? 0m;
		var baseValue = portfolio.CurrentValue ?? portfolio.BeginValue;
		if (baseValue == null)
		return true;

		var freeMargin = baseValue.Value - blocked;
		return freeMargin > balance * MinimumFreeMarginRatio;
	}

	private static void AddEntry(List<PositionEntry> entries, decimal price, decimal volume)
	{
		if (volume <= 0m)
		return;

		entries.Add(new PositionEntry(price, volume));
	}

	private static decimal GetTotalVolume(List<PositionEntry> entries)
	{
		decimal total = 0m;

		foreach (var entry in entries)
		total += entry.Volume;

		return total;
	}

	private static PositionEntry GetMaxVolumeEntry(List<PositionEntry> entries)
	{
		PositionEntry result = null;
		decimal maxVolume = 0m;

		foreach (var entry in entries)
		{
			if (entry.Volume > maxVolume)
			{
				maxVolume = entry.Volume;
				result = entry;
			}
		}

		return result;
	}

	private static decimal GetMaxVolume(List<PositionEntry> entries)
	{
		decimal maxVolume = 0m;

		foreach (var entry in entries)
		if (entry.Volume > maxVolume)
		maxVolume = entry.Volume;

		return maxVolume;
	}

	private static decimal GetExtremePrice(List<PositionEntry> entries, bool isLong)
	{
		var hasValue = false;
		decimal result = 0m;

		foreach (var entry in entries)
		{
			var price = entry.Price;

			if (!hasValue)
			{
				result = price;
				hasValue = true;
				continue;
			}

			if (isLong)
			{
				if (price < result)
				result = price;
			}
			else if (price > result)
			{
				result = price;
			}
		}

		return result;
	}

	private sealed class PositionEntry
	{
		public PositionEntry(decimal price, decimal volume)
		{
			Price = price;
			Volume = volume;
		}

		public decimal Price { get; }

		public decimal Volume { get; }
	}
}
