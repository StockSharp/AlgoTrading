namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Multi-indicator trend following strategy converted from the MetaTrader Lego EA.
/// Combines dual moving averages and stochastic oscillator to confirm entries and exits.
/// </summary>
public class LegoEaStrategy : Strategy
{
	private readonly StrategyParam<int> _maFastPeriod;
	private readonly StrategyParam<int> _maSlowPeriod;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<decimal> _stochasticLevelUp;
	private readonly StrategyParam<decimal> _stochasticLevelDown;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFastMa;
	private decimal _prevSlowMa;
	private decimal _prevStochK;
	private decimal _prevStochD;
	private bool _hasPrev;

	public int MaFastPeriod { get => _maFastPeriod.Value; set => _maFastPeriod.Value = value; }
	public int MaSlowPeriod { get => _maSlowPeriod.Value; set => _maSlowPeriod.Value = value; }
	public int StochasticKPeriod { get => _stochasticKPeriod.Value; set => _stochasticKPeriod.Value = value; }
	public int StochasticDPeriod { get => _stochasticDPeriod.Value; set => _stochasticDPeriod.Value = value; }
	public decimal StochasticLevelUp { get => _stochasticLevelUp.Value; set => _stochasticLevelUp.Value = value; }
	public decimal StochasticLevelDown { get => _stochasticLevelDown.Value; set => _stochasticLevelDown.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LegoEaStrategy()
	{
		_maFastPeriod = Param(nameof(MaFastPeriod), 14)
			.SetDisplay("Fast MA Period", "Fast moving average lookback", "Indicators");

		_maSlowPeriod = Param(nameof(MaSlowPeriod), 67)
			.SetDisplay("Slow MA Period", "Slow moving average lookback", "Indicators");

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
			.SetDisplay("Stochastic %K", "%K calculation length", "Indicators");

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
			.SetDisplay("Stochastic %D", "%D smoothing length", "Indicators");

		_stochasticLevelUp = Param(nameof(StochasticLevelUp), 30m)
			.SetDisplay("Stochastic Oversold", "Oversold level", "Levels");

		_stochasticLevelDown = Param(nameof(StochasticLevelDown), 70m)
			.SetDisplay("Stochastic Overbought", "Overbought level", "Levels");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var maFast = new SimpleMovingAverage { Length = MaFastPeriod };
		var maSlow = new SimpleMovingAverage { Length = MaSlowPeriod };
		var stochastic = new StochasticOscillator();
		stochastic.K.Length = StochasticKPeriod;
		stochastic.D.Length = StochasticDPeriod;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(maFast, maSlow, stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, maFast);
			DrawIndicator(area, maSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maFastVal, IIndicatorValue maSlowVal, IIndicatorValue stochVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!maFastVal.IsFinal || !maSlowVal.IsFinal || !stochVal.IsFinal)
			return;

		if (!maFastVal.IsFormed || !maSlowVal.IsFormed || !stochVal.IsFormed)
			return;

		var fastMa = maFastVal.GetValue<decimal>();
		var slowMa = maSlowVal.GetValue<decimal>();

		var stoch = (StochasticOscillatorValue)stochVal;
		var stochK = stoch.K ?? 50m;
		var stochD = stoch.D ?? 50m;

		if (!_hasPrev)
		{
			_prevFastMa = fastMa;
			_prevSlowMa = slowMa;
			_prevStochK = stochK;
			_prevStochD = stochD;
			_hasPrev = true;
			return;
		}

		// MA crossover signals
		var maBuy = fastMa > slowMa;
		var maSell = fastMa < slowMa;

		// Stochastic signals
		var stoBuy = _prevStochK > _prevStochD && _prevStochD < StochasticLevelUp;
		var stoSell = _prevStochK < _prevStochD && _prevStochD > StochasticLevelDown;

		var openBuy = maBuy && stoBuy;
		var openSell = maSell && stoSell;
		var closeBuy = maSell || stoSell;
		var closeSell = maBuy || stoBuy;

		if (Position == 0)
		{
			if (openBuy && !openSell)
				BuyMarket();
			else if (openSell && !openBuy)
				SellMarket();
		}
		else if (Position > 0 && closeBuy)
		{
			SellMarket();
		}
		else if (Position < 0 && closeSell)
		{
			BuyMarket();
		}

		_prevFastMa = fastMa;
		_prevSlowMa = slowMa;
		_prevStochK = stochK;
		_prevStochD = stochD;
	}
}
