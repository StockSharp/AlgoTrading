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
/// Strategy based on SMA over custom seconds timeframe.
/// </summary>
public class MtfSecondsValuesJDStrategy : Strategy
{
	private readonly StrategyParam<int> _secondsTimeframe;
	private readonly StrategyParam<int> _averageLength;

	/// <summary>
	/// Seconds timeframe for candle aggregation.
	/// </summary>
	public int SecondsTimeframe
	{
		get => _secondsTimeframe.Value;
		set => _secondsTimeframe.Value = value;
	}

	/// <summary>
	/// Period for SMA.
	/// </summary>
	public int AverageLength
	{
		get => _averageLength.Value;
		set => _averageLength.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MtfSecondsValuesJDStrategy"/>.
	/// </summary>
	public MtfSecondsValuesJDStrategy()
	{
		_secondsTimeframe = Param(nameof(SecondsTimeframe), 30)
			.SetDisplay("Seconds Timeframe", "Seconds Timeframe", "General")
			;

		_averageLength = Param(nameof(AverageLength), 20)
			.SetDisplay("Average Length", "Average Length", "General")
			;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SMA { Length = AverageLength };

		var candleType = TimeSpan.FromSeconds(SecondsTimeframe).TimeFrame();
		var subscription = SubscribeCandles(candleType);

		subscription
			.Bind(sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.ClosePrice > smaValue && Position <= 0)
			BuyMarket();
		else if (candle.ClosePrice < smaValue && Position >= 0)
			SellMarket();
	}
}
