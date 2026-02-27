using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Artificial Intelligence strategy based on a simple perceptron over Accelerator Oscillator values.
/// </summary>
public class ArtificialIntelligenceStrategy : Strategy
{
	private readonly StrategyParam<int> _x1;
	private readonly StrategyParam<int> _x2;
	private readonly StrategyParam<int> _x3;
	private readonly StrategyParam<int> _x4;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _aoBuffer = new decimal[22];
	private int _aoCount;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _prevAo;

	public int X1 { get => _x1.Value; set => _x1.Value = value; }
	public int X2 { get => _x2.Value; set => _x2.Value = value; }
	public int X3 { get => _x3.Value; set => _x3.Value = value; }
	public int X4 { get => _x4.Value; set => _x4.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ArtificialIntelligenceStrategy()
	{
		_x1 = Param(nameof(X1), 135)
			.SetDisplay("X1", "Perceptron weight 1", "Perceptron")
			.SetOptimize(0, 200, 5);

		_x2 = Param(nameof(X2), 127)
			.SetDisplay("X2", "Perceptron weight 2", "Perceptron")
			.SetOptimize(0, 200, 5);

		_x3 = Param(nameof(X3), 16)
			.SetDisplay("X3", "Perceptron weight 3", "Perceptron")
			.SetOptimize(0, 200, 5);

		_x4 = Param(nameof(X4), 93)
			.SetDisplay("X4", "Perceptron weight 4", "Perceptron")
			.SetOptimize(0, 200, 5);

		_stopLoss = Param(nameof(StopLoss), 85m)
			.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk")
			.SetOptimize(10m, 200m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		Array.Clear(_aoBuffer);
		_aoCount = 0;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_prevAo = 0m;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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

		// Use AO values directly as the perceptron inputs
		for (var i = _aoBuffer.Length - 1; i > 0; i--)
			_aoBuffer[i] = _aoBuffer[i - 1];
		_aoBuffer[0] = aoValue;
		if (_aoCount < _aoBuffer.Length)
			_aoCount++;

		if (_aoCount < _aoBuffer.Length)
			return;

		var step = Security?.PriceStep ?? 0.01m;

		var w1 = X1 - 100m;
		var w2 = X2 - 100m;
		var w3 = X3 - 100m;
		var w4 = X4 - 100m;

		var perceptron = w1 * _aoBuffer[0] + w2 * _aoBuffer[7] + w3 * _aoBuffer[14] + w4 * _aoBuffer[21];

		if (Position == 0)
		{
			if (perceptron > 0)
			{
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - StopLoss * step;
				BuyMarket();
			}
			else
			{
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + StopLoss * step;
				SellMarket();
			}
			return;
		}

		if (Position > 0)
		{
			_stopPrice = Math.Max(_stopPrice, candle.ClosePrice - StopLoss * step);

			if (candle.ClosePrice <= _stopPrice || perceptron < 0)
			{
				SellMarket();
				if (perceptron < 0)
				{
					_entryPrice = candle.ClosePrice;
					_stopPrice = _entryPrice + StopLoss * step;
				}
			}
		}
		else if (Position < 0)
		{
			_stopPrice = Math.Min(_stopPrice, candle.ClosePrice + StopLoss * step);

			if (candle.ClosePrice >= _stopPrice || perceptron > 0)
			{
				BuyMarket();
				if (perceptron > 0)
				{
					_entryPrice = candle.ClosePrice;
					_stopPrice = _entryPrice - StopLoss * step;
				}
			}
		}

		_prevAo = aoValue;
	}
}
