using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ichimoku strategy using Tenkan/Kijun crossover (midline of short/long channels).
/// </summary>
public class ExpertIchimokuStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;

	private decimal? _prevTenkan;
	private decimal? _prevKijun;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	public ExpertIchimokuStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Short channel period", "Indicators");

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Long channel period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevTenkan = null;
		_prevKijun = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevTenkan = null;
		_prevKijun = null;

		// Tenkan: midline of short highest/lowest
		var tenkanHigh = new Highest { Length = TenkanPeriod };
		var tenkanLow = new Lowest { Length = TenkanPeriod };

		// Kijun: midline of long highest/lowest
		var kijunHigh = new Highest { Length = KijunPeriod };
		var kijunLow = new Lowest { Length = KijunPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(tenkanHigh, tenkanLow, kijunHigh, kijunLow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal tHigh, decimal tLow, decimal kHigh, decimal kLow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var tenkan = (tHigh + tLow) / 2;
		var kijun = (kHigh + kLow) / 2;

		if (_prevTenkan == null || _prevKijun == null)
		{
			_prevTenkan = tenkan;
			_prevKijun = kijun;
			return;
		}

		// Tenkan crosses above Kijun → buy
		if (_prevTenkan.Value <= _prevKijun.Value && tenkan > kijun)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		// Tenkan crosses below Kijun → sell
		else if (_prevTenkan.Value >= _prevKijun.Value && tenkan < kijun)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}

		_prevTenkan = tenkan;
		_prevKijun = kijun;
	}
}
