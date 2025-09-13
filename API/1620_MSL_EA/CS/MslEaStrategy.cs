using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy based on local highs and lows.
/// </summary>
public class MslEaStrategy : Strategy
{
	private readonly StrategyParam<int> _level;
	private readonly StrategyParam<int> _distance;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _highs = new();
	private readonly Queue<decimal> _lows = new();
	private readonly List<decimal> _upLevels = new();
	private readonly List<decimal> _downLevels = new();

	private decimal? _upper;
	private decimal? _lower;

	/// <summary>
	/// Number of consecutive fractal levels to track.
	/// </summary>
	public int Level
	{
		get => _level.Value;
		set => _level.Value = value;
	}

	/// <summary>
	/// Offset in ticks from the level.
	/// </summary>
	public int Distance
	{
		get => _distance.Value;
		set => _distance.Value = value;
	}

	/// <summary>
	/// Maximum simultaneous trades.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
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
	/// Initialize <see cref="MslEaStrategy"/>.
	/// </summary>
	public MslEaStrategy()
	{
		_level = Param(nameof(Level), 1)
			.SetGreaterThanZero()
			.SetDisplay("Level", "Number of fractal levels", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_distance = Param(nameof(Distance), 4)
			.SetGreaterThanZero()
			.SetDisplay("Distance", "Offset in ticks", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_maxTrades = Param(nameof(MaxTrades), 2)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum simultaneous trades", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
		_highs.Clear();
		_lows.Clear();
		_upLevels.Clear();
		_downLevels.Clear();
		_upper = null;
		_lower = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		_highs.Enqueue(candle.HighPrice);
		_lows.Enqueue(candle.LowPrice);

		if (_highs.Count > 3)
			_highs.Dequeue();
		if (_lows.Count > 3)
			_lows.Dequeue();

		if (_highs.Count == 3 && _lows.Count == 3)
		{
			var hs = _highs.ToArray();
			var ls = _lows.ToArray();

			if (hs[1] > hs[0] && hs[1] > hs[2])
			{
				if (_upLevels.Count == 0 || hs[1] > _upLevels[^1])
				{
					_upLevels.Add(hs[1]);
					if (_upLevels.Count > Level)
						_upLevels.RemoveAt(0);
				}
			}

			if (ls[1] < ls[0] && ls[1] < ls[2])
			{
				if (_downLevels.Count == 0 || ls[1] < _downLevels[^1])
				{
					_downLevels.Add(ls[1]);
					if (_downLevels.Count > Level)
						_downLevels.RemoveAt(0);
				}
			}

			if (_upLevels.Count >= Level && _downLevels.Count >= Level)
			{
				var step = Security.PriceStep ?? 1m;
				_upper = _upLevels[^1] + step * Distance;
				_lower = _downLevels[^1] - step * Distance;
			}
		}

		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_upper is decimal up && candle.ClosePrice > up && Position < MaxTrades)
			BuyMarket(Volume + Math.Abs(Position));
		else if (_lower is decimal down && candle.ClosePrice < down && Position > -MaxTrades)
			SellMarket(Volume + Math.Abs(Position));
	}
}

