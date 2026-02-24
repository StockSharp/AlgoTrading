using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Detects entries when candle wicks touch Bollinger Bands.
/// Long entries occur on large lower wicks below the lower band.
/// Short entries occur on large upper wicks above the upper band.
/// Exits at the opposite band or when a swing level is broken.
/// </summary>
public class TradeEntryDetectorWickToBodyRatioStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<decimal> _wickToBodyRatio;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _swingLow;
	private decimal? _swingHigh;

	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	public decimal BollingerWidth
	{
		get => _bollingerWidth.Value;
		set => _bollingerWidth.Value = value;
	}

	public decimal WickToBodyRatio
	{
		get => _wickToBodyRatio.Value;
		set => _wickToBodyRatio.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public TradeEntryDetectorWickToBodyRatioStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Period of Bollinger Bands", "Indicator");

		_bollingerWidth = Param(nameof(BollingerWidth), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Width", "Standard deviation multiplier", "Indicator");

		_wickToBodyRatio = Param(nameof(WickToBodyRatio), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Wick/Body Ratio", "Minimum wick to body ratio", "Setup");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for processing", "General");
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
		_swingLow = null;
		_swingHigh = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = BollingerPeriod };
		var stdDev = new StandardDeviation { Length = BollingerPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, stdDev, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal stdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (stdVal <= 0)
			return;

		var upper = middle + BollingerWidth * stdVal;
		var lower = middle - BollingerWidth * stdVal;

		var body = Math.Abs(candle.OpenPrice - candle.ClosePrice);
		if (body == 0)
			return;

		var upperWick = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
		var lowerWick = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;

		// Exit checks first
		if (Position > 0)
		{
			if (candle.ClosePrice >= upper)
			{
				SellMarket();
				_swingLow = null;
				_swingHigh = null;
				return;
			}
			else if (_swingLow is decimal stop && candle.ClosePrice <= stop)
			{
				SellMarket();
				_swingLow = null;
				return;
			}
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice <= lower)
			{
				BuyMarket();
				_swingLow = null;
				_swingHigh = null;
				return;
			}
			else if (_swingHigh is decimal stop && candle.ClosePrice >= stop)
			{
				BuyMarket();
				_swingHigh = null;
				return;
			}
		}

		// Entry signals
		if (lowerWick / body >= WickToBodyRatio && candle.LowPrice < lower && Position <= 0)
		{
			_swingLow = candle.LowPrice;
			BuyMarket();
		}
		else if (upperWick / body >= WickToBodyRatio && candle.HighPrice > upper && Position >= 0)
		{
			_swingHigh = candle.HighPrice;
			SellMarket();
		}
	}
}
