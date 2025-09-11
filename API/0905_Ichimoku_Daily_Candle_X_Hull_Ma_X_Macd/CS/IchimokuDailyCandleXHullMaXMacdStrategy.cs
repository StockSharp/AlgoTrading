using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining Ichimoku lines, daily candle direction,
/// Hull Moving Average trend and MACD built on HMAs.
/// </summary>
public class IchimokuDailyCandleXHullMaXMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _hmaPeriod;
	private readonly StrategyParam<int> _conversionPeriod;
	private readonly StrategyParam<int> _basePeriod;
	private readonly StrategyParam<int> _spanPeriod;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<CandlePrice> _priceSource;
	private readonly StrategyParam<DataType> _candleType;

	private HullMovingAverage _hma;
	private HullMovingAverage _macdFast;
	private HullMovingAverage _macdSlow;
	private HullMovingAverage _macdSignal;
	private Ichimoku _ichimoku;

	private decimal _prevHma;
	private decimal _dailyNow;
	private decimal _dailyPrev;

	/// <summary>
	/// Hull MA period.
	/// </summary>
	public int HmaPeriod { get => _hmaPeriod.Value; set => _hmaPeriod.Value = value; }

	/// <summary>
	/// Tenkan period for Ichimoku.
	/// </summary>
	public int ConversionPeriod { get => _conversionPeriod.Value; set => _conversionPeriod.Value = value; }

	/// <summary>
	/// Kijun period for Ichimoku.
	/// </summary>
	public int BasePeriod { get => _basePeriod.Value; set => _basePeriod.Value = value; }

	/// <summary>
	/// Senkou Span B period for Ichimoku.
	/// </summary>
	public int SpanPeriod { get => _spanPeriod.Value; set => _spanPeriod.Value = value; }

	/// <summary>
	/// Fast HMA length for MACD line.
	/// </summary>
	public int MacdFastLength { get => _macdFastLength.Value; set => _macdFastLength.Value = value; }

	/// <summary>
	/// Slow HMA length for MACD line.
	/// </summary>
	public int MacdSlowLength { get => _macdSlowLength.Value; set => _macdSlowLength.Value = value; }

	/// <summary>
	/// HMA length for MACD signal.
	/// </summary>
	public int MacdSignalLength { get => _macdSignalLength.Value; set => _macdSignalLength.Value = value; }

	/// <summary>
	/// Price source for calculations.
	/// </summary>
	public CandlePrice PriceSource { get => _priceSource.Value; set => _priceSource.Value = value; }

	/// <summary>
	/// Candle type for main timeframe.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public IchimokuDailyCandleXHullMaXMacdStrategy()
	{
		_hmaPeriod = Param(nameof(HmaPeriod), 14)
			.SetDisplay("HMA Period", "Hull MA period", "Indicators");

		_conversionPeriod = Param(nameof(ConversionPeriod), 9)
			.SetDisplay("Tenkan Period", "Ichimoku Tenkan period", "Ichimoku");

		_basePeriod = Param(nameof(BasePeriod), 26)
			.SetDisplay("Kijun Period", "Ichimoku Kijun period", "Ichimoku");

		_spanPeriod = Param(nameof(SpanPeriod), 52)
			.SetDisplay("Span Period", "Ichimoku Senkou Span B period", "Ichimoku");

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetDisplay("MACD Fast", "Fast HMA length", "MACD");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetDisplay("MACD Slow", "Slow HMA length", "MACD");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetDisplay("MACD Signal", "Signal HMA length", "MACD");

		_priceSource = Param(nameof(PriceSource), CandlePrice.Open)
			.SetDisplay("Price Source", "Candle price for indicators", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Main candle timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, TimeSpan.FromDays(1).TimeFrame())];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHma = default;
		_dailyNow = default;
		_dailyPrev = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_hma = new HullMovingAverage { Length = HmaPeriod, CandlePrice = PriceSource };
		_macdFast = new HullMovingAverage { Length = MacdFastLength, CandlePrice = PriceSource };
		_macdSlow = new HullMovingAverage { Length = MacdSlowLength, CandlePrice = PriceSource };
		_macdSignal = new HullMovingAverage { Length = MacdSignalLength };
		_ichimoku = new Ichimoku
		{
			Tenkan = { Length = ConversionPeriod },
			Kijun = { Length = BasePeriod },
			SenkouB = { Length = SpanPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_hma, _macdFast, _macdSlow, _ichimoku, ProcessCandle)
			.Start();

		var dailySubscription = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		dailySubscription
			.Bind(ProcessDaily)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _hma);
			DrawIndicator(area, _ichimoku);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDaily(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_dailyPrev = _dailyNow;
		_dailyNow = GetPrice(candle);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue hmaValue, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hma = hmaValue.ToDecimal();
		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();
		var macdLine = fast - slow;
		var signal = _macdSignal.Process(macdLine, candle.OpenTime, true).ToDecimal();

		var ichimokuTyped = (IchimokuValue)ichimokuValue;
		if (ichimokuTyped.SenkouA is not decimal lead1 || ichimokuTyped.SenkouB is not decimal lead2)
			return;

		if (_dailyPrev == default)
		{
			_prevHma = hma;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevHma = hma;
			return;
		}

		var price = GetPrice(candle);
		var hmaBull = hma > _prevHma;
		var hmaBear = hma < _prevHma;
		var dailyBull = _dailyNow > _dailyPrev;
		var dailyBear = _dailyNow < _dailyPrev;

		var longCondition = hmaBull && dailyBull && price > _prevHma && lead1 > lead2 && macdLine > signal;
		var shortCondition = hmaBear && dailyBear && price < _prevHma && lead1 < lead2 && macdLine < signal;

		if (longCondition && Position <= 0)
			BuyMarket();
		else if (shortCondition && Position >= 0)
			SellMarket();

		_prevHma = hma;
	}

	private decimal GetPrice(ICandleMessage candle)
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
			_ => candle.ClosePrice,
		};
	}
}
