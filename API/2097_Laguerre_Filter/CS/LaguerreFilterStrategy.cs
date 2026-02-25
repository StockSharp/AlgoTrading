using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Laguerre filter and WMA (FIR) crossover.
/// </summary>
public class LaguerreFilterStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFir;
	private decimal? _prevLaguerre;

	public decimal StopLossPct
	{
		get => _stopLossPct.Value;
		set => _stopLossPct.Value = value;
	}

	public decimal TakeProfitPct
	{
		get => _takeProfitPct.Value;
		set => _takeProfitPct.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public LaguerreFilterStrategy()
	{
		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevFir = _prevLaguerre = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var laguerre = new ExponentialMovingAverage { Length = 10 };
		var fir = new WeightedMovingAverage { Length = 4 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(laguerre, fir, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, laguerre);
			DrawIndicator(area, fir);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal laguerreValue, decimal firValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFir is null || _prevLaguerre is null)
		{
			_prevFir = firValue;
			_prevLaguerre = laguerreValue;
			return;
		}

		var firWasAbove = _prevFir > _prevLaguerre;
		var firIsAbove = firValue > laguerreValue;

		if (!firWasAbove && firIsAbove && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (firWasAbove && !firIsAbove && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevFir = firValue;
		_prevLaguerre = laguerreValue;
	}
}
