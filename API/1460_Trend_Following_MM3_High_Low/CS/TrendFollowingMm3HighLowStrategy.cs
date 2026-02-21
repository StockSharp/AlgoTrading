using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend Following strategy using SMA on highs and lows.
/// Enters long when close is above SMA of highs and exits when close drops below SMA of lows.
/// </summary>
public class TrendFollowingMm3HighLowStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _maHigh = null!;
	private SimpleMovingAverage _maLow = null!;

	/// <summary>
	/// SMA period.
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
	/// Initializes a new instance of the <see cref="TrendFollowingMm3HighLowStrategy"/>.
	/// </summary>
	public TrendFollowingMm3HighLowStrategy()
	{
		_length = Param(nameof(Length), 3)
			.SetDisplay("SMA Length", "Period for moving averages", "Parameters")
			
			.SetOptimize(2, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_maHigh = new SMA { Length = Length };
		_maLow = new SMA { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _maHigh);
			DrawIndicator(area, _maLow);
			DrawOwnTrades(area);
		}

		StartProtection(null, null);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var highMa = _maHigh.Process(new DecimalIndicatorValue(_maHigh, candle.HighPrice, candle.OpenTime)).ToDecimal();
		var lowMa = _maLow.Process(new DecimalIndicatorValue(_maLow, candle.LowPrice, candle.OpenTime)).ToDecimal();

		if (candle.ClosePrice > highMa && Position <= 0)
		{
		BuyMarket(Volume + Math.Abs(Position));
		}
		else if (candle.ClosePrice < lowMa && Position > 0)
		{
		SellMarket(Position);
		}
	}
}
