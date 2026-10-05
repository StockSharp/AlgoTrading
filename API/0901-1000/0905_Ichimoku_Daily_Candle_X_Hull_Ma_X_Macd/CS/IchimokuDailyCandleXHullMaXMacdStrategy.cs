using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ichimoku daily candle X Hull MA X MACD strategy.
/// A Hull moving average of HmaPeriod and an HMA-based MACD (HMA(MacdFastLength) - HMA(MacdSlowLength), signal HMA(MacdSignalLength))
/// are computed on the PriceSource price. A long requires a rising HMA, the close above the previous HMA value, the current daily
/// candle's PriceSource price above the previous day's, Senkou A above Senkou B and the MACD line above its signal; a short requires
/// all of them reversed. The opposite signal reverses the position; there are no stops.
/// </summary>
public class IchimokuDailyCandleXHullMaXMacdStrategy : Strategy
{
	/// <summary>
	/// Candle price used by the averages and the daily comparison.
	/// </summary>
	public enum PriceSources
	{
		/// <summary>
		/// Open price.
		/// </summary>
		Open,

		/// <summary>
		/// High price.
		/// </summary>
		High,

		/// <summary>
		/// Low price.
		/// </summary>
		Low,

		/// <summary>
		/// Close price.
		/// </summary>
		Close,
	}

	private readonly StrategyParam<int> _hmaPeriod;
	private readonly StrategyParam<int> _conversionPeriod;
	private readonly StrategyParam<int> _basePeriod;
	private readonly StrategyParam<int> _spanPeriod;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<PriceSources> _priceSource;
	private readonly StrategyParam<DataType> _candleType;

	private HullMovingAverage _hma;
	private HullMovingAverage _macdFast;
	private HullMovingAverage _macdSlow;
	private HullMovingAverage _macdSignal;
	private decimal? _prevHma;
	private DateTime? _currentDay;
	private decimal _currentDayPrice;
	private decimal? _previousDayPrice;

	/// <summary>
	/// Hull moving average period.
	/// </summary>
	public int HmaPeriod
	{
		get => _hmaPeriod.Value;
		set => _hmaPeriod.Value = value;
	}

	/// <summary>
	/// Ichimoku conversion line period.
	/// </summary>
	public int ConversionPeriod
	{
		get => _conversionPeriod.Value;
		set => _conversionPeriod.Value = value;
	}

	/// <summary>
	/// Ichimoku base line period.
	/// </summary>
	public int BasePeriod
	{
		get => _basePeriod.Value;
		set => _basePeriod.Value = value;
	}

	/// <summary>
	/// Ichimoku leading span B period.
	/// </summary>
	public int SpanPeriod
	{
		get => _spanPeriod.Value;
		set => _spanPeriod.Value = value;
	}

	/// <summary>
	/// Fast HMA period of the MACD.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow HMA period of the MACD.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal HMA period of the MACD.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Candle price used by the averages and the daily comparison.
	/// </summary>
	public PriceSources PriceSource
	{
		get => _priceSource.Value;
		set => _priceSource.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public IchimokuDailyCandleXHullMaXMacdStrategy()
	{
		_hmaPeriod = Param(nameof(HmaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("HMA Period", "Hull moving average period", "Indicators");

		_conversionPeriod = Param(nameof(ConversionPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Conversion Period", "Ichimoku conversion line period", "Ichimoku");

		_basePeriod = Param(nameof(BasePeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Base Period", "Ichimoku base line period", "Ichimoku");

		_spanPeriod = Param(nameof(SpanPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Span Period", "Ichimoku leading span B period", "Ichimoku");

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast HMA period of the MACD", "MACD");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow HMA period of the MACD", "MACD");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal HMA period of the MACD", "MACD");

		_priceSource = Param(nameof(PriceSource), PriceSources.Open)
			.SetDisplay("Price Source", "Candle price used by the averages and the daily comparison", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_prevHma = null;
		_currentDay = null;
		_currentDayPrice = 0m;
		_previousDayPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = ConversionPeriod },
			Kijun = { Length = BasePeriod },
			SenkouB = { Length = SpanPeriod },
		};

		_hma = new HullMovingAverage { Length = HmaPeriod };
		_macdFast = new HullMovingAverage { Length = MacdFastLength };
		_macdSlow = new HullMovingAverage { Length = MacdSlowLength };
		_macdSignal = new HullMovingAverage { Length = MacdSignalLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ichimoku, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ichimoku);
			DrawIndicator(area, _hma);
			DrawOwnTrades(area);
		}
	}

	private decimal GetPrice(ICandleMessage candle)
	{
		return PriceSource switch
		{
			PriceSources.High => candle.HighPrice,
			PriceSources.Low => candle.LowPrice,
			PriceSources.Close => candle.ClosePrice,
			_ => candle.OpenPrice,
		};
	}

	private void UpdateDaily(ICandleMessage candle)
	{
		var day = candle.OpenTime.Date;

		if (_currentDay != day)
		{
			if (_currentDay != null)
				_previousDayPrice = _currentDayPrice;

			_currentDay = day;
			_currentDayPrice = GetPrice(candle);
			return;
		}

		// The daily candle is built from the intraday candles of the same day.
		switch (PriceSource)
		{
			case PriceSources.Close:
				_currentDayPrice = candle.ClosePrice;
				break;
			case PriceSources.High:
				_currentDayPrice = Math.Max(_currentDayPrice, candle.HighPrice);
				break;
			case PriceSources.Low:
				_currentDayPrice = Math.Min(_currentDayPrice, candle.LowPrice);
				break;
		}
	}

	private static decimal? ProcessValue(IIndicator indicator, decimal input, DateTime time)
	{
		var value = indicator.Process(new DecimalIndicatorValue(indicator, input, time) { IsFinal = true });
		return indicator.IsFormed && !value.IsEmpty ? value.GetValue<decimal>() : null;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateDaily(candle);

		var price = GetPrice(candle);
		var time = candle.OpenTime;

		var hmaResult = ProcessValue(_hma, price, time);
		var fast = ProcessValue(_macdFast, price, time);
		var slow = ProcessValue(_macdSlow, price, time);

		decimal? macd = fast is decimal f && slow is decimal s ? f - s : null;
		var signal = macd is decimal m ? ProcessValue(_macdSignal, m, time) : null;

		if (hmaResult is not decimal hma)
			return;

		var prevHma = _prevHma;
		_prevHma = hma;

		if (prevHma is not decimal previousHma || macd is not decimal macdLine || signal is not decimal signalLine)
			return;

		if (!ichimokuValue.IsFormed || ichimokuValue is not IchimokuValue { SenkouA: decimal senkouA, SenkouB: decimal senkouB })
			return;

		if (_previousDayPrice is not decimal previousDayPrice)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		var longSignal = hma > previousHma && close > previousHma && _currentDayPrice > previousDayPrice && senkouA > senkouB && macdLine > signalLine;
		var shortSignal = hma < previousHma && close < previousHma && _currentDayPrice < previousDayPrice && senkouA < senkouB && macdLine < signalLine;

		if (longSignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
