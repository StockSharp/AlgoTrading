using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Perceptron strategy driven by four Accelerator Oscillator samples spaced seven bars apart.
/// </summary>
public class ArtificialIntelligencePerceptronStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _shift;
	private readonly StrategyParam<decimal> _x1;
	private readonly StrategyParam<decimal> _x2;
	private readonly StrategyParam<decimal> _x3;
	private readonly StrategyParam<decimal> _x4;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _median = [];
	private readonly List<decimal> _ao = [];
	private readonly List<decimal> _ac = [];
	private decimal _entryPrice;
	private decimal? _stopPrice;

	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public int Shift { get => _shift.Value; set => _shift.Value = value; }
	public decimal X1 { get => _x1.Value; set => _x1.Value = value; }
	public decimal X2 { get => _x2.Value; set => _x2.Value = value; }
	public decimal X3 { get => _x3.Value; set => _x3.Value = value; }
	public decimal X4 { get => _x4.Value; set => _x4.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ArtificialIntelligencePerceptronStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 850m).SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop distance in price steps.", "Risk");
		_shift = Param(nameof(Shift), 1).SetNotNegative()
			.SetDisplay("Shift", "Latest AC sample shift.", "Perceptron");
		_x1 = Param(nameof(X1), 1m).SetDisplay("X1", "Weight for the newest AC sample.", "Perceptron");
		_x2 = Param(nameof(X2), 1m).SetDisplay("X2", "Weight for AC sample 7 bars older.", "Perceptron");
		_x3 = Param(nameof(X3), 1m).SetDisplay("X3", "Weight for AC sample 14 bars older.", "Perceptron");
		_x4 = Param(nameof(X4), 1m).SetDisplay("X4", "Weight for AC sample 21 bars older.", "Perceptron");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe.", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_median.Clear();
		_ao.Clear();
		_ac.Clear();
		_entryPrice = 0m;
		_stopPrice = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		_median.Add(median);

		if (_median.Count < 34)
			return;

		var ao = AverageTail(_median, 5) - AverageTail(_median, 34);
		_ao.Add(ao);

		if (_ao.Count < 5)
			return;

		var ac = ao - AverageTail(_ao, 5);
		_ac.Add(ac);

		ApplyStop(candle);

		var newest = _ac.Count - 1 - Shift;
		if (newest - 21 < 0)
			return;

		var output =
			X1 * _ac[newest] +
			X2 * _ac[newest - 7] +
			X3 * _ac[newest - 14] +
			X4 * _ac[newest - 21];

		var signal = output > 0m ? 1 : output < 0m ? -1 : 0;
		if (signal == 0)
			return;

		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;
		var stopDistance = StopLoss * step;

		if (Position == 0)
		{
			Enter(signal, Volume, candle.ClosePrice, stopDistance);
			return;
		}

		var currentDirection = Position > 0 ? 1 : -1;
		if (signal == currentDirection)
			return;

		var profitDistance = currentDirection > 0
			? candle.ClosePrice - _entryPrice
			: _entryPrice - candle.ClosePrice;

		if (profitDistance > stopDistance * 2m)
		{
			// Reverse and increase the new exposure to twice the base volume.
			var orderVolume = Math.Abs(Position) + Volume * 2m;
			if (signal > 0)
				BuyMarket(orderVolume);
			else
				SellMarket(orderVolume);

			_entryPrice = candle.ClosePrice;
			_stopPrice = signal > 0 ? _entryPrice - stopDistance : _entryPrice + stopDistance;
		}
		else
		{
			// Opposite signal without enough profit: protect the current trade at break-even.
			_stopPrice = _entryPrice;
		}
	}

	private void ApplyStop(ICandleMessage candle)
	{
		if (Position > 0 && _stopPrice is decimal longStop && candle.LowPrice <= longStop)
		{
			SellMarket(Math.Abs(Position));
			_entryPrice = 0m;
			_stopPrice = null;
		}
		else if (Position < 0 && _stopPrice is decimal shortStop && candle.HighPrice >= shortStop)
		{
			BuyMarket(Math.Abs(Position));
			_entryPrice = 0m;
			_stopPrice = null;
		}
	}

	private void Enter(int signal, decimal volume, decimal price, decimal stopDistance)
	{
		if (signal > 0)
			BuyMarket(volume);
		else
			SellMarket(volume);

		_entryPrice = price;
		_stopPrice = signal > 0 ? price - stopDistance : price + stopDistance;
	}

	private static decimal AverageTail(List<decimal> values, int count)
	{
		var start = values.Count - count;
		var sum = 0m;
		for (var i = start; i < values.Count; i++)
			sum += values[i];
		return sum / count;
	}
}
