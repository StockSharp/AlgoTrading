using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Vortex multi-timeframe strategy.
/// Goes long when VI+ crosses above VI- and short on the opposite signal.
/// </summary>
public class VortexMtfStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Vortex indicator period length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public VortexMtfStrategy()
	{
		_length = Param(nameof(Length), 14)
			.SetGreaterThanZero()
			.SetDisplay("Vortex Length", "Period of the Vortex indicator", "General")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(240).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for Vortex calculation", "General");
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

		var vortex = new VortexIndicator { Length = Length };

		var subscription = SubscribeCandles(CandleType);

		var prevPlus = 0m;
		var prevMinus = 0m;
		var isInitialized = false;

		subscription
			.Bind(vortex, (candle, viPlus, viMinus) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (!isInitialized && vortex.IsFormed)
				{
					prevPlus = viPlus;
					prevMinus = viMinus;
					isInitialized = true;
					return;
				}

				if (!isInitialized)
					return;

				if (prevPlus <= prevMinus && viPlus > viMinus && Position <= 0)
				{
					BuyMarket(Volume + Math.Abs(Position));
				}
				else if (prevPlus >= prevMinus && viPlus < viMinus && Position >= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
				}

				prevPlus = viPlus;
				prevMinus = viMinus;
			})
			.Start();
	}
}
