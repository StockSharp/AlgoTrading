namespace StockSharp.Samples.Strategies;

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
/// <summary>
/// Multi-indicator scalping strategy converted from the MQL HistoScalper expert advisor.
/// Combines eight histogram-style filters and requires signal agreement before trading.
/// </summary>
public class HistoScalperStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<bool> _allowPyramiding;
	private readonly StrategyParam<bool> _closeOnOppositeSignal;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPoints;

	private readonly StrategyParam<bool> _useIndicator1;
	private readonly StrategyParam<SignalDirections> _modeIndicator1;
	private readonly StrategyParam<int> _periodIndicator1;

	private readonly StrategyParam<bool> _useIndicator2;
	private readonly StrategyParam<SignalDirections> _modeIndicator2;
	private readonly StrategyParam<int> _periodIndicator2;
	private readonly StrategyParam<int> _atrAveragePeriod;
	private readonly StrategyParam<decimal> _atrPositiveThreshold;
	private readonly StrategyParam<decimal> _atrNegativeThreshold;

	private readonly StrategyParam<bool> _useIndicator3;
	private readonly StrategyParam<SignalDirections> _modeIndicator3;
	private readonly StrategyParam<int> _periodIndicator3;
	private readonly StrategyParam<decimal> _bollingerDeviation;

	private readonly StrategyParam<bool> _useIndicator4;
	private readonly StrategyParam<SignalDirections> _modeIndicator4;
	private readonly StrategyParam<int> _periodIndicator4;
	private readonly StrategyParam<decimal> _bullsThreshold;
	private readonly StrategyParam<decimal> _bearsThreshold;

	private readonly StrategyParam<bool> _useIndicator5;
	private readonly StrategyParam<SignalDirections> _modeIndicator5;
	private readonly StrategyParam<int> _periodIndicator5;
	private readonly StrategyParam<decimal> _cciBuyLevel;
	private readonly StrategyParam<decimal> _cciSellLevel;

	private readonly StrategyParam<bool> _useIndicator6;
	private readonly StrategyParam<SignalDirections> _modeIndicator6;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<decimal> _macdPositiveThreshold;
	private readonly StrategyParam<decimal> _macdNegativeThreshold;

	private readonly StrategyParam<bool> _useIndicator7;
	private readonly StrategyParam<SignalDirections> _modeIndicator7;
	private readonly StrategyParam<int> _periodIndicator7;
	private readonly StrategyParam<decimal> _rsiBuyLevel;
	private readonly StrategyParam<decimal> _rsiSellLevel;

	private readonly StrategyParam<bool> _useIndicator8;
	private readonly StrategyParam<SignalDirections> _modeIndicator8;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<decimal> _stochasticBuyLevel;
	private readonly StrategyParam<decimal> _stochasticSellLevel;

	private DirectionalIndex _dmi = null!;
	private AverageTrueRange _atr = null!;
	private SimpleMovingAverage _atrAverage = null!;
	private BollingerBands _bollinger = null!;
	private BullPower _bulls = null!;
	private BearPower _bears = null!;
	private CommodityChannelIndex _cci = null!;
	private MovingAverageConvergenceDivergence _macd = null!;
	private RelativeStrengthIndex _rsi = null!;
	private StochasticOscillator _stochastic = null!;

	private readonly int?[] _currentSignals = new int?[9];
	private readonly int?[] _previousSignals = new int?[9];

	public HistoScalperStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for calculations", "General");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Base volume for every new entry", "Orders");

		_allowPyramiding = Param(nameof(AllowPyramiding), true)
			.SetDisplay("Allow Pyramiding", "Enable additional entries in the same direction", "Orders");

		_closeOnOppositeSignal = Param(nameof(CloseOnOppositeSignal), true)
			.SetDisplay("Close On Opposite", "Exit when the combined signal flips", "Orders");

		_useTimeFilter = Param(nameof(UseTimeFilter), false)
			.SetDisplay("Use Time Filter", "Restrict trading to a daily session", "Timing");

		_sessionStart = Param(nameof(SessionStart), TimeSpan.Zero)
			.SetDisplay("Session Start", "Daily session start time", "Timing");

		_sessionEnd = Param(nameof(SessionEnd), TimeSpan.FromHours(23))
			.SetDisplay("Session End", "Daily session end time", "Timing");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", "Apply take profit after every entry", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Profit target in price steps", "Risk");

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Apply stop loss after every entry", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 80m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Loss limit in price steps", "Risk");

		_useIndicator1 = Param(nameof(UseIndicator1), true)
			.SetDisplay("Use ADX", "Enable directional index confirmation", "ADX");

		_modeIndicator1 = Param(nameof(ModeIndicator1), SignalDirections.Straight)
			.SetDisplay("ADX Mode", "Invert ADX decision if needed", "ADX");

		_periodIndicator1 = Param(nameof(PeriodIndicator1), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Directional index length", "ADX");

		_useIndicator2 = Param(nameof(UseIndicator2), true)
			.SetDisplay("Use ATR", "Enable ATR momentum filter", "ATR");

		_modeIndicator2 = Param(nameof(ModeIndicator2), SignalDirections.Straight)
			.SetDisplay("ATR Mode", "Invert ATR decision if needed", "ATR");

		_periodIndicator2 = Param(nameof(PeriodIndicator2), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Average True Range length", "ATR");

		_atrAveragePeriod = Param(nameof(AtrAveragePeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("ATR Average", "Smoothing window for ATR baseline", "ATR");

		_atrPositiveThreshold = Param(nameof(AtrPositiveThreshold), 25m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Positive %", "Percentage above baseline to trigger buys", "ATR");

		_atrNegativeThreshold = Param(nameof(AtrNegativeThreshold), 25m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Negative %", "Percentage below baseline to trigger sells", "ATR");

		_useIndicator3 = Param(nameof(UseIndicator3), true)
			.SetDisplay("Use Bollinger", "Enable Bollinger band breakout filter", "Bollinger");

		_modeIndicator3 = Param(nameof(ModeIndicator3), SignalDirections.Straight)
			.SetDisplay("Bollinger Mode", "Invert Bollinger breakout direction", "Bollinger");

		_periodIndicator3 = Param(nameof(PeriodIndicator3), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Bollinger band calculation period", "Bollinger");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Band width multiplier", "Bollinger");

		_useIndicator4 = Param(nameof(UseIndicator4), true)
			.SetDisplay("Use Bulls/Bears", "Enable bulls/bears power filter", "Power");

		_modeIndicator4 = Param(nameof(ModeIndicator4), SignalDirections.Straight)
			.SetDisplay("Power Mode", "Invert bulls/bears logic", "Power");

		_periodIndicator4 = Param(nameof(PeriodIndicator4), 14)
			.SetGreaterThanZero()
			.SetDisplay("Power Period", "EMA period for bulls/bears power", "Power");

		_bullsThreshold = Param(nameof(BullsThreshold), 0.0005m)
			.SetDisplay("Bulls Threshold", "Minimum bulls power for longs", "Power");

		_bearsThreshold = Param(nameof(BearsThreshold), 0.0005m)
			.SetDisplay("Bears Threshold", "Minimum bears power magnitude for shorts", "Power");

		_useIndicator5 = Param(nameof(UseIndicator5), true)
			.SetDisplay("Use CCI", "Enable CCI filter", "CCI");

		_modeIndicator5 = Param(nameof(ModeIndicator5), SignalDirections.Straight)
			.SetDisplay("CCI Mode", "Invert CCI levels", "CCI");

		_periodIndicator5 = Param(nameof(PeriodIndicator5), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Commodity Channel Index length", "CCI");

		_cciBuyLevel = Param(nameof(CciBuyLevel), -100m)
			.SetDisplay("CCI Buy Level", "Level that marks oversold territory", "CCI");

		_cciSellLevel = Param(nameof(CciSellLevel), 100m)
			.SetDisplay("CCI Sell Level", "Level that marks overbought territory", "CCI");

		_useIndicator6 = Param(nameof(UseIndicator6), true)
			.SetDisplay("Use MACD", "Enable MACD histogram filter", "MACD");

		_modeIndicator6 = Param(nameof(ModeIndicator6), SignalDirections.Straight)
			.SetDisplay("MACD Mode", "Invert MACD histogram", "MACD");

		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length", "MACD");

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length", "MACD");

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA length", "MACD");

		_macdPositiveThreshold = Param(nameof(MacdPositiveThreshold), 0.0003m)
			.SetDisplay("MACD Positive", "Histogram level for longs", "MACD");

		_macdNegativeThreshold = Param(nameof(MacdNegativeThreshold), 0.0003m)
			.SetDisplay("MACD Negative", "Histogram level for shorts", "MACD");

		_useIndicator7 = Param(nameof(UseIndicator7), true)
			.SetDisplay("Use RSI", "Enable RSI filter", "RSI");

		_modeIndicator7 = Param(nameof(ModeIndicator7), SignalDirections.Straight)
			.SetDisplay("RSI Mode", "Invert RSI logic", "RSI");

		_periodIndicator7 = Param(nameof(PeriodIndicator7), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI length", "RSI");

		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 30m)
			.SetDisplay("RSI Buy", "RSI threshold for longs", "RSI");

		_rsiSellLevel = Param(nameof(RsiSellLevel), 70m)
			.SetDisplay("RSI Sell", "RSI threshold for shorts", "RSI");

		_useIndicator8 = Param(nameof(UseIndicator8), true)
			.SetDisplay("Use Stochastic", "Enable stochastic oscillator filter", "Stochastic");

		_modeIndicator8 = Param(nameof(ModeIndicator8), SignalDirections.Straight)
			.SetDisplay("Stochastic Mode", "Invert stochastic logic", "Stochastic");

		_kPeriod = Param(nameof(KPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "Primary lookback for %K", "Stochastic");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "Smoothing period for %D", "Stochastic");

		_slowing = Param(nameof(Slowing), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Slowing", "Additional smoothing for %K", "Stochastic");

		_stochasticBuyLevel = Param(nameof(StochasticBuyLevel), 20m)
			.SetDisplay("Stochastic Buy", "Oversold threshold", "Stochastic");

		_stochasticSellLevel = Param(nameof(StochasticSellLevel), 80m)
			.SetDisplay("Stochastic Sell", "Overbought threshold", "Stochastic");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public bool AllowPyramiding
	{
		get => _allowPyramiding.Value;
		set => _allowPyramiding.Value = value;
	}

	public bool CloseOnOppositeSignal
	{
		get => _closeOnOppositeSignal.Value;
		set => _closeOnOppositeSignal.Value = value;
	}

	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	public TimeSpan SessionStart
	{
		get => _sessionStart.Value;
		set => _sessionStart.Value = value;
	}

	public TimeSpan SessionEnd
	{
		get => _sessionEnd.Value;
		set => _sessionEnd.Value = value;
	}

	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public bool UseIndicator1
	{
		get => _useIndicator1.Value;
		set => _useIndicator1.Value = value;
	}

	public SignalDirections ModeIndicator1
	{
		get => _modeIndicator1.Value;
		set => _modeIndicator1.Value = value;
	}

	public int PeriodIndicator1
	{
		get => _periodIndicator1.Value;
		set => _periodIndicator1.Value = value;
	}

	public bool UseIndicator2
	{
		get => _useIndicator2.Value;
		set => _useIndicator2.Value = value;
	}

	public SignalDirections ModeIndicator2
	{
		get => _modeIndicator2.Value;
		set => _modeIndicator2.Value = value;
	}

	public int PeriodIndicator2
	{
		get => _periodIndicator2.Value;
		set => _periodIndicator2.Value = value;
	}

	public int AtrAveragePeriod
	{
		get => _atrAveragePeriod.Value;
		set => _atrAveragePeriod.Value = value;
	}

	public decimal AtrPositiveThreshold
	{
		get => _atrPositiveThreshold.Value;
		set => _atrPositiveThreshold.Value = value;
	}

	public decimal AtrNegativeThreshold
	{
		get => _atrNegativeThreshold.Value;
		set => _atrNegativeThreshold.Value = value;
	}

	public bool UseIndicator3
	{
		get => _useIndicator3.Value;
		set => _useIndicator3.Value = value;
	}

	public SignalDirections ModeIndicator3
	{
		get => _modeIndicator3.Value;
		set => _modeIndicator3.Value = value;
	}

	public int PeriodIndicator3
	{
		get => _periodIndicator3.Value;
		set => _periodIndicator3.Value = value;
	}

	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	public bool UseIndicator4
	{
		get => _useIndicator4.Value;
		set => _useIndicator4.Value = value;
	}

	public SignalDirections ModeIndicator4
	{
		get => _modeIndicator4.Value;
		set => _modeIndicator4.Value = value;
	}

	public int PeriodIndicator4
	{
		get => _periodIndicator4.Value;
		set => _periodIndicator4.Value = value;
	}

	public decimal BullsThreshold
	{
		get => _bullsThreshold.Value;
		set => _bullsThreshold.Value = value;
	}

	public decimal BearsThreshold
	{
		get => _bearsThreshold.Value;
		set => _bearsThreshold.Value = value;
	}

	public bool UseIndicator5
	{
		get => _useIndicator5.Value;
		set => _useIndicator5.Value = value;
	}

	public SignalDirections ModeIndicator5
	{
		get => _modeIndicator5.Value;
		set => _modeIndicator5.Value = value;
	}

	public int PeriodIndicator5
	{
		get => _periodIndicator5.Value;
		set => _periodIndicator5.Value = value;
	}

	public decimal CciBuyLevel
	{
		get => _cciBuyLevel.Value;
		set => _cciBuyLevel.Value = value;
	}

	public decimal CciSellLevel
	{
		get => _cciSellLevel.Value;
		set => _cciSellLevel.Value = value;
	}

	public bool UseIndicator6
	{
		get => _useIndicator6.Value;
		set => _useIndicator6.Value = value;
	}

	public SignalDirections ModeIndicator6
	{
		get => _modeIndicator6.Value;
		set => _modeIndicator6.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	public decimal MacdPositiveThreshold
	{
		get => _macdPositiveThreshold.Value;
		set => _macdPositiveThreshold.Value = value;
	}

	public decimal MacdNegativeThreshold
	{
		get => _macdNegativeThreshold.Value;
		set => _macdNegativeThreshold.Value = value;
	}

	public bool UseIndicator7
	{
		get => _useIndicator7.Value;
		set => _useIndicator7.Value = value;
	}

	public SignalDirections ModeIndicator7
	{
		get => _modeIndicator7.Value;
		set => _modeIndicator7.Value = value;
	}

	public int PeriodIndicator7
	{
		get => _periodIndicator7.Value;
		set => _periodIndicator7.Value = value;
	}

	public decimal RsiBuyLevel
	{
		get => _rsiBuyLevel.Value;
		set => _rsiBuyLevel.Value = value;
	}

	public decimal RsiSellLevel
	{
		get => _rsiSellLevel.Value;
		set => _rsiSellLevel.Value = value;
	}

	public bool UseIndicator8
	{
		get => _useIndicator8.Value;
		set => _useIndicator8.Value = value;
	}

	public SignalDirections ModeIndicator8
	{
		get => _modeIndicator8.Value;
		set => _modeIndicator8.Value = value;
	}

	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}

	public decimal StochasticBuyLevel
	{
		get => _stochasticBuyLevel.Value;
		set => _stochasticBuyLevel.Value = value;
	}

	public decimal StochasticSellLevel
	{
		get => _stochasticSellLevel.Value;
		set => _stochasticSellLevel.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		Array.Clear(_currentSignals, 0, _currentSignals.Length);
		Array.Clear(_previousSignals, 0, _previousSignals.Length);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_dmi = new DirectionalIndex
		{
			Length = PeriodIndicator1
		};

		_atr = new AverageTrueRange
		{
			Length = PeriodIndicator2
		};

		_atrAverage = new SimpleMovingAverage
		{
			Length = AtrAveragePeriod
		};

		_bollinger = new BollingerBands
		{
			Length = PeriodIndicator3,
			Width = BollingerDeviation
		};

		_bulls = new BullPower
		{
			Length = PeriodIndicator4
		};

		_bears = new BearPower
		{
			Length = PeriodIndicator4
		};

		_cci = new CommodityChannelIndex
		{
			Length = PeriodIndicator5
		};

		_macd = new MovingAverageConvergenceDivergence
		{
			ShortMa = { Length = FastPeriod },
			LongMa = { Length = SlowPeriod },
			SignalMa = { Length = SignalPeriod }
		};

		_rsi = new RelativeStrengthIndex
		{
			Length = PeriodIndicator7
		};

		_stochastic = new StochasticOscillator
		{
			Length = KPeriod,
			K = { Length = Slowing },
			D = { Length = DPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_dmi, _atr, _bollinger, _bulls, _bears, _cci, _macd, _rsi, _stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger, "Bollinger Bands");
			DrawOwnTrades(area);
		}
	}

	// Process each finished candle and evaluate the indicator matrix.
	private void ProcessCandle(
	ICandleMessage candle,
	IIndicatorValue dmiValue,
	IIndicatorValue atrValue,
	IIndicatorValue bollingerValue,
	IIndicatorValue bullsValue,
	IIndicatorValue bearsValue,
	IIndicatorValue cciValue,
	IIndicatorValue macdValue,
	IIndicatorValue rsiValue,
	IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (UseTimeFilter && !IsWithinSession(candle.OpenTime.TimeOfDay))
		return;

		var newSignals = new int?[9];

		if (UseIndicator1)
		{
			if (!_dmi.IsFormed || !dmiValue.IsFinal)
			return;

			var dmiData = (DirectionalIndexValue)dmiValue;
			var signal = 0;

			if (dmiData.Plus is decimal plus && dmiData.Minus is decimal minus)
			{
				if (plus > minus)
				signal = 1;
				else if (plus < minus)
				signal = -1;
			}

			newSignals[1] = AdjustSignal(1, signal);
		}

		if (UseIndicator2)
		{
			if (!_atr.IsFormed || !atrValue.IsFinal)
			return;

			var atrDecimal = atrValue.ToDecimal();
			var averageValue = _atrAverage.Process(new DecimalIndicatorValue(_atrAverage, atrDecimal));

			if (!averageValue.IsFinal)
			return;

			var baseline = averageValue.GetValue<decimal>();
			if (baseline == 0m)
			baseline = atrDecimal;

			var percent = baseline == 0m ? 0m : (atrDecimal - baseline) / baseline * 100m;
			var signal = 0;

			if (percent >= AtrPositiveThreshold)
			signal = 1;
			else if (percent <= -AtrNegativeThreshold)
			signal = -1;

			newSignals[2] = AdjustSignal(2, signal);
		}

		if (UseIndicator3)
		{
			if (!_bollinger.IsFormed || !bollingerValue.IsFinal)
			return;

			var bands = (BollingerBandsValue)bollingerValue;
			if (bands.UpBand is not decimal upper || bands.LowBand is not decimal lower || bands.MovingAverage is not decimal middle)
			return;

			var close = candle.ClosePrice;
			var signal = 0;

			if (close > upper)
			signal = 1;
			else if (close < lower)
			signal = -1;

			newSignals[3] = AdjustSignal(3, signal);
		}

		if (UseIndicator4)
		{
			if (!_bulls.IsFormed || !bullsValue.IsFinal || !_bears.IsFormed || !bearsValue.IsFinal)
			return;

			var bulls = bullsValue.ToDecimal();
			var bears = bearsValue.ToDecimal();
			var signal = 0;

			if (bulls >= BullsThreshold)
			signal = 1;
			else if (Math.Abs(bears) >= BearsThreshold)
			signal = -1;

			newSignals[4] = AdjustSignal(4, signal);
		}

		if (UseIndicator5)
		{
			if (!_cci.IsFormed || !cciValue.IsFinal)
			return;

			var cci = cciValue.ToDecimal();
			var signal = 0;

			if (cci <= CciBuyLevel)
			signal = 1;
			else if (cci >= CciSellLevel)
			signal = -1;

			newSignals[5] = AdjustSignal(5, signal);
		}

		if (UseIndicator6)
		{
			if (!_macd.IsFormed || !macdValue.IsFinal)
			return;

			var macdData = (MovingAverageConvergenceDivergenceValue)macdValue;
			if (macdData.Macd is not decimal macdLine || macdData.Signal is not decimal signalLine)
			return;

			var histogram = macdLine - signalLine;
			var signal = 0;

			if (histogram >= MacdPositiveThreshold)
			signal = 1;
			else if (histogram <= -MacdNegativeThreshold)
			signal = -1;

			newSignals[6] = AdjustSignal(6, signal);
		}

		if (UseIndicator7)
		{
			if (!_rsi.IsFormed || !rsiValue.IsFinal)
			return;

			var rsi = rsiValue.ToDecimal();
			var signal = 0;

			if (rsi <= RsiBuyLevel)
			signal = 1;
			else if (rsi >= RsiSellLevel)
			signal = -1;

			newSignals[7] = AdjustSignal(7, signal);
		}

		if (UseIndicator8)
		{
			if (!_stochastic.IsFormed || !stochasticValue.IsFinal)
			return;

			var stoch = (StochasticOscillatorValue)stochasticValue;
			var k = stoch.K;
			var signal = 0;

			if (k <= StochasticBuyLevel)
			signal = 1;
			else if (k >= StochasticSellLevel)
			signal = -1;

			newSignals[8] = AdjustSignal(8, signal);
		}

		var buySignal = EvaluateBuy(newSignals);
		var sellSignal = EvaluateSell(newSignals);

		if (CloseOnOppositeSignal)
		{
			if (sellSignal && Position > 0)
			{
				SellMarket(Position);
			}
			else if (buySignal && Position < 0)
			{
				BuyMarket(Math.Abs(Position));
			}
		}

		if (buySignal)
		TryEnterLong(candle.ClosePrice);
		else if (sellSignal)
		TryEnterShort(candle.ClosePrice);

		for (var i = 1; i <= 8; i++)
		{
			_previousSignals[i] = _currentSignals[i];
			_currentSignals[i] = newSignals[i];
		}
	}

	private bool EvaluateBuy(int?[] signals)
	{
		var hasOppositeHistory = false;
		for (var i = 1; i <= 8; i++)
		{
			if (!IsIndicatorEnabled(i))
			continue;

			if (signals[i] != 1)
			return false;

			if (_previousSignals[i] == -1)
			hasOppositeHistory = true;
		}

		return hasOppositeHistory;
	}

	private bool EvaluateSell(int?[] signals)
	{
		var hasOppositeHistory = false;
		for (var i = 1; i <= 8; i++)
		{
			if (!IsIndicatorEnabled(i))
			continue;

			if (signals[i] != -1)
			return false;

			if (_previousSignals[i] == 1)
			hasOppositeHistory = true;
		}

		return hasOppositeHistory;
	}

	// Open or extend a long position if the signal allows.
	private void TryEnterLong(decimal referencePrice)
	{
		if (Position > 0 && !AllowPyramiding)
		return;

		var requiredVolume = TradeVolume;
		if (Position < 0)
		requiredVolume += Math.Abs(Position);

		if (requiredVolume <= 0m)
		return;

		var resultingPosition = Position + requiredVolume;
		BuyMarket(requiredVolume);

		ApplyRiskManagement(resultingPosition, referencePrice);
	}

	// Open or extend a short position if the signal allows.
	private void TryEnterShort(decimal referencePrice)
	{
		if (Position < 0 && !AllowPyramiding)
		return;

		var requiredVolume = TradeVolume;
		if (Position > 0)
		requiredVolume += Math.Abs(Position);

		if (requiredVolume <= 0m)
		return;

		var resultingPosition = Position - requiredVolume;
		SellMarket(requiredVolume);

		ApplyRiskManagement(resultingPosition, referencePrice);
	}

	// Apply stop-loss and take-profit in price steps around the new position.
	private void ApplyRiskManagement(decimal resultingPosition, decimal referencePrice)
	{
		if (resultingPosition > 0)
		{
			if (UseTakeProfit && TakeProfitPoints > 0)
			SetTakeProfit(TakeProfitPoints, referencePrice, resultingPosition);

			if (UseStopLoss && StopLossPoints > 0)
			SetStopLoss(StopLossPoints, referencePrice, resultingPosition);
		}
		else if (resultingPosition < 0)
		{
			if (UseTakeProfit && TakeProfitPoints > 0)
			SetTakeProfit(TakeProfitPoints, referencePrice, resultingPosition);

			if (UseStopLoss && StopLossPoints > 0)
			SetStopLoss(StopLossPoints, referencePrice, resultingPosition);
		}
	}

	private bool IsIndicatorEnabled(int index)
	{
		return index switch
		{
			1 => UseIndicator1,
			2 => UseIndicator2,
			3 => UseIndicator3,
			4 => UseIndicator4,
			5 => UseIndicator5,
			6 => UseIndicator6,
			7 => UseIndicator7,
			8 => UseIndicator8,
			_ => false,
		};
	}

	private int? AdjustSignal(int index, int signal)
	{
		if (signal == 0)
		return 0;

		var mode = index switch
		{
			1 => ModeIndicator1,
			2 => ModeIndicator2,
			3 => ModeIndicator3,
			4 => ModeIndicator4,
			5 => ModeIndicator5,
			6 => ModeIndicator6,
			7 => ModeIndicator7,
			8 => ModeIndicator8,
			_ => SignalDirections.Straight,
		};

		return mode == SignalDirections.Reverse ? -signal : signal;
	}

	private bool IsWithinSession(TimeSpan time)
	{
		if (SessionStart == SessionEnd)
		return true;

		if (SessionStart < SessionEnd)
		return time >= SessionStart && time < SessionEnd;

		return time >= SessionStart || time < SessionEnd;
	}

	/// <summary>
	/// Defines how indicator signals are interpreted.
	/// </summary>
	public enum SignalDirections
	{
		Straight,
		Reverse,
	}
}
