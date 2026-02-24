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
/// Martingale-style hedging strategy that alternates buy/sell based on price movement.
/// Opens initial position and doubles down on adverse moves.
/// </summary>
public class TwoDirectionMartinStylizedStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<int> _maxSteps;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private int _stepCount;
	private int _direction; // 1=long, -1=short

	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public int MaxSteps { get => _maxSteps.Value; set => _maxSteps.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TwoDirectionMartinStylizedStrategy()
	{
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0.35m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit as percent of price", "General");

		_maxSteps = Param(nameof(MaxSteps), 3)
			.SetGreaterThanZero()
			.SetDisplay("Max Steps", "Maximum martingale doublings", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		if (Position == 0)
		{
			// Initial entry based on EMA trend
			if (price > emaValue)
			{
				BuyMarket();
				_direction = 1;
			}
			else
			{
				SellMarket();
				_direction = -1;
			}
			_entryPrice = price;
			_stepCount = 0;
			return;
		}

		var tp = _entryPrice * TakeProfitPercent / 100m;

		// Check take profit
		if (_direction == 1 && price >= _entryPrice + tp)
		{
			// Close long
			while (Position > 0) SellMarket();
			_stepCount = 0;
		}
		else if (_direction == -1 && price <= _entryPrice - tp)
		{
			// Close short
			while (Position < 0) BuyMarket();
			_stepCount = 0;
		}
		// Check adverse move - double down
		else if (_direction == 1 && price <= _entryPrice - tp && _stepCount < MaxSteps)
		{
			BuyMarket();
			_entryPrice = (_entryPrice + price) / 2m;
			_stepCount++;
		}
		else if (_direction == -1 && price >= _entryPrice + tp && _stepCount < MaxSteps)
		{
			SellMarket();
			_entryPrice = (_entryPrice + price) / 2m;
			_stepCount++;
		}
		// Max steps reached - cut loss
		else if (_stepCount >= MaxSteps)
		{
			if (Position > 0) SellMarket();
			else if (Position < 0) BuyMarket();
			_stepCount = 0;
		}
	}
}
