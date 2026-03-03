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
/// Trades on Tenkan/Kijun crosses with cloud confirmation.
/// </summary>
public class IchimokuKumoBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevTenkanAboveKijun;
	private bool _hasPrevValues;
	private int _candlesSinceLastTrade;

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
			.SetDisplay("Tenkan-sen Period", "Period for Tenkan-sen line", "Indicators")
			.SetOptimize(7, 13, 2);

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetDisplay("Kijun-sen Period", "Period for Kijun-sen line", "Indicators")
			.SetOptimize(20, 30, 2);

		_senkouSpanPeriod = Param(nameof(SenkouSpanPeriod), 52)
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
		_prevTenkanAboveKijun = default;
		_hasPrevValues = default;
		_candlesSinceLastTrade = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanPeriod }
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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ichimokuTyped = (IchimokuValue)ichimokuValue;

		if (ichimokuTyped.Tenkan is not decimal tenkan)
			return;

		if (ichimokuTyped.Kijun is not decimal kijun)
			return;

		// Skip zero values
		if (tenkan == 0 || kijun == 0)
			return;

		var tenkanAboveKijun = tenkan > kijun;

		_candlesSinceLastTrade++;

		if (!_hasPrevValues)
		{
			_hasPrevValues = true;
			_prevTenkanAboveKijun = tenkanAboveKijun;
			return;
		}

		// Detect cross
		var isCross = tenkanAboveKijun != _prevTenkanAboveKijun;
		_prevTenkanAboveKijun = tenkanAboveKijun;

		if (!isCross)
			return;

		// Cooldown to avoid too many trades
		if (_candlesSinceLastTrade < 4)
			return;

		if (tenkanAboveKijun && Position <= 0)
		{
			// Bullish cross
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_candlesSinceLastTrade = 0;
		}
		else if (!tenkanAboveKijun && Position >= 0)
		{
			// Bearish cross
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_candlesSinceLastTrade = 0;
		}
	}
}
