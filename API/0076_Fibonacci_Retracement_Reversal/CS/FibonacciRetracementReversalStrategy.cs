using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fibonacci Retracement Reversal strategy.
/// Identifies swing high/low over a lookback window and enters at key Fibonacci retracement levels.
/// Bullish reversal at 61.8% retracement from swing low.
/// Bearish reversal at 61.8% retracement from swing high.
/// Uses SMA for exit signals.
/// </summary>
public class FibonacciRetracementReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _swingLookback;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private int _cooldown;

	/// <summary>
	/// Swing lookback period.
	/// </summary>
	public int SwingLookback
	{
		get => _swingLookback.Value;
		set => _swingLookback.Value = value;
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
	public FibonacciRetracementReversalStrategy()
	{
		_swingLookback = Param(nameof(SwingLookback), 20)
			.SetGreaterThanZero()
			.SetDisplay("Swing Lookback", "Lookback for swing high/low", "Indicators");

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

		// Track highs and lows
		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		if (_highs.Count > SwingLookback)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_highs.Count < SwingLookback)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Find swing high and swing low from lookback
		decimal swingHigh = decimal.MinValue;
		decimal swingLow = decimal.MaxValue;

		for (int i = 0; i < _highs.Count; i++)
		{
			if (_highs[i] > swingHigh) swingHigh = _highs[i];
			if (_lows[i] < swingLow) swingLow = _lows[i];
		}

		var range = swingHigh - swingLow;
		if (range <= 0)
			return;

		// Fibonacci 61.8% retracement levels
		var fib618FromHigh = swingHigh - range * 0.618m;
		var fib618FromLow = swingLow + range * 0.618m;
		var buffer = range * 0.02m; // 2% buffer

		var isBullish = candle.ClosePrice > candle.OpenPrice;
		var isBearish = candle.ClosePrice < candle.OpenPrice;

		// Buy at 61.8% retracement from high (near swing low area) with bullish candle
		if (Position == 0 && Math.Abs(candle.ClosePrice - fib618FromHigh) < buffer && isBullish)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Sell at 61.8% retracement from low (near swing high area) with bearish candle
		else if (Position == 0 && Math.Abs(candle.ClosePrice - fib618FromLow) < buffer && isBearish)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit long above SMA
		else if (Position > 0 && candle.ClosePrice < smaValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short below SMA
		else if (Position < 0 && candle.ClosePrice > smaValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
