namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Recreates the Exp_ColorMaRsi-Trigger_Duplex expert advisor.
/// Combines fast/slow MA and fast/slow RSI comparisons into a color code (+1/0/-1).
/// Trades based on color code transitions.
/// </summary>
public class ColorMaRsiTriggerDuplexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _fastRsiPeriod;
	private readonly StrategyParam<int> _slowRsiPeriod;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _signalCooldownBars;

	private readonly List<decimal> _colorHistory = new();
	private int _cooldownRemaining;

	/// <summary>Candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	/// <summary>Fast MA period.</summary>
	public int FastMaPeriod { get => _fastMaPeriod.Value; set => _fastMaPeriod.Value = value; }
	/// <summary>Slow MA period.</summary>
	public int SlowMaPeriod { get => _slowMaPeriod.Value; set => _slowMaPeriod.Value = value; }
	/// <summary>Fast RSI period.</summary>
	public int FastRsiPeriod { get => _fastRsiPeriod.Value; set => _fastRsiPeriod.Value = value; }
	/// <summary>Slow RSI period.</summary>
	public int SlowRsiPeriod { get => _slowRsiPeriod.Value; set => _slowRsiPeriod.Value = value; }
	/// <summary>Signal bar shift.</summary>
	public int SignalBar { get => _signalBar.Value; set => _signalBar.Value = value; }
	/// <summary>Bars to wait between reversals.</summary>
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }

	public ColorMaRsiTriggerDuplexStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 5)
			.SetDisplay("Fast MA Period", "Fast moving average length", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 10)
			.SetDisplay("Slow MA Period", "Slow moving average length", "Indicators");

		_fastRsiPeriod = Param(nameof(FastRsiPeriod), 3)
			.SetDisplay("Fast RSI Period", "Fast RSI length", "Indicators");

		_slowRsiPeriod = Param(nameof(SlowRsiPeriod), 13)
			.SetDisplay("Slow RSI Period", "Slow RSI length", "Indicators");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "History shift for signal evaluation", "Strategy");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 8)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between reversals", "Strategy");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		lock (_colorHistory)
			_colorHistory.Clear();

		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		lock (_colorHistory)
			_colorHistory.Clear();

		_cooldownRemaining = 0;

		var fastMa = new ExponentialMovingAverage { Length = FastMaPeriod };
		var slowMa = new ExponentialMovingAverage { Length = SlowMaPeriod };
		var fastRsi = new RelativeStrengthIndex { Length = FastRsiPeriod };
		var slowRsi = new RelativeStrengthIndex { Length = SlowRsiPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fastMa, slowMa, fastRsi, slowRsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastMaVal, decimal slowMaVal, decimal fastRsiVal, decimal slowRsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		// Calculate color code from MA and RSI comparisons
		var score = 0m;

		if (fastMaVal > slowMaVal)
			score = 1m;
		else if (fastMaVal < slowMaVal)
			score = -1m;

		if (fastRsiVal > slowRsiVal)
			score += 1m;
		else if (fastRsiVal < slowRsiVal)
			score -= 1m;

		// Clamp to [-1, 1]
		if (score > 1m) score = 1m;
		else if (score < -1m) score = -1m;

		decimal recent;
		decimal older;

		lock (_colorHistory)
		{
			_colorHistory.Insert(0, score);
			var maxHistory = Math.Max(2, SignalBar + 2);
			while (_colorHistory.Count > maxHistory)
				_colorHistory.RemoveAt(_colorHistory.Count - 1);

			if (_colorHistory.Count <= SignalBar + 1)
				return;

			recent = _colorHistory[SignalBar];
			older = _colorHistory[SignalBar + 1];
		}

		var longOpen = older == -1m && recent == 1m;
		var shortOpen = older == 1m && recent == -1m;
		var longExit = Position > 0 && recent < 0m;
		var shortExit = Position < 0 && recent > 0m;

		if (longExit)
		{
			SellMarket(Position);
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (shortExit)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (_cooldownRemaining == 0 && longOpen && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (_cooldownRemaining == 0 && shortOpen && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_cooldownRemaining = SignalCooldownBars;
		}
	}
}
