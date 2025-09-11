using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Intraday combo strategy combining Stoch RSI, MACD, Supertrend, Bollinger Bands and ADX filters.
/// </summary>
public class IntradayComboStrategyHHStrategy : Strategy
{
	private readonly StrategyParam<bool> _useStochRsi;
	private readonly StrategyParam<bool> _useMacd;
	private readonly StrategyParam<bool> _useSupertrend;
	private readonly StrategyParam<bool> _useBollinger;
	private readonly StrategyParam<bool> _useAdx;
	private readonly StrategyParam<bool> _useSltp;
	private readonly StrategyParam<decimal> _slPercent;
	private readonly StrategyParam<decimal> _tpPercent;
	private readonly StrategyParam<bool> _useCooldown;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<int> _minSignals;
	private readonly StrategyParam<int> _stochRsiPeriod;
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevMacdAbove;
	private bool _isFirstMacd = true;
	private int _barIndex;
	private int _lastLongBar = int.MinValue;
	private int _lastShortBar = int.MinValue;

	/// <summary>
	/// Enable Stochastic RSI condition.
	/// </summary>
	public bool UseStochRsi { get => _useStochRsi.Value; set => _useStochRsi.Value = value; }

	/// <summary>
	/// Enable MACD crossover condition.
	/// </summary>
	public bool UseMacd { get => _useMacd.Value; set => _useMacd.Value = value; }

	/// <summary>
	/// Enable Supertrend direction condition.
	/// </summary>
	public bool UseSupertrend { get => _useSupertrend.Value; set => _useSupertrend.Value = value; }

	/// <summary>
	/// Enable Bollinger Bands condition.
	/// </summary>
	public bool UseBollinger { get => _useBollinger.Value; set => _useBollinger.Value = value; }

	/// <summary>
	/// Enable ADX trend filter.
	/// </summary>
	public bool UseAdx { get => _useAdx.Value; set => _useAdx.Value = value; }

	/// <summary>
	/// Enable stop-loss and take-profit protection.
	/// </summary>
	public bool UseSltp { get => _useSltp.Value; set => _useSltp.Value = value; }

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal SlPercent { get => _slPercent.Value; set => _slPercent.Value = value; }

	/// <summary>
	/// Take-profit percentage.
	/// </summary>
	public decimal TpPercent { get => _tpPercent.Value; set => _tpPercent.Value = value; }

	/// <summary>
	/// Use cooldown after signals.
	/// </summary>
	public bool UseCooldown { get => _useCooldown.Value; set => _useCooldown.Value = value; }

	/// <summary>
	/// Bars to wait after a trade.
	/// </summary>
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	/// <summary>
	/// Minimum required number of conditions.
	/// </summary>
	public int MinSignals { get => _minSignals.Value; set => _minSignals.Value = value; }

	/// <summary>
	/// RSI period for Stoch RSI.
	/// </summary>
	public int StochRsiPeriod { get => _stochRsiPeriod.Value; set => _stochRsiPeriod.Value = value; }

	/// <summary>
	/// Stochastic K period.
	/// </summary>
	public int StochKPeriod { get => _stochKPeriod.Value; set => _stochKPeriod.Value = value; }

	/// <summary>
	/// Stochastic D period.
	/// </summary>
	public int StochDPeriod { get => _stochDPeriod.Value; set => _stochDPeriod.Value = value; }

	/// <summary>
	/// Stochastic RSI overbought level.
	/// </summary>
	public decimal StochOverbought { get => _stochOverbought.Value; set => _stochOverbought.Value = value; }

	/// <summary>
	/// Stochastic RSI oversold level.
	/// </summary>
	public decimal StochOversold { get => _stochOversold.Value; set => _stochOversold.Value = value; }

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }

	/// <summary>
	/// Signal period for MACD.
	/// </summary>
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }

	/// <summary>
	/// ATR period for Supertrend.
	/// </summary>
	public int SupertrendPeriod { get => _supertrendPeriod.Value; set => _supertrendPeriod.Value = value; }

	/// <summary>
	/// ATR multiplier for Supertrend.
	/// </summary>
	public decimal SupertrendMultiplier { get => _supertrendMultiplier.Value; set => _supertrendMultiplier.Value = value; }

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BbLength { get => _bbLength.Value; set => _bbLength.Value = value; }

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BbMultiplier { get => _bbMultiplier.Value; set => _bbMultiplier.Value = value; }

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }

	/// <summary>
	/// ADX threshold.
	/// </summary>
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }

	/// <summary>
	/// Candle type used for indicators.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public IntradayComboStrategyHHStrategy()
	{
		_useStochRsi = Param(nameof(UseStochRsi), true)
			.SetDisplay("Use Stoch RSI", "Enable Stochastic RSI condition", "Flags");
		_useMacd = Param(nameof(UseMacd), true)
			.SetDisplay("Use MACD", "Enable MACD condition", "Flags");
		_useSupertrend = Param(nameof(UseSupertrend), true)
			.SetDisplay("Use Supertrend", "Enable Supertrend condition", "Flags");
		_useBollinger = Param(nameof(UseBollinger), true)
			.SetDisplay("Use Bollinger Bands", "Enable Bollinger Bands condition", "Flags");
		_useAdx = Param(nameof(UseAdx), true)
			.SetDisplay("Use ADX", "Enable ADX trend filter", "Flags");
		_useSltp = Param(nameof(UseSltp), true)
			.SetDisplay("Use SL/TP", "Enable stop-loss and take-profit", "Risk");
		_slPercent = Param(nameof(SlPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop-loss percent", "Risk");
		_tpPercent = Param(nameof(TpPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take-profit percent", "Risk");
		_useCooldown = Param(nameof(UseCooldown), true)
			.SetDisplay("Use Cooldown", "Enable bar cooldown", "Flags");
		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Bars to wait after trade", "Flags");
		_minSignals = Param(nameof(MinSignals), 3)
			.SetGreaterThanZero()
			.SetDisplay("Min Signals", "Minimum required conditions", "Flags");
		_stochRsiPeriod = Param(nameof(StochRsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stoch RSI Period", "RSI period for Stoch RSI", "Stoch RSI");
		_stochKPeriod = Param(nameof(StochKPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stoch K", "K period", "Stoch RSI");
		_stochDPeriod = Param(nameof(StochDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stoch D", "D period", "Stoch RSI");
		_stochOverbought = Param(nameof(StochOverbought), 0.8m)
			.SetDisplay("Stoch Overbought", "Overbought level", "Stoch RSI");
		_stochOversold = Param(nameof(StochOversold), 0.2m)
			.SetDisplay("Stoch Oversold", "Oversold level", "Stoch RSI");
		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period", "MACD");
		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period", "MACD");
		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal period", "MACD");
		_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend Period", "ATR period", "Supertrend");
		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend Multiplier", "ATR multiplier", "Supertrend");
		_bbLength = Param(nameof(BbLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger period", "Bollinger");
		_bbMultiplier = Param(nameof(BbMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Multiplier", "Bollinger deviation multiplier", "Bollinger");
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "ADX calculation period", "ADX");
		_adxThreshold = Param(nameof(AdxThreshold), 20m)
			.SetGreaterThanZero()
			.SetDisplay("ADX Threshold", "Minimum ADX value", "ADX");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
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
		_prevMacdAbove = false;
		_isFirstMacd = true;
		_barIndex = 0;
		_lastLongBar = int.MinValue;
		_lastShortBar = int.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = StochRsiPeriod };
		var stoch = new StochasticOscillator
		{
			K = { Length = StochKPeriod },
			D = { Length = StochDPeriod }
		};
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};
		var supertrend = new SuperTrend { Length = SupertrendPeriod, Multiplier = SupertrendMultiplier };
		var bollinger = new BollingerBands { Length = BbLength, Width = BbMultiplier };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stoch, rsi, macd, supertrend, bollinger, adx, ProcessCandle)
			.Start();

	       if (UseSltp)
		       StartProtection(new Unit(TpPercent / 100m, UnitTypes.Percent), new Unit(SlPercent / 100m, UnitTypes.Percent), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stoch);
			DrawIndicator(area, macd);
			DrawIndicator(area, supertrend);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue stochValue,
		IIndicatorValue rsiValue,
		IIndicatorValue macdValue,
		IIndicatorValue supertrendValue,
		IIndicatorValue bollingerValue,
		IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_barIndex++;

		var buyConditions = 0;
		var sellConditions = 0;

		if (UseStochRsi)
		{
			var stochTyped = (StochasticOscillatorValue)stochValue;
			if (stochTyped.K is decimal k && stochTyped.D is decimal d)
			{
				if (k < StochOversold && k > d)
					buyConditions++;
				if (k > StochOverbought && k < d)
					sellConditions++;
			}
		}

		bool macdBuy = false;
		bool macdSell = false;
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdLine = macdTyped.Macd;
		var signalLine = macdTyped.Signal;
		var macdAbove = macdLine > signalLine;
		if (_isFirstMacd)
		{
			_prevMacdAbove = macdAbove;
			_isFirstMacd = false;
		}
		else
		{
			macdBuy = macdAbove && !_prevMacdAbove;
			macdSell = !macdAbove && _prevMacdAbove;
			_prevMacdAbove = macdAbove;
		}
		if (UseMacd)
		{
			if (macdBuy)
				buyConditions++;
			if (macdSell)
				sellConditions++;
		}

		if (UseSupertrend)
		{
			var st = (SuperTrendIndicatorValue)supertrendValue;
			if (st.IsUpTrend)
				buyConditions++;
			else
				sellConditions++;
		}

		if (UseBollinger)
		{
			var bb = (BollingerBandsValue)bollingerValue;
			if (candle.ClosePrice < bb.LowBand)
				buyConditions++;
			if (candle.ClosePrice > bb.UpBand)
				sellConditions++;
		}

		if (UseAdx)
		{
			var adxTyped = (AverageDirectionalIndexValue)adxValue;
			var adxTrendUp = adxTyped.Dx.Plus > adxTyped.Dx.Minus;
			var trendOk = adxTyped.MovingAverage > AdxThreshold && adxTrendUp;
			if (trendOk)
			{
				buyConditions++;
				sellConditions++;
			}
		}

		var cooldownReadyLong = !UseCooldown || _barIndex - _lastLongBar >= CooldownBars;
		var cooldownReadyShort = !UseCooldown || _barIndex - _lastShortBar >= CooldownBars;

		var buySignal = cooldownReadyLong && buyConditions >= MinSignals;
		var sellSignal = cooldownReadyShort && sellConditions >= MinSignals;

		if (buySignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_lastLongBar = _barIndex;
		}
		else if (sellSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_lastShortBar = _barIndex;
		}
	}
}

