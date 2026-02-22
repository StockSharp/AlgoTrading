using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OutOfTheNoiseIntradayWithVwapStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _dayOpen;
	private DateTime _currentDay;
	private readonly List<decimal> _moves = new();

	public int Period { get => _period.Value; set => _period.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OutOfTheNoiseIntradayWithVwapStrategy()
	{
		_period = Param(nameof(Period), 20).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_dayOpen = 0;
		_currentDay = default;
		_moves.Clear();

		var vwap = new VolumeWeightedMovingAverage();
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(vwap, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, vwap);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue vwapVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!vwapVal.IsFinal || !vwapVal.IsFormed)
			return;

		var vwap = vwapVal.ToDecimal();
		var day = candle.OpenTime.Date;

		if (_currentDay != day)
		{
			_currentDay = day;
			_dayOpen = candle.OpenPrice;
		}

		if (_dayOpen == 0) return;

		var move = Math.Abs(candle.ClosePrice / _dayOpen - 1m);
		_moves.Add(move);

		if (_moves.Count < Period) return;

		// Take last Period moves
		var recentMoves = _moves.Skip(_moves.Count - Period).Take(Period);
		var avgMove = recentMoves.Average();

		var upperBound = _dayOpen * (1 + avgMove);
		var lowerBound = _dayOpen * (1 - avgMove);

		// Breakout above noise band
		if (candle.ClosePrice > upperBound && Position <= 0)
		{
			if (Position < 0) BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
		}
		// Breakout below noise band
		else if (candle.ClosePrice < lowerBound && Position >= 0)
		{
			if (Position > 0) SellMarket(Math.Abs(Position));
			SellMarket(Volume);
		}
		// Exit long on VWAP cross below
		else if (Position > 0 && candle.ClosePrice < vwap)
		{
			SellMarket(Math.Abs(Position));
		}
		// Exit short on VWAP cross above
		else if (Position < 0 && candle.ClosePrice > vwap)
		{
			BuyMarket(Math.Abs(Position));
		}

		if (_moves.Count > Period * 3)
			_moves.RemoveRange(0, _moves.Count - Period * 3);
	}
}
