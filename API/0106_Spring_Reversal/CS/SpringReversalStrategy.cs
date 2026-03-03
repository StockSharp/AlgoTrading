using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Spring Reversal strategy (Wyckoff).
/// Enters long when price dips below recent support then closes back above it.
/// Enters short when price spikes above recent resistance then closes back below it.
/// Uses SMA for exit confirmation.
/// Uses cooldown to control trade frequency.
/// </summary>
public class SpringReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private readonly List<decimal> _lows = new();
	private readonly List<decimal> _highs = new();
	private int _cooldown;

	/// <summary>
	/// Lookback period.
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
	public SpringReversalStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetRange(5, 50)
			.SetDisplay("Lookback", "Period for support/resistance", "Range");

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
		_lows.Clear();
		_highs.Clear();
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_lows.Clear();
		_highs.Clear();
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

		// Maintain rolling window of lows and highs
		_lows.Add(candle.LowPrice);
		_highs.Add(candle.HighPrice);
		if (_lows.Count > LookbackPeriod + 1)
		{
			_lows.RemoveAt(0);
			_highs.RemoveAt(0);
		}

		if (_lows.Count < LookbackPeriod + 1)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Find support (lowest low) and resistance (highest high) of previous N bars
		decimal support = decimal.MaxValue;
		decimal resistance = decimal.MinValue;
		for (int i = 0; i < _lows.Count - 1; i++)
		{
			if (_lows[i] < support) support = _lows[i];
			if (_highs[i] > resistance) resistance = _highs[i];
		}

		// Spring: price dips below support but closes above it (bullish)
		var isSpring = candle.LowPrice < support && candle.ClosePrice > support && candle.ClosePrice > candle.OpenPrice;

		// Upthrust: price spikes above resistance but closes below it (bearish)
		var isUpthrust = candle.HighPrice > resistance && candle.ClosePrice < resistance && candle.ClosePrice < candle.OpenPrice;

		if (Position == 0 && isSpring)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && isUpthrust)
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
