using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Big Candle Identifier with RSI divergence and trailing stops.
/// Enters when the current candle body is the largest of the last N candles.
/// Uses RSI fast/slow divergence as confirmation.
/// </summary>
public class BigCandleRsiDivergenceStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _trailStartPercent;
	private readonly StrategyParam<decimal> _trailDistancePercent;
	private readonly StrategyParam<int> _lookbackBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _bodies = new();
	private decimal _entryPrice;
	private decimal _highestSinceEntry;
	private decimal _lowestSinceEntry;
	private bool _trailingActive;

	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TrailStartPercent { get => _trailStartPercent.Value; set => _trailStartPercent.Value = value; }
	public decimal TrailDistancePercent { get => _trailDistancePercent.Value; set => _trailDistancePercent.Value = value; }
	public int LookbackBars { get => _lookbackBars.Value; set => _lookbackBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BigCandleRsiDivergenceStrategy()
	{
		_stopLossPercent = Param(nameof(StopLossPercent), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Initial stop loss percent", "Risk");

		_trailStartPercent = Param(nameof(TrailStartPercent), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Start %", "Profit percent to activate trailing", "Risk");

		_trailDistancePercent = Param(nameof(TrailDistancePercent), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Distance %", "Trailing stop distance percent", "Risk");

		_lookbackBars = Param(nameof(LookbackBars), 3)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Bars", "Number of bars for big candle comparison", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_bodies.Clear();
		_entryPrice = 0m;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
		_trailingActive = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsiFast = new RelativeStrengthIndex { Length = 5 };
		var rsiSlow = new RelativeStrengthIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsiFast, rsiSlow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiFast, decimal rsiSlow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);

		_bodies.Add(body);
		if (_bodies.Count > LookbackBars + 1)
			_bodies.RemoveAt(0);

		if (_bodies.Count <= LookbackBars)
			return;

		// Check if current body is the largest in lookback window
		var isBiggest = true;
		for (var i = 0; i < _bodies.Count - 1; i++)
		{
			if (_bodies[i] >= body)
			{
				isBiggest = false;
				break;
			}
		}

		var isBullish = candle.ClosePrice > candle.OpenPrice;
		var isBearish = candle.ClosePrice < candle.OpenPrice;
		var rsiDivergence = rsiFast - rsiSlow;

		if (Position == 0)
		{
			if (isBiggest && isBullish && rsiDivergence > 0)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_highestSinceEntry = candle.ClosePrice;
				_trailingActive = false;
			}
			else if (isBiggest && isBearish && rsiDivergence < 0)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_lowestSinceEntry = candle.ClosePrice;
				_trailingActive = false;
			}
		}
		else if (Position > 0 && _entryPrice > 0)
		{
			_highestSinceEntry = Math.Max(_highestSinceEntry, candle.ClosePrice);

			var profitPercent = (candle.ClosePrice - _entryPrice) / _entryPrice * 100m;

			if (!_trailingActive && profitPercent >= TrailStartPercent)
				_trailingActive = true;

			if (_trailingActive)
			{
				var stop = _highestSinceEntry * (1 - TrailDistancePercent / 100m);
				if (candle.ClosePrice <= stop)
				{
					SellMarket();
					_entryPrice = 0m;
					_trailingActive = false;
				}
			}
			else
			{
				var stop = _entryPrice * (1 - StopLossPercent / 100m);
				if (candle.ClosePrice <= stop)
				{
					SellMarket();
					_entryPrice = 0m;
				}
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, candle.ClosePrice);

			var profitPercent = (_entryPrice - candle.ClosePrice) / _entryPrice * 100m;

			if (!_trailingActive && profitPercent >= TrailStartPercent)
				_trailingActive = true;

			if (_trailingActive)
			{
				var stop = _lowestSinceEntry * (1 + TrailDistancePercent / 100m);
				if (candle.ClosePrice >= stop)
				{
					BuyMarket();
					_entryPrice = 0m;
					_trailingActive = false;
				}
			}
			else
			{
				var stop = _entryPrice * (1 + StopLossPercent / 100m);
				if (candle.ClosePrice >= stop)
				{
					BuyMarket();
					_entryPrice = 0m;
				}
			}
		}
	}
}
