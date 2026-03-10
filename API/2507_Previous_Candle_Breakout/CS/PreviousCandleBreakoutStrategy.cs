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
/// Breakout strategy that trades when the close crosses the previous candle's high or low.
/// Ported from the BreakOut.mq4 expert by Soubra2003.
/// </summary>
public class PreviousCandleBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossOffset;
	private readonly StrategyParam<decimal> _takeProfitOffset;

	private decimal? _previousHigh;
	private decimal? _previousLow;
	private decimal _entryPrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossOffset { get => _stopLossOffset.Value; set => _stopLossOffset.Value = value; }
	public decimal TakeProfitOffset { get => _takeProfitOffset.Value; set => _takeProfitOffset.Value = value; }

	public PreviousCandleBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candle subscription", "General");

		_stopLossOffset = Param(nameof(StopLossOffset), 1000m)
			.SetDisplay("Stop Loss", "Price distance for the stop-loss. Set 0 to disable.", "Risk")
			;

		_takeProfitOffset = Param(nameof(TakeProfitOffset), 1500m)
			.SetDisplay("Take Profit", "Price distance for the take-profit. Set 0 to disable.", "Risk")
			;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_previousHigh = null;
		_previousLow = null;
		_entryPrice = 0m;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_previousHigh is null || _previousLow is null)
		{
			// Store the first finished candle to obtain reference high/low levels.
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			return;
		}

		var previousHigh = _previousHigh.Value;
		var previousLow = _previousLow.Value;
		var close = candle.ClosePrice;

		var breakoutAbove = close > previousHigh;
		var breakoutBelow = close < previousLow;

		// Manage protective exits while a position is open.
		if (Position > 0)
		{
			if (StopLossOffset > 0m && close <= _entryPrice - StopLossOffset)
			{
				if (Position > 0) SellMarket(); else if (Position < 0) BuyMarket();
				_entryPrice = 0m;
			}
			else if (TakeProfitOffset > 0m && close >= _entryPrice + TakeProfitOffset)
			{
				if (Position > 0) SellMarket(); else if (Position < 0) BuyMarket();
				_entryPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if (StopLossOffset > 0m && close >= _entryPrice + StopLossOffset)
			{
				if (Position > 0) SellMarket(); else if (Position < 0) BuyMarket();
				_entryPrice = 0m;
			}
			else if (TakeProfitOffset > 0m && close <= _entryPrice - TakeProfitOffset)
			{
				if (Position > 0) SellMarket(); else if (Position < 0) BuyMarket();
				_entryPrice = 0m;
			}
		}

		// Breakout above the previous high opens or reverses into a long position.
		if (breakoutAbove)
		{
			if (Position < 0)
			{
				if (Position > 0) SellMarket(); else if (Position < 0) BuyMarket();
				_entryPrice = 0m;
			}

			if (Position <= 0)
			{
				BuyMarket();
				_entryPrice = close;
			}
		}
		else if (breakoutBelow)
		{
			// Breakout below the previous low opens or reverses into a short position.
			if (Position > 0)
			{
				if (Position > 0) SellMarket(); else if (Position < 0) BuyMarket();
				_entryPrice = 0m;
			}

			if (Position >= 0)
			{
				SellMarket();
				_entryPrice = close;
			}
		}

		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
	}
}
