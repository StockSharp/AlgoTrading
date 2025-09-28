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
/// Port of the "SwingTrader" MetaTrader expert advisor that trades Bollinger Band reversals
/// and builds a martingale-style averaging grid with adaptive profit and loss liquidation targets.
/// </summary>
public class SwingTraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitFactor;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<GridEntry> _longEntries = new();
	private readonly List<GridEntry> _shortEntries = new();

	private BollingerSnapshot? _previousBands;
	private CandleSnapshot? _previousCandle;

	private bool _upTouch;
	private bool _downTouch;
	private decimal? _savedPrice;
	private decimal _gridWidth;
	private int _martingaleStep;

	/// <summary>
	/// Profit factor applied to the invested capital in order to close all positions.
	/// </summary>
	public decimal TakeProfitFactor
	{
		get => _takeProfitFactor.Value;
		set => _takeProfitFactor.Value = value;
	}

	/// <summary>
	/// Volume multiplier that grows every new grid order.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Bollinger Bands averaging period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Base order volume used for the first grid trade.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Candle type used to drive the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SwingTraderStrategy"/> class.
	/// </summary>
	public SwingTraderStrategy()
	{
		_takeProfitFactor = Param(nameof(TakeProfitFactor), 0.05m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit Factor", "Multiplier applied to invested capital when closing the basket", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 0.2m, 0.01m);

		_multiplier = Param(nameof(Multiplier), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Volume Multiplier", "Scaling factor for every new averaging order", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1.1m, 3m, 0.1m);

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Period", "Number of candles used in the Bollinger Bands", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 5);

		_initialVolume = Param(nameof(InitialVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Initial Volume", "Volume of the first grid order", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 5m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Candle aggregation used by the strategy", "Trading");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = 2m
		};

		SubscribeCandles(CandleType)
		.Bind(bollinger, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var currentBands = new BollingerSnapshot(middleBand, upperBand, lowerBand);
		var currentCandle = new CandleSnapshot(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice);

		if (_previousBands is null || _previousCandle is null)
		{
		_previousBands = currentBands;
		_previousCandle = currentCandle;
		return;
		}

		var prevBands = _previousBands.Value;
		var prevCandle = _previousCandle.Value;
		var bandWidth = prevBands.Upper - prevBands.Lower;

		UpdateTouchState(prevCandle, prevBands);

		var buySignal = _downTouch && currentCandle.Close > prevBands.Middle && prevCandle.Open < prevBands.Middle;
		var sellSignal = _upTouch && currentCandle.Close < prevBands.Middle && prevCandle.Open > prevBands.Middle;

		if (bandWidth > 0m && buySignal && _longEntries.Count == 0 && _shortEntries.Count == 0)
		{
		OpenInitialPosition(currentCandle.Close, bandWidth, true);
		}
		else if (bandWidth > 0m && sellSignal && _longEntries.Count == 0 && _shortEntries.Count == 0)
		{
		OpenInitialPosition(currentCandle.Close, bandWidth, false);
		}
		else
		{
		if (_longEntries.Count > 0)
		HandleAveraging(currentCandle, true);
		else if (_shortEntries.Count > 0)
		HandleAveraging(currentCandle, false);
		}

		if (_longEntries.Count > 0)
		CheckExitConditions(currentCandle.Close, true);
		else if (_shortEntries.Count > 0)
		CheckExitConditions(currentCandle.Close, false);

		_previousBands = currentBands;
		_previousCandle = currentCandle;
	}

	private void OpenInitialPosition(decimal price, decimal bandWidth, bool isLong)
	{
	var volume = AdjustVolume(InitialVolume);
	if (volume <= 0m)
	return;

	if (isLong)
	{
	BuyMarket(volume);
	_longEntries.Add(new GridEntry(price, volume));
	}
	else
	{
	SellMarket(volume);
	_shortEntries.Add(new GridEntry(price, volume));
	}

	_savedPrice = price;
	_gridWidth = bandWidth;
	_martingaleStep = 1;
	}

	private void UpdateTouchState(CandleSnapshot candle, BollingerSnapshot bands)
	{
	if (!_upTouch && !_downTouch)
	{
	_upTouch = candle.High > bands.Upper;
	_downTouch = candle.Low < bands.Lower;
	}

	if (_upTouch)
	{
	_downTouch = candle.Low < bands.Lower;
	_upTouch = !_downTouch;
	}

	if (_downTouch)
	{
	_upTouch = candle.High > bands.Upper;
	_downTouch = !_upTouch;
	}
	}

	private void HandleAveraging(CandleSnapshot candle, bool isLong)
	{
	if (_savedPrice is null || _gridWidth <= 0m)
	return;

	var entries = isLong ? _longEntries : _shortEntries;

	var threshold = isLong
	? _savedPrice.Value - _martingaleStep * _gridWidth
	: _savedPrice.Value + _martingaleStep * _gridWidth;

	var shouldAdd = isLong ? candle.Low <= threshold : candle.High >= threshold;
	if (!shouldAdd)
	return;

	var nextVolume = AdjustVolume(CalculateNextVolume(_martingaleStep + 1));
	if (nextVolume <= 0m)
	return;

	if (isLong)
	{
	BuyMarket(nextVolume);
	entries.Add(new GridEntry(candle.Close, nextVolume));
	}
	else
	{
	SellMarket(nextVolume);
	entries.Add(new GridEntry(candle.Close, nextVolume));
	}

	_martingaleStep++;
	}

	private void CheckExitConditions(decimal price, bool isLong)
	{
	if (_savedPrice is null)
	return;

	var entries = isLong ? _longEntries : _shortEntries;
	var profit = CalculateUnrealizedProfit(price, entries, isLong);
	var invested = CalculateInvestedCapital(entries, _savedPrice.Value);

	if (invested <= 0m)
	return;

	var target = invested * TakeProfitFactor;
	var stop = invested * TakeProfitFactor * 10m;

	if (profit >= target)
	{
	CloseAll(isLong);
	}
	else if (profit <= -stop)
	{
	CloseAll(isLong);
	}
	}

	private void CloseAll(bool isLong)
	{
	var entries = isLong ? _longEntries : _shortEntries;
	var total = GetTotalVolume(entries);
	if (total <= 0m)
	{
	ResetGridState();
	return;
	}

	if (isLong)
	SellMarket(total);
	else
	BuyMarket(total);

	entries.Clear();
	ResetGridState();
	}

	private void ResetGridState()
	{
	_savedPrice = null;
	_gridWidth = 0m;
	_martingaleStep = 0;
	_upTouch = false;
	_downTouch = false;
	}

	private decimal CalculateNextVolume(int step)
	{
	var volume = InitialVolume;
	for (var i = 1; i < step; i++)
	volume *= Multiplier;
	return volume;
	}

	private decimal AdjustVolume(decimal volume)
	{
	var result = volume;

	var minVolume = Security.MinVolume ?? 0m;
	var maxVolume = Security.MaxVolume;
	var step = Security.VolumeStep ?? 0m;

	if (result < minVolume)
	result = minVolume;

	if (maxVolume.HasValue && result > maxVolume.Value)
	result = maxVolume.Value;

	if (step > 0m)
	{
	var steps = Math.Round(result / step, MidpointRounding.AwayFromZero);
	result = steps * step;
	}

	return result;
	}

	private decimal CalculateUnrealizedProfit(decimal price, List<GridEntry> entries, bool isLong)
	{
	if (entries.Count == 0)
	return 0m;

	var priceStep = Security.PriceStep ?? 0.0001m;
	var stepPrice = Security.StepPrice ?? 1m;

	decimal total = 0m;
	for (var i = 0; i < entries.Count; i++)
	{
	var entry = entries[i];
	var distance = isLong ? price - entry.Price : entry.Price - price;
	total += (distance / priceStep) * stepPrice * entry.Volume;
	}

	return total;
	}

	private decimal CalculateInvestedCapital(List<GridEntry> entries, decimal basePrice)
	{
	if (entries.Count == 0)
	return 0m;

	var priceStep = Security.PriceStep ?? 0.0001m;
	var stepPrice = Security.StepPrice ?? 1m;
	var totalVolume = GetTotalVolume(entries);

	return (basePrice / priceStep) * stepPrice * totalVolume / 30m;
	}

	private static decimal GetTotalVolume(List<GridEntry> entries)
	{
	decimal total = 0m;
	for (var i = 0; i < entries.Count; i++)
	total += entries[i].Volume;
	return total;
	}

	private readonly struct BollingerSnapshot
	{
	public BollingerSnapshot(decimal middle, decimal upper, decimal lower)
	{
	Middle = middle;
	Upper = upper;
	Lower = lower;
	}

	public decimal Middle { get; }

	public decimal Upper { get; }

	public decimal Lower { get; }
	}

	private readonly struct CandleSnapshot
	{
	public CandleSnapshot(decimal open, decimal high, decimal low, decimal close)
	{
	Open = open;
	High = high;
	Low = low;
	Close = close;
	}

	public decimal Open { get; }

	public decimal High { get; }

	public decimal Low { get; }

	public decimal Close { get; }
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

