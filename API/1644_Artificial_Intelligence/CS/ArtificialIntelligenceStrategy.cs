using System;
using System.Collections.Generic;

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

	private readonly SimpleMovingAverage _aoSma = new() { Length = 5 };
	private readonly decimal[] _acBuffer = new decimal[22];
	private int _acCount;
	private decimal _entryPrice;
	private decimal _stopPrice;

	/// <summary>
	/// First perceptron input weight.
	/// </summary>
	public int X1
	{
		get => _x1.Value;
		set => _x1.Value = value;
	}

	/// <summary>
	/// Second perceptron input weight.
	/// </summary>
	public int X2
	{
		get => _x2.Value;
		set => _x2.Value = value;
	}

	/// <summary>
	/// Third perceptron input weight.
	/// </summary>
	public int X3
	{
		get => _x3.Value;
		set => _x3.Value = value;
	}

	/// <summary>
	/// Fourth perceptron input weight.
	/// </summary>
	public int X4
	{
		get => _x4.Value;
		set => _x4.Value = value;
	}

	/// <summary>
	/// Stop loss in price points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ArtificialIntelligenceStrategy"/>.
	/// </summary>
	public ArtificialIntelligenceStrategy()
	{
		_x1 = Param(nameof(X1), 135)
			.SetDisplay("X1", "Perceptron weight 1", "Perceptron")
			.SetCanOptimize(true)
			.SetOptimize(0, 200, 5);

		_x2 = Param(nameof(X2), 127)
			.SetDisplay("X2", "Perceptron weight 2", "Perceptron")
			.SetCanOptimize(true)
			.SetOptimize(0, 200, 5);

		_x3 = Param(nameof(X3), 16)
			.SetDisplay("X3", "Perceptron weight 3", "Perceptron")
			.SetCanOptimize(true)
			.SetOptimize(0, 200, 5);

		_x4 = Param(nameof(X4), 93)
			.SetDisplay("X4", "Perceptron weight 4", "Perceptron")
			.SetCanOptimize(true)
			.SetOptimize(0, 200, 5);

		_stopLoss = Param(nameof(StopLoss), 85m)
			.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 200m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		Volume = 1;
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
		_acCount = 0;
		_entryPrice = 0m;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ao = new AwesomeOscillator();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ao, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ao);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue aoValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!aoValue.IsFormed)
			return;

		var ao = aoValue.GetValue<decimal>();
		var aoSmaValue = _aoSma.Process(ao, candle.ServerTime, true);

		if (!aoSmaValue.IsFormed)
			return;

		var ac = ao - aoSmaValue.GetValue<decimal>();

		for (var i = _acBuffer.Length - 1; i > 0; i--)
			_acBuffer[i] = _acBuffer[i - 1];
		_acBuffer[0] = ac;
		if (_acCount < _acBuffer.Length)
			_acCount++;

		if (_acCount < _acBuffer.Length)
			return;

		var step = Security?.PriceStep ?? 0.01m;

		var w1 = X1 - 100m;
		var w2 = X2 - 100m;
		var w3 = X3 - 100m;
		var w4 = X4 - 100m;

		var perceptron = w1 * _acBuffer[0] + w2 * _acBuffer[7] + w3 * _acBuffer[14] + w4 * _acBuffer[21];

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

			if (candle.ClosePrice <= _stopPrice)
			{
				SellMarket(Position);
				return;
			}

			if (perceptron < 0)
			{
				SellMarket(Volume + Position);
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + StopLoss * step;
			}
		}
		else if (Position < 0)
		{
			_stopPrice = Math.Min(_stopPrice, candle.ClosePrice + StopLoss * step);

			if (candle.ClosePrice >= _stopPrice)
			{
				BuyMarket(-Position);
				return;
			}

			if (perceptron > 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - StopLoss * step;
			}
		}
	}
}
