using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Connors VIX Reversal III strategy.
/// The candles are the VIX series of the traded security. A bar whose low is above the LengthMA simple average and whose close is
/// PercentThreshold percent above it buys; a bar whose high is below the average and whose close is PercentThreshold percent below
/// it sells short. A long closes when the close falls below the previous bar's average and a short when it rises above it.
/// </summary>
public class ConnorsVixReversalIIIStrategy : Strategy
{
	private readonly StrategyParam<int> _lengthMA;
	private readonly StrategyParam<decimal> _percentThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevMa;

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int LengthMA
	{
		get => _lengthMA.Value;
		set => _lengthMA.Value = value;
	}

	/// <summary>
	/// Distance from the average in percent that confirms a spike.
	/// </summary>
	public decimal PercentThreshold
	{
		get => _percentThreshold.Value;
		set => _percentThreshold.Value = value;
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
	/// Constructor.
	/// </summary>
	public ConnorsVixReversalIIIStrategy()
	{
		_lengthMA = Param(nameof(LengthMA), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average length", "Indicators");

		_percentThreshold = Param(nameof(PercentThreshold), 10m)
			.SetNotNegative()
			.SetDisplay("Percent Threshold", "Distance from the average in percent that confirms a spike", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
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
		_prevMa = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMa = null;

		var sma = new SimpleMovingAverage { Length = LengthMA };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!smaValue.IsFormed)
			return;

		var ma = smaValue.ToDecimal();
		var prevMa = _prevMa;
		_prevMa = ma;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var factor = PercentThreshold / 100m;

		if (candle.LowPrice > ma && close > ma * (1m + factor) && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (candle.HighPrice < ma && close < ma * (1m - factor) && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (prevMa is decimal yesterdayMa)
		{
			if (Position > 0 && close < yesterdayMa)
				SellMarket(Position);
			else if (Position < 0 && close > yesterdayMa)
				BuyMarket(-Position);
		}
	}
}
