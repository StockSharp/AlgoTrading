using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert advisor «FarhadCrab1».
/// Applies EMA and SMA filters on the primary timeframe while supervising a daily smoothed moving average for risk exits.
/// Implements symmetric take-profit and trailing-stop management for long and short positions.
/// </summary>
public class FarhadCrabStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _longTakeProfitPips;
	private readonly StrategyParam<decimal> _shortTakeProfitPips;
	private readonly StrategyParam<decimal> _longTrailingStopPips;
	private readonly StrategyParam<decimal> _shortTrailingStopPips;
	private readonly StrategyParam<int> _dailyMaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _emaTypical = null!;
	private SimpleMovingAverage _smaOpen = null!;
	private SmoothedMovingAverage _dailySmma = null!;

	private decimal _pipSize;

	private decimal? _longEntryPrice;
	private decimal? _longHighestPrice;
	private decimal? _shortEntryPrice;
	private decimal? _shortLowestPrice;

	private decimal? _prevDailyMa;
	private decimal? _currentDailyMa;
	private decimal? _prevDailyClose;
	private decimal? _currentDailyClose;

	/// <summary>
	/// Initializes a new instance of the <see cref="FarhadCrabStrategy"/> class.
	/// </summary>
	public FarhadCrabStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume used for market entries", "Trading")
		.SetCanOptimize();

		_longTakeProfitPips = Param(nameof(LongTakeProfitPips), 10m)
		.SetRange(0m, 500m)
		.SetDisplay("Long TP (pips)", "Distance to long take-profit target", "Risk")
		.SetCanOptimize();

		_shortTakeProfitPips = Param(nameof(ShortTakeProfitPips), 10m)
		.SetRange(0m, 500m)
		.SetDisplay("Short TP (pips)", "Distance to short take-profit target", "Risk")
		.SetCanOptimize();

		_longTrailingStopPips = Param(nameof(LongTrailingStopPips), 8m)
		.SetRange(0m, 500m)
		.SetDisplay("Long Trail (pips)", "Trailing distance for long trades", "Risk")
		.SetCanOptimize();

		_shortTrailingStopPips = Param(nameof(ShortTrailingStopPips), 8m)
		.SetRange(0m, 500m)
		.SetDisplay("Short Trail (pips)", "Trailing distance for short trades", "Risk")
		.SetCanOptimize();

		_dailyMaPeriod = Param(nameof(DailyMaPeriod), 55)
		.SetRange(2, 200)
		.SetDisplay("Daily MA", "Length of the daily smoothed moving average", "Trend")
		.SetCanOptimize();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for trading", "General");
	}

	/// <summary>
	/// Volume assigned to market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Take-profit distance for long positions in pips.
	/// </summary>
	public decimal LongTakeProfitPips
	{
		get => _longTakeProfitPips.Value;
		set => _longTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance for short positions in pips.
	/// </summary>
	public decimal ShortTakeProfitPips
	{
		get => _shortTakeProfitPips.Value;
		set => _shortTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance for long positions in pips.
	/// </summary>
	public decimal LongTrailingStopPips
	{
		get => _longTrailingStopPips.Value;
		set => _longTrailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance for short positions in pips.
	/// </summary>
	public decimal ShortTrailingStopPips
	{
		get => _shortTrailingStopPips.Value;
		set => _shortTrailingStopPips.Value = value;
	}

	/// <summary>
	/// Length of the daily smoothed moving average used for protective exits.
	/// </summary>
	public int DailyMaPeriod
	{
		get => _dailyMaPeriod.Value;
		set => _dailyMaPeriod.Value = value;
	}

	/// <summary>
	/// Candle type that drives the primary calculations.
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

		_longEntryPrice = null;
		_longHighestPrice = null;
		_shortEntryPrice = null;
		_shortLowestPrice = null;

		_prevDailyMa = null;
		_currentDailyMa = null;
		_prevDailyClose = null;
		_currentDailyClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_emaTypical = new ExponentialMovingAverage
		{
			Length = 9,
			CandlePrice = CandlePrice.Typical,
		};

		_smaOpen = new SimpleMovingAverage
		{
			Length = 9,
			CandlePrice = CandlePrice.Open,
		};

		_dailySmma = new SmoothedMovingAverage
		{
			Length = DailyMaPeriod,
			CandlePrice = CandlePrice.Typical,
		};

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
			.Bind(_emaTypical, _smaOpen, ProcessMainCandle)
			.Start();

		var dailySubscription = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		dailySubscription
			.Bind(_dailySmma, ProcessDailyCandle)
			.Start();

		_pipSize = CalculatePipSize();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, _emaTypical);
			DrawIndicator(area, _smaOpen);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessDailyCandle(ICandleMessage candle, decimal dailyMaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_dailySmma.IsFormed)
		return;

		_prevDailyMa = _currentDailyMa;
		_prevDailyClose = _currentDailyClose;

		_currentDailyMa = dailyMaValue;
		_currentDailyClose = candle.ClosePrice;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal emaValue, decimal openSmaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_emaTypical.IsFormed || !_smaOpen.IsFormed || !_dailySmma.IsFormed)
		return;

		UpdateActivePositionTargets(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position > 0m)
		{
		if (ShouldExitLong(candle))
		{
			SellMarket(Position);
			ResetLongState();
		}
		}
		else if (Position < 0m)
		{
		if (ShouldExitShort(candle))
		{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
		}
		}

		if (Position != 0m)
		return;

		var canOpenLong = candle.LowPrice > emaValue;
		var canOpenShort = candle.HighPrice < openSmaValue;

		if (canOpenLong && !canOpenShort)
		{
		BuyMarket();
		InitializeLongState(candle.ClosePrice, candle.HighPrice);
		}
		else if (canOpenShort && !canOpenLong)
		{
		SellMarket();
		InitializeShortState(candle.ClosePrice, candle.LowPrice);
		}
	}

	private bool ShouldExitLong(ICandleMessage candle)
	{
		if (_longEntryPrice == null)
		return true;

		if (CheckDailyExitForLong())
		return true;

		var longTakeProfit = LongTakeProfitPips * _pipSize;
		if (longTakeProfit > 0m && candle.HighPrice >= _longEntryPrice.Value + longTakeProfit)
		return true;

		var longTrailing = LongTrailingStopPips * _pipSize;
		if (longTrailing > 0m && _longHighestPrice.HasValue)
		{
		var stopLevel = _longHighestPrice.Value - longTrailing;
		if (candle.LowPrice <= stopLevel)
		return true;
		}

		return false;
	}

	private bool ShouldExitShort(ICandleMessage candle)
	{
		if (_shortEntryPrice == null)
		return true;

		if (CheckDailyExitForShort())
		return true;

		var shortTakeProfit = ShortTakeProfitPips * _pipSize;
		if (shortTakeProfit > 0m && candle.LowPrice <= _shortEntryPrice.Value - shortTakeProfit)
		return true;

		var shortTrailing = ShortTrailingStopPips * _pipSize;
		if (shortTrailing > 0m && _shortLowestPrice.HasValue)
		{
		var stopLevel = _shortLowestPrice.Value + shortTrailing;
		if (candle.HighPrice >= stopLevel)
		return true;
		}

		return false;
	}

	private bool CheckDailyExitForLong()
	{
		if (_prevDailyMa == null || _currentDailyMa == null || _prevDailyClose == null || _currentDailyClose == null)
		return false;

		return _currentDailyMa.Value > _currentDailyClose.Value && _prevDailyMa.Value < _prevDailyClose.Value;
	}

	private bool CheckDailyExitForShort()
	{
		if (_prevDailyMa == null || _currentDailyMa == null || _prevDailyClose == null || _currentDailyClose == null)
		return false;

		return _currentDailyMa.Value < _currentDailyClose.Value && _prevDailyMa.Value > _prevDailyClose.Value;
	}

	private void UpdateActivePositionTargets(ICandleMessage candle)
	{
		if (_longHighestPrice.HasValue)
		{
		_longHighestPrice = Math.Max(_longHighestPrice.Value, candle.HighPrice);
		}

		if (_shortLowestPrice.HasValue)
		{
		_shortLowestPrice = Math.Min(_shortLowestPrice.Value, candle.LowPrice);
		}
	}

	private void InitializeLongState(decimal entryPrice, decimal candleHigh)
	{
		_longEntryPrice = entryPrice;
		_longHighestPrice = Math.Max(entryPrice, candleHigh);

		ResetShortState();
	}

	private void InitializeShortState(decimal entryPrice, decimal candleLow)
	{
		_shortEntryPrice = entryPrice;
		_shortLowestPrice = Math.Min(entryPrice, candleLow);

		ResetLongState();
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longHighestPrice = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortLowestPrice = null;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0.0001m;
		if (step <= 0m)
		step = 0.0001m;

		var decimals = Security?.Decimals ?? 0;
		if (decimals is 3 or 5)
		step *= 10m;

	return step;
	}
}
