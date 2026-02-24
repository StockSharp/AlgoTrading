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
/// Tick Marubozu strategy. Detects strong-body candles with small wicks
/// and trades in the direction of the candle.
/// </summary>
public class TickMarubozuStrategy : Strategy
{
	private readonly StrategyParam<decimal> _bodyRatio;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _volumes = new();
	private const int VolWindow = 20;

	/// <summary>Minimum body-to-range ratio for marubozu.</summary>
	public decimal BodyRatio { get => _bodyRatio.Value; set => _bodyRatio.Value = value; }
	/// <summary>Candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TickMarubozuStrategy()
	{
		_bodyRatio = Param(nameof(BodyRatio), 0.8m)
			.SetDisplay("Body Ratio", "Min body/range ratio", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_volumes.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 10 };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0)
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var ratio = body / range;

		// Track volume average
		_volumes.Add(candle.TotalVolume);
		if (_volumes.Count > VolWindow)
			_volumes.RemoveAt(0);

		if (_volumes.Count < VolWindow)
			return;

		var avgVol = _volumes.Average();
		var highVolume = candle.TotalVolume > avgVol;

		var bullish = candle.ClosePrice > candle.OpenPrice && ratio >= BodyRatio;
		var bearish = candle.ClosePrice < candle.OpenPrice && ratio >= BodyRatio;

		if (bullish && highVolume && Position <= 0)
			BuyMarket();
		else if (bearish && highVolume && Position >= 0)
			SellMarket();
	}
}
