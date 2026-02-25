using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trailing stop management based on CandleStop logic.
/// Uses EMA crossover for entries and Highest/Lowest channel for trailing exits.
/// </summary>
public class CandleStopTrailingStrategy : Strategy
{
	private readonly StrategyParam<int> _trailPeriod;
	private readonly StrategyParam<int> _fastEma;
	private readonly StrategyParam<int> _slowEma;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private decimal _stopPrice;

	public int TrailPeriod { get => _trailPeriod.Value; set => _trailPeriod.Value = value; }
	public int FastEma { get => _fastEma.Value; set => _fastEma.Value = value; }
	public int SlowEma { get => _slowEma.Value; set => _slowEma.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public CandleStopTrailingStrategy()
	{
		_trailPeriod = Param(nameof(TrailPeriod), 5)
			.SetDisplay("Trail Period", "Look-back for channel trailing", "Parameters");

		_fastEma = Param(nameof(FastEma), 10)
			.SetDisplay("Fast EMA", "Fast EMA period", "Parameters");

		_slowEma = Param(nameof(SlowEma), 30)
			.SetDisplay("Slow EMA", "Slow EMA period", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for analysis", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = FastEma };
		var slow = new ExponentialMovingAverage { Length = SlowEma };
		_highest = new Highest { Length = TrailPeriod };
		_lowest = new Lowest { Length = TrailPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, (candle, fastVal, slowVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var highResult = _highest.Process(candle);
				var lowResult = _lowest.Process(candle);

				if (!highResult.IsFormed || !lowResult.IsFormed)
					return;

				var upper = highResult.ToDecimal();
				var lower = lowResult.ToDecimal();

				// Entry logic based on EMA crossover
				if (Position == 0)
				{
					if (fastVal > slowVal && candle.ClosePrice > slowVal)
					{
						BuyMarket();
						_stopPrice = lower;
					}
					else if (fastVal < slowVal && candle.ClosePrice < slowVal)
					{
						SellMarket();
						_stopPrice = upper;
					}
				}
				else if (Position > 0)
				{
					if (lower > _stopPrice)
						_stopPrice = lower;

					if (candle.LowPrice <= _stopPrice)
					{
						SellMarket();
						_stopPrice = 0m;
					}
				}
				else if (Position < 0)
				{
					if (_stopPrice == 0 || upper < _stopPrice)
						_stopPrice = upper;

					if (candle.HighPrice >= _stopPrice)
					{
						BuyMarket();
						_stopPrice = 0m;
					}
				}
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}
}
