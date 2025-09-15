using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on difference between fast and slow Williams %R.
/// Enter long when fast WPR is above slow WPR while slow WPR is above the level.
/// Enter short when fast WPR is below slow WPR while slow WPR is below the level.
/// </summary>
public class DeltaWprStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _level;
	private readonly StrategyParam<DataType> _candleType;

	private int _prevSignal;

	/// <summary>
	/// Fast Williams %R period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow Williams %R period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Threshold level for the slow Williams %R.
	/// </summary>
	public decimal Level
	{
		get => _level.Value;
		set => _level.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="DeltaWprStrategy"/>.
	/// </summary>
	public DeltaWprStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 14)
			.SetDisplay("Fast WPR Period", "Period for the fast Williams %R", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 7);

		_slowPeriod = Param(nameof(SlowPeriod), 30)
			.SetDisplay("Slow WPR Period", "Period for the slow Williams %R", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 5);

		_level = Param(nameof(Level), -50m)
			.SetDisplay("Signal Level", "Threshold level for signals", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(-80m, -20m, 10m);

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
		_prevSignal = 1; // Pass
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fast = new WilliamsR { Length = FastPeriod };
		var slow = new WilliamsR { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fast, slow, (candle, fastValue, slowValue) =>
			{
				// Process only finished candles
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var signal = 1;

				if (slowValue > Level && fastValue > slowValue)
					signal = 0; // Up
				else if (slowValue < Level && fastValue < slowValue)
					signal = 2; // Down

				if (signal == _prevSignal)
					return;

				if (signal == 0 && Position <= 0)
				{
					// Close short positions and open long
					BuyMarket(Volume + Math.Abs(Position));
				}
				else if (signal == 2 && Position >= 0)
				{
					// Close long positions and open short
					SellMarket(Volume + Math.Abs(Position));
				}

				_prevSignal = signal;
			})
			.Start();
	}
}
