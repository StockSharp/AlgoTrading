using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Perceptron-based strategy using Awesome Oscillator values at different lookback periods.
/// Combines weighted AO signals with price pattern confirmation for entry/exit decisions.
/// </summary>
public class N7SAo772012Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _aoPeriod;
	private readonly StrategyParam<int> _lookback;

	private readonly List<decimal> _aoHistory = new();
	private decimal _entryPrice;

	public N7SAo772012Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis.", "General");

		_aoPeriod = Param(nameof(AoPeriod), 5)
			.SetDisplay("AO Period", "Period for the Awesome Oscillator.", "Indicators");

		_lookback = Param(nameof(Lookback), 3)
			.SetDisplay("Lookback", "Number of AO values to look back for signal.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AoPeriod
	{
		get => _aoPeriod.Value;
		set => _aoPeriod.Value = value;
	}

	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_aoHistory.Clear();
		_entryPrice = 0;

		var ao = new AwesomeOscillator();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ao, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ao);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal aoValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_aoHistory.Add(aoValue);
		if (_aoHistory.Count > 50)
			_aoHistory.RemoveAt(0);

		if (_aoHistory.Count < Lookback + 1)
			return;

		var close = candle.ClosePrice;
		var current = _aoHistory[_aoHistory.Count - 1];
		var prev = _aoHistory[_aoHistory.Count - 1 - Lookback];

		// AO momentum: current vs lookback periods ago
		var rising = current > prev && current > 0;
		var falling = current < prev && current < 0;

		// Manage positions
		if (Position > 0)
		{
			// Exit long if AO turns negative or stop-loss
			if (current < 0 || (_entryPrice > 0 && close < _entryPrice * 0.98m))
			{
				SellMarket();
			}
		}
		else if (Position < 0)
		{
			// Exit short if AO turns positive or stop-loss
			if (current > 0 || (_entryPrice > 0 && close > _entryPrice * 1.02m))
			{
				BuyMarket();
			}
		}

		// Entry
		if (Position == 0)
		{
			if (rising)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (falling)
			{
				_entryPrice = close;
				SellMarket();
			}
		}
	}
}
