using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Vortex indicator cross strategy.
/// Goes long when VI+ crosses above VI- and short on the opposite signal.
/// </summary>
public class VortexIndicatorCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;

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
	/// Stop loss in price steps.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in price steps.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public VortexIndicatorCrossStrategy()
	{
		_length = Param(nameof(Length), 14)
			.SetGreaterThanZero()
			.SetDisplay("Vortex Length", "Period for Vortex indicator", "General")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for Vortex calculation", "General");

		_stopLoss = Param(nameof(StopLoss), 1000)
			.SetDisplay("Stop Loss", "Protective stop in price steps", "General");

		_takeProfit = Param(nameof(TakeProfit), 2000)
			.SetDisplay("Take Profit", "Target profit in price steps", "General");
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

		StartProtection(
			stopLoss: new Unit(StopLoss, UnitTypes.Step),
			takeProfit: new Unit(TakeProfit, UnitTypes.Step));

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

				if (!isInitialized)
				{
					if (!vortex.IsFormed)
						return;

					prevPlus = viPlus;
					prevMinus = viMinus;
					isInitialized = true;
					return;
				}

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
