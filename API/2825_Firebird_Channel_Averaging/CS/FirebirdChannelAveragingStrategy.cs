using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Firebird grid strategy that trades price deviations from a moving average channel
/// and averages into positions at configurable pip intervals.
/// </summary>
public class FirebirdChannelAveragingStrategy : Strategy
{
	/// <summary>
	/// Moving average calculation modes supported by the strategy.
	/// </summary>
	public enum MovingAverageTypeEnum
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential,

		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		Smoothed,

		/// <summary>
		/// Weighted moving average.
		/// </summary>
		Weighted
	}

	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageTypeEnum> _maType;
	private readonly StrategyParam<CandlePrice> _priceSource;
	private readonly StrategyParam<decimal> _pricePercent;
	private readonly StrategyParam<bool> _tradeOnFriday;
	private readonly StrategyParam<int> _stepPips;
	private readonly StrategyParam<decimal> _stepExponent;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverage _ma;
	private readonly Queue<decimal> _maHistory = new();
	private readonly List<PositionEntry> _entries = new();
	private bool? _isLong;
	private DateTimeOffset? _lastEntryTime;

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Moving average lookback period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the moving average in candles.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving average calculation mode.
	/// </summary>
	public MovingAverageTypeEnum MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Candle price source used for the moving average and signal checks.
	/// </summary>
	public CandlePrice PriceSource
	{
		get => _priceSource.Value;
		set => _priceSource.Value = value;
	}

	/// <summary>
	/// Channel width as percentage offset from the moving average.
	/// </summary>
	public decimal PricePercent
	{
		get => _pricePercent.Value;
		set => _pricePercent.Value = value;
	}

	/// <summary>
	/// Enables trading on Fridays.
	/// </summary>
	public bool TradeOnFriday
	{
		get => _tradeOnFriday.Value;
		set => _tradeOnFriday.Value = value;
	}

	/// <summary>
	/// Minimum distance between averaged entries expressed in pips.
	/// </summary>
	public int StepPips
	{
		get => _stepPips.Value;
		set => _stepPips.Value = value;
	}

	/// <summary>
	/// Exponent controlling how the averaging step grows with position count.
	/// </summary>
	public decimal StepExponent
	{
		get => _stepExponent.Value;
		set => _stepExponent.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="FirebirdChannelAveragingStrategy"/>.
	/// </summary>
	public FirebirdChannelAveragingStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
			SetGreaterThanZero()
			SetDisplay("Volume", "Order volume in lots", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 50)
			SetGreaterThanZero()
			SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk")
			SetCanOptimize(true)
			SetOptimize(20, 150, 10);

		_takeProfitPips = Param(nameof(TakeProfitPips), 150)
			SetGreaterThanZero()
			SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk")
			SetCanOptimize(true)
			SetOptimize(50, 300, 10);

		_maPeriod = Param(nameof(MaPeriod), 10)
			SetGreaterThanZero()
			SetDisplay("MA Period", "Moving average length", "Indicator")
			SetCanOptimize(true)
			SetOptimize(5, 30, 1);

		_maShift = Param(nameof(MaShift), 0)
			SetGreaterOrEqual(0)
			SetDisplay("MA Shift", "Forward shift for moving average", "Indicator");

		_maType = Param(nameof(MaType), MovingAverageTypeEnum.Exponential)
			SetDisplay("MA Type", "Moving average calculation mode", "Indicator");

		_priceSource = Param(nameof(PriceSource), CandlePrice.Close)
			SetDisplay("Price Source", "Candle price used for signals", "Data");

		_pricePercent = Param(nameof(PricePercent), 0.3m)
			SetGreaterThanZero()
			SetDisplay("Channel %", "Channel width percentage", "Indicator")
			SetCanOptimize(true)
			SetOptimize(0.1m, 1m, 0.1m);

		_tradeOnFriday = Param(nameof(TradeOnFriday), true)
			SetDisplay("Trade Friday", "Allow trading on Fridays", "Risk");

		_stepPips = Param(nameof(StepPips), 30)
			SetGreaterThanZero()
			SetDisplay("Step (pips)", "Distance between averaged entries", "Grid")
			SetCanOptimize(true)
			SetOptimize(10, 60, 5);

		_stepExponent = Param(nameof(StepExponent), 0m)
			SetGreaterOrEqualZero()
			SetDisplay("Step Exponent", "Power growth for step size", "Grid")
			SetCanOptimize(true)
			SetOptimize(0m, 2m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			SetDisplay("Candle Type", "Working timeframe", "Data");
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
		_maHistory.Clear();
		_isLong = null;
		_lastEntryTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = CreateMovingAverage(MaType);
		_ma.Length = MaPeriod;
		_ma.CandlePrice = PriceSource;

		var subscription = SubscribeCandles(CandleType);
		subscription
			Bind(_ma, ProcessCandle)
			Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		// Only work with closed candles to avoid intra-bar noise.
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		// Ensure the moving average has enough historical data.
		if (_ma == null || !_ma.IsFormed)
		{
			return;
		}

		var shiftedValue = ApplyShift(maValue);
		if (shiftedValue is null)
		{
			return;
		}

		var price = GetCandlePrice(candle);
		var ma = shiftedValue.Value;

		var lowerBand = ma * (1m - PricePercent / 100m);
		var upperBand = ma * (1m + PricePercent / 100m);

		var allowEntry = TradeOnFriday || candle.OpenTime.DayOfWeek != DayOfWeek.Friday;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			allowEntry = false;
		}

		var pipSize = GetPipSize();
		var baseStep = StepPips * pipSize;
		if (baseStep <= 0)
		{
			baseStep = pipSize;
		}

		var entriesCount = _entries.Count;
		var stepMultiplier = StepExponent <= 0m
			? 1m
			: (decimal)Math.Pow(Math.Max(entriesCount, 1), (double)StepExponent);
		var currentStep = baseStep * stepMultiplier;
		if (currentStep <= 0)
		{
			currentStep = baseStep;
		}

		var canOpenByTime = true;
		var timeFrame = GetTimeFrame();
		if (entriesCount > 0 && _lastEntryTime.HasValue && timeFrame != null)
		{
			var minDelay = timeFrame.Value + timeFrame.Value;
			canOpenByTime = candle.CloseTime - _lastEntryTime.Value >= minDelay;
		}

		if (allowEntry)
		{
			TryOpenLong(candle, price, lowerBand, currentStep, canOpenByTime);
			TryOpenShort(candle, price, upperBand, currentStep, canOpenByTime);
		}

		ManageOpenPositions(candle, price, pipSize);
	}

	private void TryOpenLong(ICandleMessage candle, decimal price, decimal lowerBand, decimal currentStep, bool canOpenByTime)
	{
		if (price >= lowerBand)
		{
			return;
		}

		if (_entries.Count > 0 && _isLong != true)
		{
			return;
		}

		if (_entries.Count > 0 && !canOpenByTime)
		{
			return;
		}

		if (_entries.Count > 0)
		{
			var lastEntry = _entries[_entries.Count - 1];
			if (price > lastEntry.Price - currentStep)
			{
				return;
			}
		}

		BuyMarket(Volume);

		var entry = new PositionEntry
		{
			Price = price,
			Time = candle.CloseTime
		};

		_entries.Add(entry);
		_isLong = true;
		_lastEntryTime = entry.Time;
	}

	private void TryOpenShort(ICandleMessage candle, decimal price, decimal upperBand, decimal currentStep, bool canOpenByTime)
	{
		if (price <= upperBand)
		{
			return;
		}

		if (_entries.Count > 0 && _isLong != false)
		{
			return;
		}

		if (_entries.Count > 0 && !canOpenByTime)
		{
			return;
		}

		if (_entries.Count > 0)
		{
			var lastEntry = _entries[_entries.Count - 1];
			if (price < lastEntry.Price + currentStep)
			{
				return;
			}
		}

		SellMarket(Volume);

		var entry = new PositionEntry
		{
			Price = price,
			Time = candle.CloseTime
		};

		_entries.Add(entry);
		_isLong = false;
		_lastEntryTime = entry.Time;
	}

	private void ManageOpenPositions(ICandleMessage candle, decimal price, decimal pipSize)
	{
		if (_entries.Count == 0)
		{
			return;
		}

		if (pipSize <= 0)
		{
			pipSize = 0.0001m;
		}

		var stopDistance = StopLossPips * pipSize;
		var takeDistance = TakeProfitPips * pipSize;

		decimal averagePrice = 0m;
		for (var i = 0; i < _entries.Count; i++)
		{
			averagePrice += _entries[i].Price;
		}
		averagePrice /= _entries.Count;

		if (_isLong == true)
		{
			var stopPrice = stopDistance > 0
			? averagePrice - (_entries.Count > 1 ? stopDistance / _entries.Count : stopDistance)
			: averagePrice;
			var takePrice = takeDistance > 0 ? averagePrice + takeDistance : decimal.MaxValue;

			if (price <= stopPrice)
			{
				CloseLongPositions();
				return;
			}

			if (price >= takePrice)
			{
				CloseLongPositions();
			}
		}
		else if (_isLong == false)
		{
			var stopPrice = stopDistance > 0
			? averagePrice + (_entries.Count > 1 ? stopDistance / _entries.Count : stopDistance)
			: averagePrice;
			var takePrice = takeDistance > 0 ? averagePrice - takeDistance : decimal.MinValue;

			if (price >= stopPrice)
			{
				CloseShortPositions();
				return;
			}

			if (price <= takePrice)
			{
				CloseShortPositions();
			}
		}
	}

	private void CloseLongPositions()
	{
		var volume = Position;
		if (volume > 0)
		{
			SellMarket(volume);
		}

		ResetEntries();
	}

	private void CloseShortPositions()
	{
		var volume = Math.Abs(Position);
		if (volume > 0)
		{
			BuyMarket(volume);
		}

		ResetEntries();
	}

	private void ResetEntries()
	{
		_entries.Clear();
		_isLong = null;
		_lastEntryTime = null;
	}

	private decimal? ApplyShift(decimal maValue)
	{
		var shift = MaShift;
		if (shift <= 0)
		{
			return maValue;
		}

		_maHistory.Enqueue(maValue);

		if (_maHistory.Count <= shift)
		{
			return null;
		}

		while (_maHistory.Count > shift + 1)
		{
			_maHistory.Dequeue();
		}

		return _maHistory.Peek();
	}

	private MovingAverage CreateMovingAverage(MovingAverageTypeEnum type)
	{
		return type switch
		{
			MovingAverageTypeEnum.Simple => new SimpleMovingAverage(),
			MovingAverageTypeEnum.Smoothed => new SmoothedMovingAverage(),
			MovingAverageTypeEnum.Weighted => new WeightedMovingAverage(),
			_ => new ExponentialMovingAverage()
		};
	}

	private decimal GetCandlePrice(ICandleMessage candle)
	{
		return PriceSource switch
		{
			CandlePrice.Open => candle.OpenPrice,
			CandlePrice.High => candle.HighPrice,
			CandlePrice.Low => candle.LowPrice,
			CandlePrice.Close => candle.ClosePrice,
			CandlePrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			CandlePrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			CandlePrice.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}

	private decimal GetPipSize()
	{
		var security = Security;
		if (security == null)
		{
			return 0.0001m;
		}

		if (security.PriceStep > 0)
		{
			return security.PriceStep;
		}

		if (security.MinStep > 0)
		{
			return security.MinStep;
		}

		return 0.0001m;
	}

	private TimeSpan? GetTimeFrame()
	{
		return CandleType.Arg is TimeSpan span ? span : null;
	}

	private sealed class PositionEntry
	{
		public decimal Price { get; set; }

		public DateTimeOffset Time { get; set; }
	}
}
