using System;
using System.Collections.Generic;

using StockSharp.BusinessEntities;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Order block finder strategy.
/// Buys on detected bullish order blocks and sells on bearish ones.
/// </summary>
public class OrderBlockFinderStrategy : Strategy
{
	private readonly StrategyParam<int> _periods;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<bool> _useWholeRange;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Number of subsequent candles to confirm an order block.
	/// </summary>
	public int Periods
	{
		get => _periods.Value;
		set => _periods.Value = value;
	}

	/// <summary>
	/// Minimum percent move to validate an order block.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Use High/Low range instead of Open boundary.
	/// </summary>
	public bool UseWholeRange
	{
		get => _useWholeRange.Value;
		set => _useWholeRange.Value = value;
	}

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderBlockFinderStrategy"/> class.
	/// </summary>
	public OrderBlockFinderStrategy()
	{
		_periods = Param(nameof(Periods), 5)
			.SetGreaterThanZero()
			.SetDisplay("Relevant Periods", "Number of consecutive candles", "Parameters")
			.SetCanOptimize();

		_threshold = Param(nameof(Threshold), 0m)
			.SetDisplay("Min Percent Move", "Minimum percent move", "Parameters")
			.SetCanOptimize();

		_useWholeRange = Param(nameof(UseWholeRange), false)
			.SetDisplay("Use Whole Range", "Use High/Low instead of Open range", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		var buffer = new Queue<ICandleMessage>();
		DateTimeOffset? lastBull = null;
		DateTimeOffset? lastBear = null;

		subscription
			.Bind(ProcessCandle)
			.Start();

		void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
			return;

			buffer.Enqueue(candle);

			var need = Periods + 1;

			while (buffer.Count > need)
			buffer.Dequeue();

			if (buffer.Count < need)
			return;

			var arr = buffer.ToArray();
			var ob = arr[0];
			var last = arr[^1];

			var move = Math.Abs((last.ClosePrice - ob.ClosePrice) / ob.ClosePrice) * 100m;
			var relMove = move >= Threshold;

			var up = 0;
			var down = 0;

			for (var i = 1; i < arr.Length; i++)
			{
			var c = arr[i];
			if (c.ClosePrice > c.OpenPrice)
			up++;
			if (c.ClosePrice < c.OpenPrice)
			down++;
			}

			if (ob.ClosePrice < ob.OpenPrice && up == Periods && relMove && ob.OpenTime != lastBull)
			{
			if (Position <= 0)
			BuyMarket();

			lastBull = ob.OpenTime;
			}
			else if (ob.ClosePrice > ob.OpenPrice && down == Periods && relMove && ob.OpenTime != lastBear)
			{
			if (Position >= 0)
			SellMarket();

			lastBear = ob.OpenTime;
			}
		}
	}
}
