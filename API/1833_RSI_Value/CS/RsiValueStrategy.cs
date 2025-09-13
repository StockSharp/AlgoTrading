using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on RSI crossing the middle level.
/// Buy when RSI crosses above the configured level and sell when it crosses below.
/// Includes optional stop-loss, take-profit and trailing stop.
/// </summary>
public class RsiValueStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiLevel;
	private readonly StrategyParam<Unit> _stopLoss;
	private readonly StrategyParam<Unit> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevIsAbove;
	private decimal _trailingPrice;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level for crossing (e.g., 50).
	/// </summary>
	public decimal RsiLevel
	{
		get => _rsiLevel.Value;
		set => _rsiLevel.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price units.
	/// </summary>
	public Unit StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price units.
	/// </summary>
	public Unit TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price units.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the <see cref="RsiValueStrategy"/>.
	/// </summary>
	public RsiValueStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetDisplay("RSI Period", "Period for RSI indicator", "Indicators");

		_rsiLevel = Param(nameof(RsiLevel), 50m)
		.SetRange(0m, 100m)
		.SetDisplay("RSI Level", "Level used for crossings", "Indicators");

		_stopLoss = Param(nameof(StopLoss), new Unit(100, UnitTypes.Absolute))
		.SetDisplay("Stop Loss", "Stop-loss distance", "Risk Management");

		_takeProfit = Param(nameof(TakeProfit), new Unit(200, UnitTypes.Absolute))
		.SetDisplay("Take Profit", "Take-profit distance", "Risk Management");

		_trailingStop = Param(nameof(TrailingStop), 0m)
		.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to subscribe", "General");
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

		_prevIsAbove = false;
		_trailingPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(rsi, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			var rsiArea = CreateChartArea();
			if (rsiArea != null)
			{
				DrawIndicator(rsiArea, rsi);
			}
			DrawOwnTrades(area);
		}

		StartProtection(takeProfit: TakeProfit, stopLoss: StopLoss);
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var isAbove = rsiValue > RsiLevel;
		var crossedAbove = !_prevIsAbove && isAbove;
		var crossedBelow = _prevIsAbove && !isAbove;

		if (crossedAbove)
		{
			if (Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				_trailingPrice = 0m;
			}
			else if (Position == 0)
			{
				BuyMarket(Volume);
				if (TrailingStop > 0)
				_trailingPrice = candle.ClosePrice - TrailingStop;
			}
		}
		else if (crossedBelow)
		{
			if (Position > 0)
			{
				SellMarket(Position);
				_trailingPrice = 0m;
			}
			else if (Position == 0)
			{
				SellMarket(Volume);
				if (TrailingStop > 0)
				_trailingPrice = candle.ClosePrice + TrailingStop;
			}
		}

		// Trailing stop handling
		if (TrailingStop > 0)
		{
			if (Position > 0)
			{
				var candidate = candle.ClosePrice - TrailingStop;
				if (_trailingPrice == 0m || candidate > _trailingPrice)
				_trailingPrice = candidate;
				if (candle.ClosePrice <= _trailingPrice)
				{
					SellMarket(Position);
					_trailingPrice = 0m;
				}
			}
			else if (Position < 0)
			{
				var candidate = candle.ClosePrice + TrailingStop;
				if (_trailingPrice == 0m || candidate < _trailingPrice)
				_trailingPrice = candidate;
				if (candle.ClosePrice >= _trailingPrice)
				{
					BuyMarket(Math.Abs(Position));
					_trailingPrice = 0m;
				}
			}
		}

		_prevIsAbove = isAbove;
	}
}
