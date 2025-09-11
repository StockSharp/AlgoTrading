using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that compares potential gain and adjusted loss over a lookback period.
/// Goes long when potential gain exceeds adjusted loss, otherwise goes short.
/// </summary>
public class MaxGainStrategy : Strategy
{
	private readonly StrategyParam<int> _periodLength;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Lookback period length.
	/// </summary>
	public int PeriodLength
	{
		get => _periodLength.Value;
		set => _periodLength.Value = value;
	}

	/// <summary>
	/// The type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MaxGainStrategy"/>.
	/// </summary>
	public MaxGainStrategy()
	{
		_periodLength = Param(nameof(PeriodLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Period Length", "Number of candles for high/low calculation", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
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

		StartProtection();

		var highest = new Highest { Length = PeriodLength };
		var lowest = new Lowest { Length = PeriodLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maxHigh, decimal minLow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var maxGain = (candle.HighPrice - minLow) / minLow * 100m;
		var maxLoss = (candle.LowPrice - maxHigh) / maxHigh * -100m;
		var adjustedMaxLoss = maxLoss / (100m - maxLoss) * 100m;

		if (maxGain > adjustedMaxLoss)
		{
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
