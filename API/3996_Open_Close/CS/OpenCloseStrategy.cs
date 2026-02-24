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
/// Contrarian pattern strategy converted from the MetaTrader expert "open_close".
/// Evaluates relationships between consecutive candle opens and closes.
/// Buys when a bearish candle opens above the previous open (fading the move),
/// and sells when a bullish candle opens below the previous open.
/// </summary>
public class OpenCloseStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;

	private bool _hasPreviousCandle;
	private decimal _previousOpen;
	private decimal _previousClose;

	public OpenCloseStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 500m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss in absolute points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 500m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit in absolute points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time-frame used to evaluate the open/close pattern.", "Data");

		Volume = 1;
	}

	/// <summary>
	/// Stop loss distance in absolute points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in absolute points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle series used to evaluate the pattern.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
		_hasPreviousCandle = false;
		_previousOpen = 0m;
		_previousClose = 0m;
		_ema = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		_ema = new ExponentialMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}

		var tp = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.Absolute) : null;
		var sl = StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.Absolute) : null;
		if (tp != null || sl != null)
			StartProtection(tp, sl);

		base.OnStarted2(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var open = candle.OpenPrice;
		var close = candle.ClosePrice;

		if (!_hasPreviousCandle)
		{
			_previousOpen = open;
			_previousClose = close;
			_hasPreviousCandle = true;
			return;
		}

		// Exit logic
		if (Position > 0)
		{
			// Close long on bearish continuation
			if (open < _previousOpen && close < _previousClose)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			// Close short on bullish continuation
			if (open > _previousOpen && close > _previousClose)
				BuyMarket(Math.Abs(Position));
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousOpen = open;
			_previousClose = close;
			return;
		}

		// Entry logic
		if (Position == 0)
		{
			// Buy: fade a bearish candle that opened above the previous open
			if (open > _previousOpen && close < _previousClose)
			{
				BuyMarket(Volume);
			}
			// Sell: fade a bullish candle that opened below the previous open
			else if (open < _previousOpen && close > _previousClose)
			{
				SellMarket(Volume);
			}
		}

		_previousOpen = open;
		_previousClose = close;
	}
}
