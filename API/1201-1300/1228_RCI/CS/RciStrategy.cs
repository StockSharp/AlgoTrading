using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum RciTradeDirection
{
	Both,
	LongOnly,
	ShortOnly,
}

public enum RciMaType
{
	Simple,
	Exponential,
}

/// <summary>
/// Rank Correlation Index crossover strategy.
/// </summary>
public class RciStrategy : Strategy
{
	private readonly StrategyParam<int> _rciLength;
	private readonly StrategyParam<RciMaType> _maType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<RciTradeDirection> _direction;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closes = [];
	private readonly List<decimal> _rciValues = [];
	private decimal? _maEma;
	private decimal? _previousRci;
	private decimal? _previousMa;

	public int RciLength { get => _rciLength.Value; set => _rciLength.Value = value; }
	public RciMaType MaType { get => _maType.Value; set => _maType.Value = value; }
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public RciTradeDirection Direction { get => _direction.Value; set => _direction.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RciStrategy()
	{
		_rciLength = Param(nameof(RciLength), 10).SetGreaterThanZero();
		_maType = Param(nameof(MaType), RciMaType.Simple);
		_maLength = Param(nameof(MaLength), 14).SetGreaterThanZero();
		_direction = Param(nameof(Direction), RciTradeDirection.Both);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_closes.Clear();
		_rciValues.Clear();
		_maEma = null;
		_previousRci = null;
		_previousMa = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Add(candle.ClosePrice);
		if (_closes.Count > RciLength)
			_closes.RemoveAt(0);

		if (_closes.Count < RciLength)
			return;

		var rci = CalculateRci(_closes);
		_rciValues.Add(rci);
		if (_rciValues.Count > MaLength)
			_rciValues.RemoveAt(0);

		decimal ma;
		if (MaType == RciMaType.Simple)
		{
			if (_rciValues.Count < MaLength)
				return;

			ma = _rciValues.Average();
		}
		else
		{
			_maEma = Ema(_maEma, rci, MaLength);
			if (_rciValues.Count < MaLength)
				return;

			ma = _maEma.Value;
		}

		if (_previousRci is decimal previousRci && _previousMa is decimal previousMa)
		{
			if (previousRci <= previousMa && rci > ma)
				ApplySignal(1);
			else if (previousRci >= previousMa && rci < ma)
				ApplySignal(-1);
		}

		_previousRci = rci;
		_previousMa = ma;
	}

	private void ApplySignal(int signal)
	{
		var allowLong = Direction is RciTradeDirection.Both or RciTradeDirection.LongOnly;
		var allowShort = Direction is RciTradeDirection.Both or RciTradeDirection.ShortOnly;

		if (signal > 0)
		{
			if (Position < 0m)
				BuyMarket(Math.Abs(Position) + (allowLong ? Volume : 0m));
			else if (Position == 0m && allowLong)
				BuyMarket();
		}
		else if (signal < 0)
		{
			if (Position > 0m)
				SellMarket(Math.Abs(Position) + (allowShort ? Volume : 0m));
			else if (Position == 0m && allowShort)
				SellMarket();
		}
	}

	internal static decimal CalculateRci(IReadOnlyList<decimal> values)
	{
		var n = values?.Count ?? 0;
		if (n < 2)
			return 0m;

		var priceRanks = new decimal[n];

		foreach (var group in values
			.Select((value, index) => (value, index))
			.OrderBy(x => x.value)
			.GroupBy(x => x.value))
		{
			var ordered = group.ToArray();
			var lower = values.Count(v => v < group.Key);
			var firstRank = lower + 1m;
			var lastRank = lower + ordered.Length;
			var averageRank = (firstRank + lastRank) / 2m;

			foreach (var item in ordered)
				priceRanks[item.index] = averageRank;
		}

		var sumSquared = 0m;
		for (var i = 0; i < n; i++)
		{
			var timeRank = i + 1m;
			var diff = timeRank - priceRanks[i];
			sumSquared += diff * diff;
		}

		return (1m - 6m * sumSquared / (n * (n * n - 1m))) * 100m;
	}

	private static decimal Ema(decimal? previous, decimal value, int period)
	{
		if (previous is null)
			return value;

		var alpha = 2m / (period + 1m);
		return previous.Value + alpha * (value - previous.Value);
	}
}
