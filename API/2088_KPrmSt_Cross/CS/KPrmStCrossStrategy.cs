using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// KPrmSt cross strategy based on the stochastic oscillator.
/// Opens long when %K crosses above %D from below.
/// Opens short when %K crosses below %D from above.
/// </summary>
public class KprmStCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;

	private StochasticOscillator _stochastic;
	private decimal? _prevK;
	private decimal? _prevD;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

	public KprmStCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for indicator calculation", "General");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");
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
		_prevK = null;
		_prevD = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_stochastic = new StochasticOscillator();

		var passthrough = new SimpleMovingAverage { Length = 1 };
		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(passthrough, (candle, _) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			var stochResult = _stochastic.Process(candle);
			if (!stochResult.IsFormed)
				return;

			var stochVal = (StochasticOscillatorValue)stochResult;
			if (stochVal.K is not decimal k || stochVal.D is not decimal d)
				return;

			if (_prevK.HasValue && _prevD.HasValue)
			{
				var wasBelow = _prevK.Value < _prevD.Value;
				var isAbove = k > d;

				// K crosses above D -> buy
				if (wasBelow && isAbove && Position <= 0)
				{
					if (Position < 0) BuyMarket();
					BuyMarket();
				}
				// K crosses below D -> sell
				else if (!wasBelow && !isAbove && _prevK.Value > _prevD.Value && Position >= 0)
				{
					if (Position > 0) SellMarket();
					SellMarket();
				}
			}

			_prevK = k;
			_prevD = d;
		}).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
}
