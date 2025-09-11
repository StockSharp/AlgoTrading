namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// LSMA Fast And Simple Alternative Calculation Strategy.
/// Buys when close price crosses above LSMA and sells when it crosses below.
/// </summary>
public class LsmaFastSimpleAlternativeCalculationStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevDiff;

	public LsmaFastSimpleAlternativeCalculationStrategy()
	{
		_length = Param(nameof(Length), 25)
			.SetDisplay("LSMA Length", "Length for LSMA calculation", "LSMA");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevDiff = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var wma = new WeightedMovingAverage { Length = Length };
		var sma = new SMA { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(wma, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wmaValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var lsma = 3m * wmaValue - 2m * smaValue;
		var diff = candle.ClosePrice - lsma;

		if (_prevDiff <= 0m && diff > 0m && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (_prevDiff >= 0m && diff < 0m && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevDiff = diff;
	}
}
