using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD combined with simple neural network.
/// Perceptron-based filter validates MACD signals.
/// </summary>
public class NeuralNetworkMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _x11;
	private readonly StrategyParam<int> _x12;
	private readonly StrategyParam<int> _x13;
	private readonly StrategyParam<int> _x14;
	private readonly StrategyParam<decimal> _tp1;
	private readonly StrategyParam<decimal> _sl1;
	private readonly StrategyParam<int> _p1;

	private readonly StrategyParam<int> _x21;
	private readonly StrategyParam<int> _x22;
	private readonly StrategyParam<int> _x23;
	private readonly StrategyParam<int> _x24;
	private readonly StrategyParam<decimal> _tp2;
	private readonly StrategyParam<decimal> _sl2;
	private readonly StrategyParam<int> _p2;

	private readonly StrategyParam<int> _x31;
	private readonly StrategyParam<int> _x32;
	private readonly StrategyParam<int> _x33;
	private readonly StrategyParam<int> _x34;
	private readonly StrategyParam<int> _p3;

	private readonly StrategyParam<int> _pass;
	private readonly StrategyParam<DataType> _candleType;

	private bool _macdInitialized;
	private decimal _prevMacd;
	private decimal _prevSignal;

	private decimal[] _openHistory = Array.Empty<decimal>();
	private int _historyIndex;
	private bool _historyFilled;

	private decimal _currentClose;
	private decimal _currentStopLoss;
	private decimal _currentTakeProfit;

	private decimal _entryPrice;
	private bool _isLong;

	/// <summary>
	/// Weight 1 for perceptron 1.
	/// </summary>
	public int X11 { get => _x11.Value; set => _x11.Value = value; }

	/// <summary>
	/// Weight 2 for perceptron 1.
	/// </summary>
	public int X12 { get => _x12.Value; set => _x12.Value = value; }

	/// <summary>
	/// Weight 3 for perceptron 1.
	/// </summary>
	public int X13 { get => _x13.Value; set => _x13.Value = value; }

	/// <summary>
	/// Weight 4 for perceptron 1.
	/// </summary>
	public int X14 { get => _x14.Value; set => _x14.Value = value; }

	/// <summary>
	/// Take profit for perceptron 1.
	/// </summary>
	public decimal Tp1 { get => _tp1.Value; set => _tp1.Value = value; }

	/// <summary>
	/// Stop loss for perceptron 1.
	/// </summary>
	public decimal Sl1 { get => _sl1.Value; set => _sl1.Value = value; }

	/// <summary>
	/// Shift parameter for perceptron 1.
	/// </summary>
	public int P1 { get => _p1.Value; set => _p1.Value = value; }

	/// <summary>
	/// Weight 1 for perceptron 2.
	/// </summary>
	public int X21 { get => _x21.Value; set => _x21.Value = value; }

	/// <summary>
	/// Weight 2 for perceptron 2.
	/// </summary>
	public int X22 { get => _x22.Value; set => _x22.Value = value; }

	/// <summary>
	/// Weight 3 for perceptron 2.
	/// </summary>
	public int X23 { get => _x23.Value; set => _x23.Value = value; }

	/// <summary>
	/// Weight 4 for perceptron 2.
	/// </summary>
	public int X24 { get => _x24.Value; set => _x24.Value = value; }

	/// <summary>
	/// Take profit for perceptron 2.
	/// </summary>
	public decimal Tp2 { get => _tp2.Value; set => _tp2.Value = value; }

	/// <summary>
	/// Stop loss for perceptron 2.
	/// </summary>
	public decimal Sl2 { get => _sl2.Value; set => _sl2.Value = value; }

	/// <summary>
	/// Shift parameter for perceptron 2.
	/// </summary>
	public int P2 { get => _p2.Value; set => _p2.Value = value; }

	/// <summary>
	/// Weight 1 for perceptron 3.
	/// </summary>
	public int X31 { get => _x31.Value; set => _x31.Value = value; }

	/// <summary>
	/// Weight 2 for perceptron 3.
	/// </summary>
	public int X32 { get => _x32.Value; set => _x32.Value = value; }

	/// <summary>
	/// Weight 3 for perceptron 3.
	/// </summary>
	public int X33 { get => _x33.Value; set => _x33.Value = value; }

	/// <summary>
	/// Weight 4 for perceptron 3.
	/// </summary>
	public int X34 { get => _x34.Value; set => _x34.Value = value; }

	/// <summary>
	/// Shift parameter for perceptron 3.
	/// </summary>
	public int P3 { get => _p3.Value; set => _p3.Value = value; }

	/// <summary>
	/// Number of perceptrons to use.
	/// </summary>
	public int Pass { get => _pass.Value; set => _pass.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="NeuralNetworkMacdStrategy"/>.
	/// </summary>
	public NeuralNetworkMacdStrategy()
	{
		_x11 = Param(nameof(X11), 100).SetDisplay("X11", "Weight 1 for perceptron 1", "Perceptron1");
		_x12 = Param(nameof(X12), 100).SetDisplay("X12", "Weight 2 for perceptron 1", "Perceptron1");
		_x13 = Param(nameof(X13), 100).SetDisplay("X13", "Weight 3 for perceptron 1", "Perceptron1");
		_x14 = Param(nameof(X14), 100).SetDisplay("X14", "Weight 4 for perceptron 1", "Perceptron1");
		_tp1 = Param(nameof(Tp1), 100m).SetDisplay("Take Profit 1", "Take profit for perceptron 1", "Perceptron1");
		_sl1 = Param(nameof(Sl1), 50m).SetDisplay("Stop Loss 1", "Stop loss for perceptron 1", "Perceptron1");
		_p1 = Param(nameof(P1), 10).SetDisplay("P1", "Shift parameter for perceptron 1", "Perceptron1");

		_x21 = Param(nameof(X21), 100).SetDisplay("X21", "Weight 1 for perceptron 2", "Perceptron2");
		_x22 = Param(nameof(X22), 100).SetDisplay("X22", "Weight 2 for perceptron 2", "Perceptron2");
		_x23 = Param(nameof(X23), 100).SetDisplay("X23", "Weight 3 for perceptron 2", "Perceptron2");
		_x24 = Param(nameof(X24), 100).SetDisplay("X24", "Weight 4 for perceptron 2", "Perceptron2");
		_tp2 = Param(nameof(Tp2), 100m).SetDisplay("Take Profit 2", "Take profit for perceptron 2", "Perceptron2");
		_sl2 = Param(nameof(Sl2), 50m).SetDisplay("Stop Loss 2", "Stop loss for perceptron 2", "Perceptron2");
		_p2 = Param(nameof(P2), 10).SetDisplay("P2", "Shift parameter for perceptron 2", "Perceptron2");

		_x31 = Param(nameof(X31), 100).SetDisplay("X31", "Weight 1 for perceptron 3", "Perceptron3");
		_x32 = Param(nameof(X32), 100).SetDisplay("X32", "Weight 2 for perceptron 3", "Perceptron3");
		_x33 = Param(nameof(X33), 100).SetDisplay("X33", "Weight 3 for perceptron 3", "Perceptron3");
		_x34 = Param(nameof(X34), 100).SetDisplay("X34", "Weight 4 for perceptron 3", "Perceptron3");
		_p3 = Param(nameof(P3), 10).SetDisplay("P3", "Shift parameter for perceptron 3", "Perceptron3");

		_pass = Param(nameof(Pass), 3).SetDisplay("Pass", "Number of perceptrons to use", "General");
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

		_macdInitialized = false;
		_prevMacd = 0m;
		_prevSignal = 0m;

		_openHistory = Array.Empty<decimal>();
		_historyIndex = 0;
		_historyFilled = false;

		_entryPrice = 0m;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = 12,
			LongPeriod = 26,
			SignalPeriod = 9
		};

		var maxLag = Math.Max(Math.Max(P1, P2), P3) * 4 + 1;
		_openHistory = new decimal[maxLag];
		_historyIndex = 0;
		_historyFilled = false;

		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(macd, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdValue, decimal signalValue, decimal histogram)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_currentClose = candle.ClosePrice;

		var macdDir = EvaluateMacd(macdValue, signalValue);
		var percDir = Supervisor();

		if (macdDir > 0 && percDir > 0 && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_isLong = true;
		}
		else if (macdDir < 0 && percDir < 0 && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_isLong = false;
		}

		if (Position > 0 && _isLong)
		{
			if (_currentTakeProfit > 0 && candle.ClosePrice >= _entryPrice + _currentTakeProfit)
				SellMarket(Math.Abs(Position));
			else if (_currentStopLoss > 0 && candle.ClosePrice <= _entryPrice - _currentStopLoss)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0 && !_isLong)
		{
			if (_currentTakeProfit > 0 && candle.ClosePrice <= _entryPrice - _currentTakeProfit)
				BuyMarket(Math.Abs(Position));
			else if (_currentStopLoss > 0 && candle.ClosePrice >= _entryPrice + _currentStopLoss)
				BuyMarket(Math.Abs(Position));
		}

		AddOpen(candle.OpenPrice);
	}

	private int EvaluateMacd(decimal macd, decimal signal)
	{
		if (!_macdInitialized)
		{
			_prevMacd = macd;
			_prevSignal = signal;
			_macdInitialized = true;
			return 0;
		}

		var result = 0;

		if (macd < 0 && macd >= signal && _prevMacd <= _prevSignal)
			result = 1;
		else if (macd > 0 && macd <= signal && _prevMacd >= _prevSignal)
			result = -1;

		_prevMacd = macd;
		_prevSignal = signal;

		return result;
	}

	private void AddOpen(decimal open)
	{
		if (_openHistory.Length == 0)
			return;

		_openHistory[_historyIndex] = open;
		_historyIndex++;

		if (_historyIndex >= _openHistory.Length)
		{
			_historyIndex = 0;
			_historyFilled = true;
		}
	}

	private bool TryGetOpen(int shift, out decimal price)
	{
		if (_openHistory.Length == 0)
		{
			price = 0m;
			return false;
		}

		if (!_historyFilled && shift >= _historyIndex)
		{
			price = 0m;
			return false;
		}

		var index = _historyIndex - 1 - shift;
		if (index < 0)
			index += _openHistory.Length;

		price = _openHistory[index];
		return true;
	}

	private decimal Perceptron1()
	{
		if (!TryGetOpen(P1, out var o1) ||
			!TryGetOpen(P1 * 2, out var o2) ||
			!TryGetOpen(P1 * 3, out var o3) ||
			!TryGetOpen(P1 * 4, out var o4))
			return 0m;

		var w1 = X11 - 100;
		var w2 = X12 - 100;
		var w3 = X13 - 100;
		var w4 = X14 - 100;

		var a1 = _currentClose - o1;
		var a2 = o1 - o2;
		var a3 = o2 - o3;
		var a4 = o3 - o4;

		return w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4;
	}

	private decimal Perceptron2()
	{
		if (!TryGetOpen(P2, out var o1) ||
			!TryGetOpen(P2 * 2, out var o2) ||
			!TryGetOpen(P2 * 3, out var o3) ||
			!TryGetOpen(P2 * 4, out var o4))
			return 0m;

		var w1 = X21 - 100;
		var w2 = X22 - 100;
		var w3 = X23 - 100;
		var w4 = X24 - 100;

		var a1 = _currentClose - o1;
		var a2 = o1 - o2;
		var a3 = o2 - o3;
		var a4 = o3 - o4;

		return w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4;
	}

	private decimal Perceptron3()
	{
		if (!TryGetOpen(P3, out var o1) ||
			!TryGetOpen(P3 * 2, out var o2) ||
			!TryGetOpen(P3 * 3, out var o3) ||
			!TryGetOpen(P3 * 4, out var o4))
			return 0m;

		var w1 = X31 - 100;
		var w2 = X32 - 100;
		var w3 = X33 - 100;
		var w4 = X34 - 100;

		var a1 = _currentClose - o1;
		var a2 = o1 - o2;
		var a3 = o2 - o3;
		var a4 = o3 - o4;

		return w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4;
	}

	private int Supervisor()
	{
		if (Pass >= 3)
		{
			if (Perceptron3() > 0m)
			{
				if (Perceptron2() > 0m)
				{
					_currentStopLoss = Sl2;
					_currentTakeProfit = Tp2;
					return 1;
				}
			}
			else
			{
				if (Perceptron1() < 0m)
				{
					_currentStopLoss = Sl1;
					_currentTakeProfit = Tp1;
					return -1;
				}
			}

			return 0;
		}

		if (Pass == 2)
		{
			if (Perceptron2() > 0m)
			{
				_currentStopLoss = Sl2;
				_currentTakeProfit = Tp2;
				return 1;
			}

			return 0;
		}

		if (Pass == 1)
		{
			if (Perceptron1() < 0m)
			{
				_currentStopLoss = Sl1;
				_currentTakeProfit = Tp1;
				return -1;
			}

			return 0;
		}

		return 0;
	}
}
