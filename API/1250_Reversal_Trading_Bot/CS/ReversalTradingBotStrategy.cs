using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Reversal Trading Bot Strategy using RSI divergence with optional filters.
/// </summary>
public class ReversalTradingBotStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _fastRsiLength;
	private readonly StrategyParam<int> _slowRsiLength;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<int> _adxThreshold;
	private readonly StrategyParam<int> _divLookback;
	private readonly StrategyParam<bool> _volumeFilter;
	private readonly StrategyParam<bool> _adxFilter;
	private readonly StrategyParam<bool> _bbFilter;
	private readonly StrategyParam<bool> _rsiCrossFilter;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private Lowest _priceLowest;
	private Lowest _rsiLowest;
	private Highest _priceHighest;
	private Highest _rsiHighest;

	private decimal? _prevPriceLow;
	private decimal? _prevPriceLow1;
	private decimal? _prevRsiLow;
	private decimal? _prevRsiLow1;
	private decimal? _prevPriceHigh;
	private decimal? _prevPriceHigh1;
	private decimal? _prevRsiHigh;
	private decimal? _prevRsiHigh1;
	private decimal? _prevFastRsi;
	private decimal? _prevSlowRsi;

	/// <summary>
	/// Initializes a new instance of <see cref="ReversalTradingBotStrategy"/>.
	/// </summary>
	public ReversalTradingBotStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 8)
			.SetDisplay("RSI Length", "Number of bars for RSI", "Parameters")
			.SetGreaterThanZero();
		_fastRsiLength = Param(nameof(FastRsiLength), 14)
			.SetDisplay("Fast RSI Length", "Bars for fast RSI", "Parameters")
			.SetGreaterThanZero();
		_slowRsiLength = Param(nameof(SlowRsiLength), 21)
			.SetDisplay("Slow RSI Length", "Bars for slow RSI", "Parameters")
			.SetGreaterThanZero();
		_bbLength = Param(nameof(BbLength), 20)
			.SetDisplay("BB Length", "Bars for Bollinger Bands", "Parameters")
			.SetGreaterThanZero();
		_adxThreshold = Param(nameof(AdxThreshold), 20)
			.SetDisplay("ADX Threshold", "Minimum ADX to confirm trend", "Parameters")
			.SetGreaterThanZero();
		_divLookback = Param(nameof(DivLookback), 5)
			.SetDisplay("Divergence Lookback", "Bars for divergence detection", "Parameters")
			.SetGreaterThanZero();
		_volumeFilter = Param(nameof(VolumeFilter), false)
			.SetDisplay("Volume ≥ 2× SMA", "Require volume at least 2× SMA", "Filters");
		_adxFilter = Param(nameof(AdxFilter), false)
			.SetDisplay("ADX Strength & Alignment", "Require ADX above threshold and DI alignment", "Filters");
		_bbFilter = Param(nameof(BbFilter), false)
			.SetDisplay("BB Close Confirmation", "Require close beyond Bollinger Band", "Filters");
		_rsiCrossFilter = Param(nameof(RsiCrossFilter), false)
			.SetDisplay("RSI Crossover Confirmation", "Require fast RSI crossover", "Filters");
		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetDisplay("Stop-Loss %", "Percent below/above entry for stop-loss", "Protection");
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetDisplay("Take-Profit %", "Percent above/below entry for take-profit", "Protection");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "Parameters");
	}

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Fast RSI length.
	/// </summary>
	public int FastRsiLength
	{
		get => _fastRsiLength.Value;
		set => _fastRsiLength.Value = value;
	}

	/// <summary>
	/// Slow RSI length.
	/// </summary>
	public int SlowRsiLength
	{
		get => _slowRsiLength.Value;
		set => _slowRsiLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands length.
	/// </summary>
	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// ADX threshold.
	/// </summary>
	public int AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Lookback for divergence detection.
	/// </summary>
	public int DivLookback
	{
		get => _divLookback.Value;
		set => _divLookback.Value = value;
	}

	/// <summary>
	/// Use volume filter.
	/// </summary>
	public bool VolumeFilter
	{
		get => _volumeFilter.Value;
		set => _volumeFilter.Value = value;
	}

	/// <summary>
	/// Use ADX filter.
	/// </summary>
	public bool AdxFilter
	{
		get => _adxFilter.Value;
		set => _adxFilter.Value = value;
	}

	/// <summary>
	/// Use Bollinger Bands filter.
	/// </summary>
	public bool BbFilter
	{
		get => _bbFilter.Value;
		set => _bbFilter.Value = value;
	}

	/// <summary>
	/// Use RSI crossover filter.
	/// </summary>
	public bool RsiCrossFilter
	{
		get => _rsiCrossFilter.Value;
		set => _rsiCrossFilter.Value = value;
	}

	/// <summary>
	/// Stop-loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take-profit percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Candle type used by strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_priceLowest = default;
		_rsiLowest = default;
		_priceHighest = default;
		_rsiHighest = default;
		_prevPriceLow = null;
		_prevPriceLow1 = null;
		_prevRsiLow = null;
		_prevRsiLow1 = null;
		_prevPriceHigh = null;
		_prevPriceHigh1 = null;
		_prevRsiHigh = null;
		_prevRsiHigh1 = null;
		_prevFastRsi = null;
		_prevSlowRsi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var fastRsi = new RelativeStrengthIndex { Length = FastRsiLength };
		var slowRsi = new RelativeStrengthIndex { Length = SlowRsiLength };
		var adx = new AverageDirectionalIndex { Length = 14 };
		var bb = new BollingerBands { Length = BbLength, Width = 2m };
		var volSma = new SimpleMovingAverage { Length = 20 };

		_priceLowest = new Lowest { Length = DivLookback };
		_rsiLowest = new Lowest { Length = DivLookback };
		_priceHighest = new Highest { Length = DivLookback };
		_rsiHighest = new Highest { Length = DivLookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, fastRsi, slowRsi, adx, bb, volSma, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawIndicator(area, adx);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle,
		IIndicatorValue rsiVal,
		IIndicatorValue fastRsiVal,
		IIndicatorValue slowRsiVal,
		IIndicatorValue adxVal,
		IIndicatorValue bbVal,
		IIndicatorValue volSmaVal)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!rsiVal.IsFinal || !fastRsiVal.IsFinal || !slowRsiVal.IsFinal || !adxVal.IsFinal || !bbVal.IsFinal || !volSmaVal.IsFinal)
		return;

		var rsi = rsiVal.GetValue<decimal>();
		var fastRsi = fastRsiVal.GetValue<decimal>();
		var slowRsi = slowRsiVal.GetValue<decimal>();
		var adx = (AverageDirectionalIndexValue)adxVal;
		var bb = (BollingerBandsValue)bbVal;
		var volSma = volSmaVal.GetValue<decimal>();

		var priceLow = _priceLowest.Process(candle.LowPrice).ToDecimal();
		var rsiLow = _rsiLowest.Process(rsi).ToDecimal();
		var priceHigh = _priceHighest.Process(candle.HighPrice).ToDecimal();
		var rsiHigh = _rsiHighest.Process(rsi).ToDecimal();

		var bullDiv = false;
		var bearDiv = false;

		if (_prevPriceLow1.HasValue && _prevRsiLow1.HasValue)
		bullDiv = _prevPriceLow < _prevPriceLow1 && _prevRsiLow > _prevRsiLow1;

		if (_prevPriceHigh1.HasValue && _prevRsiHigh1.HasValue)
		bearDiv = _prevPriceHigh > _prevPriceHigh1 && _prevRsiHigh < _prevRsiHigh1;

		_prevPriceLow1 = _prevPriceLow;
		_prevPriceLow = priceLow;
		_prevRsiLow1 = _prevRsiLow;
		_prevRsiLow = rsiLow;

		_prevPriceHigh1 = _prevPriceHigh;
		_prevPriceHigh = priceHigh;
		_prevRsiHigh1 = _prevRsiHigh;
		_prevRsiHigh = rsiHigh;

		var fastCrossUp = false;
		var fastCrossDown = false;
		if (_prevFastRsi.HasValue && _prevSlowRsi.HasValue)
		{
		fastCrossUp = _prevFastRsi <= _prevSlowRsi && fastRsi > slowRsi;
		fastCrossDown = _prevFastRsi >= _prevSlowRsi && fastRsi < slowRsi;
		}

		_prevFastRsi = fastRsi;
		_prevSlowRsi = slowRsi;

		var rsiCrossOkBull = !RsiCrossFilter || fastCrossUp;
		var rsiCrossOkBear = !RsiCrossFilter || fastCrossDown;

		var volOkBull = !VolumeFilter || candle.TotalVolume >= 2m * volSma;
		var volOkBear = volOkBull;
		var adxOkBull = !AdxFilter || (adx.MovingAverage > AdxThreshold && adx.Dx.Plus > adx.Dx.Minus);
		var adxOkBear = !AdxFilter || (adx.MovingAverage > AdxThreshold && adx.Dx.Minus > adx.Dx.Plus);
		var bbOkBull = !BbFilter || candle.ClosePrice <= bb.LowBand;
		var bbOkBear = !BbFilter || candle.ClosePrice >= bb.UpBand;

		var bullEntry = bullDiv && volOkBull && adxOkBull && bbOkBull && rsiCrossOkBull;
		var bearEntry = bearDiv && volOkBear && adxOkBear && bbOkBear && rsiCrossOkBear;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var volume = Volume + Math.Abs(Position);

		if (bullEntry && Position <= 0)
		BuyMarket(volume);

		if (bearEntry && Position >= 0)
		SellMarket(volume);
	}
}
