using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD-based martingale strategy. Uses two MACD indicators to detect turning points.
/// Doubles volume after a loss up to MaxDoublingCount times.
/// </summary>
public class ProMartMacdMartingaleStrategy : Strategy
{
	private readonly StrategyParam<int> _maxDoublingCount;
	private readonly StrategyParam<int> _macd1Fast;
	private readonly StrategyParam<int> _macd1Slow;
	private readonly StrategyParam<int> _macd2Fast;
	private readonly StrategyParam<int> _macd2Slow;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _macd1History = new();
	private readonly List<decimal> _macd2History = new();

	private decimal _entryPrice;
	private bool _inPosition;
	private bool _isLong;
	private bool _lastTradeWasLoss;
	private int _martingaleCounter;
	private decimal _currentVolume;

	public int MaxDoublingCount
	{
		get => _maxDoublingCount.Value;
		set => _maxDoublingCount.Value = value;
	}

	public int Macd1Fast
	{
		get => _macd1Fast.Value;
		set => _macd1Fast.Value = value;
	}

	public int Macd1Slow
	{
		get => _macd1Slow.Value;
		set => _macd1Slow.Value = value;
	}

	public int Macd2Fast
	{
		get => _macd2Fast.Value;
		set => _macd2Fast.Value = value;
	}

	public int Macd2Slow
	{
		get => _macd2Slow.Value;
		set => _macd2Slow.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ProMartMacdMartingaleStrategy()
	{
		_maxDoublingCount = Param(nameof(MaxDoublingCount), 2)
			.SetNotNegative()
			.SetDisplay("Max Doubling", "Maximum number of volume doublings after losses.", "Risk");

		_macd1Fast = Param(nameof(Macd1Fast), 5)
			.SetGreaterThanZero()
			.SetDisplay("MACD1 Fast", "Fast EMA period for the primary MACD.", "Signal");

		_macd1Slow = Param(nameof(Macd1Slow), 20)
			.SetGreaterThanZero()
			.SetDisplay("MACD1 Slow", "Slow EMA period for the primary MACD.", "Signal");

		_macd2Fast = Param(nameof(Macd2Fast), 10)
			.SetGreaterThanZero()
			.SetDisplay("MACD2 Fast", "Fast EMA period for the secondary MACD.", "Filter");

		_macd2Slow = Param(nameof(Macd2Slow), 15)
			.SetGreaterThanZero()
			.SetDisplay("MACD2 Slow", "Slow EMA period for the secondary MACD.", "Filter");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Data type used for signal generation.", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_macd1History.Clear();
		_macd2History.Clear();
		_inPosition = false;
		_isLong = false;
		_lastTradeWasLoss = false;
		_martingaleCounter = 0;
		_currentVolume = Volume;
		_entryPrice = 0;

		var macd1 = new MovingAverageConvergenceDivergence(
			new ExponentialMovingAverage { Length = Macd1Slow },
			new ExponentialMovingAverage { Length = Macd1Fast });

		var macd2 = new MovingAverageConvergenceDivergence(
			new ExponentialMovingAverage { Length = Macd2Slow },
			new ExponentialMovingAverage { Length = Macd2Fast });

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(macd1, macd2, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd1);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macd1Value, decimal macd2Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_macd1History.Add(macd1Value);
		_macd2History.Add(macd2Value);

		if (_macd1History.Count > 4)
			_macd1History.RemoveAt(0);
		if (_macd2History.Count > 3)
			_macd2History.RemoveAt(0);

		// Check exit for open position
		if (_inPosition)
		{
			var pnl = _isLong
				? candle.ClosePrice - _entryPrice
				: _entryPrice - candle.ClosePrice;

			// Detect reversal to exit
			var shouldExit = false;
			if (_macd1History.Count >= 3)
			{
				var m0 = _macd1History[^1];
				var m1 = _macd1History[^2];
				var m2 = _macd1History[^3];

				if (_isLong && m0 < m1 && m1 > m2)
					shouldExit = true;
				else if (!_isLong && m0 > m1 && m1 < m2)
					shouldExit = true;
			}

			if (shouldExit)
			{
				if (_isLong)
					SellMarket();
				else
					BuyMarket();

				_lastTradeWasLoss = pnl < 0;
				if (_lastTradeWasLoss && _martingaleCounter < MaxDoublingCount)
				{
					_currentVolume *= 2;
					_martingaleCounter++;
				}
				else
				{
					_currentVolume = Volume;
					_martingaleCounter = 0;
				}

				_inPosition = false;
				return;
			}
		}

		// Check entry
		if (!_inPosition && _macd1History.Count >= 3 && _macd2History.Count >= 2)
		{
			var m0 = _macd1History[^1];
			var m1 = _macd1History[^2];
			var m2 = _macd1History[^3];
			var f0 = _macd2History[^1];
			var f1 = _macd2History[^2];

			// MACD1 turns up from bottom + MACD2 confirms
			var buySignal = m0 > m1 && m1 < m2 && f1 > f0;
			var sellSignal = m0 < m1 && m1 > m2 && f1 < f0;

			if (buySignal && Position <= 0)
			{
				BuyMarket();
				_inPosition = true;
				_isLong = true;
				_entryPrice = candle.ClosePrice;
			}
			else if (sellSignal && Position >= 0)
			{
				SellMarket();
				_inPosition = true;
				_isLong = false;
				_entryPrice = candle.ClosePrice;
			}
		}
	}
}
