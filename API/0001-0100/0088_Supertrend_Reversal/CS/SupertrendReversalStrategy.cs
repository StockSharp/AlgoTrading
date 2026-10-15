using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Supertrend Reversal strategy.
/// Uses the built-in SuperTrend indicator.
/// Enters long when SuperTrend flips to uptrend (below price).
/// Enters short when SuperTrend flips to downtrend (above price).
/// Uses cooldown to control trade frequency.
/// </summary>
public class SupertrendReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private bool? _prevIsUpTrend;
	private int _cooldown;

	/// <summary>
	/// ATR period for SuperTrend.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// ATR multiplier for SuperTrend.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
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
	public SupertrendReversalStrategy()
	{
		_period = Param(nameof(Period), 10)
			.SetRange(7, 20)
			.SetDisplay("Period", "ATR period for SuperTrend", "SuperTrend");

		_multiplier = Param(nameof(Multiplier), 3.0m)
			.SetRange(2.0m, 4.0m)
			.SetDisplay("Multiplier", "ATR multiplier for SuperTrend", "SuperTrend");

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
		_prevIsUpTrend = null;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevIsUpTrend = null;
		_cooldown = 0;

		var superTrend = new SuperTrend
		{
			Length = Period,
			Multiplier = Multiplier
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(superTrend, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, superTrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stValue.IsFormed)
			return;

		var stTyped = (SuperTrendIndicatorValue)stValue;
		var isUpTrend = stTyped.IsUpTrend;

		if (_prevIsUpTrend == null)
		{
			_prevIsUpTrend = isUpTrend;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevIsUpTrend = isUpTrend;
			return;
		}

		// SuperTrend flipped to uptrend = bullish
		var flippedUp = _prevIsUpTrend == false && isUpTrend;
		// SuperTrend flipped to downtrend = bearish
		var flippedDown = _prevIsUpTrend == true && !isUpTrend;

		if (Position == 0 && flippedUp)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && flippedDown)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && flippedDown)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && flippedUp)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevIsUpTrend = isUpTrend;
	}
}
