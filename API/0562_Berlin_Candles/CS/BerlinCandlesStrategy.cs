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
/// Strategy based on Berlin candles with Donchian baseline.
/// </summary>
public class BerlinCandlesStrategy : Strategy
{
	private readonly StrategyParam<int> _smoothing;
	private readonly StrategyParam<int> _baselinePeriod;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;
	private DonchianChannels _donchian;
	private decimal _prevEma;
	private bool _isInitialized;
	private int _cooldown;

	public int Smoothing
	{
		get => _smoothing.Value;
		set => _smoothing.Value = value;
	}

	public int BaselinePeriod
	{
		get => _baselinePeriod.Value;
		set => _baselinePeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public BerlinCandlesStrategy()
	{
		_smoothing = Param(nameof(Smoothing), 1)
			.SetDisplay("Smoothing", "EMA smoothing for Berlin open", "Berlin")
			.SetOptimize(1, 10, 1);

		_baselinePeriod = Param(nameof(BaselinePeriod), 26)
			.SetDisplay("Baseline Period", "Donchian baseline period", "Berlin")
			.SetOptimize(10, 50, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevEma = default;
		_isInitialized = false;
		_cooldown = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = Smoothing + 1 };
		_donchian = new DonchianChannels { Length = BaselinePeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Manually process Donchian
		var donchianResult = _donchian.Process(candle);
		if (donchianResult is not DonchianChannelsValue dv || dv.Middle is not decimal middleBand)
			return;

		if (!_isInitialized)
		{
			_prevEma = emaValue;
			_isInitialized = true;
			return;
		}

		var openExpr = _prevEma;
		var closeExpr = candle.ClosePrice;

		decimal openValue;
		decimal closeValue;

		if (openExpr > closeExpr)
		{
			openValue = Math.Min(openExpr, candle.HighPrice);
			closeValue = Math.Max(closeExpr, candle.LowPrice);
		}
		else
		{
			openValue = Math.Max(openExpr, candle.LowPrice);
			closeValue = Math.Min(closeExpr, candle.HighPrice);
		}

		var baseline = middleBand;

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevEma = emaValue;
			return;
		}

		if (closeValue > openValue && closeValue > baseline && Position <= 0)
		{
			BuyMarket();
			_cooldown = 100;
		}
		else if (closeValue < openValue && closeValue < baseline && Position >= 0)
		{
			SellMarket();
			_cooldown = 100;
		}

		_prevEma = emaValue;
	}
}
