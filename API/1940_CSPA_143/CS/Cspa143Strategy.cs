using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified currency strength strategy based on RSI.
/// </summary>
public class Cspa143Strategy : Strategy
{
	private readonly StrategyParam<int> _strengthPeriod;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	public int StrengthPeriod { get => _strengthPeriod.Value; set => _strengthPeriod.Value = value; }
	public decimal Threshold { get => _threshold.Value; set => _threshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Cspa143Strategy()
	{
		_strengthPeriod = Param(nameof(StrengthPeriod), 14)
			.SetDisplay("Strength Period", "RSI period", "Parameters");

		_threshold = Param(nameof(Threshold), 10m)
			.SetDisplay("Threshold", "RSI distance from 50", "Parameters")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		var rsi = new RSI { Length = StrengthPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var upper = 50m + Threshold;
		var lower = 50m - Threshold;

		// Entry logic based on RSI thresholds
		if (rsi > upper && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (rsi < lower && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
		}

		// Exit when momentum fades back to neutral zone
		if (Position > 0 && rsi < 50m)
			SellMarket();
		else if (Position < 0 && rsi > 50m)
			BuyMarket();
	}
}
