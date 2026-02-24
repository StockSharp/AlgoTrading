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
/// Stochastic oscillator based strategy.
/// Buys on K/D crossover in oversold, sells on crossover in overbought.
/// </summary>
public class StochasticAutomatedStrategy : Strategy
{
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<decimal> _overBought;
	private readonly StrategyParam<decimal> _overSold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevK;
	private decimal? _prevD;

	public int KPeriod { get => _kPeriod.Value; set => _kPeriod.Value = value; }
	public int DPeriod { get => _dPeriod.Value; set => _dPeriod.Value = value; }
	public decimal OverBought { get => _overBought.Value; set => _overBought.Value = value; }
	public decimal OverSold { get => _overSold.Value; set => _overSold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public StochasticAutomatedStrategy()
	{
		_kPeriod = Param(nameof(KPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("%K Period", "Stochastic %K period", "Stochastic");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("%D Period", "Stochastic %D period", "Stochastic");

		_overBought = Param(nameof(OverBought), 80m)
			.SetDisplay("Overbought", "Overbought threshold", "Stochastic");

		_overSold = Param(nameof(OverSold), 20m)
			.SetDisplay("Oversold", "Oversold threshold", "Stochastic");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevK = _prevD = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stochastic = new StochasticOscillator();
		stochastic.K.Length = KPeriod;
		stochastic.D.Length = DPeriod;

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(stochastic, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stoch = (IStochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		if (_prevK is decimal pk && _prevD is decimal pd)
		{
			// Buy: K crosses above D in oversold zone
			if (pk <= pd && k > d && pd < OverSold && Position <= 0)
				BuyMarket();

			// Sell: K crosses below D in overbought zone
			if (pk >= pd && k < d && pd > OverBought && Position >= 0)
				SellMarket();
		}

		_prevK = k;
		_prevD = d;
	}
}
