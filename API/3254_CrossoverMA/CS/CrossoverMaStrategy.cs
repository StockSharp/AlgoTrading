using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CrossoverMA strategy: Buys when candle crosses above SMA with rising slope.
/// Sells when candle crosses below SMA with falling slope.
/// </summary>
public class CrossoverMaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _movingAverageLength;

	private decimal? _previousAverageValue;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MovingAverageLength
	{
		get => _movingAverageLength.Value;
		set => _movingAverageLength.Value = value;
	}

	public CrossoverMaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_movingAverageLength = Param(nameof(MovingAverageLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "SMA period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousAverageValue = null;

		var sma = new SimpleMovingAverage { Length = MovingAverageLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal averageValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var previousAverageValue = _previousAverageValue;
		_previousAverageValue = averageValue;

		if (previousAverageValue is null)
			return;

		var slope = averageValue - previousAverageValue.Value;
		var close = candle.ClosePrice;

		// Buy: close above MA + MA rising
		if (close > averageValue && slope > 0m && Position <= 0)
		{
			BuyMarket();
		}
		// Sell: close below MA + MA falling
		else if (close < averageValue && slope < 0m && Position >= 0)
		{
			SellMarket();
		}
	}
}
