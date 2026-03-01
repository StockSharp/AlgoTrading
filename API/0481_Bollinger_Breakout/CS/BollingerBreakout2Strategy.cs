namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// 4H Bollinger Breakout Strategy - trades Bollinger Band breakouts with trend and RSI filters.
/// </summary>
public class BollingerBreakout2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<bool> _useLongSignals;
	private readonly StrategyParam<bool> _useShortSignals;

	private decimal _prevClose;
	private decimal _prevUpper;
	private decimal _prevLower;
	private bool _isInitial = true;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int BollingerLength { get => _bollingerLength.Value; set => _bollingerLength.Value = value; }
	public decimal BollingerMultiplier { get => _bollingerMultiplier.Value; set => _bollingerMultiplier.Value = value; }
	public int TrendLength { get => _trendLength.Value; set => _trendLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public bool UseLongSignals { get => _useLongSignals.Value; set => _useLongSignals.Value = value; }
	public bool UseShortSignals { get => _useShortSignals.Value; set => _useShortSignals.Value = value; }

	public BollingerBreakout2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Bollinger Bands period", "Bollinger Bands")
			.SetOptimize(10, 40, 5);

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 1.8m)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Multiplier", "Standard deviation multiplier", "Bollinger Bands")
			.SetOptimize(1m, 3m, 0.2m);

		_trendLength = Param(nameof(TrendLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Trend MA Length", "Length for trend moving average", "Filters")
			.SetOptimize(30, 120, 10);

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation length", "Filters")
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
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = 0;
		_prevUpper = 0;
		_prevLower = 0;
		_isInitial = true;

		var trendSma = new SimpleMovingAverage { Length = TrendLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var bollinger = new BollingerBands { Length = BollingerLength, Width = BollingerMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, trendSma, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue trendValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bb = (BollingerBandsValue)bollingerValue;

		if (bb.UpBand is not decimal upperBand ||
			bb.LowBand is not decimal lowerBand ||
			bb.MovingAverage is not decimal middleBand)
			return;

		var trend = trendValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();

		var trendConditionLong = candle.ClosePrice > trend;
		var trendConditionShort = candle.ClosePrice < trend;
		var rsiConditionShort = rsi < 85m;

		if (_isInitial)
		{
			_prevClose = candle.ClosePrice;
			_prevUpper = upperBand;
			_prevLower = lowerBand;
			_isInitial = false;
			return;
		}

		// Bollinger band crossover detection
		var bbCrossoverLong = _prevClose <= _prevLower && candle.ClosePrice > lowerBand;
		var bbCrossoverShortExit = _prevClose <= _prevUpper && candle.ClosePrice > upperBand;
		var bbCrossunderShort = _prevClose >= _prevUpper && candle.ClosePrice < upperBand;
		var bbCrossunderLongExit = _prevClose >= _prevLower && candle.ClosePrice < lowerBand;

		if (UseLongSignals && bbCrossoverLong && Position <= 0 && trendConditionLong)
			BuyMarket();

		if (UseLongSignals && bbCrossoverShortExit && Position > 0)
			SellMarket();

		if (UseShortSignals && bbCrossunderShort && Position >= 0 && trendConditionShort && rsiConditionShort)
			SellMarket();

		if (UseShortSignals && bbCrossunderLongExit && Position < 0)
			BuyMarket();

		_prevClose = candle.ClosePrice;
		_prevUpper = upperBand;
		_prevLower = lowerBand;
	}
}
