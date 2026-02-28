namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// TradePad Sample strategy.
/// Classifies market state using Stochastic oscillator and trades on state transitions.
/// Buys when stochastic crosses up from oversold, sells when it crosses down from overbought.
/// </summary>
public class TradePadSampleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<decimal> _upperLevel;
	private readonly StrategyParam<decimal> _lowerLevel;

	private decimal _prevK;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int StochasticKPeriod { get => _stochasticKPeriod.Value; set => _stochasticKPeriod.Value = value; }
	public int StochasticDPeriod { get => _stochasticDPeriod.Value; set => _stochasticDPeriod.Value = value; }
	public decimal UpperLevel { get => _upperLevel.Value; set => _upperLevel.Value = value; }
	public decimal LowerLevel { get => _lowerLevel.Value; set => _lowerLevel.Value = value; }

	public TradePadSampleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 10)
			.SetDisplay("Stochastic %K", "%K period", "Indicators");

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
			.SetDisplay("Stochastic %D", "%D period", "Indicators");

		_upperLevel = Param(nameof(UpperLevel), 75m)
			.SetDisplay("Upper Threshold", "Overbought level", "Signals");

		_lowerLevel = Param(nameof(LowerLevel), 25m)
			.SetDisplay("Lower Threshold", "Oversold level", "Signals");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var stochastic = new StochasticOscillator();
		stochastic.K.Length = StochasticKPeriod;
		stochastic.D.Length = StochasticDPeriod;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochVal.IsFinal || !stochVal.IsFormed)
			return;

		var stoch = (StochasticOscillatorValue)stochVal;
		if (stoch.K is not decimal kValue)
			return;

		if (!_hasPrev)
		{
			_prevK = kValue;
			_hasPrev = true;
			return;
		}

		// Buy: stochastic crosses up through lower level (leaving oversold)
		var crossUp = _prevK <= LowerLevel && kValue > LowerLevel;
		// Sell: stochastic crosses down through upper level (leaving overbought)
		var crossDown = _prevK >= UpperLevel && kValue < UpperLevel;

		if (crossUp && Position <= 0)
		{
			BuyMarket();
		}
		else if (crossDown && Position >= 0)
		{
			SellMarket();
		}

		_prevK = kValue;
	}
}
