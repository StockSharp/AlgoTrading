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
/// Enters long when Tenkan crosses above Kijun and price is above Kumo.
/// Enters short when Tenkan crosses below Kijun and price is below Kumo.
/// Exits on opposite cross or Kumo breach.
/// </summary>
public class IchimokuTenkanKijunStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevTenkan;
	private decimal _prevKijun;
	private int _cooldown;

	/// <summary>
	/// Tenkan period.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Kijun period.
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
	/// Cooldown bars.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public IchimokuTenkanKijunStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetRange(7, 13)
			.SetDisplay("Tenkan Period", "Period for Tenkan-sen", "Ichimoku");

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetRange(20, 30)
			.SetDisplay("Kijun Period", "Period for Kijun-sen", "Ichimoku");

		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
			.SetRange(40, 60)
			.SetDisplay("Senkou Span B Period", "Period for Senkou Span B", "Ichimoku");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 500)
			.SetRange(1, 1000)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");
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
		_prevTenkan = default;
		_prevKijun = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevTenkan = 0;
		_prevKijun = 0;
		_cooldown = 0;

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

		if (!ichimokuIv.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var iv = (IchimokuValue)ichimokuIv;

		if (iv.Tenkan is not decimal tenkan || iv.Kijun is not decimal kijun ||
		    iv.SenkouA is not decimal senkouA || iv.SenkouB is not decimal senkouB)
			return;

		if (_prevTenkan == 0 || _prevKijun == 0)
		{
			_prevTenkan = tenkan;
			_prevKijun = kijun;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevTenkan = tenkan;
			_prevKijun = kijun;
			return;
		}

		var bullishCross = _prevTenkan <= _prevKijun && tenkan > kijun;
		var bearishCross = _prevTenkan >= _prevKijun && tenkan < kijun;

		var upperKumo = Math.Max(senkouA, senkouB);
		var lowerKumo = Math.Min(senkouA, senkouB);

		if (Position == 0 && bullishCross && candle.ClosePrice > upperKumo)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && bearishCross && candle.ClosePrice < lowerKumo)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && (bearishCross || candle.ClosePrice < lowerKumo))
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && (bullishCross || candle.ClosePrice > upperKumo))
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevTenkan = tenkan;
		_prevKijun = kijun;
	}
}
