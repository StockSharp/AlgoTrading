using System;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on SMA over custom timeframe.
/// </summary>
public class MtfSecondsValuesJDStrategy : Strategy
{
	private readonly StrategyParam<int> _averageLength;
	private readonly StrategyParam<DataType> _candleType;

	public int AverageLength { get => _averageLength.Value; set => _averageLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MtfSecondsValuesJDStrategy()
	{
		_averageLength = Param(nameof(AverageLength), 20);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SMA { Length = AverageLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.ClosePrice > smaValue && Position <= 0)
			BuyMarket();
		else if (candle.ClosePrice < smaValue && Position >= 0)
			SellMarket();
	}
}
