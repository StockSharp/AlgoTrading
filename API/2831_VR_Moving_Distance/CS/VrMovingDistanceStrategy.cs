using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VR Moving strategy converted from MetaTrader 5 expert advisor.
/// Opens positions when price deviates from a moving average by a configurable distance and scales using a multiplier.
/// </summary>
public class VrMovingDistanceStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<MovingAverageTypeEnum> _maType;
	private readonly StrategyParam<CandlePrice> _priceSource;
	private readonly StrategyParam<decimal> _distancePips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<decimal> _baseVolume;

	private LengthIndicator<decimal> _movingAverage = null!;
	private decimal _pipSize;

	private int _longEntries;
	private int _shortEntries;
	private decimal _longHighestEntry;
	private decimal _shortLowestEntry;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	/// <summary>
	/// Candle type used for the moving average and decision logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Moving average smoothing type.
	/// </summary>
	public MovingAverageTypeEnum MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Candle price source for the moving average.
	/// </summary>
	public CandlePrice PriceSource
	{
		get => _priceSource.Value;
		set => _priceSource.Value = value;
	}

	/// <summary>
	/// Distance from the moving average in pips.
	/// </summary>
	public decimal DistancePips
	{
		get => _distancePips.Value;
		set => _distancePips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips when a single position is active.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Volume multiplier for additional entries in the same direction.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Base order volume.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public VrMovingDistanceStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculations", "General");

		_maLength = Param(nameof(MaLength), 60)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average period", "Moving Average")
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 10);

		_maType = Param(nameof(MaType), MovingAverageTypeEnum.Exponential)
			.SetDisplay("MA Type", "Moving average smoothing method", "Moving Average");

		_priceSource = Param(nameof(PriceSource), CandlePrice.Close)
			.SetDisplay("Price Source", "Price used for the moving average", "Moving Average");

		_distancePips = Param(nameof(DistancePips), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Distance (pips)", "Offset from the moving average", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(10m, 150m, 10m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetGreaterThanOrEqualsZero()
			.SetDisplay("Take Profit (pips)", "Exit distance when only one position is open", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(10m, 150m, 10m);

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Multiplier", "Multiplier for additional entries", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.25m);

		_baseVolume = Param(nameof(BaseVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Volume of the initial order", "Trading");
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

		UpdatePipSize();

		_movingAverage = CreateMovingAverage(MaType, MaLength, PriceSource);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_movingAverage, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _movingAverage);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_movingAverage.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var distance = DistancePips * _pipSize;
		var takeProfit = TakeProfitPips * _pipSize;

		var longTrigger = _longEntries == 0 ? maValue + distance : _longHighestEntry + distance;
		var shortTrigger = _shortEntries == 0 ? maValue - distance : _shortLowestEntry - distance;

		if (takeProfit > 0m)
		{
			// Close a single long position once the take profit level is reached.
			if (_longEntries == 1 && Position > 0 && _longEntryPrice.HasValue)
			{
				var target = _longEntryPrice.Value + takeProfit;
				if (candle.HighPrice >= target)
				{
					SellMarket(Position);
					ResetLongState();
				}
			}

			// Close a single short position once the take profit level is reached.
			if (_shortEntries == 1 && Position < 0 && _shortEntryPrice.HasValue)
			{
				var target = _shortEntryPrice.Value - takeProfit;
				if (candle.LowPrice <= target)
				{
					BuyMarket(Math.Abs(Position));
					ResetShortState();
				}
			}
		}

		var baseVolume = BaseVolume;
		if (baseVolume <= 0m)
			return;

		// Open or scale a long position when price moves sufficiently above the moving average.
		if (candle.HighPrice >= longTrigger)
		{
			ExecuteLongEntry(longTrigger);
		}
		// Open or scale a short position when price moves sufficiently below the moving average.
		else if (candle.LowPrice <= shortTrigger)
		{
			ExecuteShortEntry(shortTrigger);
		}
	}

	private void ExecuteLongEntry(decimal triggerPrice)
	{
		var volume = _longEntries == 0 ? BaseVolume : BaseVolume * VolumeMultiplier;
		if (volume <= 0m)
			return;

		var orderVolume = volume;

		// Reverse short exposure before adding new long volume.
		if (Position < 0)
		{
			orderVolume += Math.Abs(Position);
			ResetShortState();
		}

		BuyMarket(orderVolume);

		_longEntries++;
		_longHighestEntry = _longEntries == 1 ? triggerPrice : Math.Max(_longHighestEntry, triggerPrice);
		_longEntryPrice = _longEntries == 1 ? triggerPrice : null;
	}

	private void ExecuteShortEntry(decimal triggerPrice)
	{
		var volume = _shortEntries == 0 ? BaseVolume : BaseVolume * VolumeMultiplier;
		if (volume <= 0m)
			return;

		var orderVolume = volume;

		// Reverse long exposure before adding new short volume.
		if (Position > 0)
		{
			orderVolume += Position;
			ResetLongState();
		}

		SellMarket(orderVolume);

		_shortEntries++;
		_shortLowestEntry = _shortEntries == 1 ? triggerPrice : Math.Min(_shortLowestEntry, triggerPrice);
		_shortEntryPrice = _shortEntries == 1 ? triggerPrice : null;
	}

	private void ResetLongState()
	{
		_longEntries = 0;
		_longHighestEntry = 0m;
		_longEntryPrice = null;
	}

	private void ResetShortState()
	{
		_shortEntries = 0;
		_shortLowestEntry = 0m;
		_shortEntryPrice = null;
	}

	private void UpdatePipSize()
	{
		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var digits = GetDecimalDigits(step);
		_pipSize = (digits == 3 || digits == 5) ? step * 10m : step;
	}

	private static int GetDecimalDigits(decimal value)
	{
		value = Math.Abs(value);
		var digits = 0;
		while (value != Math.Truncate(value) && digits < 10)
		{
			value *= 10m;
			digits++;
		}

		return digits;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageTypeEnum type, int length, CandlePrice priceSource)
	{
		LengthIndicator<decimal> indicator = type switch
		{
			MovingAverageTypeEnum.Simple => new SimpleMovingAverage(),
			MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage(),
			MovingAverageTypeEnum.Smoothed => new SmoothedMovingAverage(),
			MovingAverageTypeEnum.Weighted => new WeightedMovingAverage(),
			MovingAverageTypeEnum.VolumeWeighted => new VolumeWeightedMovingAverage(),
			_ => new SimpleMovingAverage(),
		};

		indicator.Length = length;

		switch (indicator)
		{
			case SimpleMovingAverage sma:
				sma.CandlePrice = priceSource;
				break;
			case ExponentialMovingAverage ema:
				ema.CandlePrice = priceSource;
				break;
			case SmoothedMovingAverage smoothed:
				smoothed.CandlePrice = priceSource;
				break;
			case WeightedMovingAverage wma:
				wma.CandlePrice = priceSource;
				break;
			case VolumeWeightedMovingAverage vwma:
				vwma.CandlePrice = priceSource;
				break;
		}

		return indicator;
	}
}
