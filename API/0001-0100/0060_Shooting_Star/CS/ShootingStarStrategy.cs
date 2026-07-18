using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Shooting Star strategy.
/// Enters short on shooting star pattern (long upper shadow, small lower shadow).
/// Enters long on inverted shooting star (long lower shadow, small upper shadow).
/// Exits via SMA crossover.
/// </summary>
public class ShootingStarStrategy : Strategy
{
	private readonly StrategyParam<decimal> _shadowToBodyRatio;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private int _cooldown;

	/// <summary>
	/// Shadow to body ratio.
	/// </summary>
	public decimal ShadowToBodyRatio
	{
		get => _shadowToBodyRatio.Value;
		set => _shadowToBodyRatio.Value = value;
	}

	/// <summary>
	/// MA Period for exit.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
	/// Constructor.
	/// </summary>
	public ShootingStarStrategy()
	{
		_shadowToBodyRatio = Param(nameof(ShadowToBodyRatio), 2.0m)
			.SetRange(1.5m, 5.0m)
			.SetDisplay("Shadow/Body Ratio", "Min ratio of shadow to body", "Pattern");

		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for SMA", "Indicators");

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
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_cooldown = 0;

		var sma = new SimpleMovingAverage { Length = MAPeriod };

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

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
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

		var bodySize = Math.Abs(candle.OpenPrice - candle.ClosePrice);
		var upperShadow = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
		var lowerShadow = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;

		// Shooting star: long upper shadow, small lower shadow (bearish)
		var isShootingStar = bodySize > 0 && upperShadow > bodySize * ShadowToBodyRatio && lowerShadow < bodySize * 0.5m;
		// Hammer: long lower shadow, small upper shadow (bullish)
		var isHammer = bodySize > 0 && lowerShadow > bodySize * ShadowToBodyRatio && upperShadow < bodySize * 0.5m;

		if (Position == 0 && isShootingStar && candle.ClosePrice > smaValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && isHammer && candle.ClosePrice < smaValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && candle.ClosePrice < smaValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && candle.ClosePrice > smaValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
