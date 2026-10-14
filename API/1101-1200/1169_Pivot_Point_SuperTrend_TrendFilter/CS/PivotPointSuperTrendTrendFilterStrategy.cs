namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Confirmed pivot SuperTrend signals combined with a price SuperTrend and SMA entry filter.
/// </summary>
public class PivotPointSuperTrendTrendFilterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _pivotPeriod;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _trendAtrPeriod;
	private readonly StrategyParam<decimal> _trendMultiplier;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DateTime> _startDate;
	private readonly StrategyParam<DateTime> _endDate;
	private readonly List<(decimal High, decimal Low)> _pivots = [];
	private decimal? _center;
	private decimal? _support;
	private decimal? _resistance;
	private decimal? _previousClose;
	private bool? _pivotUp;
	private bool? _filterUp;

	/// <summary>
	/// Initializes the published indicator periods and an unrestricted UTC date window.
	/// </summary>
	public PivotPointSuperTrendTrendFilterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of finished candles", "General");
		_pivotPeriod = Param(nameof(PivotPeriod), 2).SetGreaterThanZero()
			.SetDisplay("Pivot Period", "Finished candles on each side of a confirmed pivot", "Indicators");
		_factor = Param(nameof(Factor), 3m).SetGreaterThanZero()
			.SetDisplay("Factor", "ATR width of the pivot bands", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 10).SetGreaterThanZero()
			.SetDisplay("ATR Period", "Wilder ATR period of the pivot bands", "Indicators");
		_trendAtrPeriod = Param(nameof(TrendAtrPeriod), 10).SetGreaterThanZero()
			.SetDisplay("Trend ATR Period", "Wilder ATR period of the price SuperTrend", "Indicators");
		_trendMultiplier = Param(nameof(TrendMultiplier), 3m).SetGreaterThanZero()
			.SetDisplay("Trend Multiplier", "ATR width of the price SuperTrend", "Indicators");
		_maPeriod = Param(nameof(MaPeriod), 20).SetGreaterThanZero()
			.SetDisplay("MA Period", "Close-price SMA period for trend-filter entries", "Indicators");
		_startDate = Param(nameof(StartDate), DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc))
			.SetDisplay("Start Date", "Inclusive UTC candle-open time for pivot entries", "General");
		_endDate = Param(nameof(EndDate), DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc))
			.SetDisplay("End Date", "Inclusive UTC candle-open time for pivot entries", "General");
	}

	/// <summary>
	/// Candle type; five minutes by default.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Number of finished candles required on each side of a pivot.
	/// </summary>
	public int PivotPeriod { get => _pivotPeriod.Value; set => _pivotPeriod.Value = value; }

	/// <summary>
	/// ATR multiplier for the pivot bands.
	/// </summary>
	public decimal Factor { get => _factor.Value; set => _factor.Value = value; }

	/// <summary>
	/// Wilder ATR period for the pivot bands.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// ATR period of the price SuperTrend filter.
	/// </summary>
	public int TrendAtrPeriod { get => _trendAtrPeriod.Value; set => _trendAtrPeriod.Value = value; }

	/// <summary>
	/// ATR multiplier of the price SuperTrend filter.
	/// </summary>
	public decimal TrendMultiplier { get => _trendMultiplier.Value; set => _trendMultiplier.Value = value; }

	/// <summary>
	/// Close-price SMA period for trend-filter entries.
	/// </summary>
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

	/// <summary>
	/// Inclusive lower UTC candle-open time for pivot entries.
	/// </summary>
	public DateTime StartDate { get => _startDate.Value; set => _startDate.Value = value; }

	/// <summary>
	/// Inclusive upper UTC candle-open time for pivot entries.
	/// </summary>
	public DateTime EndDate { get => _endDate.Value; set => _endDate.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetSignals();
	}

	private void ResetSignals()
	{
		_pivots.Clear();
		_center = _support = _resistance = _previousClose = null;
		_pivotUp = _filterUp = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (StartDate.Kind != DateTimeKind.Utc || EndDate.Kind != DateTimeKind.Utc || StartDate > EndDate)
			throw new ArgumentException("The pivot entry window requires ordered UTC dates.");
		ResetSignals();
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var trend = new SuperTrend { Length = TrendAtrPeriod, Multiplier = TrendMultiplier };
		var average = new SimpleMovingAverage { Length = MaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(atr, trend, average, ProcessCandle, allowEmpty: true).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, trend);
			DrawIndicator(area, average);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue, IIndicatorValue trendValue, IIndicatorValue averageValue)
	{
		if (candle.State != CandleStates.Finished)
			return;
		var previousClose = _previousClose ?? candle.ClosePrice;
		_previousClose = candle.ClosePrice;
		_pivots.Add((candle.HighPrice, candle.LowPrice));
		if (_pivots.Count > 2 * PivotPeriod + 1)
			_pivots.RemoveAt(0);
		if (_pivots.Count == 2 * PivotPeriod + 1)
		{
			var candidate = _pivots[PivotPeriod];
			var isHigh = true;
			var isLow = true;
			for (var i = 0; i < _pivots.Count; i++)
			{
				if (i == PivotPeriod)
					continue;
				isHigh &= candidate.High > _pivots[i].High;
				isLow &= candidate.Low < _pivots[i].Low;
			}
			if (isHigh || isLow)
			{
				var pivot = isHigh ? candidate.High : candidate.Low;
				_center = _center is decimal center ? (2m * center + pivot) / 3m : pivot;
			}
		}
		var previousPivotUp = _pivotUp;
		if (_center is decimal pivotCenter && atrValue.IsFormed && !atrValue.IsEmpty)
		{
			var width = Factor * atrValue.ToDecimal();
			var oldSupport = _support;
			var oldResistance = _resistance;
			_support = oldSupport is decimal support && previousClose > support ? Math.Max(pivotCenter - width, support) : pivotCenter - width;
			_resistance = oldResistance is decimal resistance && previousClose < resistance ? Math.Min(pivotCenter + width, resistance) : pivotCenter + width;
			_pivotUp = oldResistance is decimal upper && candle.ClosePrice > upper ? true
				: oldSupport is decimal lower && candle.ClosePrice < lower ? false : _pivotUp ?? true;
		}
		var previousFilterUp = _filterUp;
		if (trendValue is SuperTrendIndicatorValue { IsFormed: true, IsEmpty: false } filter)
			_filterUp = filter.IsUpTrend;
		if (_pivotUp is null || _filterUp is null || !averageValue.IsFormed || averageValue.IsEmpty || !IsFormedAndOnlineAndAllowTrading())
			return;
		var pivotBuy = previousPivotUp == false && _pivotUp == true;
		var pivotSell = previousPivotUp == true && _pivotUp == false;
		var filterBuy = previousFilterUp == false && _filterUp == true;
		var filterSell = previousFilterUp == true && _filterUp == false;
		var withinDates = candle.OpenTime >= StartDate && candle.OpenTime <= EndDate;
		var upSignal = pivotBuy || filterBuy;
		var downSignal = pivotSell || filterSell;
		var averagePrice = averageValue.ToDecimal();
		var enterLong = !downSignal && (filterBuy && candle.ClosePrice > averagePrice || pivotBuy && withinDates);
		var enterShort = !upSignal && (filterSell && candle.ClosePrice < averagePrice || pivotSell && withinDates);
		if (Position > 0m && downSignal)
			SellMarket(Position + (enterShort ? Volume : 0m));
		else if (Position < 0m && upSignal)
			BuyMarket(-Position + (enterLong ? Volume : 0m));
		else if (Position == 0m && enterLong)
			BuyMarket(Volume);
		else if (Position == 0m && enterShort)
			SellMarket(Volume);
	}
}
