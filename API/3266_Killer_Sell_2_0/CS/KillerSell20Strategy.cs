using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Killer Sell 2.0 strategy: WilliamsR + Stochastic momentum entry.
/// Sells when WilliamsR enters overbought and Stochastic K crosses below D from overbought.
/// Buys when WilliamsR enters oversold and Stochastic K crosses above D from oversold.
/// </summary>
public class KillerSell20Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<int> _stochPeriod;

	private decimal? _prevK;
	private decimal? _prevD;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	public int StochPeriod
	{
		get => _stochPeriod.Value;
		set => _stochPeriod.Value = value;
	}

	public KillerSell20Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_wprPeriod = Param(nameof(WprPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("WPR Period", "Williams %R period", "Indicators");

		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Stochastic K period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevK = null;
		_prevD = null;

		var wpr = new WilliamsR { Length = WprPeriod };
		var stoch = new StochasticOscillator
		{
			K = { Length = StochPeriod },
			D = { Length = 3 }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(wpr, stoch, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue wprValue, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var wprVal = wprValue.ToDecimal();
		var stochTyped = (StochasticOscillatorValue)stochValue;
		if (stochTyped.K is not decimal k || stochTyped.D is not decimal d)
			return;

		if (_prevK.HasValue && _prevD.HasValue)
		{
			// Sell: WPR overbought (> -20) and stochastic K crosses below D from overbought
			if (wprVal > -20m && _prevK.Value >= _prevD.Value && k < d && k > 70m && Position >= 0)
			{
				SellMarket();
			}
			// Buy: WPR oversold (< -80) and stochastic K crosses above D from oversold
			else if (wprVal < -80m && _prevK.Value <= _prevD.Value && k > d && k < 30m && Position <= 0)
			{
				BuyMarket();
			}
		}

		_prevK = k;
		_prevD = d;
	}
}
