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
/// Strategy based on Beginner indicator breakout.
/// Opens a long position when price closes near the recent high.
/// Opens a short position when price closes near the recent low.
/// </summary>
public class BeginnerBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _shiftPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private TrendDirections _trend = TrendDirections.None;

	/// <summary>
	/// Lookback period for highest/highest calculation.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Percentage shift from the channel borders.
	/// </summary>
	public decimal ShiftPercent
	{
		get => _shiftPercent.Value;
		set => _shiftPercent.Value = value;
	}

	/// <summary>
	/// Candle type to work on.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}


	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BeginnerBreakoutStrategy"/> class.
	/// </summary>
	public BeginnerBreakoutStrategy()
	{
		_period = Param(nameof(Period), 9)
		.SetDisplay("Period", "Lookback period for highs/lows", "General")
		.SetGreaterThanZero()
		;

		_shiftPercent = Param(nameof(ShiftPercent), 30m)
		.SetDisplay("Shift %", "Percentage shift from channel", "General")
		.SetGreaterThanZero()
		;

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for strategy", "General");


		_stopLoss = Param(nameof(StopLoss), 2m)
		.SetDisplay("Stop Loss", "Stop loss in percent", "Risk")
		.SetGreaterThanZero();

		_takeProfit = Param(nameof(TakeProfit), 4m)
		.SetDisplay("Take Profit", "Take profit in percent", "Risk")
		.SetGreaterThanZero();
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
		_trend = TrendDirections.None;
		_highest = null!;
		_lowest = null!;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highest = new Highest { Length = Period };
		_lowest = new Lowest { Length = Period };

		StartProtection(new Unit(TakeProfit, UnitTypes.Percent), new Unit(StopLoss, UnitTypes.Percent));

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_highest, _lowest, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
		DrawCandles(area, subscription);
		DrawIndicator(area, _highest);
		DrawIndicator(area, _lowest);
		DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highValue, decimal lowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		var range = (highValue - lowValue) * ShiftPercent / 100m;
		var close = candle.ClosePrice;

		if (_trend != TrendDirections.Down && close <= lowValue + range)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
			{
				SellMarket();
				_trend = TrendDirections.Down;
			}
		}
		else if (_trend != TrendDirections.Up && close >= highValue - range)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
			{
				BuyMarket();
				_trend = TrendDirections.Up;
			}
		}
	}

	private enum TrendDirections
	{
		None,
		Up,
		Down
	}
}