using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Martingale strategy based on dual MACD indicators.
/// Buys when MACD1 turns up and MACD2 turns down (divergence).
/// Sells on the opposite condition.
/// </summary>
public class MartingaleMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _macd1Fast;
	private readonly StrategyParam<int> _macd1Slow;
	private readonly StrategyParam<int> _macd2Fast;
	private readonly StrategyParam<int> _macd2Slow;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _macd1Prev1;
	private decimal? _macd1Prev2;
	private decimal? _macd2Prev;

	public int Macd1Fast { get => _macd1Fast.Value; set => _macd1Fast.Value = value; }
	public int Macd1Slow { get => _macd1Slow.Value; set => _macd1Slow.Value = value; }
	public int Macd2Fast { get => _macd2Fast.Value; set => _macd2Fast.Value = value; }
	public int Macd2Slow { get => _macd2Slow.Value; set => _macd2Slow.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MartingaleMacdStrategy()
	{
		_macd1Fast = Param(nameof(Macd1Fast), 5)
			.SetDisplay("MACD1 Fast", "Fast EMA for first MACD", "Indicators");

		_macd1Slow = Param(nameof(Macd1Slow), 20)
			.SetDisplay("MACD1 Slow", "Slow EMA for first MACD", "Indicators");

		_macd2Fast = Param(nameof(Macd2Fast), 10)
			.SetDisplay("MACD2 Fast", "Fast EMA for second MACD", "Indicators");

		_macd2Slow = Param(nameof(Macd2Slow), 15)
			.SetDisplay("MACD2 Slow", "Slow EMA for second MACD", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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
		_macd1Prev1 = null;
		_macd1Prev2 = null;
		_macd2Prev = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_macd1Prev1 = null;
		_macd1Prev2 = null;
		_macd2Prev = null;

		var macd1 = new MovingAverageConvergenceDivergence
		{
			ShortMa = { Length = Macd1Fast },
			LongMa = { Length = Macd1Slow }
		};
		var macd2 = new MovingAverageConvergenceDivergence
		{
			ShortMa = { Length = Macd2Fast },
			LongMa = { Length = Macd2Slow }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(macd1, macd2, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd1);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macd1Val, decimal macd2Val)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_macd1Prev1.HasValue && _macd1Prev2.HasValue && _macd2Prev.HasValue)
		{
			var t0 = macd1Val;
			var t1 = _macd1Prev1.Value;
			var t2 = _macd1Prev2.Value;
			var k0 = macd2Val;
			var k1 = _macd2Prev.Value;

			// MACD1 turns up (V-shape) and MACD2 turns down => buy
			if (t0 > t1 && t1 < t2 && k1 > k0 && Position <= 0)
				BuyMarket();
			// MACD1 turns down (inverted V) and MACD2 turns up => sell
			else if (t0 < t1 && t1 > t2 && k1 < k0 && Position >= 0)
				SellMarket();
		}

		_macd1Prev2 = _macd1Prev1;
		_macd1Prev1 = macd1Val;
		_macd2Prev = macd2Val;
	}
}
