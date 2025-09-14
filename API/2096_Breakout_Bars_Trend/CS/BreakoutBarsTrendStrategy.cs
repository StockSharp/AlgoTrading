using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following breakout strategy based on Parabolic SAR reversals.
/// The system opens trades after a specified number of negative reversals
/// and uses distance in pips or percent for stop-loss and take-profit.
/// </summary>
public class BreakoutBarsTrendStrategy : Strategy
{
	public enum ReversalMode
	{
		Pips,
		Percent,
	}

	private enum TrendDirection
	{
		Down = -1,
		None = 0,
		Up = 1,
	}

	private readonly StrategyParam<ReversalMode> _reversalMode;
	private readonly StrategyParam<decimal> _delta;
	private readonly StrategyParam<int> _negatives;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _lastReversalPrice;
	private TrendDirection _lastTrend = TrendDirection.None;
	private int _negativeCounter;
	private bool _isLong;

	/// <summary>
	/// Calculation mode for distances.
	/// </summary>
	public ReversalMode Reversal
	{
		get => _reversalMode.Value;
		set => _reversalMode.Value = value;
	}

	/// <summary>
	/// Minimal movement between reversals.
	/// </summary>
	public decimal Delta
	{
		get => _delta.Value;
		set => _delta.Value = value;
	}

	/// <summary>
	/// Number of negative reversals before entry.
	/// </summary>
	public int Negatives
	{
		get => _negatives.Value;
		set => _negatives.Value = value;
	}

	/// <summary>
	/// Stop-loss distance.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take-profit distance.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public BreakoutBarsTrendStrategy()
	{
		_reversalMode = Param(nameof(Reversal), ReversalMode.Percent)
			.SetDisplay("Reversal Mode", "Distance calculation type", "General");

		_delta = Param(nameof(Delta), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Delta", "Minimal distance between reversals", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

		_negatives = Param(nameof(Negatives), 1)
			.SetGreaterOrEqual(0)
			.SetDisplay("Negative Signals", "Number of negative reversals before entry", "General")
			.SetCanOptimize(true)
			.SetOptimize(0, 5, 1);

		_stopLoss = Param(nameof(StopLoss), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop-loss distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

		_takeProfit = Param(nameof(TakeProfit), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take-profit distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 6m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for analysis", "General");
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
		_lastReversalPrice = 0m;
		_lastTrend = TrendDirection.None;
		_negativeCounter = 0;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var parabolic = new ParabolicSar();
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(parabolic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, parabolic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var trend = sarValue < candle.ClosePrice ? TrendDirection.Up : TrendDirection.Down;

		if (_lastTrend != trend)
		{
			if (_lastTrend != TrendDirection.None && _lastReversalPrice != 0m)
			{
				var move = _lastTrend == TrendDirection.Up ? candle.ClosePrice - _lastReversalPrice : _lastReversalPrice - candle.ClosePrice;
				if (move < 0)
					_negativeCounter++;
				else
					_negativeCounter = 0;
			}

			_lastReversalPrice = candle.ClosePrice;
			_lastTrend = trend;

			if ((trend == TrendDirection.Up && Position < 0) || (trend == TrendDirection.Down && Position > 0))
				ClosePosition();

			if (Negatives == 0 || _negativeCounter >= Negatives)
			{
				if (trend == TrendDirection.Up && Position <= 0)
				{
					_entryPrice = candle.ClosePrice;
					_isLong = true;
					BuyMarket(Volume + Math.Abs(Position));
				}
				else if (trend == TrendDirection.Down && Position >= 0)
				{
					_entryPrice = candle.ClosePrice;
					_isLong = false;
					SellMarket(Volume + Math.Abs(Position));
				}

				_negativeCounter = 0;
			}
		}

		if (Position != 0 && _entryPrice != 0m)
			CheckRisk(candle.ClosePrice);
	}

	private decimal GetDistance(decimal price, decimal value)
	{
		return Reversal == ReversalMode.Pips ? value * Security.PriceStep : price * value / 100m;
	}

	private void CheckRisk(decimal price)
	{
		var sl = GetDistance(_entryPrice, StopLoss);
		var tp = GetDistance(_entryPrice, TakeProfit);

		if (_isLong)
		{
			if (price <= _entryPrice - sl || price >= _entryPrice + tp)
				ClosePosition();
		}
		else
		{
			if (price >= _entryPrice + sl || price <= _entryPrice - tp)
				ClosePosition();
		}
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Math.Abs(Position));
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));

		_entryPrice = 0m;
		_isLong = false;
	}
}
