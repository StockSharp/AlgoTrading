namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Long-only volume-weighted range breakouts with optional ADX, high/low and filter-value VWMA confirmation.
/// </summary>
public class PowertrendVolumeRangeFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _rangePeriod;
	private readonly StrategyParam<decimal> _rangeMultiplier;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _vwmaPeriod;
	private readonly StrategyParam<bool> _isAdxFilterEnabled;
	private readonly StrategyParam<bool> _isHighLowFilterEnabled;
	private readonly StrategyParam<bool> _isVwmaFilterEnabled;
	private readonly StrategyParam<DataType> _candleType;
	private readonly Queue<(decimal delta, decimal volume)> _changes = [];
	private readonly Queue<(decimal price, decimal volume)> _filtered = [];
	private decimal? _previousClose;
	private decimal? _previousHigh;
	private decimal? _previousLow;
	private decimal? _filter;
	private decimal _weightedRange;
	private decimal _rangeVolume;
	private decimal _weightedFilter;
	private decimal _filterVolume;
	private Order _pendingOrder;

	/// <summary>Initializes a 40-change range multiplied by 1.5, disabled optional filters and five-minute candles.</summary>
	public PowertrendVolumeRangeFilterStrategy()
	{
		_rangePeriod = Param(nameof(RangePeriod), 40).SetGreaterThanZero()
			.SetDisplay("Range Period", "Volume-weighted absolute close changes", "Range");
		_rangeMultiplier = Param(nameof(RangeMultiplier), 1.5m).SetGreaterThanZero()
			.SetDisplay("Range Multiplier", "Multiplier of the volume-weighted price-change range", "Range");
		_adxPeriod = Param(nameof(AdxPeriod), 14).SetGreaterThanZero()
			.SetDisplay("ADX Period", "Native directional-index period", "Filters");
		_adxThreshold = Param(nameof(AdxThreshold), 20m).SetRange(0m, 100m)
			.SetDisplay("ADX Threshold", "Strict lower bound on formed ADX for entries", "Filters");
		_vwmaPeriod = Param(nameof(VwmaPeriod), 20).SetGreaterThanZero()
			.SetDisplay("VWMA Period", "Volume-weighted average of formed filter values", "Filters");
		_isAdxFilterEnabled = Param(nameof(IsAdxFilterEnabled), false)
			.SetDisplay("ADX Enabled", "Require formed ADX above its threshold", "Filters");
		_isHighLowFilterEnabled = Param(nameof(IsHighLowFilterEnabled), false)
			.SetDisplay("High/Low Enabled", "Require both a higher high and a higher low", "Filters");
		_isVwmaFilterEnabled = Param(nameof(IsVwmaFilterEnabled), false)
			.SetDisplay("VWMA Enabled", "Require close above the formed filter-value VWMA", "Filters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Finished candles supplying price and true total volume", "General");
		OrderRegistering += order => _pendingOrder = order;
	}

	/// <summary>Number of close changes used to form a volume-weighted range.</summary>
	public int RangePeriod { get => _rangePeriod.Value; set => _rangePeriod.Value = value; }
	/// <summary>Positive multiplier applied to the weighted range.</summary>
	public decimal RangeMultiplier { get => _rangeMultiplier.Value; set => _rangeMultiplier.Value = value; }
	/// <summary>Native ADX length.</summary>
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	/// <summary>Strict ADX entry threshold in [0, 100].</summary>
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	/// <summary>Number of formed filter values in the optional VWMA.</summary>
	public int VwmaPeriod { get => _vwmaPeriod.Value; set => _vwmaPeriod.Value = value; }
	/// <summary>Require a formed native ADX above the threshold for entry.</summary>
	public bool IsAdxFilterEnabled { get => _isAdxFilterEnabled.Value; set => _isAdxFilterEnabled.Value = value; }
	/// <summary>Require current high and low to exceed the previous candle's high and low.</summary>
	public bool IsHighLowFilterEnabled { get => _isHighLowFilterEnabled.Value; set => _isHighLowFilterEnabled.Value = value; }
	/// <summary>Require current close above the formed filter-value VWMA.</summary>
	public bool IsVwmaFilterEnabled { get => _isVwmaFilterEnabled.Value; set => _isVwmaFilterEnabled.Value = value; }
	/// <summary>Base candle type; five minutes by default.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	private void ResetState()
	{
		_changes.Clear();
		_filtered.Clear();
		_previousClose = _previousHigh = _previousLow = _filter = null;
		_weightedRange = _rangeVolume = _weightedFilter = _filterVolume = 0m;
		_pendingOrder = null;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (RangePeriod <= 0 || RangeMultiplier <= 0m || AdxPeriod <= 0 || AdxThreshold < 0m
			|| AdxThreshold > 100m || VwmaPeriod <= 0)
			throw new ArgumentException("Range/ADX/VWMA periods and multiplier must be positive; ADX threshold must be within [0, 100].");
		ResetState();
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var subscription = SubscribeCandles(CandleType);
		if (IsAdxFilterEnabled)
			subscription.BindEx([adx], ProcessCandle, allowEmpty: true).Start();
		else
			subscription.Bind(candle => ProcessCandle(candle, [])).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (IsAdxFilterEnabled)
				DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}
	}

	private void Submit(Sides side, decimal volume)
		=> RegisterOrder(new Order { Security = Security, Portfolio = Portfolio, Type = OrderTypes.Market, Side = side, Volume = volume });

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;
		var higherHighLow = _previousHigh is decimal high && _previousLow is decimal low
			&& candle.HighPrice > high && candle.LowPrice > low;
		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
		if (_previousClose is decimal previous)
		{
			var delta = Math.Abs(candle.ClosePrice - previous);
			_changes.Enqueue((delta, candle.TotalVolume));
			_weightedRange += delta * candle.TotalVolume;
			_rangeVolume += candle.TotalVolume;
			if (_changes.Count > RangePeriod)
			{
				var removed = _changes.Dequeue();
				_weightedRange -= removed.delta * removed.volume;
				_rangeVolume -= removed.volume;
			}
		}
		_previousClose = candle.ClosePrice;
		if (_changes.Count < RangePeriod || _rangeVolume <= 0m)
			return;
		var width = RangeMultiplier * _weightedRange / _rangeVolume;
		var prior = _filter;
		if (prior is null)
			_filter = candle.ClosePrice;
		else if (candle.ClosePrice > prior + width)
			_filter = candle.ClosePrice - width;
		else if (candle.ClosePrice < prior - width)
			_filter = candle.ClosePrice + width;
		_filtered.Enqueue((_filter.Value, candle.TotalVolume));
		_weightedFilter += _filter.Value * candle.TotalVolume;
		_filterVolume += candle.TotalVolume;
		if (_filtered.Count > VwmaPeriod)
		{
			var removed = _filtered.Dequeue();
			_weightedFilter -= removed.price * removed.volume;
			_filterVolume -= removed.volume;
		}
		if (prior is not decimal center || !IsFormedAndOnlineAndAllowTrading()
			|| _pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (Position > 0m && candle.ClosePrice < center - width)
		{
			Submit(Sides.Sell, Position);
			return;
		}
		if (Position != 0m || width <= 0m || candle.ClosePrice <= center + width || _filter <= center
			|| IsHighLowFilterEnabled && !higherHighLow
			|| IsVwmaFilterEnabled && (_filtered.Count < VwmaPeriod || _filterVolume <= 0m || candle.ClosePrice <= _weightedFilter / _filterVolume))
			return;
		if (IsAdxFilterEnabled && (!values[0].IsFormed
			|| ((IAverageDirectionalIndexValue)values[0]).MovingAverage is not decimal strength || strength <= AdxThreshold))
			return;
		Submit(Sides.Buy, Volume);
	}
}
