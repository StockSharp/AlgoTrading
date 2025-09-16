using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on LeManSignal indicator.
/// Opens long on buy signal and short on sell signal.
/// Signals are derived from high and low breakouts over two consecutive periods.
/// </summary>
public class LeManSignalStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();

	/// <summary>
	/// Indicator period length.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Offset to confirm signal.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public LeManSignalStrategy()
	{
		_period = Param(nameof(Period), 12)
			.SetDisplay("Period", "LeManSignal lookback period", "Indicator")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "Offset for confirmed signal", "Indicator")
			.SetGreaterOrEqual(0)
			.SetCanOptimize(true)
			.SetOptimize(0, 2, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		var maxLen = 2 * Period + 3;
		if (_highs.Count > maxLen)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_highs.Count < maxLen)
			return;

		var signal = GetSignal(SignalBar);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (signal > 0 && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (signal < 0 && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
	}

	private int GetSignal(int bar)
	{
		var size = _highs.Count;

		var bar1 = bar + 1;
		var bar2 = bar + 2;
		var bar1p = bar1 + Period;
		var bar2p = bar2 + Period;

		var h1 = Highest(size - bar1 - Period, Period);
		var h2 = Highest(size - bar1p - Period, Period);
		var h3 = Highest(size - bar2 - Period, Period);
		var h4 = Highest(size - bar2p - Period, Period);

		var l1 = Lowest(size - bar1 - Period, Period);
		var l2 = Lowest(size - bar1p - Period, Period);
		var l3 = Lowest(size - bar2 - Period, Period);
		var l4 = Lowest(size - bar2p - Period, Period);

		var buy = h3 <= h4 && h1 > h2;
		var sell = l3 >= l4 && l1 < l2;

		if (buy)
			return 1;
		if (sell)
			return -1;
		return 0;
	}

	private decimal Highest(int start, int length)
	{
		var max = decimal.MinValue;
		for (var i = start; i < start + length; i++)
		{
			var v = _highs[i];
			if (v > max)
				max = v;
		}
		return max;
	}

	private decimal Lowest(int start, int length)
	{
		var min = decimal.MaxValue;
		for (var i = start; i < start + length; i++)
		{
			var v = _lows[i];
			if (v < min)
				min = v;
		}
		return min;
	}
}
