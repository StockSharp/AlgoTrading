using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pivot Point Reversal strategy.
/// Calculates pivot points from a rolling window of highs, lows, closes.
/// P = (H + L + C) / 3, S1 = 2*P - H, R1 = 2*P - L
/// Buys on bounce off S1, sells on bounce off R1, exits at pivot.
/// </summary>
public class PivotPointReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private readonly List<decimal> _closes = new();
	private int _cooldown;

	/// <summary>
	/// Lookback period for pivot calculation.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
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
	public PivotPointReversalStrategy()
	{
		_lookback = Param(nameof(Lookback), 60)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Lookback for pivot calc", "Indicators");

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
		_closes.Clear();
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highs.Clear();
		_lows.Clear();
		_closes.Clear();
		_cooldown = 0;

		var sma = new SimpleMovingAverage { Length = 20 };

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

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);
		_closes.Add(candle.ClosePrice);

		if (_highs.Count > Lookback)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
			_closes.RemoveAt(0);
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_highs.Count < Lookback)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Calculate pivot points from lookback window
		decimal high = decimal.MinValue, low = decimal.MaxValue, close = 0;
		for (int i = 0; i < _highs.Count; i++)
		{
			if (_highs[i] > high) high = _highs[i];
			if (_lows[i] < low) low = _lows[i];
		}
		close = _closes[_closes.Count - 1];

		var pivot = (high + low + close) / 3;
		var r1 = 2 * pivot - low;
		var s1 = 2 * pivot - high;
		var buffer = (r1 - s1) * 0.02m;

		if (buffer <= 0)
			return;

		var isBullish = candle.ClosePrice > candle.OpenPrice;
		var isBearish = candle.ClosePrice < candle.OpenPrice;

		// Bounce off S1 (buy)
		if (Position == 0 && candle.LowPrice <= s1 + buffer && isBullish)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Bounce off R1 (sell)
		else if (Position == 0 && candle.HighPrice >= r1 - buffer && isBearish)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit at pivot
		else if (Position > 0 && candle.ClosePrice > pivot)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && candle.ClosePrice < pivot)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
