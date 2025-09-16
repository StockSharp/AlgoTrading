using System;
using System.Collections.Generic;

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
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private TrendDirection _trend = TrendDirection.None;

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
	/// Trade volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
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
		.SetCanOptimize();

		_shiftPercent = Param(nameof(ShiftPercent), 30m)
		.SetDisplay("Shift %", "Percentage shift from channel", "General")
		.SetGreaterThanZero()
		.SetCanOptimize();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for strategy", "General");

		_volume = Param(nameof(Volume), 1m)
		.SetDisplay("Volume", "Order volume", "General")
		.SetGreaterThanZero();

		_stopLoss = Param(nameof(StopLoss), 1000m)
		.SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
		.SetGreaterThanZero();

		_takeProfit = Param(nameof(TakeProfit), 2000m)
		.SetDisplay("Take Profit", "Take profit in price units", "Risk")
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
		_trend = TrendDirection.None;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = Period };
		_lowest = new Lowest { Length = Period };

		StartProtection(new Unit(TakeProfit, UnitTypes.Absolute), new Unit(StopLoss, UnitTypes.Absolute));

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_highest, _lowest, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
		DrawCandles(area, subscription);
		DrawIndicator(area, _highest, "Highest");
		DrawIndicator(area, _lowest, "Lowest");
		DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highValue, decimal lowValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var range = (highValue - lowValue) * ShiftPercent / 100m;
		var close = candle.ClosePrice;

		if (_trend != TrendDirection.Down && close <= lowValue + range)
		{
		// Close long and open short if allowed
		if (Position > 0)
		SellMarket(Position);

		if (Position >= 0)
		{
		SellMarket(Volume);
		_trend = TrendDirection.Down;
		}
		}
		else if (_trend != TrendDirection.Up && close >= highValue - range)
		{
		// Close short and open long if allowed
		if (Position < 0)
		BuyMarket(-Position);

		if (Position <= 0)
		{
		BuyMarket(Volume);
		_trend = TrendDirection.Up;
		}
		}
	}

	private enum TrendDirection
	{
		None,
		Up,
		Down
	}
}
