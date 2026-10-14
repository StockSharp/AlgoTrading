using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams R Ichimoku strategy.
/// Williams %R below WilliamsROversold with a close above the cloud and Tenkan-sen above Kijun-sen goes long, %R above WilliamsROverbought
/// with a close below the cloud and Tenkan-sen below Kijun-sen goes short, reversing an opposite position. The cloud is the trailing stop: a long closes
/// when price closes below the cloud and a short when it closes above it.
/// </summary>
public class WilliamsIchimokuStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<int> _williamsRPeriod;
	private readonly StrategyParam<decimal> _williamsROversold;
	private readonly StrategyParam<decimal> _williamsROverbought;
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
	/// Period of Williams %R.
	/// </summary>
	public int WilliamsRPeriod
	{
		get => _williamsRPeriod.Value;
		set => _williamsRPeriod.Value = value;
	}

	/// <summary>
	/// Williams %R level for longs.
	/// </summary>
	public decimal WilliamsROversold
	{
		get => _williamsROversold.Value;
		set => _williamsROversold.Value = value;
	}

	/// <summary>
	/// Williams %R level for shorts.
	/// </summary>
	public decimal WilliamsROverbought
	{
		get => _williamsROverbought.Value;
		set => _williamsROverbought.Value = value;
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
	public WilliamsIchimokuStrategy()
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

		_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Period of Williams %R", "Williams %R");

		_williamsROversold = Param(nameof(WilliamsROversold), -80m)
			.SetDisplay("Williams %R Oversold", "Williams %R level for longs", "Williams %R");

		_williamsROverbought = Param(nameof(WilliamsROverbought), -20m)
			.SetDisplay("Williams %R Overbought", "Williams %R level for shorts", "Williams %R");

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
		var williams = new WilliamsR { Length = WilliamsRPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(williams, ichimoku, ProcessCandle)
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
				DrawIndicator(oscillators, williams);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue williamsValue, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (ichimokuValue is not IIchimokuValue { Tenkan: decimal tenkan, Kijun: decimal kijun, SenkouA: decimal senkouA, SenkouB: decimal senkouB })
			return;

		if (!williamsValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var cloudTop = Math.Max(senkouA, senkouB);
		var cloudBottom = Math.Min(senkouA, senkouB);
		var williams = williamsValue.GetValue<decimal>();

		if (williams < WilliamsROversold && close > cloudTop && tenkan > kijun && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (williams > WilliamsROverbought && close < cloudBottom && tenkan < kijun && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && close < cloudBottom)
			SellMarket(Position);
		else if (Position < 0 && close > cloudTop)
			BuyMarket(-Position);
	}
}
