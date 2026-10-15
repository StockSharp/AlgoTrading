using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple grid trading strategy.
/// Buys when price moves up a grid level, sells when down.
/// Closes on profit target.
/// </summary>
public class GridStrategy : Strategy
{
	private readonly StrategyParam<decimal> _gridStep;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastGridLevel;
	private decimal _entryPrice;

	public decimal GridStep { get => _gridStep.Value; set => _gridStep.Value = value; }
	public decimal ProfitTarget { get => _profitTarget.Value; set => _profitTarget.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public GridStrategy()
	{
		_gridStep = Param(nameof(GridStep), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Grid Step", "Step size in price units", "General");

		_profitTarget = Param(nameof(ProfitTarget), 2000m)
			.SetDisplay("Profit Target", "Profit to close position", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_lastGridLevel = 0;
		_entryPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var step = GridStep;
		var currentLevel = Math.Floor(candle.ClosePrice / step) * step;

		if (_lastGridLevel == 0)
		{
			_lastGridLevel = currentLevel;
			return;
		}

		if (currentLevel > _lastGridLevel)
		{
			if (Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
		}
		else if (currentLevel < _lastGridLevel)
		{
			if (Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}

		_lastGridLevel = currentLevel;

		// Check profit target
		if (Position > 0 && _entryPrice > 0)
		{
			if (candle.ClosePrice - _entryPrice >= ProfitTarget)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (_entryPrice - candle.ClosePrice >= ProfitTarget)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}
	}
}
