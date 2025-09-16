using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MSL EA strategy.
/// Builds support and resistance lines from local extremes and trades breakouts.
/// </summary>
public class MsleaStrategy : Strategy
{
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<int> _level;
	private readonly StrategyParam<int> _distance;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highLevels = new();
	private readonly List<decimal> _lowLevels = new();

	private decimal? _prevHigh1;
	private decimal? _prevHigh2;
	private decimal? _prevLow1;
	private decimal? _prevLow2;

	private decimal? _msh;
	private decimal? _msl;

	/// <summary>
	/// Maximum allowed open trades.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Number of local extremes used to build levels.
	/// </summary>
	public int Level
	{
		get => _level.Value;
		set => _level.Value = value;
	}

	/// <summary>
	/// Distance from extremes in ticks.
	/// </summary>
	public int Distance
	{
		get => _distance.Value;
		set => _distance.Value = value;
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
	/// Initialize Mslea strategy.
	/// </summary>
	public MsleaStrategy()
	{
		_maxTrades = Param(nameof(MaxTrades), 2)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum simultaneous trades", "General");

		_level = Param(nameof(Level), 1)
			.SetGreaterThanZero()
			.SetDisplay("Level", "Number of extremes to look back", "General");

		_distance = Param(nameof(Distance), 4)
			.SetGreaterThanZero()
			.SetDisplay("Distance", "Offset from extreme in ticks", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_highLevels.Clear();
		_lowLevels.Clear();
		_prevHigh1 = _prevHigh2 = _prevLow1 = _prevLow2 = null;
		_msh = _msl = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevHigh2 is decimal h2 && _prevHigh1 is decimal h1 && h2 < h1 && h1 > candle.HighPrice)
			AddHigh(h1);

		if (_prevLow2 is decimal l2 && _prevLow1 is decimal l1 && l2 > l1 && l1 < candle.LowPrice)
			AddLow(l1);

		_prevHigh2 = _prevHigh1;
		_prevHigh1 = candle.HighPrice;
		_prevLow2 = _prevLow1;
		_prevLow1 = candle.LowPrice;

		if (_msh is decimal top && _msl is decimal bottom)
		{
			var offset = Security.PriceStep * Distance;
			var upper = top + offset;
			var lower = bottom - offset;

			if (IsFormedAndOnlineAndAllowTrading() && Math.Abs(Position) < MaxTrades)
			{
				if (candle.ClosePrice > upper && Position <= 0)
					BuyMarket();
				else if (candle.ClosePrice < lower && Position >= 0)
					SellMarket();
			}
		}
	}

	private void AddHigh(decimal high)
	{
		_highLevels.Insert(0, high);
		TrimList(_highLevels);
		_msh = GetMax(_highLevels);
	}

	private void AddLow(decimal low)
	{
		_lowLevels.Insert(0, low);
		TrimList(_lowLevels);
		_msl = GetMin(_lowLevels);
	}

	private void TrimList(List<decimal> list)
	{
		while (list.Count > Level)
			list.RemoveAt(list.Count - 1);
	}

	private static decimal GetMax(List<decimal> list)
	{
		var max = list[0];
		for (var i = 1; i < list.Count; i++)
		{
			var v = list[i];
			if (v > max)
				max = v;
		}
		return max;
	}

	private static decimal GetMin(List<decimal> list)
	{
		var min = list[0];
		for (var i = 1; i < list.Count; i++)
		{
			var v = list[i];
			if (v < min)
				min = v;
		}
		return min;
	}
}

