using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI based trading strategy with fixed stop loss, take profit and trailing stop.
/// Opens long when RSI falls below oversold level and short when RSI rises above overbought level.
/// Positions close when RSI returns to a mid level or when stops are reached.
/// </summary>
public class RsiAutomatedStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _exitLevel;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// RSI level to open short positions.
	/// </summary>
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }

	/// <summary>
	/// RSI level to open long positions.
	/// </summary>
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }

	/// <summary>
	/// RSI level that closes any existing position.
	/// </summary>
	public decimal ExitLevel { get => _exitLevel.Value; set => _exitLevel.Value = value; }

	/// <summary>
	/// Initial stop loss distance in price points.
	/// </summary>
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }

	/// <summary>
	/// Take profit distance in price points.
	/// </summary>
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }

	/// <summary>
	/// Trailing stop distance in price points.
	/// </summary>
	public decimal TrailingStopPoints { get => _trailingStopPoints.Value; set => _trailingStopPoints.Value = value; }

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public RsiAutomatedStrategy()
	{
	_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "RSI calculation length", "RSI")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 5);

	_overbought = Param(nameof(Overbought), 75m)
		.SetDisplay("Overbought", "RSI value to open short", "RSI");

	_oversold = Param(nameof(Oversold), 25m)
		.SetDisplay("Oversold", "RSI value to open long", "RSI");

	_exitLevel = Param(nameof(ExitLevel), 50m)
		.SetDisplay("Exit Level", "RSI level to close position", "RSI");

	_stopLossPoints = Param(nameof(StopLossPoints), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Initial stop loss in points", "Risk");

	_takeProfitPoints = Param(nameof(TakeProfitPoints), 150m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Take profit distance in points", "Risk");

	_trailingStopPoints = Param(nameof(TrailingStopPoints), 25m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing", "Trailing stop distance in points", "Risk");

	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
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
	_entryPrice = 0m;
	_stopPrice = 0m;
	_takeProfitPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	StartProtection();

	var rsi = new RelativeStrengthIndex
	{
		Length = RsiPeriod
	};

	var subscription = SubscribeCandles(CandleType);
	subscription
		.Bind(rsi, ProcessCandle)
		.Start();

	var area = CreateChartArea();
	if (area != null)
	{
		DrawCandles(area, subscription);
		DrawIndicator(area, rsi);
		DrawOwnTrades(area);
	}
	}

	private void ResetState()
	{
	_entryPrice = 0m;
	_stopPrice = 0m;
	_takeProfitPrice = 0m;
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
	if (candle.State != CandleStates.Finished)
		return;

	if (!IsFormedAndOnlineAndAllowTrading())
		return;

	if (Position == 0)
	{
		if (rsiValue < Oversold)
		{
		BuyMarket(Volume);
		_entryPrice = candle.ClosePrice;
		_stopPrice = _entryPrice - StopLossPoints;
		_takeProfitPrice = _entryPrice + TakeProfitPoints;
		}
		else if (rsiValue > Overbought)
		{
		SellMarket(Volume);
		_entryPrice = candle.ClosePrice;
		_stopPrice = _entryPrice + StopLossPoints;
		_takeProfitPrice = _entryPrice - TakeProfitPoints;
		}

		return;
	}

	if (Position > 0)
	{
		if (rsiValue > ExitLevel)
		{
		SellMarket(Position);
		ResetState();
		return;
		}

		if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
		{
		SellMarket(Position);
		ResetState();
		return;
		}

		if (TrailingStopPoints > 0 && candle.ClosePrice - _entryPrice > TrailingStopPoints)
		{
		var newStop = candle.ClosePrice - TrailingStopPoints;
		if (newStop > _stopPrice)
			_stopPrice = newStop;
		}
	}
	else
	{
		if (rsiValue < ExitLevel)
		{
		BuyMarket(Math.Abs(Position));
		ResetState();
		return;
		}

		if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takeProfitPrice)
		{
		BuyMarket(Math.Abs(Position));
		ResetState();
		return;
		}

		if (TrailingStopPoints > 0 && _entryPrice - candle.ClosePrice > TrailingStopPoints)
		{
		var newStop = candle.ClosePrice + TrailingStopPoints;
		if (_stopPrice == 0m || newStop < _stopPrice)
			_stopPrice = newStop;
		}
	}
	}
}
