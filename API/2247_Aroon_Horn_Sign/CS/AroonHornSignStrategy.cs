using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Aroon Horn Sign trend reversal strategy.
/// Opens long when Aroon Up crosses above Aroon Down above 50.
/// Opens short when the opposite occurs.
/// Includes stop-loss and take-profit protection.
/// </summary>
public class AroonHornSignStrategy : Strategy
{
	private readonly StrategyParam<int> _aroonPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	private int _prevTrend;

	/// <summary>
	/// Initializes a new instance of the <see cref="AroonHornSignStrategy"/> class.
	/// </summary>
	public AroonHornSignStrategy()
	{
		_aroonPeriod = Param(nameof(AroonPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Aroon Period", "Aroon indicator period", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for processing", "General");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");
	}

	/// <summary>
	/// Aroon indicator period.
	/// </summary>
	public int AroonPeriod
	{
		get => _aroonPeriod.Value;
		set => _aroonPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for data processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Take profit value in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss value in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevTrend = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(new Unit(TakeProfit, UnitTypes.Price), new Unit(StopLoss, UnitTypes.Price));

		var aroon = new Aroon { Length = AroonPeriod };

		var sub = SubscribeCandles(CandleType);

		sub.BindEx(aroon, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue aroonValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var value = (AroonValue)aroonValue;

		var up = value.Up;
		var down = value.Down;

		var trend = _prevTrend;

		if (up > down && up >= 50m)
			trend = 1;
		else if (down > up && down >= 50m)
			trend = -1;

		if (_prevTrend <= 0 && trend > 0)
		{
			if (Position < 0)
				BuyMarket(-Position);

			if (Position == 0)
				BuyMarket();
		}
		else if (_prevTrend >= 0 && trend < 0)
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position == 0)
				SellMarket();
		}

		_prevTrend = trend;
	}
}

