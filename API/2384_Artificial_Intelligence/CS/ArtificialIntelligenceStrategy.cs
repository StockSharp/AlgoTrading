using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Perceptron-based strategy using Acceleration/Deceleration values.
/// </summary>
public class ArtificialIntelligenceStrategy : Strategy
{
	private readonly StrategyParam<int> _x1;
	private readonly StrategyParam<int> _x2;
	private readonly StrategyParam<int> _x3;
	private readonly StrategyParam<int> _x4;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private readonly SimpleMovingAverage _aoFast = new() { Length = 5 };
	private readonly SimpleMovingAverage _aoSlow = new() { Length = 34 };
	private readonly SimpleMovingAverage _acMa = new() { Length = 5 };

	private readonly decimal[] _acBuffer = new decimal[22];
	private int _bufferCount;
	private decimal _entryPrice;

	/// <summary>
	/// First weight of perceptron.
	/// </summary>
	public int X1 { get => _x1.Value; set => _x1.Value = value; }

	/// <summary>
	/// Second weight of perceptron.
	/// </summary>
	public int X2 { get => _x2.Value; set => _x2.Value = value; }

	/// <summary>
	/// Third weight of perceptron.
	/// </summary>
	public int X3 { get => _x3.Value; set => _x3.Value = value; }

	/// <summary>
	/// Fourth weight of perceptron.
	/// </summary>
	public int X4 { get => _x4.Value; set => _x4.Value = value; }

	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Candle series type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="ArtificialIntelligenceStrategy"/>.
	/// </summary>
	public ArtificialIntelligenceStrategy()
	{
		_x1 = Param(nameof(X1), 76)
			.SetDisplay("X1", "Perceptron weight 1", "Parameters");
		_x2 = Param(nameof(X2), 47)
			.SetDisplay("X2", "Perceptron weight 2", "Parameters");
		_x3 = Param(nameof(X3), 153)
			.SetDisplay("X3", "Perceptron weight 3", "Parameters");
		_x4 = Param(nameof(X4), 135)
			.SetDisplay("X4", "Perceptron weight 4", "Parameters");

		_stopLoss = Param(nameof(StopLoss), 8355m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		Array.Clear(_acBuffer);
		_bufferCount = 0;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Calculate Accelerator Oscillator value.
		var hl2 = (candle.HighPrice + candle.LowPrice) / 2m;

		var aoFast = _aoFast.Process(hl2);
		var aoSlow = _aoSlow.Process(hl2);
		if (!aoFast.IsFinal || !aoSlow.IsFinal)
			return;

		var ao = aoFast.GetValue<decimal>() - aoSlow.GetValue<decimal>();

		var acMa = _acMa.Process(ao);
		if (!acMa.IsFinal)
			return;

		var ac = ao - acMa.GetValue<decimal>();

		for (var i = 21; i > 0; i--)
			_acBuffer[i] = _acBuffer[i - 1];
		_acBuffer[0] = ac;
		if (_bufferCount < 22)
		{
			_bufferCount++;
			return;
		}

		var signal = Perceptron();

		if (Position > 0)
		{
			if (candle.ClosePrice <= _entryPrice - StopLoss)
				SellMarket(Position);
			else if (signal < 0)
			{
				SellMarket(Position * 2);
				_entryPrice = candle.ClosePrice;
			}
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice >= _entryPrice + StopLoss)
				BuyMarket(-Position);
			else if (signal > 0)
			{
				BuyMarket(-Position * 2);
				_entryPrice = candle.ClosePrice;
			}
		}
		else
		{
			if (signal > 0)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (signal < 0)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}
	}

	private decimal Perceptron()
	{
		var w1 = X1 - 100m;
		var w2 = X2 - 100m;
		var w3 = X3 - 100m;
		var w4 = X4 - 100m;

		return w1 * _acBuffer[0] + w2 * _acBuffer[7] + w3 * _acBuffer[14] + w4 * _acBuffer[21];
	}
}
