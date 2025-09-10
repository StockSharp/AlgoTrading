using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Balance of Power strategy - buys when BOP crosses above threshold and exits when it falls below negative threshold.
/// </summary>
public class BalanceOfPowerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _threshold;

	private decimal? _previousBop;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Balance of Power threshold.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BalanceOfPowerStrategy"/>.
	/// </summary>
	public BalanceOfPowerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_threshold = Param(nameof(Threshold), 0.8m)
			.SetRange(0.1m, 5.0m)
			.SetDisplay("BOP Threshold", "Balance of Power entry threshold", "Balance of Power")
			.SetCanOptimize(true);
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

		_previousBop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.HighPrice == candle.LowPrice)
			return;

		var bop = (candle.ClosePrice - candle.OpenPrice) / (candle.HighPrice - candle.LowPrice);

		if (_previousBop is decimal prev)
		{
			if (prev <= Threshold && bop > Threshold && Position <= 0)
				BuyMarket();
			else if (prev >= -Threshold && bop < -Threshold && Position > 0)
				SellMarket();
		}

		_previousBop = bop;
	}
}

