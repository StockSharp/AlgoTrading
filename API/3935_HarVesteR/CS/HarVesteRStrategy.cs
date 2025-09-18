using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// HarVesteR trend-following strategy converted from the original MQL expert advisor.
/// The logic combines MACD confirmation with two simple moving averages and an optional ADX filter.
/// Positions are managed through a stop-loss derived from recent extremes and a staged take-profit.
/// </summary>
public class HarVesteRStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _confirmationBars;
	private readonly StrategyParam<int> _trendSmaLength;
	private readonly StrategyParam<int> _filterSmaLength;
	private readonly StrategyParam<int> _offsetPoints;
	private readonly StrategyParam<int> _stopLossBars;
	private readonly StrategyParam<decimal> _profitMultiplier;
	private readonly StrategyParam<bool> _useAdxFilter;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergence _macd = null!;
	private SimpleMovingAverage _trendSma = null!;
	private SimpleMovingAverage _filterSma = null!;
	private AverageDirectionalIndex _adx = null!;
	private Lowest _lowest = null!;
	private Highest _highest = null!;

	private bool _allowLong;
	private bool _allowShort;
	private bool _longPartialClosed;
	private bool _shortPartialClosed;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private int _barsSinceMacdNegative;
	private int _barsSinceMacdPositive;

	/// <summary>
	/// Fast EMA period used in the MACD indicator.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period used in the MACD indicator.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA period used in the MACD indicator.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Maximum number of candles allowed between opposite MACD values when confirming a crossover.
	/// </summary>
	public int ConfirmationBars
	{
		get => _confirmationBars.Value;
		set => _confirmationBars.Value = value;
	}

	/// <summary>
	/// Period of the faster simple moving average used for trade management.
	/// </summary>
	public int TrendSmaLength
	{
		get => _trendSmaLength.Value;
		set => _trendSmaLength.Value = value;
	}

	/// <summary>
	/// Period of the slower simple moving average used for directional filtering.
	/// </summary>
	public int FilterSmaLength
	{
		get => _filterSmaLength.Value;
		set => _filterSmaLength.Value = value;
	}

	/// <summary>
	/// Offset in points added to price comparisons for entries and exits.
	/// </summary>
	public int OffsetPoints
	{
		get => _offsetPoints.Value;
		set => _offsetPoints.Value = value;
	}

	/// <summary>
	/// Number of candles used to locate the protective stop.
	/// </summary>
	public int StopLossBars
	{
		get => _stopLossBars.Value;
		set => _stopLossBars.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the initial risk distance when projecting the staged profit target.
	/// </summary>
	public decimal ProfitMultiplier
	{
		get => _profitMultiplier.Value;
		set => _profitMultiplier.Value = value;
	}

	/// <summary>
	/// Enables the ADX trend strength filter when true.
	/// </summary>
	public bool UseAdxFilter
	{
		get => _useAdxFilter.Value;
		set => _useAdxFilter.Value = value;
	}

	/// <summary>
	/// Period of the ADX indicator when the filter is active.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Type of candles supplied to the indicators.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with defaults that match the original advisor.
	/// </summary>
	public HarVesteRStrategy()
	{
		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Short-period EMA used in MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(6, 18, 1);

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 24)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Long-period EMA used in MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal EMA", "Signal smoothing period for MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(6, 12, 1);

		_confirmationBars = Param(nameof(ConfirmationBars), 6)
			.SetGreaterThanZero()
			.SetDisplay("MACD Confirmation Bars", "Maximum bars allowed between opposite MACD signs", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_trendSmaLength = Param(nameof(TrendSmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Trend SMA", "Shorter SMA guiding trade management", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(20, 80, 5);

		_filterSmaLength = Param(nameof(FilterSmaLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Filter SMA", "Longer SMA confirming the main trend", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(60, 140, 5);

		_offsetPoints = Param(nameof(OffsetPoints), 10)
			.SetGreaterThanZero()
			.SetDisplay("Offset (points)", "Price offset applied in comparisons", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_stopLossBars = Param(nameof(StopLossBars), 6)
			.SetGreaterThanZero()
			.SetDisplay("Stop Bars", "Number of bars to evaluate stop extremes", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(3, 12, 1);

		_profitMultiplier = Param(nameof(ProfitMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Multiplier", "Multiplier for projecting the staged target", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 4m, 0.5m);

		_useAdxFilter = Param(nameof(UseAdxFilter), false)
			.SetDisplay("Use ADX", "Enable ADX strength filter", "Filters");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "ADX lookback when the filter is active", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(10, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Source candle series", "General");
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

		_allowLong = false;
		_allowShort = false;
		_longPartialClosed = false;
		_shortPartialClosed = false;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_barsSinceMacdNegative = ConfirmationBars + 1;
		_barsSinceMacdPositive = ConfirmationBars + 1;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = FastEmaPeriod,
			LongPeriod = SlowEmaPeriod,
			SignalPeriod = SignalPeriod
		};

		_trendSma = new SimpleMovingAverage { Length = TrendSmaLength };
		_filterSma = new SimpleMovingAverage { Length = FilterSmaLength };
		_adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_lowest = new Lowest { Length = StopLossBars };
		_highest = new Highest { Length = StopLossBars };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_macd, _trendSma, _filterSma, _adx, _lowest, _highest, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _trendSma);
			DrawIndicator(area, _filterSma);
			DrawIndicator(area, _macd);
			if (UseAdxFilter)
				DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(
		ICandleMessage candle,
		decimal macdValue,
		decimal macdSignal,
		decimal macdHistogram,
		decimal trendSmaValue,
		decimal filterSmaValue,
		decimal adxValue,
		decimal lowestValue,
		decimal highestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_macd.IsFormed || !_trendSma.IsFormed || !_filterSma.IsFormed)
			return;

		if (UseAdxFilter && !_adx.IsFormed)
			return;

		if (!_lowest.IsFormed || !_highest.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var step = GetPriceStep();
		var offset = OffsetPoints * step;
		var close = candle.ClosePrice;

		// Update directional permissions based on the slower SMA as in the original EA.
		if (close < filterSmaValue)
			_allowLong = true;
		if (close > filterSmaValue)
			_allowShort = true;

		// Track how many bars passed since MACD was on the opposite side.
		if (macdValue < 0m)
			_barsSinceMacdNegative = 0;
		else if (_barsSinceMacdNegative <= ConfirmationBars)
			_barsSinceMacdNegative++;
		else
			_barsSinceMacdNegative = ConfirmationBars + 1;

		if (macdValue > 0m)
			_barsSinceMacdPositive = 0;
		else if (_barsSinceMacdPositive <= ConfirmationBars)
			_barsSinceMacdPositive++;
		else
			_barsSinceMacdPositive = ConfirmationBars + 1;

		var adxThreshold = UseAdxFilter ? adxValue : 60m;
		var strongTrend = adxThreshold > 50m;

		TryEnterLong(close, trendSmaValue, filterSmaValue, lowestValue, macdValue, offset, strongTrend);
		TryEnterShort(close, trendSmaValue, filterSmaValue, highestValue, macdValue, offset, strongTrend);

		ManageLongPosition(candle, trendSmaValue, offset);
		ManageShortPosition(candle, trendSmaValue, offset);
	}

	private void TryEnterLong(decimal close, decimal trendSmaValue, decimal filterSmaValue, decimal lowestValue, decimal macdValue, decimal offset, bool strongTrend)
	{
		if (Position > 0)
			return;

		if (!_allowLong)
			return;

		var confirmationReady = _barsSinceMacdNegative > 0 && _barsSinceMacdNegative <= ConfirmationBars;
		var longCondition = close + offset > trendSmaValue &&
			close + offset > filterSmaValue &&
			macdValue > 0m &&
			confirmationReady &&
			strongTrend;

		if (!longCondition)
			return;

		var volume = Volume + Math.Max(0m, -Position);
		if (volume <= 0m)
			return;

		BuyMarket(volume);

		_allowLong = false;
		_longPartialClosed = false;
		_longEntryPrice = close;
		_longStopPrice = lowestValue;
	}

	private void TryEnterShort(decimal close, decimal trendSmaValue, decimal filterSmaValue, decimal highestValue, decimal macdValue, decimal offset, bool strongTrend)
	{
		if (Position < 0)
			return;

		if (!_allowShort)
			return;

		var confirmationReady = _barsSinceMacdPositive > 0 && _barsSinceMacdPositive <= ConfirmationBars;
		var shortCondition = close - offset < trendSmaValue &&
			close - offset < filterSmaValue &&
			macdValue < 0m &&
			confirmationReady &&
			strongTrend;

		if (!shortCondition)
			return;

		var volume = Volume + Math.Max(0m, Position);
		if (volume <= 0m)
			return;

		SellMarket(volume);

		_allowShort = false;
		_shortPartialClosed = false;
		_shortEntryPrice = close;
		_shortStopPrice = highestValue;
	}

	private void ManageLongPosition(ICandleMessage candle, decimal trendSmaValue, decimal offset)
	{
		if (Position <= 0)
		{
			ResetLongState();
			return;
		}

		if (!_longEntryPrice.HasValue || !_longStopPrice.HasValue)
			return;

		var stop = _longStopPrice.Value;
		if (candle.LowPrice <= stop)
		{
			SellMarket(Position);
			ResetLongState();
			return;
		}

		var entry = _longEntryPrice.Value;
		var target = entry + (entry - stop) * ProfitMultiplier;

		if (!_longPartialClosed && candle.ClosePrice > target)
		{
			var halfVolume = Position / 2m;
			if (halfVolume > 0m)
			{
				SellMarket(halfVolume);
				_longPartialClosed = true;
				_longStopPrice = entry;
			}
		}

		if (_longPartialClosed)
		{
			var adjustedClose = candle.ClosePrice - offset;
			if (trendSmaValue > adjustedClose)
			{
				SellMarket(Position);
				ResetLongState();
			}
		}
	}

	private void ManageShortPosition(ICandleMessage candle, decimal trendSmaValue, decimal offset)
	{
		if (Position >= 0)
		{
			ResetShortState();
			return;
		}

		if (!_shortEntryPrice.HasValue || !_shortStopPrice.HasValue)
			return;

		var stop = _shortStopPrice.Value;
		if (candle.HighPrice >= stop)
		{
			BuyMarket(-Position);
			ResetShortState();
			return;
		}

		var entry = _shortEntryPrice.Value;
		var target = entry - (stop - entry) * ProfitMultiplier;

		if (!_shortPartialClosed && candle.ClosePrice < target)
		{
			var halfVolume = -Position / 2m;
			if (halfVolume > 0m)
			{
				BuyMarket(halfVolume);
				_shortPartialClosed = true;
				_shortStopPrice = entry;
			}
		}

		if (_shortPartialClosed)
		{
			var adjustedClose = candle.ClosePrice - offset;
			if (adjustedClose > trendSmaValue)
			{
				BuyMarket(-Position);
				ResetShortState();
			}
		}
	}

	private void ResetLongState()
	{
		_longPartialClosed = false;
		_longEntryPrice = null;
		_longStopPrice = null;
	}

	private void ResetShortState()
	{
		_shortPartialClosed = false;
		_shortEntryPrice = null;
		_shortStopPrice = null;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.Step ?? Security?.PriceStep;
		if (!step.HasValue || step.Value <= 0m)
			return 0.0001m;

		return step.Value;
	}
}
