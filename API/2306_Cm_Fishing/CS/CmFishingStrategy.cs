using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid trading strategy based on fixed price steps.
/// Buys when price drops by step amount, sells when price rises by step amount.
/// Closes on profit threshold.
/// </summary>
public class CmFishingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stepSize;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _referencePrice;
	private decimal _entryPrice;

	public decimal StepSize
	{
		get => _stepSize.Value;
		set => _stepSize.Value = value;
	}

	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public CmFishingStrategy()
	{
		_stepSize = Param(nameof(StepSize), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Step Size", "Price step for grid entries", "Parameters");

		_profitTarget = Param(nameof(ProfitTarget), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Target", "Price profit to close position", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
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
		_referencePrice = 0;
		_entryPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_referencePrice = 0;
		_entryPrice = 0;

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

		var price = candle.ClosePrice;

		if (_referencePrice == 0)
		{
			_referencePrice = price;
			return;
		}

		// Check profit target first
		if (Position > 0 && price >= _entryPrice + ProfitTarget)
		{
			SellMarket();
			_referencePrice = price;
			_entryPrice = 0;
			return;
		}
		else if (Position < 0 && price <= _entryPrice - ProfitTarget)
		{
			BuyMarket();
			_referencePrice = price;
			_entryPrice = 0;
			return;
		}

		// Grid entries: buy on dip, sell on rise
		if (price <= _referencePrice - StepSize && Position <= 0)
		{
			BuyMarket();
			_entryPrice = price;
			_referencePrice = price;
		}
		else if (price >= _referencePrice + StepSize && Position >= 0)
		{
			SellMarket();
			_entryPrice = price;
			_referencePrice = price;
		}
	}
}
