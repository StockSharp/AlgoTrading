using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Accelerator Oscillator values processed by a linear perceptron.
/// </summary>
public class ArtificialIntelligenceStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _shift;
	private readonly StrategyParam<int> _x1;
	private readonly StrategyParam<int> _x2;
	private readonly StrategyParam<int> _x3;
	private readonly StrategyParam<int> _x4;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _acValues = new();

	private decimal _entryPrice;
	private decimal _stopPrice;

	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public int Shift { get => _shift.Value; set => _shift.Value = value; }
	public int X1 { get => _x1.Value; set => _x1.Value = value; }
	public int X2 { get => _x2.Value; set => _x2.Value = value; }
	public int X3 { get => _x3.Value; set => _x3.Value = value; }
	public int X4 { get => _x4.Value; set => _x4.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ArtificialIntelligenceStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 850)
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk");

		_shift = Param(nameof(Shift), 1)
			.SetDisplay("Shift", "Bar shift for indicator values", "Perceptron");

		_x1 = Param(nameof(X1), 135).SetDisplay("X1", "Perceptron weight 1", "Perceptron");
		_x2 = Param(nameof(X2), 127).SetDisplay("X2", "Perceptron weight 2", "Perceptron");
		_x3 = Param(nameof(X3), 16).SetDisplay("X3", "Perceptron weight 3", "Perceptron");
		_x4 = Param(nameof(X4), 93).SetDisplay("X4", "Perceptron weight 4", "Perceptron");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ac = new AcceleratorOscillator();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ac, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal acValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_acValues.Add(acValue);
		var required = Shift + 21;

		if (_acValues.Count <= required)
			return;

		if (_acValues.Count > required + 10)
			_acValues.RemoveAt(0);

		var index = _acValues.Count - 1;
		var v1 = _acValues[index - Shift];
		var v2 = _acValues[index - (Shift + 7)];
		var v3 = _acValues[index - (Shift + 14)];
		var v4 = _acValues[index - (Shift + 21)];

		var w1 = X1 - 100m;
		var w2 = X2 - 100m;
		var w3 = X3 - 100m;
		var w4 = X4 - 100m;

		var perceptron = w1 * v1 + w2 * v2 + w3 * v3 + w4 * v4;

		var openBuy = perceptron > 0;
		var openSell = perceptron < 0;

		var price = candle.ClosePrice;
		var step = Security.PriceStep ?? 1m;

		if (Position == 0)
		{
			if (openBuy)
			{
				_entryPrice = price;
				_stopPrice = price - StopLoss * step;
				BuyMarket();
			}
			else if (openSell)
			{
				_entryPrice = price;
				_stopPrice = price + StopLoss * step;
				SellMarket();
			}

			return;
		}

		if (Position > 0)
		{
			if (price <= _stopPrice)
			{
				SellMarket();
				return;
			}

			if (openSell)
			{
				var profit = price - _stopPrice;
				if (profit > StopLoss * 2m * step)
				{
					SellMarket(Math.Abs(Position) + Volume);
					_entryPrice = price;
					_stopPrice = price + StopLoss * step;
				}
				else
				{
					_stopPrice = _entryPrice;
				}
			}
		}
		else if (Position < 0)
		{
			if (price >= _stopPrice)
			{
				BuyMarket();
				return;
			}

			if (openBuy)
			{
				var profit = _stopPrice - price;
				if (profit > StopLoss * 2m * step)
				{
					BuyMarket(Math.Abs(Position) + Volume);
					_entryPrice = price;
					_stopPrice = price - StopLoss * step;
				}
				else
				{
					_stopPrice = _entryPrice;
				}
			}
		}
	}
}
