using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "Grid Trading at Volatile Market" MetaTrader expert.
/// Uses RSI + SMA trend filter for initial entry then manages a simple
/// grid of averaging orders based on ATR distance.
/// </summary>
public class GridTradingAtVolatileMarketStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _maxGridLevels;

	private RelativeStrengthIndex _rsi;
	private readonly Queue<decimal> _smaQueue = new();
	private decimal _smaSum;

	private Sides? _gridDirection;
	private int _gridLevel;
	private decimal _lastEntryPrice;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	public int MaxGridLevels
	{
		get => _maxGridLevels.Value;
		set => _maxGridLevels.Value = value;
	}

	public GridTradingAtVolatileMarketStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for signal detection", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period for entry signals", "Indicators");

		_smaPeriod = Param(nameof(SmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA period for trend filter", "Indicators");

		_maxGridLevels = Param(nameof(MaxGridLevels), 3)
			.SetGreaterThanZero()
			.SetDisplay("Max Grid Levels", "Maximum averaging levels", "Grid");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_smaQueue.Clear();
		_smaSum = 0;
		_gridDirection = null;
		_gridLevel = 0;
		_lastEntryPrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		// Manual SMA
		_smaQueue.Enqueue(close);
		_smaSum += close;
		while (_smaQueue.Count > SmaPeriod)
			_smaSum -= _smaQueue.Dequeue();

		if (!_rsi.IsFormed || _smaQueue.Count < SmaPeriod)
			return;

		var smaValue = _smaSum / _smaQueue.Count;

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// If no grid active, look for entry signals
		if (_gridDirection is null)
		{
			// Buy: RSI oversold + price below SMA (mean reversion)
			if (rsiValue < 35 && close < smaValue)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));

				BuyMarket(volume);
				_gridDirection = Sides.Buy;
				_gridLevel = 1;
				_lastEntryPrice = close;
			}
			// Sell: RSI overbought + price above SMA
			else if (rsiValue > 65 && close > smaValue)
			{
				if (Position > 0)
					SellMarket(Position);

				SellMarket(volume);
				_gridDirection = Sides.Sell;
				_gridLevel = 1;
				_lastEntryPrice = close;
			}
		}
		else
		{
			// Grid management - add levels on adverse moves
			var distanceThreshold = _lastEntryPrice * 0.005m; // 0.5% grid step

			if (_gridDirection == Sides.Buy)
			{
				// Price moved further down - add to grid
				if (_gridLevel < MaxGridLevels && close < _lastEntryPrice - distanceThreshold)
				{
					BuyMarket(volume);
					_gridLevel++;
					_lastEntryPrice = close;
				}
				// Take profit - price recovered above SMA
				else if (close > smaValue && rsiValue > 50)
				{
					if (Position > 0)
						SellMarket(Position);
					_gridDirection = null;
					_gridLevel = 0;
				}
			}
			else if (_gridDirection == Sides.Sell)
			{
				// Price moved further up - add to grid
				if (_gridLevel < MaxGridLevels && close > _lastEntryPrice + distanceThreshold)
				{
					SellMarket(volume);
					_gridLevel++;
					_lastEntryPrice = close;
				}
				// Take profit - price fell below SMA
				else if (close < smaValue && rsiValue < 50)
				{
					if (Position < 0)
						BuyMarket(Math.Abs(Position));
					_gridDirection = null;
					_gridLevel = 0;
				}
			}
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_rsi = null;
		_smaQueue.Clear();
		_smaSum = 0;
		_gridDirection = null;
		_gridLevel = 0;
		_lastEntryPrice = 0;

		base.OnReseted();
	}
}
