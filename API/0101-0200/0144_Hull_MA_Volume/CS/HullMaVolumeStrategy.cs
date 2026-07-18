using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that uses Hull Moving Average for trend direction.
/// Enters when HMA direction changes.
/// </summary>
public class HullMaVolumeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _hullPeriod;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevHullValue;
	private int _cooldown;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Hull Moving Average period.
	/// </summary>
	public int HullPeriod
	{
		get => _hullPeriod.Value;
		set => _hullPeriod.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Strategy constructor.
	/// </summary>
	public HullMaVolumeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_hullPeriod = Param(nameof(HullPeriod), 9)
			.SetRange(5, 30)
			.SetDisplay("Hull MA Period", "Period of the Hull Moving Average", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 100)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General")
			.SetRange(5, 500);
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
		_prevHullValue = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var hullMa = new HullMovingAverage { Length = HullPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(hullMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hullMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal hullValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevHullValue == 0)
		{
			_prevHullValue = hullValue;
			return;
		}

		var rising = hullValue > _prevHullValue;
		var falling = hullValue < _prevHullValue;
		_prevHullValue = hullValue;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Entry: HMA turning up
		if (rising && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Entry: HMA turning down
		else if (falling && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit long: HMA turns down
		if (Position > 0 && falling)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short: HMA turns up
		else if (Position < 0 && rising)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
