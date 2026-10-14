using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Ichimoku Kumo (cloud) breakout.
/// Buys when the close is above the cloud with Tenkan-sen above Kijun-sen, sells when it is below the cloud with Tenkan-sen
/// below Kijun-sen, acting when the last of the two conditions appears. A position is held until the close goes through the cloud.
/// </summary>
public class IchimokuKumoBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private bool? _prevLongSetup;
	private bool? _prevShortSetup;

	/// <summary>
	/// Period for Tenkan-sen line.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Period for Kijun-sen line.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Period for Senkou Span B.
	/// </summary>
	public int SenkouSpanPeriod
	{
		get => _senkouSpanPeriod.Value;
		set => _senkouSpanPeriod.Value = value;
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
	/// Initialize the Ichimoku Kumo Breakout strategy.
	/// </summary>
	public IchimokuKumoBreakoutStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan-sen Period", "Period for Tenkan-sen line", "Indicators")
			.SetOptimize(7, 13, 2);

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun-sen Period", "Period for Kijun-sen line", "Indicators")
			.SetOptimize(20, 30, 2);

		_senkouSpanPeriod = Param(nameof(SenkouSpanPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Senkou Span B Period", "Period for Senkou Span B", "Indicators")
			.SetOptimize(40, 60, 4);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_prevLongSetup = null;
		_prevShortSetup = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanPeriod },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ichimoku, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ichimoku);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The cloud is plotted ahead, so the spans of this candle were set Kijun periods ago.
		if (value is not IIchimokuValue { Tenkan: decimal tenkan, Kijun: decimal kijun, SenkouA: decimal spanA, SenkouB: decimal spanB })
			return;

		var close = candle.ClosePrice;
		var cloudTop = Math.Max(spanA, spanB);
		var cloudBottom = Math.Min(spanA, spanB);

		var longSetup = close > cloudTop && tenkan > kijun;
		var shortSetup = close < cloudBottom && tenkan < kijun;

		var wasLongSetup = _prevLongSetup;
		var wasShortSetup = _prevShortSetup;
		_prevLongSetup = longSetup;
		_prevShortSetup = shortSetup;

		if (wasLongSetup is not bool wasLong || wasShortSetup is not bool wasShort)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (longSetup && !wasLong && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortSetup && !wasShort && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (Position > 0 && close < cloudBottom)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && close > cloudTop)
		{
			BuyMarket(-Position);
		}
	}
}
