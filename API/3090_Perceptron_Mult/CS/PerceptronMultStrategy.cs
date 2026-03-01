using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Perceptron strategy that uses weighted Acceleration/Deceleration oscillator values
/// to determine trade direction. Simplified from multi-symbol to single security.
/// </summary>
public class PerceptronMultStrategy : Strategy
{
	private readonly StrategyParam<int> _weight1;
	private readonly StrategyParam<int> _weight2;
	private readonly StrategyParam<int> _weight3;
	private readonly StrategyParam<int> _weight4;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private AwesomeOscillator _ao;
	private SimpleMovingAverage _aoAverage;
	private readonly decimal?[] _acValues = new decimal?[22];
	private int _valuesStored;
	private decimal _pointValue;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// First perceptron weight.
	/// </summary>
	public int Weight1
	{
		get => _weight1.Value;
		set => _weight1.Value = value;
	}

	/// <summary>
	/// Second perceptron weight.
	/// </summary>
	public int Weight2
	{
		get => _weight2.Value;
		set => _weight2.Value = value;
	}

	/// <summary>
	/// Third perceptron weight.
	/// </summary>
	public int Weight3
	{
		get => _weight3.Value;
		set => _weight3.Value = value;
	}

	/// <summary>
	/// Fourth perceptron weight.
	/// </summary>
	public int Weight4
	{
		get => _weight4.Value;
		set => _weight4.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type for signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public PerceptronMultStrategy()
	{
		_weight1 = Param(nameof(Weight1), 100)
			.SetDisplay("Weight 1", "First perceptron weight", "Perceptron");

		_weight2 = Param(nameof(Weight2), 20)
			.SetDisplay("Weight 2", "Second perceptron weight", "Perceptron");

		_weight3 = Param(nameof(Weight3), 60)
			.SetDisplay("Weight 3", "Third perceptron weight", "Perceptron");

		_weight4 = Param(nameof(Weight4), 40)
			.SetDisplay("Weight 4", "Fourth perceptron weight", "Perceptron");

		_stopLossPoints = Param(nameof(StopLossPoints), 40m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop-loss distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 95m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take-profit distance in points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for signals", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_ao = null;
		_aoAverage = null;
		Array.Clear(_acValues, 0, _acValues.Length);
		_valuesStored = 0;
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_pointValue = Security?.PriceStep ?? 1m;
		if (_pointValue <= 0m)
			_pointValue = 1m;

		_ao = new AwesomeOscillator
		{
			ShortMa = { Length = 5 },
			LongMa = { Length = 34 }
		};
		_aoAverage = new SimpleMovingAverage { Length = 5 };

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		// Check protective levels
		if (Position != 0)
		{
			if (Position > 0)
			{
				if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
				{
					SellMarket(Math.Abs(Position));
					ResetProtection();
					return;
				}
				if (_takePrice.HasValue && candle.HighPrice >= _takePrice.Value)
				{
					SellMarket(Math.Abs(Position));
					ResetProtection();
					return;
				}
			}
			else
			{
				if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
				{
					BuyMarket(Math.Abs(Position));
					ResetProtection();
					return;
				}
				if (_takePrice.HasValue && candle.LowPrice <= _takePrice.Value)
				{
					BuyMarket(Math.Abs(Position));
					ResetProtection();
					return;
				}
			}
		}

		// Process AO indicator
		var aoResult = _ao.Process(candle);
		if (!aoResult.IsFinal)
			return;

		var aoValue = aoResult.GetValue<decimal>();
		var avgResult = _aoAverage.Process(aoResult);
		if (!avgResult.IsFinal)
			return;

		var avgValue = avgResult.GetValue<decimal>();
		var acValue = aoValue - avgValue;

		// Update circular buffer
		for (var i = _acValues.Length - 1; i > 0; i--)
			_acValues[i] = _acValues[i - 1];
		_acValues[0] = acValue;
		if (_valuesStored < _acValues.Length)
			_valuesStored++;

		// Need full buffer for perceptron
		if (_valuesStored < _acValues.Length)
			return;

		if (_acValues[0] is not decimal a1 ||
			_acValues[7] is not decimal a2 ||
			_acValues[14] is not decimal a3 ||
			_acValues[21] is not decimal a4)
			return;

		var w1 = Weight1 - 100m;
		var w2 = Weight2 - 100m;
		var w3 = Weight3 - 100m;
		var w4 = Weight4 - 100m;

		var signal = w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4;

		if (Position != 0)
			return;

		if (signal > 0m)
		{
			BuyMarket();
			_entryPrice = price;
			_stopPrice = StopLossPoints > 0m ? price - StopLossPoints * _pointValue : null;
			_takePrice = TakeProfitPoints > 0m ? price + TakeProfitPoints * _pointValue : null;
		}
		else if (signal < 0m)
		{
			SellMarket();
			_entryPrice = price;
			_stopPrice = StopLossPoints > 0m ? price + StopLossPoints * _pointValue : null;
			_takePrice = TakeProfitPoints > 0m ? price - TakeProfitPoints * _pointValue : null;
		}
	}

	private void ResetProtection()
	{
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
	}
}
