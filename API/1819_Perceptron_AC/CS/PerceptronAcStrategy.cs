
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Perceptron-based strategy using Accelerator Oscillator.
/// </summary>
public class PerceptronAcStrategy : Strategy
{
	private readonly StrategyParam<int> _x1;
	private readonly StrategyParam<int> _x2;
	private readonly StrategyParam<int> _x3;
	private readonly StrategyParam<int> _x4;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _aoFast;
	private SimpleMovingAverage _aoSlow;
	private SimpleMovingAverage _acMa;

	private readonly List<decimal> _acValues = new();
	private decimal _entryPrice;
	private decimal _stopPrice;

	/// <summary>
	/// Weight for current AC value.
	/// </summary>
	public int X1 { get => _x1.Value; set => _x1.Value = value; }

	/// <summary>
	/// Weight for AC value 7 bars ago.
	/// </summary>
	public int X2 { get => _x2.Value; set => _x2.Value = value; }

	/// <summary>
	/// Weight for AC value 14 bars ago.
	/// </summary>
	public int X3 { get => _x3.Value; set => _x3.Value = value; }

	/// <summary>
	/// Weight for AC value 21 bars ago.
	/// </summary>
	public int X4 { get => _x4.Value; set => _x4.Value = value; }

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }

	/// <summary>
	/// Candle type for calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes <see cref="PerceptronAcStrategy"/>.
	/// </summary>
	public PerceptronAcStrategy()
	{
		_x1 = Param(nameof(X1), 288)
			.SetDisplay("X1", "Weight for current AC", "Perceptron");

		_x2 = Param(nameof(X2), 216)
			.SetDisplay("X2", "Weight for AC 7 bars ago", "Perceptron");

		_x3 = Param(nameof(X3), 144)
			.SetDisplay("X3", "Weight for AC 14 bars ago", "Perceptron");

		_x4 = Param(nameof(X4), 72)
			.SetDisplay("X4", "Weight for AC 21 bars ago", "Perceptron");

		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk");

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleType) };
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_acValues.Clear();
		_entryPrice = 0m;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_aoFast = new SimpleMovingAverage { Length = 5 };
		_aoSlow = new SimpleMovingAverage { Length = 34 };
		_acMa = new SimpleMovingAverage { Length = 5 };

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

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hl2 = (candle.HighPrice + candle.LowPrice) / 2m;
		var fast = _aoFast.Process(hl2, candle.OpenTime, true).ToDecimal();
		var slow = _aoSlow.Process(hl2, candle.OpenTime, true).ToDecimal();
		var ao = fast - slow;
		var ac = ao - _acMa.Process(ao, candle.OpenTime, true).ToDecimal();

		_acValues.Insert(0, ac);
		if (_acValues.Count > 22)
			_acValues.RemoveAt(_acValues.Count - 1);

		if (_acValues.Count < 22 || !IsFormedAndOnlineAndAllowTrading())
			return;

		var w1 = X1 - 100;
		var w2 = X2 - 100;
		var w3 = X3 - 100;
		var w4 = X4 - 100;
		var p = w1 * _acValues[0] + w2 * _acValues[7] + w3 * _acValues[14] + w4 * _acValues[21];

		if (Position > 0)
		{
			if (candle.ClosePrice > _stopPrice + StopLoss * 2m)
			{
				if (p < 0)
				{
					SellMarket(Position + Volume);
					_entryPrice = candle.ClosePrice;
					_stopPrice = _entryPrice + StopLoss;
				}
				else
				{
					_stopPrice = candle.ClosePrice - StopLoss;
				}
			}
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice < _stopPrice - StopLoss * 2m)
			{
				if (p > 0)
				{
					BuyMarket(Volume + Math.Abs(Position));
					_entryPrice = candle.ClosePrice;
					_stopPrice = _entryPrice - StopLoss;
				}
				else
				{
					_stopPrice = candle.ClosePrice + StopLoss;
				}
			}
		}
		else
		{
			if (p > 0)
			{
				BuyMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - StopLoss;
			}
			else
			{
				SellMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + StopLoss;
			}
		}
	}
}
