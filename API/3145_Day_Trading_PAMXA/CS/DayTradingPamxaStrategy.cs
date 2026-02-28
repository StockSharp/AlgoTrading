namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Day Trading PAMXA strategy.
/// Combines Awesome Oscillator reversals with a stochastic filter.
/// </summary>
public class DayTradingPamxaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<decimal> _stochasticLevelUp;
	private readonly StrategyParam<decimal> _stochasticLevelDown;
	private readonly StrategyParam<int> _aoShortPeriod;
	private readonly StrategyParam<int> _aoLongPeriod;

	private decimal? _aoPrevious;
	private decimal? _aoPreviousPrevious;
	private decimal _lastStochK;
	private decimal _lastStochD;
	private bool _stochReady;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int StochasticKPeriod { get => _stochasticKPeriod.Value; set => _stochasticKPeriod.Value = value; }
	public int StochasticDPeriod { get => _stochasticDPeriod.Value; set => _stochasticDPeriod.Value = value; }
	public decimal StochasticLevelUp { get => _stochasticLevelUp.Value; set => _stochasticLevelUp.Value = value; }
	public decimal StochasticLevelDown { get => _stochasticLevelDown.Value; set => _stochasticLevelDown.Value = value; }
	public int AoShortPeriod { get => _aoShortPeriod.Value; set => _aoShortPeriod.Value = value; }
	public int AoLongPeriod { get => _aoLongPeriod.Value; set => _aoLongPeriod.Value = value; }

	public DayTradingPamxaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
			.SetDisplay("Stochastic %K", "%K period", "Indicators");

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
			.SetDisplay("Stochastic %D", "%D period", "Indicators");

		_stochasticLevelUp = Param(nameof(StochasticLevelUp), 75m)
			.SetDisplay("Level Up", "Upper stochastic threshold", "Indicators");

		_stochasticLevelDown = Param(nameof(StochasticLevelDown), 25m)
			.SetDisplay("Level Down", "Lower stochastic threshold", "Indicators");

		_aoShortPeriod = Param(nameof(AoShortPeriod), 5)
			.SetDisplay("AO Fast", "Short AO period", "Indicators");

		_aoLongPeriod = Param(nameof(AoLongPeriod), 34)
			.SetDisplay("AO Slow", "Long AO period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_aoPrevious = null;
		_aoPreviousPrevious = null;
		_stochReady = false;

		var awesome = new AwesomeOscillator();
		awesome.ShortMa.Length = AoShortPeriod;
		awesome.LongMa.Length = AoLongPeriod;

		var stochastic = new StochasticOscillator();
		stochastic.K.Length = StochasticKPeriod;
		stochastic.D.Length = StochasticDPeriod;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(awesome, stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue aoValue, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update stochastic values
		if (stochValue.IsFinal && stochValue.IsFormed)
		{
			var stoch = (StochasticOscillatorValue)stochValue;
			if (stoch.K is decimal k && stoch.D is decimal d)
			{
				_lastStochK = k;
				_lastStochD = d;
				_stochReady = true;
			}
		}

		// Update AO values
		if (aoValue.IsFinal && aoValue.IsFormed)
		{
			var ao = aoValue.GetValue<decimal>();
			_aoPreviousPrevious = _aoPrevious;
			_aoPrevious = ao;
		}

		if (_aoPreviousPrevious == null || _aoPrevious == null || !_stochReady)
			return;

		var bullishAoCross = _aoPreviousPrevious.Value < 0m && _aoPrevious.Value > 0m;
		var bearishAoCross = _aoPreviousPrevious.Value > 0m && _aoPrevious.Value < 0m;

		var stochasticOversold = _lastStochK < StochasticLevelDown || _lastStochD < StochasticLevelDown;
		var stochasticOverbought = _lastStochK > StochasticLevelUp || _lastStochD > StochasticLevelUp;

		if (bullishAoCross)
		{
			if (Position < 0)
				BuyMarket();

			if (Position <= 0 && stochasticOversold)
				BuyMarket();
		}
		else if (bearishAoCross)
		{
			if (Position > 0)
				SellMarket();

			if (Position >= 0 && stochasticOverbought)
				SellMarket();
		}
	}
}
