using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "Pause Trading On Consecutive Loss" MetaTrader expert.
/// Uses simple momentum entries (close vs previous close) with a pause mechanism
/// that halts trading after consecutive losing trades within a time window.
/// </summary>
public class PauseTradingOnConsecutiveLossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _consecutiveLosses;
	private readonly StrategyParam<int> _pauseBars;

	private decimal? _previousClose;
	private int _lossStreak;
	private int _pauseCountdown;
	private decimal _entryPrice;
	private Sides? _entryDirection;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int ConsecutiveLosses
	{
		get => _consecutiveLosses.Value;
		set => _consecutiveLosses.Value = value;
	}

	public int PauseBars
	{
		get => _pauseBars.Value;
		set => _pauseBars.Value = value;
	}

	public PauseTradingOnConsecutiveLossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for momentum entries", "General");

		_consecutiveLosses = Param(nameof(ConsecutiveLosses), 3)
			.SetGreaterThanZero()
			.SetDisplay("Consecutive Losses", "Losses before pausing", "Risk");

		_pauseBars = Param(nameof(PauseBars), 4)
			.SetGreaterThanZero()
			.SetDisplay("Pause Bars", "Number of bars to pause after loss streak", "Risk");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousClose = null;
		_lossStreak = 0;
		_pauseCountdown = 0;
		_entryPrice = 0;
		_entryDirection = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (_previousClose is null)
		{
			_previousClose = close;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Check if we should pause
		if (_pauseCountdown > 0)
		{
			_pauseCountdown--;
			_previousClose = close;
			return;
		}

		// Check for exit and track wins/losses
		if (Position != 0)
		{
			var shouldExit = false;

			if (Position > 0 && close < candle.OpenPrice)
				shouldExit = true;
			else if (Position < 0 && close > candle.OpenPrice)
				shouldExit = true;

			if (shouldExit)
			{
				// Determine if this was a winning or losing trade
				var isLoss = false;
				if (_entryDirection == Sides.Buy && close < _entryPrice)
					isLoss = true;
				else if (_entryDirection == Sides.Sell && close > _entryPrice)
					isLoss = true;

				if (isLoss)
				{
					_lossStreak++;
					if (_lossStreak >= ConsecutiveLosses)
					{
						_pauseCountdown = PauseBars;
						_lossStreak = 0;
					}
				}
				else
				{
					_lossStreak = 0;
				}

				// Close position
				if (Position > 0)
					SellMarket(Position);
				else if (Position < 0)
					BuyMarket(Math.Abs(Position));

				_entryDirection = null;
			}
		}

		// New entry: momentum - close > prev close -> buy, close < prev close -> sell
		if (Position == 0 && _entryDirection is null)
		{
			if (close > _previousClose.Value)
			{
				BuyMarket(volume);
				_entryPrice = close;
				_entryDirection = Sides.Buy;
			}
			else if (close < _previousClose.Value)
			{
				SellMarket(volume);
				_entryPrice = close;
				_entryDirection = Sides.Sell;
			}
		}

		_previousClose = close;
	}
}
