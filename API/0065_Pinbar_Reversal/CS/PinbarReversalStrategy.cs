using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pinbar Reversal strategy.
/// Enters long on bullish pinbar (long lower wick) below SMA.
/// Enters short on bearish pinbar (long upper wick) above SMA.
/// Exits via SMA crossover.
/// </summary>
public class PinbarReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tailToBodyRatio;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private int _cooldown;

	/// <summary>
	/// Tail to body ratio.
	/// </summary>
	public decimal TailToBodyRatio
	{
		get => _tailToBodyRatio.Value;
		set => _tailToBodyRatio.Value = value;
	}

	/// <summary>
	/// MA Period.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
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
	public PinbarReversalStrategy()
	{
		_tailToBodyRatio = Param(nameof(TailToBodyRatio), 2.0m)
			.SetRange(1.5m, 5.0m)
			.SetDisplay("Tail/Body Ratio", "Min tail to body ratio", "Pattern");

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
		var lowerWick = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;
		var upperWick = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);

		// Bullish pinbar: long lower wick, small upper wick
		var isBullishPinbar = bodySize > 0 && lowerWick > bodySize * TailToBodyRatio && upperWick < bodySize * 0.5m;
		// Bearish pinbar: long upper wick, small lower wick
		var isBearishPinbar = bodySize > 0 && upperWick > bodySize * TailToBodyRatio && lowerWick < bodySize * 0.5m;

		if (Position == 0 && isBullishPinbar && candle.ClosePrice < smaValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && isBearishPinbar && candle.ClosePrice > smaValue)
		{
			SellMarket();
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
