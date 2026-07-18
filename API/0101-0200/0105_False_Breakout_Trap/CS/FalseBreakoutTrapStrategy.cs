using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// False Breakout Trap strategy.
/// Detects when price breaks a recent high/low range then reverses back.
/// Trades against the failed breakout direction.
/// Uses SMA for exit confirmation.
/// Uses cooldown to control trade frequency.
/// </summary>
public class FalseBreakoutTrapStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private int _cooldown;

	/// <summary>
	/// Lookback period for range.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// MA period for exit.
	/// </summary>
	public int MaPeriod
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
	public FalseBreakoutTrapStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetRange(5, 50)
			.SetDisplay("Lookback", "Period for high/low range", "Range");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetRange(5, 50)
			.SetDisplay("MA Period", "Period for SMA exit", "Indicators");

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
		_highs.Clear();
		_lows.Clear();
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highs.Clear();
		_lows.Clear();
		_cooldown = 0;

		var sma = new SimpleMovingAverage { Length = MaPeriod };

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

		// Maintain rolling high/low window
		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);
		if (_highs.Count > LookbackPeriod + 1)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_highs.Count < LookbackPeriod + 1)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Find highest high and lowest low of the previous N bars (excluding current)
		decimal rangeHigh = decimal.MinValue;
		decimal rangeLow = decimal.MaxValue;
		for (int i = 0; i < _highs.Count - 1; i++)
		{
			if (_highs[i] > rangeHigh) rangeHigh = _highs[i];
			if (_lows[i] < rangeLow) rangeLow = _lows[i];
		}

		// False upside breakout: candle broke above range high but closed below it
		var falseBreakUp = candle.HighPrice > rangeHigh && candle.ClosePrice < rangeHigh;
		// False downside breakout: candle broke below range low but closed above it
		var falseBreakDown = candle.LowPrice < rangeLow && candle.ClosePrice > rangeLow;

		if (Position == 0 && falseBreakDown)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && falseBreakUp)
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
