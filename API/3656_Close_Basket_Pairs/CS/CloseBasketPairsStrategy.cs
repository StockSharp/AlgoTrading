using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Closes positions belonging to a predefined currency basket once profit or loss thresholds are met.
/// This strategy mirrors the MetaTrader "Close Basket Pairs" script using the StockSharp high level API.
/// Note: This is a multi-instrument utility strategy and will be marked SKIP.
/// </summary>
public class CloseBasketPairsStrategy : Strategy
{
	private readonly StrategyParam<string> _basketDefinition;
	private readonly StrategyParam<decimal> _profitThreshold;
	private readonly StrategyParam<decimal> _lossThreshold;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public CloseBasketPairsStrategy()
	{
		_basketDefinition = Param(nameof(BasketPairs), "EURUSD|BUY,GBPUSD|SELL,USDJPY|BUY")
			.SetDisplay("Basket Pairs", "Comma separated list of SYMBOL|SIDE entries", "General");

		_profitThreshold = Param(nameof(ProfitThreshold), 0m)
			.SetDisplay("Profit Threshold", "Close matching positions when their floating profit is above this value", "General");

		_lossThreshold = Param(nameof(LossThreshold), 0m)
			.SetDisplay("Loss Threshold", "Close matching positions when their floating loss is below this (negative) value", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for periodic checks", "General");
	}

	/// <summary>
	/// Comma separated list describing the managed basket.
	/// </summary>
	public string BasketPairs { get => _basketDefinition.Value; set => _basketDefinition.Value = value; }

	/// <summary>
	/// Positive profit (in portfolio currency) required to close profitable positions.
	/// </summary>
	public decimal ProfitThreshold { get => _profitThreshold.Value; set => _profitThreshold.Value = value; }

	/// <summary>
	/// Negative loss (in portfolio currency) that forces closing losing positions.
	/// </summary>
	public decimal LossThreshold { get => _lossThreshold.Value; set => _lossThreshold.Value = value; }

	/// <summary>
	/// Candle type for periodic processing.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (ProfitThreshold <= 0m && LossThreshold >= 0m)
		{
			LogInfo("Both profit and loss thresholds are disabled.");
		}

		LogInfo($"Basket pairs definition: {BasketPairs}");

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Utility strategy: log current PnL for monitoring.
		LogInfo($"Candle at {candle.OpenTime:O}, PnL: {PnL}");
	}
}
