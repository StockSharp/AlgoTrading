using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Berlin Range Index strategy.
/// Uses filtered choppiness index to detect trending and ranging markets.
/// </summary>
public class BerlinRangeIndexStrategy : Strategy {
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _chopMax;
	private readonly StrategyParam<decimal> _chopMin;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _lowLookback;
	private readonly StrategyParam<bool> _useNormalized;
	private readonly StrategyParam<int> _stdDevLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly StandardDeviation _stdDev;
	private readonly Lowest _stdDevLow;

	private decimal _prevRangeIndex;

	/// <summary>
	/// Choppiness index period.
	/// </summary>
	public int Length {
	get => _length.Value;
	set => _length.Value = value;
	}

	/// <summary>
	/// Maximum value for trend threshold.
	/// </summary>
	public decimal ChopMax {
	get => _chopMax.Value;
	set => _chopMax.Value = value;
	}

	/// <summary>
	/// Minimum value for exhausted trend threshold.
	/// </summary>
	public decimal ChopMin {
	get => _chopMin.Value;
	set => _chopMin.Value = value;
	}

	/// <summary>
	/// ATR filter period.
	/// </summary>
	public int AtrLength {
	get => _atrLength.Value;
	set => _atrLength.Value = value;
	}

	/// <summary>
	/// Lookback period for lowest standard deviation.
	/// </summary>
	public int LowLookback {
	get => _lowLookback.Value;
	set => _lowLookback.Value = value;
	}

	/// <summary>
	/// Use normalized true range for ATR filter.
	/// </summary>
	public bool UseNormalized {
	get => _useNormalized.Value;
	set => _useNormalized.Value = value;
	}

	/// <summary>
	/// Standard deviation calculation length.
	/// </summary>
	public int StdDevLength {
	get => _stdDevLength.Value;
	set => _stdDevLength.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType {
	get => _candleType.Value;
	set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="BerlinRangeIndexStrategy"/>.
	/// </summary>
	public BerlinRangeIndexStrategy() {
	_length =
		Param(nameof(Length), 9)
		.SetGreaterThanZero()
		.SetDisplay("Length", "Choppiness index period", "General")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

	_chopMax = Param(nameof(ChopMax), 40m)
			   .SetRange(1m, 100m)
			   .SetDisplay("Trend Threshold",
				   "Maximum choppiness value considered trend",
				   "General")
			   .SetCanOptimize(true)
			   .SetOptimize(30m, 60m, 5m);

	_chopMin =
		Param(nameof(ChopMin), 10m)
		.SetRange(0m, 99m)
		.SetDisplay(
			"Exhausted Threshold",
			"Minimum choppiness value considered exhausted trend",
			"General")
		.SetCanOptimize(true)
		.SetOptimize(5m, 20m, 5m);

	_atrLength =
		Param(nameof(AtrLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "ATR filter period", "General")
		.SetCanOptimize(true)
		.SetOptimize(10, 20, 2);

	_lowLookback =
		Param(nameof(LowLookback), 14)
		.SetGreaterThanZero()
		.SetDisplay("Low Lookback", "Lookback period for lowest StdDev",
				"General")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 5);

	_useNormalized =
		Param(nameof(UseNormalized), true)
		.SetDisplay("Use Normalized TR",
				"Divide ATR by price for normalization", "General");

	_stdDevLength =
		Param(nameof(StdDevLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("StdDev Length",
				"Length for ATR standard deviation", "General")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 5);

	_candleType =
		Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles for strategy",
				"General");

	_stdDev = new StandardDeviation { Length = StdDevLength };
	_stdDevLow = new Lowest { Length = LowLookback };
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)>
	GetWorkingSecurities() {
	return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted() {
	base.OnReseted();
	_prevRangeIndex = 50m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time) {
	base.OnStarted(time);

	var atr = new AverageTrueRange { Length = AtrLength };
	var choppiness = new ChoppinessIndex { Length = Length };

	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(atr, choppiness, ProcessCandle).Start();

	var area = CreateChartArea();
	if (area != null) {
		DrawCandles(area, subscription);
		DrawIndicator(area, choppiness);
		DrawOwnTrades(area);
	}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue,
				   decimal chopValue) {
	if (candle.State != CandleStates.Finished)
		return;

	var atrVal = UseNormalized ? atrValue / candle.ClosePrice : atrValue;

	var stdDevValue = _stdDev.Process(atrVal, candle.ServerTime, true);
	var stdDev = stdDevValue.ToDecimal();

	var lowValue = _stdDevLow.Process(stdDev, candle.ServerTime, true);
	var stdDevLow = lowValue.ToDecimal();

	if (!_stdDev.IsFormed || !_stdDevLow.IsFormed)
		return;

	var stdDevFactor = stdDev == 0m ? 0m : stdDevLow / stdDev;
	var rangeIndex = chopValue * stdDevFactor;

	var chopCondition = rangeIndex > ChopMax;
	var trendCondition = _prevRangeIndex > ChopMin &&
				 rangeIndex < ChopMax && rangeIndex > ChopMin;
	var strongTrendCondition = rangeIndex < ChopMin;
	var weakeningTrendCondition =
		_prevRangeIndex < ChopMin && rangeIndex > ChopMin;

	_prevRangeIndex = rangeIndex;

	if (!IsFormedAndOnlineAndAllowTrading())
		return;

	if (strongTrendCondition) {
		if (candle.ClosePrice > candle.OpenPrice && Position <= 0)
		BuyMarket(Volume + Math.Abs(Position));
		else if (candle.ClosePrice < candle.OpenPrice && Position >= 0)
		SellMarket(Volume + Math.Abs(Position));
	} else if (chopCondition || weakeningTrendCondition) {
		if (Position > 0)
		SellMarket(Math.Abs(Position));
		else if (Position < 0)
		BuyMarket(Math.Abs(Position));
	}
	}
}
