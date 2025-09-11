using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy trading around highs and lows range.
/// </summary>
public class HighsLowsStrategy : Strategy
{
	private readonly StrategyParam<int> _range;
	private readonly StrategyParam<decimal> _lowThreshold;
	private readonly StrategyParam<decimal> _highThreshold;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Range length.
	/// </summary>
	public int Range
	{
		get => _range.Value;
		set => _range.Value = value;
	}

	/// <summary>
	/// Oversold threshold.
	/// </summary>
	public decimal LowThreshold
	{
		get => _lowThreshold.Value;
		set => _lowThreshold.Value = value;
	}

	/// <summary>
	/// Overbought threshold.
	/// </summary>
	public decimal HighThreshold
	{
		get => _highThreshold.Value;
		set => _highThreshold.Value = value;
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
	/// Initialize strategy parameters.
	/// </summary>
	public HighsLowsStrategy()
	{
		_range = Param(nameof(Range), 100)
			.SetDisplay("Range", "Number of candles for high/low", "General")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 25);

		_lowThreshold = Param(nameof(LowThreshold), 15m)
			.SetDisplay("Low Threshold", "Oversold level", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(5m, 25m, 5m);

		_highThreshold = Param(nameof(HighThreshold), 85m)
			.SetDisplay("High Threshold", "Overbought level", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(75m, 95m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(240).TimeFrame())
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

		var highest = new Highest { Length = Range };
		var lowest = new Lowest { Length = Range };

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
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal high, decimal low)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (high == low)
			return;

		var average = (high + low) / 2m;
		var currentMove = (candle.HighPrice + candle.LowPrice) / 2m;
		var currentMoveVal = (currentMove - low) * 100m / (high - low);

		if (currentMove < average && currentMoveVal < LowThreshold && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (currentMove >= average && currentMoveVal >= HighThreshold && Position > 0)
		{
			ClosePosition();
		}
	}
}
