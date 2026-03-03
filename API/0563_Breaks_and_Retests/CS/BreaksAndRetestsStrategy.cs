using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout and retest strategy with trailing stop.
/// </summary>
public class BreaksAndRetestsStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _profitThresholdPercent;
	private readonly StrategyParam<decimal> _trailingStopGapPercent;
	private readonly StrategyParam<int> _maxHoldBars;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private decimal _prevHighest;
	private decimal _prevLowest;
	private decimal _entryPrice;
	private bool _trailingStopActive;
	private decimal _highestSinceTrailing;
	private decimal _lowestSinceTrailing;
	private int _barsInPosition;
	private int _barsSinceExit;

	public int LookbackPeriod { get => _lookbackPeriod.Value; set => _lookbackPeriod.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal ProfitThresholdPercent { get => _profitThresholdPercent.Value; set => _profitThresholdPercent.Value = value; }
	public decimal TrailingStopGapPercent { get => _trailingStopGapPercent.Value; set => _trailingStopGapPercent.Value = value; }
	public int MaxHoldBars { get => _maxHoldBars.Value; set => _maxHoldBars.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BreaksAndRetestsStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Number of bars for support/resistance", "Levels");

		_stopLossPercent = Param(nameof(StopLossPercent), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Initial stop loss", "Risk");

		_profitThresholdPercent = Param(nameof(ProfitThresholdPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Threshold %", "Activate trailing after profit", "Risk");

		_trailingStopGapPercent = Param(nameof(TrailingStopGapPercent), 0.8m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Gap %", "Gap for trailing stop", "Risk");

		_maxHoldBars = Param(nameof(MaxHoldBars), 25)
			.SetGreaterThanZero()
			.SetDisplay("Max Hold Bars", "Max bars to hold position", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 3)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Bars to wait after exit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candles for calculations", "General");
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
		_highs.Clear();
		_lows.Clear();
		_prevHighest = 0m;
		_prevLowest = 0m;
		_entryPrice = 0m;
		_trailingStopActive = false;
		_highestSinceTrailing = 0m;
		_lowestSinceTrailing = 0m;
		_barsInPosition = 0;
		_barsSinceExit = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket();
		else if (Position < 0)
			BuyMarket();

		_entryPrice = 0m;
		_trailingStopActive = false;
		_barsInPosition = 0;
		_barsSinceExit = 0;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		if (_highs.Count > LookbackPeriod + 1)
			_highs.RemoveAt(0);
		if (_lows.Count > LookbackPeriod + 1)
			_lows.RemoveAt(0);

		if (_highs.Count <= LookbackPeriod)
			return;

		// Compute highest/lowest from previous N candles (excluding current)
		var highest = decimal.MinValue;
		var lowest = decimal.MaxValue;
		for (var i = 0; i < _highs.Count - 1; i++)
		{
			if (_highs[i] > highest) highest = _highs[i];
			if (_lows[i] < lowest) lowest = _lows[i];
		}

		if (Position != 0)
		{
			_barsInPosition++;

			// Handle stops
			HandleStop(candle);

			// Max hold exit
			if (Position != 0 && _barsInPosition >= MaxHoldBars)
				ClosePosition();
		}
		else
		{
			_barsSinceExit++;

			// Breakout detection with cooldown
			if (_barsSinceExit >= CooldownBars && _prevHighest > 0 && _prevLowest > 0)
			{
				if (candle.ClosePrice > _prevHighest)
				{
					BuyMarket();
					_entryPrice = candle.ClosePrice;
					_trailingStopActive = false;
					_barsInPosition = 0;
				}
				else if (candle.ClosePrice < _prevLowest)
				{
					SellMarket();
					_entryPrice = candle.ClosePrice;
					_trailingStopActive = false;
					_barsInPosition = 0;
				}
			}
		}

		_prevHighest = highest;
		_prevLowest = lowest;
	}

	private void HandleStop(ICandleMessage candle)
	{
		if (Position > 0 && _entryPrice > 0)
		{
			var profitPercent = (candle.ClosePrice - _entryPrice) / _entryPrice * 100m;
			if (!_trailingStopActive && profitPercent >= ProfitThresholdPercent)
			{
				_trailingStopActive = true;
				_highestSinceTrailing = candle.ClosePrice;
			}

			if (_trailingStopActive)
			{
				_highestSinceTrailing = Math.Max(_highestSinceTrailing, candle.ClosePrice);
				var stop = _highestSinceTrailing * (1 - TrailingStopGapPercent / 100m);
				if (candle.ClosePrice <= stop)
					ClosePosition();
			}
			else
			{
				var stop = _entryPrice * (1 - StopLossPercent / 100m);
				if (candle.ClosePrice <= stop)
					ClosePosition();
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			var profitPercent = (_entryPrice - candle.ClosePrice) / _entryPrice * 100m;
			if (!_trailingStopActive && profitPercent >= ProfitThresholdPercent)
			{
				_trailingStopActive = true;
				_lowestSinceTrailing = candle.ClosePrice;
			}

			if (_trailingStopActive)
			{
				_lowestSinceTrailing = Math.Min(_lowestSinceTrailing, candle.ClosePrice);
				var stop = _lowestSinceTrailing * (1 + TrailingStopGapPercent / 100m);
				if (candle.ClosePrice >= stop)
					ClosePosition();
			}
			else
			{
				var stop = _entryPrice * (1 + StopLossPercent / 100m);
				if (candle.ClosePrice >= stop)
					ClosePosition();
			}
		}
	}
}
