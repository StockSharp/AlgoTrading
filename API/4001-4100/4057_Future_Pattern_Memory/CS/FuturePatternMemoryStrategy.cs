using System;
using System.Collections.Generic;
using System.Text;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pattern memory strategy that records normalized MA spread sequences,
/// tracks fractal outcomes, and trades when a recognized pattern has favorable statistics.
/// </summary>
public class FuturePatternMemoryStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _patternLength;
	private readonly StrategyParam<int> _minMatches;

	private readonly Queue<int> _patternWindow = new();
	private readonly Dictionary<string, (int buyCount, int sellCount)> _patterns = new();
	private decimal _entryPrice;
	private int _barIndex;
	private int _lastEntryBar;

	public FuturePatternMemoryStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis.", "General");

		_fastMaLength = Param(nameof(FastMaLength), 6)
			.SetDisplay("Fast MA", "Fast EMA period.", "Indicators");

		_slowMaLength = Param(nameof(SlowMaLength), 24)
			.SetDisplay("Slow MA", "Slow EMA period.", "Indicators");

		_patternLength = Param(nameof(PatternLength), 5)
			.SetDisplay("Pattern Length", "Number of bars in pattern signature.", "Pattern");

		_minMatches = Param(nameof(MinMatches), 3)
			.SetDisplay("Min Matches", "Minimum pattern occurrences before trading.", "Pattern");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	public int PatternLength
	{
		get => _patternLength.Value;
		set => _patternLength.Value = value;
	}

	public int MinMatches
	{
		get => _minMatches.Value;
		set => _minMatches.Value = value;
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_patternWindow.Clear();
		_patterns.Clear();
		_entryPrice = 0;
		_barIndex = 0;
		_lastEntryBar = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_patternWindow.Clear();
		_patterns.Clear();
		_entryPrice = 0;
		_barIndex = 0;
		_lastEntryBar = 0;

		var fastEma = new ExponentialMovingAverage { Length = FastMaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowMaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;
		var close = candle.ClosePrice;

		// Normalize the MA spread into a discrete value
		var spread = fastValue - slowValue;
		var normalized = spread > 0 ? 1 : (spread < 0 ? -1 : 0);

		_patternWindow.Enqueue(normalized);
		while (_patternWindow.Count > PatternLength)
			_patternWindow.Dequeue();

		if (_patternWindow.Count < PatternLength)
			return;

		var key = BuildPatternKey(_patternWindow);

		// Record outcome: if price went up, it's a buy match, otherwise sell
		if (!_patterns.TryGetValue(key, out var stats))
			stats = (0, 0);

		if (close > fastValue)
			stats = (stats.buyCount + 1, stats.sellCount);
		else if (close < fastValue)
			stats = (stats.buyCount, stats.sellCount + 1);

		_patterns[key] = stats;

		// Cooldown: minimum SlowMaLength*4 bars between any trade action
		var cooldownBars = SlowMaLength * 4;
		var cooledDown = _barIndex - _lastEntryBar >= cooldownBars;

		// Position management
		var exitedThisBar = false;
		if (Position > 0)
		{
			if (cooledDown && (spread < 0 || (_entryPrice > 0 && close < _entryPrice * 0.985m)))
			{
				SellMarket();
				_lastEntryBar = _barIndex;
				exitedThisBar = true;
			}
		}
		else if (Position < 0)
		{
			if (cooledDown && (spread > 0 || (_entryPrice > 0 && close > _entryPrice * 1.015m)))
			{
				BuyMarket();
				_lastEntryBar = _barIndex;
				exitedThisBar = true;
			}
		}

		// Entry based on pattern statistics (cooldown between entries)
		if (!exitedThisBar && Position == 0 && cooledDown)
		{
			var total = stats.buyCount + stats.sellCount;
			if (total >= MinMatches)
			{
				if (stats.buyCount > stats.sellCount && spread > 0)
				{
					_entryPrice = close;
					_lastEntryBar = _barIndex;
					BuyMarket();
				}
				else if (stats.sellCount > stats.buyCount && spread < 0)
				{
					_entryPrice = close;
					_lastEntryBar = _barIndex;
					SellMarket();
				}
			}
		}
	}

	private static string BuildPatternKey(IEnumerable<int> values)
	{
		var sb = new StringBuilder();
		var first = true;
		foreach (var v in values)
		{
			if (!first) sb.Append('_');
			sb.Append(v);
			first = false;
		}
		return sb.ToString();
	}
}
