using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on zero crossing of the price derivative.
/// The derivative is calculated as momentum divided by period.
/// When the derivative switches sign, the position is reversed.
/// </summary>
public class DerivativeZeroCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _derivativePeriod;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevDerivative;

	public int DerivativePeriod
	{
		get => _derivativePeriod.Value;
		set => _derivativePeriod.Value = value;
	}

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

	public DerivativeZeroCrossStrategy()
	{
		_derivativePeriod = Param(nameof(DerivativePeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Derivative Period", "Smoothing period for derivative", "Indicator");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
			.SetGreaterThanZero();

		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevDerivative = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var momentum = new Momentum { Length = DerivativePeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(momentum, (candle, momValue) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			var derivative = momValue / DerivativePeriod * 100m;

			if (_prevDerivative is null)
			{
				_prevDerivative = derivative;
				return;
			}

			var prev = _prevDerivative.Value;

			// Derivative crossed up through zero -> buy
			if (prev <= 0m && derivative > 0m)
			{
				if (Position < 0) BuyMarket();
				if (Position <= 0) BuyMarket();
			}
			// Derivative crossed down through zero -> sell
			else if (prev >= 0m && derivative < 0m)
			{
				if (Position > 0) SellMarket();
				if (Position >= 0) SellMarket();
			}

			_prevDerivative = derivative;
		}).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, momentum);
			DrawOwnTrades(area);
		}
	}
}
