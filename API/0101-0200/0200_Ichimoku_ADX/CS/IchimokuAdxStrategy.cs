using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ichimoku ADX strategy.
/// A close above the cloud with Tenkan-sen above Kijun-sen while ADX is above AdxThreshold goes long, a close below the cloud with Tenkan-sen
/// below Kijun-sen under the same ADX condition goes short, reversing an opposite position. The cloud is the trailing stop: a long closes
/// when price closes below the cloud and a short when it closes above it.
/// </summary>
public class IchimokuAdxStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Period of Tenkan-sen.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Period of Kijun-sen.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Period of Senkou Span B.
	/// </summary>
	public int SenkouSpanBPeriod
	{
		get => _senkouSpanBPeriod.Value;
		set => _senkouSpanBPeriod.Value = value;
	}

	/// <summary>
	/// Period of ADX.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX level of a strong trend.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
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
	public IchimokuAdxStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Period of Tenkan-sen", "Ichimoku");

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Period of Kijun-sen", "Ichimoku");

		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Senkou Span B Period", "Period of Senkou Span B", "Ichimoku");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period of ADX", "ADX");

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetDisplay("ADX Threshold", "ADX level of a strong trend", "ADX");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanBPeriod }
		};
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ichimoku, adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ichimoku);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, adx);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (ichimokuValue is not IIchimokuValue { Tenkan: decimal tenkan, Kijun: decimal kijun, SenkouA: decimal senkouA, SenkouB: decimal senkouB })
			return;

		if (!adxValue.IsFormed || adxValue is not AverageDirectionalIndexValue { MovingAverage: decimal strength })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var cloudTop = Math.Max(senkouA, senkouB);
		var cloudBottom = Math.Min(senkouA, senkouB);
		var strong = strength > AdxThreshold;

		if (close > cloudTop && tenkan > kijun && strong && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < cloudBottom && tenkan < kijun && strong && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && close < cloudBottom)
			SellMarket(Position);
		else if (Position < 0 && close > cloudTop)
			BuyMarket(-Position);
	}
}
