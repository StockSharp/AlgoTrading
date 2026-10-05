using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Donchian Channel width breakout.
/// Enters when the channel width exceeds its average by a standard deviation multiplier,
/// in the direction of the close relative to the channel middle.
/// Exits when the width falls back below its average.
/// </summary>
public class DonchianWidthBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<int> _avgPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLoss;

	private SimpleMovingAverage _widthAverage;
	private StandardDeviation _widthStdDev;

	/// <summary>
	/// Donchian Channel period.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
	}

	/// <summary>
	/// Period for the width average and standard deviation.
	/// </summary>
	public int AvgPeriod
	{
		get => _avgPeriod.Value;
		set => _avgPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for the width breakout.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="DonchianWidthBreakoutStrategy"/>.
	/// </summary>
	public DonchianWidthBreakoutStrategy()
	{
		_donchianPeriod = Param(nameof(DonchianPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Donchian Period", "Period of the Donchian Channel", "Indicators");

		_avgPeriod = Param(nameof(AvgPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Average Period", "Period for width average and deviation", "Strategy");

		_multiplier = Param(nameof(Multiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Standard deviation multiplier for breakout", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_stopLoss = Param(nameof(StopLoss), 2.0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk Management");
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
		_widthAverage = null;
		_widthStdDev = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = DonchianPeriod };
		var lowest = new Lowest { Length = DonchianPeriod };
		_widthAverage = new SimpleMovingAverage { Length = AvgPeriod };
		_widthStdDev = new StandardDeviation { Length = AvgPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: null,
			stopLoss: StopLoss > 0 ? new Unit(StopLoss, UnitTypes.Percent) : null
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var width = highestValue - lowestValue;
		var avgWidth = _widthAverage.Process(width, candle.ServerTime, true).ToDecimal();
		var stdWidth = _widthStdDev.Process(width, candle.ServerTime, true).ToDecimal();

		if (!_widthAverage.IsFormed || !_widthStdDev.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var middle = (highestValue + lowestValue) / 2m;

		if (width > avgWidth + Multiplier * stdWidth)
		{
			if (close > middle && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				return;
			}

			if (close < middle && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				return;
			}
		}

		if (width < avgWidth)
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);
		}
	}
}
