namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Combines MACD, Stochastic Oscillator, and RSI filters.
/// All three indicators must agree on direction to enter a trade.
/// </summary>
public class ThreeIndicatorsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _signalCooldownBars;

	private decimal? _previousOpen;
	private decimal? _previousMacdMain;
	private int _previousCompositeSignal;
	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MacdFastPeriod { get => _macdFastPeriod.Value; set => _macdFastPeriod.Value = value; }
	public int MacdSlowPeriod { get => _macdSlowPeriod.Value; set => _macdSlowPeriod.Value = value; }
	public int MacdSignalPeriod { get => _macdSignalPeriod.Value; set => _macdSignalPeriod.Value = value; }
	public int StochasticKPeriod { get => _stochasticKPeriod.Value; set => _stochasticKPeriod.Value = value; }
	public int StochasticDPeriod { get => _stochasticDPeriod.Value; set => _stochasticDPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }

	public ThreeIndicatorsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe", "General");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 11)
			.SetDisplay("MACD fast", "Fast EMA length", "MACD");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 53)
			.SetDisplay("MACD slow", "Slow EMA length", "MACD");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 26)
			.SetDisplay("MACD signal", "Signal smoothing", "MACD");

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 40)
			.SetDisplay("Stochastic %K", "%K length", "Stochastic");

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 23)
			.SetDisplay("Stochastic %D", "%D smoothing", "Stochastic");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI period", "RSI length", "RSI");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 2)
			.SetNotNegative()
			.SetDisplay("Signal Cooldown Bars", "Closed candles to wait before a new entry", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousOpen = null;
		_previousMacdMain = null;
		_previousCompositeSignal = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousOpen = null;
		_previousMacdMain = null;
		_previousCompositeSignal = 0;
		_cooldownRemaining = 0;

		var macd = new MovingAverageConvergenceDivergenceSignal();
		macd.Macd.ShortMa.Length = MacdFastPeriod;
		macd.Macd.LongMa.Length = MacdSlowPeriod;
		macd.SignalMa.Length = MacdSignalPeriod;

		var stochastic = new StochasticOscillator();
		stochastic.K.Length = StochasticKPeriod;
		stochastic.D.Length = StochasticDPeriod;

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(macd, stochastic, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue stochValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal || !stochValue.IsFinal || !rsiValue.IsFinal)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		if (!macdValue.IsFormed || !stochValue.IsFormed || !rsiValue.IsFormed)
			return;

		var macdVal = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdMain = macdVal.Macd ?? 0m;

		var stoch = (StochasticOscillatorValue)stochValue;
		var stochasticD = stoch.D ?? 50m;

		var rsi = rsiValue.GetValue<decimal>();

		if (_previousOpen == null || _previousMacdMain == null)
		{
			_previousOpen = candle.OpenPrice;
			_previousMacdMain = macdMain;
			return;
		}

		// Candle direction
		var candleSignal = candle.OpenPrice > _previousOpen.Value ? 1 : candle.OpenPrice < _previousOpen.Value ? -1 : 0;

		// MACD direction (difference decreasing = bullish momentum)
		var macdDelta = macdMain - _previousMacdMain.Value;
		var macdSignal = macdDelta < 0m ? 1 : macdDelta > 0m ? -1 : 0;

		// Stochastic direction
		var stochSignal = stochasticD < 50m ? 1 : stochasticD > 50m ? -1 : 0;

		// RSI direction
		var rsiSignal = rsi < 50m ? 1 : rsi > 50m ? -1 : 0;

		var longSignal = candleSignal >= 0 && macdSignal >= 0 && stochSignal >= 0 && rsiSignal >= 0;
		var shortSignal = candleSignal <= 0 && macdSignal <= 0 && stochSignal <= 0 && rsiSignal <= 0;

		var compositeSignal = longSignal ? 1 : shortSignal ? -1 : 0;

		if (_cooldownRemaining == 0 && compositeSignal != 0 && compositeSignal != _previousCompositeSignal)
		{
			if (compositeSignal > 0 && Position <= 0)
			{
				BuyMarket(Volume + (Position < 0 ? -Position : 0m));
				_cooldownRemaining = SignalCooldownBars;
			}
			else if (compositeSignal < 0 && Position >= 0)
			{
				SellMarket(Volume + (Position > 0 ? Position : 0m));
				_cooldownRemaining = SignalCooldownBars;
			}
		}

		_previousOpen = candle.OpenPrice;
		_previousMacdMain = macdMain;
		_previousCompositeSignal = compositeSignal;
	}
}
