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
/// Moving average strategy with smoothing offset.
/// </summary>
public class SmoothingAverageStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _smoothing;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private int _cooldown;

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public decimal Smoothing
	{
		get => _smoothing.Value;
		set => _smoothing.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public SmoothingAverageStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 200)
			.SetDisplay("MA Period", "Moving average period", "MA")
			;
		_smoothing = Param(nameof(Smoothing), 1400m)
			.SetDisplay("Smoothing", "Price offset from moving average", "General")
			;
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		_cooldownBars = Param(nameof(CooldownBars), 36)
			.SetDisplay("Cooldown Bars", "Bars to wait between new signals", "General")
			;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// create moving average indicator
		var sma = new SimpleMovingAverage { Length = MaPeriod };

		// subscribe to candles
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var price = candle.ClosePrice;
		var offset = (Security.PriceStep ?? 1m) * Smoothing;

		if (Position == 0)
		{
			if (price >= maValue + offset)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
			else if (price <= maValue - offset)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0 && price <= maValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && price >= maValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
	}
}
