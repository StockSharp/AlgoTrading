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
/// Stochastic-based strategy with crossover mode.
/// Generates contrarian signals when K crosses D.
/// </summary>
public class StochasticHistogramStrategy : Strategy
{
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevK;
	private decimal? _prevD;

	public int KPeriod { get => _kPeriod.Value; set => _kPeriod.Value = value; }
	public int DPeriod { get => _dPeriod.Value; set => _dPeriod.Value = value; }
	public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public StochasticHistogramStrategy()
	{
		_kPeriod = Param(nameof(KPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("K Period", "Stochastic %K period", "Stochastic");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Period", "Stochastic %D period", "Stochastic");

		_highLevel = Param(nameof(HighLevel), 80m)
			.SetDisplay("High Level", "Upper threshold", "Stochastic");

		_lowLevel = Param(nameof(LowLevel), 20m)
			.SetDisplay("Low Level", "Lower threshold", "Stochastic");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for calculation", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevK = null;
		_prevD = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stoch = new StochasticOscillator();
		stoch.K.Length = KPeriod;
		stoch.D.Length = DPeriod;

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(stoch, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stoch);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var sv = stochValue as IStochasticOscillatorValue;
		if (sv?.K is not decimal k || sv?.D is not decimal d)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevK = k;
			_prevD = d;
			return;
		}

		if (_prevK is decimal pk && _prevD is decimal pd)
		{
			// K crosses above D in oversold zone - buy
			if (pk <= pd && k > d && k < LowLevel && Position <= 0)
				BuyMarket();
			// K crosses below D in overbought zone - sell
			else if (pk >= pd && k < d && k > HighLevel && Position >= 0)
				SellMarket();
		}

		_prevK = k;
		_prevD = d;
	}
}
