using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Tenkan/Kijun cross strategy based on Ichimoku indicator.
/// Buys when Tenkan crosses above Kijun, sells when Tenkan crosses below Kijun.
/// Uses SMA proxies for Tenkan (short) and Kijun (long) since Ichimoku complex type
/// requires BindEx and special value handling.
/// </summary>
public class TenKijunCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;

	private readonly Queue<decimal> _highsTenkan = new();
	private readonly Queue<decimal> _lowsTenkan = new();
	private readonly Queue<decimal> _highsKijun = new();
	private readonly Queue<decimal> _lowsKijun = new();
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

	public TenKijunCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for Ichimoku calculations", "General");

		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Tenkan-sen conversion line period", "Indicators");

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Kijun-sen base line period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevTenkan = null;
		_prevKijun = null;
		_highsTenkan.Clear();
		_lowsTenkan.Clear();
		_highsKijun.Clear();
		_lowsKijun.Clear();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Compute Tenkan-sen = (highest high + lowest low) / 2 over TenkanPeriod
		_highsTenkan.Enqueue(candle.HighPrice);
		_lowsTenkan.Enqueue(candle.LowPrice);
		if (_highsTenkan.Count > TenkanPeriod)
		{
			_highsTenkan.Dequeue();
			_lowsTenkan.Dequeue();
		}

		// Compute Kijun-sen = (highest high + lowest low) / 2 over KijunPeriod
		_highsKijun.Enqueue(candle.HighPrice);
		_lowsKijun.Enqueue(candle.LowPrice);
		if (_highsKijun.Count > KijunPeriod)
		{
			_highsKijun.Dequeue();
			_lowsKijun.Dequeue();
		}

		if (_highsTenkan.Count < TenkanPeriod || _highsKijun.Count < KijunPeriod)
			return;

		var tenkan = (Max(_highsTenkan) + Min(_lowsTenkan)) / 2;
		var kijun = (Max(_highsKijun) + Min(_lowsKijun)) / 2;

		if (_prevTenkan is null || _prevKijun is null)
		{
			_prevTenkan = tenkan;
			_prevKijun = kijun;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		var crossUp = _prevTenkan.Value <= _prevKijun.Value && tenkan > kijun;
		var crossDown = _prevTenkan.Value >= _prevKijun.Value && tenkan < kijun;

		if (crossUp)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (crossDown)
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position >= 0)
				SellMarket(volume);
		}

		_prevTenkan = tenkan;
		_prevKijun = kijun;
	}

	private static decimal Max(Queue<decimal> queue)
	{
		decimal max = decimal.MinValue;
		foreach (var v in queue)
			if (v > max) max = v;
		return max;
	}

	private static decimal Min(Queue<decimal> queue)
	{
		decimal min = decimal.MaxValue;
		foreach (var v in queue)
			if (v < min) min = v;
		return min;
	}
}
