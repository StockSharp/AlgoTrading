using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 4H Bollinger Breakout Strategy - trades Bollinger Band breakouts with volume, trend and RSI filters.
/// </summary>
public class BollingerBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _volumeLength;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<bool> _useLongSignals;
	private readonly StrategyParam<bool> _useShortSignals;

	private BollingerBands _bollinger;
	private SimpleMovingAverage _volumeSma;
	private SimpleMovingAverage _trendSma;
	private RelativeStrengthIndex _rsi;

	private decimal _prevClose;
	private decimal _prevUpper;
	private decimal _prevLower;
	private bool _isInitial;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerLength { get => _bollingerLength.Value; set => _bollingerLength.Value = value; }

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BollingerMultiplier { get => _bollingerMultiplier.Value; set => _bollingerMultiplier.Value = value; }

	/// <summary>
	/// Volume SMA length.
	/// </summary>
	public int VolumeLength { get => _volumeLength.Value; set => _volumeLength.Value = value; }

	/// <summary>
	/// Trend SMA length.
	/// </summary>
	public int TrendLength { get => _trendLength.Value; set => _trendLength.Value = value; }

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// Enable long signals.
	/// </summary>
	public bool UseLongSignals { get => _useLongSignals.Value; set => _useLongSignals.Value = value; }

	/// <summary>
	/// Enable short signals.
	/// </summary>
	public bool UseShortSignals { get => _useShortSignals.Value; set => _useShortSignals.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public BollingerBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Bollinger Bands period", "Bollinger Bands")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 1.8m)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Multiplier", "Standard deviation multiplier", "Bollinger Bands")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.2m);

		_volumeLength = Param(nameof(VolumeLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume MA Length", "Length for volume moving average", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_trendLength = Param(nameof(TrendLength), 80)
			.SetGreaterThanZero()
			.SetDisplay("Trend MA Length", "Length for trend moving average", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(40, 120, 10);

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation length", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 2);

		_useLongSignals = Param(nameof(UseLongSignals), true)
			.SetDisplay("Use Long Signals", "Enable long signals", "Signals");

		_useShortSignals = Param(nameof(UseShortSignals), true)
			.SetDisplay("Use Short Signals", "Enable short signals", "Signals");
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

		_prevClose = default;
		_prevUpper = default;
		_prevLower = default;
		_isInitial = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_bollinger = new BollingerBands { Length = BollingerLength, Width = BollingerMultiplier };
		_volumeSma = new SimpleMovingAverage { Length = VolumeLength };
		_trendSma = new SimpleMovingAverage { Length = TrendLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_bollinger, _trendSma, _rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand, decimal trendValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeValue = _volumeSma.Process(candle.TotalVolume);
		if (!_bollinger.IsFormed || !_trendSma.IsFormed || !_rsi.IsFormed || !_volumeSma.IsFormed)
			return;

		if (volumeValue.ToNullableDecimal() is not decimal volumeMa)
			return;

		var volConditionLong = candle.TotalVolume > volumeMa * 1.05m;
		var volConditionShort = candle.TotalVolume > volumeMa * 1.2m;
		var trendConditionLong = candle.ClosePrice > trendValue;
		var trendConditionShort = candle.ClosePrice < trendValue;
		var rsiConditionShort = rsiValue < 85m;

		if (_isInitial)
		{
			_prevClose = candle.ClosePrice;
			_prevUpper = upperBand;
			_prevLower = lowerBand;
			_isInitial = false;
			return;
		}

		var bbCrossoverLong = _prevClose <= _prevLower && candle.ClosePrice > lowerBand;
		var bbCrossoverShortExit = _prevClose <= _prevUpper && candle.ClosePrice > upperBand;
		var bbCrossunderShort = _prevClose >= _prevUpper && candle.ClosePrice < upperBand;
		var bbCrossunderLongExit = _prevClose >= _prevLower && candle.ClosePrice < lowerBand;

		if (UseLongSignals && bbCrossoverLong && Position <= 0 && volConditionLong && trendConditionLong)
			RegisterBuy();

		if (UseLongSignals && bbCrossoverShortExit && Position > 0)
			RegisterSell();

		if (UseShortSignals && bbCrossunderShort && Position >= 0 && volConditionShort && trendConditionShort && rsiConditionShort)
			RegisterSell();

		if (UseShortSignals && bbCrossunderLongExit && Position < 0)
			RegisterBuy();

		_prevClose = candle.ClosePrice;
		_prevUpper = upperBand;
		_prevLower = lowerBand;
	}
}
