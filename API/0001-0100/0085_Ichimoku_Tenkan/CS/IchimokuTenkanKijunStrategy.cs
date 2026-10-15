using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ichimoku Tenkan/Kijun Cross strategy.
/// Tenkan-sen crossing above Kijun-sen while the close is above the cloud opens a long position, the opposite cross below the
/// cloud a short one. The stop is the Kijun-sen at entry; an opposite cross closes the position, reversing it when the close
/// is on the other side of the cloud.
/// </summary>
public class IchimokuTenkanKijunStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private bool? _prevTenkanAbove;
	private decimal _stopPrice;

	/// <summary>
	/// Tenkan-sen period.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Kijun-sen period.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Senkou Span B period.
	/// </summary>
	public int SenkouSpanBPeriod
	{
		get => _senkouSpanBPeriod.Value;
		set => _senkouSpanBPeriod.Value = value;
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
	public IchimokuTenkanKijunStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Period for Tenkan-sen", "Ichimoku");

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Period for Kijun-sen", "Ichimoku");

		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Senkou Span B Period", "Period for Senkou Span B", "Ichimoku");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
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
		_prevTenkanAbove = null;
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevTenkanAbove = null;
		_stopPrice = default;

		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanBPeriod }
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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuIv)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (ichimokuIv is not IIchimokuValue { Tenkan: decimal tenkan, Kijun: decimal kijun, SenkouA: decimal senkouA, SenkouB: decimal senkouB })
			return;

		// Equal lines are no cross; the previous side holds.
		var wasAbove = _prevTenkanAbove;

		if (tenkan != kijun)
			_prevTenkanAbove = tenkan > kijun;

		if (wasAbove is not bool previous || !IsFormedAndOnlineAndAllowTrading())
			return;

		var bullishCross = !previous && tenkan > kijun;
		var bearishCross = previous && tenkan < kijun;
		var close = candle.ClosePrice;
		var upperKumo = Math.Max(senkouA, senkouB);
		var lowerKumo = Math.Min(senkouA, senkouB);

		if (Position > 0)
		{
			if (bearishCross && close < lowerKumo)
			{
				SellMarket(Volume + Position);
				_stopPrice = kijun;
			}
			else if (bearishCross || close <= _stopPrice)
			{
				SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			if (bullishCross && close > upperKumo)
			{
				BuyMarket(Volume - Position);
				_stopPrice = kijun;
			}
			else if (bullishCross || close >= _stopPrice)
			{
				BuyMarket(-Position);
			}
		}
		else if (bullishCross && close > upperKumo)
		{
			BuyMarket(Volume);
			_stopPrice = kijun;
		}
		else if (bearishCross && close < lowerKumo)
		{
			SellMarket(Volume);
			_stopPrice = kijun;
		}
	}
}
