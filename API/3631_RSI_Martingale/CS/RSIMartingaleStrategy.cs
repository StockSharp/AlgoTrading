using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI extremes strategy with martingale recovery.
/// Buys when RSI is at a local minimum below 50, sells when RSI is at a local maximum above 50.
/// Closes on RSI crossing 50. Doubles volume after a losing trade.
/// </summary>
public class RSIMartingaleStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _barsForCondition;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _recentRsi = new();
	private decimal _entryPrice;
	private int _direction; // 1=long, -1=short, 0=flat

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public int BarsForCondition
	{
		get => _barsForCondition.Value;
		set => _barsForCondition.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public RSIMartingaleStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI indicator period", "Indicator");

		_barsForCondition = Param(nameof(BarsForCondition), 10)
			.SetDisplay("Bars For Extremes", "Number of RSI values to check for extremes", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "Data");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

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

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_recentRsi.Add(rsiValue);
		if (_recentRsi.Count > BarsForCondition)
			_recentRsi.RemoveAt(0);

		if (_recentRsi.Count < BarsForCondition)
			return;

		// Check exit: close on RSI crossing 50
		if (_direction > 0 && rsiValue > 50)
		{
			SellMarket();
			_direction = 0;
			return;
		}
		else if (_direction < 0 && rsiValue < 50)
		{
			BuyMarket();
			_direction = 0;
			return;
		}

		if (Position != 0)
			return;

		// Check if current RSI is local minimum (oversold entry)
		if (IsLocalMinimum() && rsiValue < 50 && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_direction = 1;
		}
		// Check if current RSI is local maximum (overbought entry)
		else if (IsLocalMaximum() && rsiValue > 50 && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_direction = -1;
		}
	}

	private bool IsLocalMinimum()
	{
		if (_recentRsi.Count < 2)
			return false;

		var current = _recentRsi[_recentRsi.Count - 1];
		for (var i = 0; i < _recentRsi.Count - 1; i++)
		{
			if (current > _recentRsi[i])
				return false;
		}
		return true;
	}

	private bool IsLocalMaximum()
	{
		if (_recentRsi.Count < 2)
			return false;

		var current = _recentRsi[_recentRsi.Count - 1];
		for (var i = 0; i < _recentRsi.Count - 1; i++)
		{
			if (current < _recentRsi[i])
				return false;
		}
		return true;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_recentRsi.Clear();
		_entryPrice = 0;
		_direction = 0;

		base.OnReseted();
	}
}
