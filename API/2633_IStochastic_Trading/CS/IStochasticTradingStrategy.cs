using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic Oscillator-based strategy with zone filtering.
/// Goes long when %K crosses above %D in the oversold zone, short when %K crosses below %D in overbought zone.
/// </summary>
public class IStochasticTradingStrategy : Strategy
{
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<decimal> _zoneBuy;
	private readonly StrategyParam<decimal> _zoneSell;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevK;
	private decimal? _prevD;

	/// <summary>
	/// %K period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// %D smoothing period.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Buy zone threshold (oversold).
	/// </summary>
	public decimal ZoneBuy
	{
		get => _zoneBuy.Value;
		set => _zoneBuy.Value = value;
	}

	/// <summary>
	/// Sell zone threshold (overbought).
	/// </summary>
	public decimal ZoneSell
	{
		get => _zoneSell.Value;
		set => _zoneSell.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public IStochasticTradingStrategy()
	{
		_kPeriod = Param(nameof(KPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("K Period", "Number of bars for %K", "Indicators");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Period", "Smoothing period for %D", "Indicators");

		_zoneBuy = Param(nameof(ZoneBuy), 30m)
			.SetRange(0m, 100m)
			.SetDisplay("Buy Zone", "Upper boundary for bullish confirmation", "Signals");

		_zoneSell = Param(nameof(ZoneSell), 70m)
			.SetRange(0m, 100m)
			.SetDisplay("Sell Zone", "Lower boundary for bearish confirmation", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for the strategy", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevK = null;
		_prevD = null;

		var stochastic = new StochasticOscillator();
		stochastic.K.Length = KPeriod;
		stochastic.D.Length = DPeriod;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochasticValue.IsFinal)
			return;

		var stoch = (StochasticOscillatorValue)stochasticValue;

		if (stoch.K is not decimal kValue || stoch.D is not decimal dValue)
			return;

		if (_prevK is decimal prevK && _prevD is decimal prevD)
		{
			var crossedUp = prevK <= prevD && kValue > dValue;
			var crossedDown = prevK >= prevD && kValue < dValue;

			if (crossedUp && dValue < ZoneBuy && Position <= 0)
			{
				BuyMarket();
			}
			else if (crossedDown && dValue > ZoneSell && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevK = kValue;
		_prevD = dValue;
	}
}
