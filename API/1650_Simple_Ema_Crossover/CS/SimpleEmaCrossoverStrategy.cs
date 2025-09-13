using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple EMA crossover strategy.
/// Enters long when the fast EMA crosses above the slow EMA and short when the opposite occurs.
/// </summary>
public class SimpleEmaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _periods;
	private readonly StrategyParam<Unit> _stopLoss;
	private readonly StrategyParam<Unit> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;

	/// <summary>
	/// Period for the fast EMA.
	/// </summary>
	public int Periods
	{
		get => _periods.Value;
		set => _periods.Value = value;
	}

	/// <summary>
	/// Stop-loss distance.
	/// </summary>
	public Unit StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take-profit distance.
	/// </summary>
	public Unit TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public SimpleEmaCrossoverStrategy()
	{
		_periods = Param(nameof(Periods), 17)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for the fast EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 1);

		_stopLoss = Param(nameof(StopLoss), new Unit(31m, UnitTypes.Absolute))
			.SetDisplay("Stop Loss", "Stop-loss distance in price", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 5);

		_takeProfit = Param(nameof(TakeProfit), new Unit(69m, UnitTypes.Absolute))
			.SetDisplay("Take Profit", "Take-profit distance in price", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for analysis", "General");
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
		_prevFast = default;
		_prevSlow = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(takeProfit: TakeProfit, stopLoss: StopLoss);

		var fastEma = new ExponentialMovingAverage { Length = Periods };
		var slowEma = new ExponentialMovingAverage { Length = Periods + 2 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevFast != default && _prevSlow != default)
		{
			var crossUp = _prevFast < _prevSlow && fast > slow;
			var crossDown = _prevFast > _prevSlow && fast < slow;

			if (crossUp && Position <= 0)
				BuyMarket();
			else if (crossDown && Position >= 0)
				SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
