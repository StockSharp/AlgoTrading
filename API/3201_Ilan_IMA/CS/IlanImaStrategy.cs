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
/// Port of the "Ilan iMA" MetaTrader 5 expert advisor that combines a trend filter
/// based on a shifted moving average with a martingale-style averaging grid.
/// </summary>
public class IlanImaStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageMethod> _maMethod;
	private readonly StrategyParam<CandlePrice> _priceMode;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _startVolume;
	private readonly StrategyParam<decimal> _gridStepPips;
	private readonly StrategyParam<decimal> _lotExponent;
	private readonly StrategyParam<decimal> _profitMinimum;
	private readonly StrategyParam<decimal> _lotMaximum;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<GridEntry> _buyEntries = new();
	private readonly List<GridEntry> _sellEntries = new();
	private readonly List<decimal> _maValues = new();

	private decimal? _buyTrailingStop;
	private decimal? _sellTrailingStop;

	/// <summary>
	/// Averaging period of the moving average filter.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift of the moving average line in bars.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving-average smoothing method.
	/// </summary>
	public MovingAverageMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Candle price applied to the moving average.
	/// </summary>
	public CandlePrice PriceMode
	{
		get => _priceMode.Value;
		set => _priceMode.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips. Set to zero to disable.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips. Set to zero to disable.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Minimum profit in pips required to activate trailing.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Additional distance in pips before the trailing stop is moved again.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Base volume for the first order in a basket.
	/// </summary>
	public decimal StartVolume
	{
		get => _startVolume.Value;
		set => _startVolume.Value = value;
	}

	/// <summary>
	/// Grid spacing between averaging entries in pips.
	/// </summary>
	public decimal GridStepPips
	{
		get => _gridStepPips.Value;
		set => _gridStepPips.Value = value;
	}

	/// <summary>
	/// Volume multiplier applied to each additional order.
	/// </summary>
	public decimal LotExponent
	{
		get => _lotExponent.Value;
		set => _lotExponent.Value = value;
	}

	/// <summary>
	/// Minimum floating profit required to close the basket when both directions were open.
	/// </summary>
	public decimal ProfitMinimum
	{
		get => _profitMinimum.Value;
		set => _profitMinimum.Value = value;
	}

	/// <summary>
	/// Maximum order volume allowed by the strategy.
	/// </summary>
	public decimal LotMaximum
	{
		get => _lotMaximum.Value;
		set => _lotMaximum.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="IlanImaStrategy"/> class.
	/// </summary>
	public IlanImaStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Averaging period of the moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 5);

		_maShift = Param(nameof(MaShift), 5)
			.SetNotNegative()
			.SetDisplay("MA Shift", "Forward shift of the moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0, 10, 1);

		_maMethod = Param(nameof(MaMethod), MovingAverageMethod.Weighted)
			.SetDisplay("MA Method", "Moving average smoothing type", "Indicators");

		_priceMode = Param(nameof(PriceMode), CandlePrice.Weighted)
			.SetDisplay("Applied Price", "Candle price used by the moving average", "Indicators");

		_stopLossPips = Param(nameof(StopLossPips), 0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 100m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take-profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 15m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop", "Profit required to start trailing", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Step", "Extra distance before moving the trailing stop", "Risk");

		_startVolume = Param(nameof(StartVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Start Volume", "Base lot size for the first order", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_gridStepPips = Param(nameof(GridStepPips), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Grid Step", "Distance between averaging orders in pips", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(10m, 120m, 10m);

		_lotExponent = Param(nameof(LotExponent), 1.6m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Exponent", "Multiplier for subsequent orders", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1.1m, 2.5m, 0.1m);

		_profitMinimum = Param(nameof(ProfitMinimum), 15m)
			.SetNotNegative()
			.SetDisplay("Profit Minimum", "Profit target used when both baskets were open", "Trading");

		_lotMaximum = Param(nameof(LotMaximum), 15m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Maximum", "Upper limit for a single order", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for calculations", "General");
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
		_maValues.Clear();
		_buyTrailingStop = null;
		_sellTrailingStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var movingAverage = CreateMovingAverage(MaMethod, MaPeriod);
		movingAverage.CandlePrice = PriceMode;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(movingAverage, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, movingAverage, "Ilan iMA");
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_maValues.Add(maValue);

		var point = GetPointValue();
		var gridStep = GridStepPips * point;

		UpdateTrailing(candle.ClosePrice);
		CheckStopsAndTargets(candle.ClosePrice, point);

		var requiredHistory = MaShift + 4;
		if (_maValues.Count < requiredHistory)
			return;

		var ma0 = GetShiftedValue(MaShift);
		var ma1 = GetShiftedValue(MaShift + 1);
		var ma2 = GetShiftedValue(MaShift + 2);
		var ma3 = GetShiftedValue(MaShift + 3);

		var downTrend = ma0 < ma1 && ma1 < ma2 && ma2 < ma3;
		var upTrend = ma0 > ma1 && ma1 > ma2 && ma2 > ma3;

		if (_buyEntries.Count == 0 && _sellEntries.Count == 0)
		{
			if (downTrend && candle.ClosePrice > ma0)
			{
				OpenSell(candle.ClosePrice, StartVolume);
			}
			else if (upTrend && candle.ClosePrice < ma0)
			{
				OpenBuy(candle.ClosePrice, StartVolume);
			}

			return;
		}

		if (_buyEntries.Count > 0)
		{
			var lowestBuy = GetExtremePrice(_buyEntries, true);
			if (lowestBuy - candle.ClosePrice >= gridStep)
			{
				var nextVolume = CalculateNextVolume(_buyEntries);
				OpenBuy(candle.ClosePrice, nextVolume);
			}

			if (_buyEntries.Count > 1)
			{
				var profit = CalculateUnrealizedProfit(candle.ClosePrice, _buyEntries, true, point);
				if (profit >= ProfitMinimum)
					CloseBuys();
			}
		}

		if (_sellEntries.Count > 0)
		{
			var highestSell = GetExtremePrice(_sellEntries, false);
			if (candle.ClosePrice - highestSell >= gridStep)
			{
				var nextVolume = CalculateNextVolume(_sellEntries);
				OpenSell(candle.ClosePrice, nextVolume);
			}

			if (_sellEntries.Count > 1)
			{
				var profit = CalculateUnrealizedProfit(candle.ClosePrice, _sellEntries, false, point);
				if (profit >= ProfitMinimum)
					CloseSells();
			}
		}
	}

	private void CheckStopsAndTargets(decimal price, decimal point)
	{
		var stopDistance = StopLossPips * point;
		var takeDistance = TakeProfitPips * point;

		if (_buyEntries.Count > 0)
		{
			var avgBuy = GetAveragePrice(_buyEntries);
			if (StopLossPips > 0m && avgBuy - price >= stopDistance)
				CloseBuys();
			else if (TakeProfitPips > 0m && price - avgBuy >= takeDistance)
				CloseBuys();
		}
		else
		{
			_buyTrailingStop = null;
		}

		if (_sellEntries.Count > 0)
		{
			var avgSell = GetAveragePrice(_sellEntries);
			if (StopLossPips > 0m && price - avgSell >= stopDistance)
				CloseSells();
			else if (TakeProfitPips > 0m && avgSell - price >= takeDistance)
				CloseSells();
		}
		else
		{
			_sellTrailingStop = null;
		}
	}

	private void UpdateTrailing(decimal price)
	{
		if (TrailingStopPips <= 0m || TrailingStepPips <= 0m)
			return;

		var point = GetPointValue();
		var startDistance = TrailingStopPips * point;
		var stepDistance = TrailingStepPips * point;

		if (_buyEntries.Count > 0)
		{
			var avgBuy = GetAveragePrice(_buyEntries);
			if (price - avgBuy >= startDistance + stepDistance)
			{
				var desiredStop = price - startDistance;
				if (_buyTrailingStop == null || desiredStop - _buyTrailingStop.Value >= stepDistance)
					_buyTrailingStop = desiredStop;

				if (_buyTrailingStop != null && price <= _buyTrailingStop.Value)
					CloseBuys();
			}
		}
		else
		{
			_buyTrailingStop = null;
		}

		if (_sellEntries.Count > 0)
		{
			var avgSell = GetAveragePrice(_sellEntries);
			if (avgSell - price >= startDistance + stepDistance)
			{
				var desiredStop = price + startDistance;
				if (_sellTrailingStop == null || _sellTrailingStop.Value - desiredStop >= stepDistance)
					_sellTrailingStop = desiredStop;

				if (_sellTrailingStop != null && price >= _sellTrailingStop.Value)
					CloseSells();
			}
		}
		else
		{
			_sellTrailingStop = null;
		}
	}

	private void OpenBuy(decimal price, decimal volume)
	{
		var adjusted = AdjustVolume(volume);
		if (adjusted <= 0m)
			return;

		BuyMarket(adjusted);
		_buyEntries.Add(new GridEntry(price, adjusted));
	}

	private void OpenSell(decimal price, decimal volume)
	{
		var adjusted = AdjustVolume(volume);
		if (adjusted <= 0m)
			return;

		SellMarket(adjusted);
		_sellEntries.Add(new GridEntry(price, adjusted));
	}

	private void CloseBuys()
	{
		var total = GetTotalVolume(_buyEntries);
		if (total <= 0m)
			return;

		SellMarket(total);
		_buyEntries.Clear();
		_buyTrailingStop = null;
	}

	private void CloseSells()
	{
		var total = GetTotalVolume(_sellEntries);
		if (total <= 0m)
			return;

		BuyMarket(total);
		_sellEntries.Clear();
		_sellTrailingStop = null;
	}

	private decimal CalculateNextVolume(List<GridEntry> entries)
	{
		var lastVolume = entries.Count == 0 ? StartVolume : entries[^1].Volume;
		var proposed = lastVolume * LotExponent;
		if (proposed <= 0m)
			proposed = StartVolume;

		if (LotMaximum > 0m && proposed > LotMaximum)
			proposed = LotMaximum;

		return proposed;
	}

	private decimal AdjustVolume(decimal volume)
	{
		var result = volume;

		if (LotMaximum > 0m && result > LotMaximum)
			result = LotMaximum;

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

	private static decimal GetTotalVolume(List<GridEntry> entries)
	{
		decimal total = 0m;
		for (var i = 0; i < entries.Count; i++)
			total += entries[i].Volume;
		return total;
	}

	private static decimal GetAveragePrice(List<GridEntry> entries)
	{
		decimal volume = 0m;
		decimal weighted = 0m;
		for (var i = 0; i < entries.Count; i++)
		{
			volume += entries[i].Volume;
			weighted += entries[i].Price * entries[i].Volume;
		}

		return volume > 0m ? weighted / volume : 0m;
	}

	private static decimal GetExtremePrice(List<GridEntry> entries, bool isBuy)
	{
		if (entries.Count == 0)
			return 0m;

		var value = entries[0].Price;
		for (var i = 1; i < entries.Count; i++)
		{
			var price = entries[i].Price;
			if (isBuy)
			{
				if (price < value)
					value = price;
			}
			else
			{
				if (price > value)
					value = price;
			}
		}

		return value;
	}

	private decimal CalculateUnrealizedProfit(decimal price, List<GridEntry> entries, bool isBuy, decimal point)
	{
		if (entries.Count == 0)
			return 0m;

		var stepPrice = Security.StepPrice ?? 1m;
		var total = 0m;

		for (var i = 0; i < entries.Count; i++)
		{
			var entry = entries[i];
			var difference = isBuy ? price - entry.Price : entry.Price - price;
			var steps = difference / point;
			total += steps * stepPrice * entry.Volume;
		}

		return total;
	}

	private decimal GetShiftedValue(int offset)
	{
		var index = _maValues.Count - 1 - offset;
		return index >= 0 ? _maValues[index] : 0m;
	}

	private decimal GetPointValue()
	{
		var point = Security.PriceStep;
		if (point == null || point == 0m)
			return 0.0001m;
		return point.Value;
	}

	private LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethod method, int length)
	{
		return method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			_ => new WeightedMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Supported moving-average modes that mirror the MetaTrader options.
	/// </summary>
	public enum MovingAverageMethod
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted,
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

