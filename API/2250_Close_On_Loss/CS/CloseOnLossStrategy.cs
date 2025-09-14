using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Closes all positions when realized PnL drops below the maximum allowed loss.
/// </summary>
public class CloseOnLossStrategy : Strategy
{
	private readonly StrategyParam<decimal> _maxLoss;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Maximum allowed loss in account currency.
	/// </summary>
	public decimal MaxLoss
	{
		get => _maxLoss.Value;
		set => _maxLoss.Value = value;
	}

	/// <summary>
	/// Candle type used for periodic PnL checks.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public CloseOnLossStrategy()
	{
		_maxLoss = Param(nameof(MaxLoss), 1000m)
			.SetDisplay("Max Loss", "Maximum loss before closing positions", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", string.Empty, "Data");
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

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var profit = PnL;

		if (profit <= -MaxLoss)
		{
			// Close long positions if any
			if (Position > 0)
				SellMarket();

			// Close short positions if any
			if (Position < 0)
				BuyMarket();
		}
	}
}
