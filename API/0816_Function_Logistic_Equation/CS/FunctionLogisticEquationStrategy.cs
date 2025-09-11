using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Logistic equation based strategy.
/// Buys when positive logistic value exceeds upper bound.
/// Sells when negative logistic value drops below lower bound.
/// </summary>
public class FunctionLogisticEquationStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Length for calculations.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
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
	/// Initializes a new instance of <see cref="FunctionLogisticEquationStrategy"/>.
	/// </summary>
	public FunctionLogisticEquationStrategy()
	{
		_length = Param(nameof(Length), 14)
			.SetDisplay("Length", "Period for logistic equation calculation", "Parameters")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)))
			.SetDisplay("Candle Type", "Type of candles for analysis", "Parameters");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var momentum = new Momentum { Length = Length };
		var highest = new Highest { Length = Length };
		var lowest = new Lowest { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(momentum, highest, lowest, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal pop, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var limit = highestValue - lowestValue;

		if (limit == 0)
			return;

		var logdeNeg = pop * (1m - pop / limit);
		var logdePos = pop + pop * pop / limit;

		var upper = limit * 0.5m;
		var lower = -upper;

		if (logdePos > upper && Position <= 0)
			BuyMarket();
		else if (logdeNeg < lower && Position >= 0)
			SellMarket();
	}
}
