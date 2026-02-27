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
/// Stochastic alignment strategy using fast and slow stochastic oscillators.
/// Enters when both stochastics agree on direction.
/// </summary>
public class StochasticThreePeriodsStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private StochasticOscillator _slowStoch;
	private decimal _prevSlowK;
	private decimal _prevSlowD;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public StochasticThreePeriodsStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast K", "Fast stochastic K period", "Parameters");

		_slowPeriod = Param(nameof(SlowPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Slow K", "Slow stochastic K period", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastStoch = new StochasticOscillator { K = { Length = FastPeriod } };
		_slowStoch = new StochasticOscillator { K = { Length = SlowPeriod } };
		_prevSlowK = 0m;
		_prevSlowD = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastStoch, (candle, fastValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				// Process slow stochastic manually
				var slowResult = _slowStoch.Process(candle);
				if (!_slowStoch.IsFormed)
					return;

				var fast = (IStochasticOscillatorValue)fastValue;
				var slow = (IStochasticOscillatorValue)slowResult;

				if (fast.K is not decimal fk || fast.D is not decimal fd)
					return;
				if (slow.K is not decimal sk || slow.D is not decimal sd)
					return;

				// Buy: fast K crosses above D, and slow K > D (trend up)
				var fastCrossUp = fk > fd;
				var slowBullish = sk > sd && _prevSlowK <= _prevSlowD;

				// Sell: fast K crosses below D, and slow K < D (trend down)
				var fastCrossDown = fk < fd;
				var slowBearish = sk < sd && _prevSlowK >= _prevSlowD;

				if ((fastCrossUp && sk > sd) && Position <= 0)
				{
					BuyMarket();
				}
				else if ((fastCrossDown && sk < sd) && Position >= 0)
				{
					SellMarket();
				}

				_prevSlowK = sk;
				_prevSlowD = sd;
			})
			.Start();

		StartProtection(
			new Unit(2000m, UnitTypes.Absolute),
			new Unit(1000m, UnitTypes.Absolute));
	}
}
