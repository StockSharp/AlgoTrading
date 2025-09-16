using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-indicator strategy inspired by the VirtualTradePad panel.
/// Aggregates signals from momentum, trend and channel indicators to trade when a configurable number of signals align.
/// </summary>
public class VirtualTradePadSignalStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticDLength;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _cciLength;
	private readonly StrategyParam<int> _williamsLength;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _envelopeLength;
	private readonly StrategyParam<decimal> _envelopeDeviation;
	private readonly StrategyParam<int> _alligatorJawLength;
	private readonly StrategyParam<int> _alligatorTeethLength;
	private readonly StrategyParam<int> _alligatorLipsLength;
	private readonly StrategyParam<int> _amaLength;
	private readonly StrategyParam<int> _amaFastPeriod;
	private readonly StrategyParam<int> _amaSlowPeriod;
	private readonly StrategyParam<int> _ichimokuTenkanLength;
	private readonly StrategyParam<int> _ichimokuKijunLength;
	private readonly StrategyParam<int> _ichimokuSenkouLength;
	private readonly StrategyParam<int> _aoShortPeriod;
	private readonly StrategyParam<int> _aoLongPeriod;
	private readonly StrategyParam<int> _minimumConfirmations;
	private readonly StrategyParam<bool> _allowLong;
	private readonly StrategyParam<bool> _allowShort;
	private readonly StrategyParam<bool> _closeOnOpposite;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastMa = null!;
	private SimpleMovingAverage _slowMa = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private StochasticOscillator _stochastic = null!;
	private RelativeStrengthIndex _rsi = null!;
	private CommodityChannelIndex _cci = null!;
	private WilliamsR _williams = null!;
	private BollingerBands _bollinger = null!;
	private SimpleMovingAverage _envelopeMa = null!;
	private SmoothedMovingAverage _alligatorJaw = null!;
	private SmoothedMovingAverage _alligatorTeeth = null!;
	private SmoothedMovingAverage _alligatorLips = null!;
	private KaufmanAdaptiveMovingAverage _ama = null!;
	private AwesomeOscillator _awesome = null!;
	private Ichimoku _ichimoku = null!;

	private decimal? _prevFastMa;
	private decimal? _prevSlowMa;
	private decimal? _prevMacd;
	private decimal? _prevMacdSignal;
	private decimal? _prevStochastic;
	private decimal? _prevRsi;
	private decimal? _prevCci;
	private decimal? _prevWpr;
	private decimal? _prevClose;
	private decimal? _prevAma;
	private decimal? _prevAo;
	private decimal? _prevBollingerUpper;
	private decimal? _prevBollingerLower;
	private decimal? _prevEnvelopeUpper;
	private decimal? _prevEnvelopeLower;
	private decimal? _prevTenkan;
	private decimal? _prevKijun;
	private decimal? _entryPrice;

	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int FastMaLength { get => _fastMaLength.Value; set => _fastMaLength.Value = value; }

	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int SlowMaLength { get => _slowMaLength.Value; set => _slowMaLength.Value = value; }

	/// <summary>
	/// MACD fast length.
	/// </summary>
	public int MacdFastLength { get => _macdFastLength.Value; set => _macdFastLength.Value = value; }

	/// <summary>
	/// MACD slow length.
	/// </summary>
	public int MacdSlowLength { get => _macdSlowLength.Value; set => _macdSlowLength.Value = value; }

	/// <summary>
	/// MACD signal length.
	/// </summary>
	public int MacdSignalLength { get => _macdSignalLength.Value; set => _macdSignalLength.Value = value; }

	/// <summary>
	/// Stochastic base length.
	/// </summary>
	public int StochasticLength { get => _stochasticLength.Value; set => _stochasticLength.Value = value; }

	/// <summary>
	/// Stochastic %D length.
	/// </summary>
	public int StochasticDLength { get => _stochasticDLength.Value; set => _stochasticDLength.Value = value; }

	/// <summary>
	/// Stochastic slowing.
	/// </summary>
	public int StochasticSlowing { get => _stochasticSlowing.Value; set => _stochasticSlowing.Value = value; }

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// CCI length.
	/// </summary>
	public int CciLength { get => _cciLength.Value; set => _cciLength.Value = value; }

	/// <summary>
	/// Williams %R length.
	/// </summary>
	public int WilliamsLength { get => _williamsLength.Value; set => _williamsLength.Value = value; }

	/// <summary>
	/// Bollinger period.
	/// </summary>
	public int BollingerLength { get => _bollingerLength.Value; set => _bollingerLength.Value = value; }

	/// <summary>
	/// Bollinger deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation { get => _bollingerDeviation.Value; set => _bollingerDeviation.Value = value; }

	/// <summary>
	/// Envelope moving average length.
	/// </summary>
	public int EnvelopeLength { get => _envelopeLength.Value; set => _envelopeLength.Value = value; }

	/// <summary>
	/// Envelope deviation in percent.
	/// </summary>
	public decimal EnvelopeDeviation { get => _envelopeDeviation.Value; set => _envelopeDeviation.Value = value; }

	/// <summary>
	/// Alligator jaw length.
	/// </summary>
	public int AlligatorJawLength { get => _alligatorJawLength.Value; set => _alligatorJawLength.Value = value; }

	/// <summary>
	/// Alligator teeth length.
	/// </summary>
	public int AlligatorTeethLength { get => _alligatorTeethLength.Value; set => _alligatorTeethLength.Value = value; }

	/// <summary>
	/// Alligator lips length.
	/// </summary>
	public int AlligatorLipsLength { get => _alligatorLipsLength.Value; set => _alligatorLipsLength.Value = value; }

	/// <summary>
	/// Adaptive moving average length.
	/// </summary>
	public int AmaLength { get => _amaLength.Value; set => _amaLength.Value = value; }

	/// <summary>
	/// Adaptive moving average fast period.
	/// </summary>
	public int AmaFastPeriod { get => _amaFastPeriod.Value; set => _amaFastPeriod.Value = value; }

	/// <summary>
	/// Adaptive moving average slow period.
	/// </summary>
	public int AmaSlowPeriod { get => _amaSlowPeriod.Value; set => _amaSlowPeriod.Value = value; }

	/// <summary>
	/// Ichimoku Tenkan length.
	/// </summary>
	public int IchimokuTenkanLength { get => _ichimokuTenkanLength.Value; set => _ichimokuTenkanLength.Value = value; }

	/// <summary>
	/// Ichimoku Kijun length.
	/// </summary>
	public int IchimokuKijunLength { get => _ichimokuKijunLength.Value; set => _ichimokuKijunLength.Value = value; }

	/// <summary>
	/// Ichimoku Senkou span B length.
	/// </summary>
	public int IchimokuSenkouLength { get => _ichimokuSenkouLength.Value; set => _ichimokuSenkouLength.Value = value; }

	/// <summary>
	/// Awesome oscillator short period.
	/// </summary>
	public int AoShortPeriod { get => _aoShortPeriod.Value; set => _aoShortPeriod.Value = value; }

	/// <summary>
	/// Awesome oscillator long period.
	/// </summary>
	public int AoLongPeriod { get => _aoLongPeriod.Value; set => _aoLongPeriod.Value = value; }

	/// <summary>
	/// Minimum aligned signals required for entry.
	/// </summary>
	public int MinimumConfirmations { get => _minimumConfirmations.Value; set => _minimumConfirmations.Value = value; }

	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool AllowLong { get => _allowLong.Value; set => _allowLong.Value = value; }

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool AllowShort { get => _allowShort.Value; set => _allowShort.Value = value; }

	/// <summary>
	/// Close current position when the opposite count meets the confirmation threshold.
	/// </summary>
	public bool CloseOnOpposite { get => _closeOnOpposite.Value; set => _closeOnOpposite.Value = value; }

	/// <summary>
	/// Take profit size in price steps. Zero disables the target.
	/// </summary>
	public decimal TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }

	/// <summary>
	/// Stop loss size in price steps. Zero disables the stop.
	/// </summary>
	public decimal StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="VirtualTradePadSignalStrategy"/>.
	/// </summary>
	public VirtualTradePadSignalStrategy()
	{
		_fastMaLength = Param(nameof(FastMaLength), 8)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA", "Length of the fast moving average", "Trend")
		.SetCanOptimize();

		_slowMaLength = Param(nameof(SlowMaLength), 16)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA", "Length of the slow moving average", "Trend")
		.SetCanOptimize();

		_macdFastLength = Param(nameof(MacdFastLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA length for MACD", "Momentum");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA length for MACD", "Momentum");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal line length", "Momentum");

		_stochasticLength = Param(nameof(StochasticLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %K", "Base lookback for Stochastic", "Oscillators");

		_stochasticDLength = Param(nameof(StochasticDLength), 3)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %D", "%D smoothing length", "Oscillators");

		_stochasticSlowing = Param(nameof(StochasticSlowing), 3)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic Slowing", "Additional smoothing for %K", "Oscillators");

		_rsiLength = Param(nameof(RsiLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Length", "Relative Strength Index length", "Oscillators");

		_cciLength = Param(nameof(CciLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("CCI Length", "Commodity Channel Index length", "Oscillators");

		_williamsLength = Param(nameof(WilliamsLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Williams %R", "Williams %R length", "Oscillators");

		_bollingerLength = Param(nameof(BollingerLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Length", "Number of periods for Bollinger Bands", "Channels");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Deviation", "Deviation multiplier for Bollinger Bands", "Channels");

		_envelopeLength = Param(nameof(EnvelopeLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Envelope Length", "Moving average length for envelopes", "Channels");

		_envelopeDeviation = Param(nameof(EnvelopeDeviation), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Envelope Deviation", "Envelope width in percent", "Channels");

		_alligatorJawLength = Param(nameof(AlligatorJawLength), 13)
		.SetGreaterThanZero()
		.SetDisplay("Jaw Length", "Alligator jaw SMMA length", "Alligator");

		_alligatorTeethLength = Param(nameof(AlligatorTeethLength), 8)
		.SetGreaterThanZero()
		.SetDisplay("Teeth Length", "Alligator teeth SMMA length", "Alligator");

		_alligatorLipsLength = Param(nameof(AlligatorLipsLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Lips Length", "Alligator lips SMMA length", "Alligator");

		_amaLength = Param(nameof(AmaLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("AMA Length", "Adaptive moving average length", "Trend");

		_amaFastPeriod = Param(nameof(AmaFastPeriod), 2)
		.SetGreaterThanZero()
		.SetDisplay("AMA Fast", "Fast adaptive period", "Trend");

		_amaSlowPeriod = Param(nameof(AmaSlowPeriod), 30)
		.SetGreaterThanZero()
		.SetDisplay("AMA Slow", "Slow adaptive period", "Trend");

		_ichimokuTenkanLength = Param(nameof(IchimokuTenkanLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("Tenkan", "Ichimoku Tenkan length", "Ichimoku");

		_ichimokuKijunLength = Param(nameof(IchimokuKijunLength), 26)
		.SetGreaterThanZero()
		.SetDisplay("Kijun", "Ichimoku Kijun length", "Ichimoku");

		_ichimokuSenkouLength = Param(nameof(IchimokuSenkouLength), 52)
		.SetGreaterThanZero()
		.SetDisplay("Senkou B", "Ichimoku Senkou span B length", "Ichimoku");

		_aoShortPeriod = Param(nameof(AoShortPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("AO Short", "Awesome oscillator short period", "Momentum");

		_aoLongPeriod = Param(nameof(AoLongPeriod), 34)
		.SetGreaterThanZero()
		.SetDisplay("AO Long", "Awesome oscillator long period", "Momentum");

		_minimumConfirmations = Param(nameof(MinimumConfirmations), 4)
		.SetGreaterThanZero()
		.SetDisplay("Confirmations", "Minimum aligned signals for entry", "Logic");

		_allowLong = Param(nameof(AllowLong), true)
		.SetDisplay("Allow Long", "Enable long trades", "Logic");

		_allowShort = Param(nameof(AllowShort), true)
		.SetDisplay("Allow Short", "Enable short trades", "Logic");

		_closeOnOpposite = Param(nameof(CloseOnOpposite), true)
		.SetDisplay("Close Opposite", "Exit when opposite signals align", "Logic");

		_takeProfitPips = Param(nameof(TakeProfitPips), 0m)
		.SetDisplay("Take Profit", "Take profit in price steps (0 disables)", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 0m)
		.SetDisplay("Stop Loss", "Stop loss in price steps (0 disables)", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles for analysis", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new SimpleMovingAverage { Length = FastMaLength };
		_slowMa = new SimpleMovingAverage { Length = SlowMaLength };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength }
			},
			SignalMa = { Length = MacdSignalLength }
		};

		_stochastic = new StochasticOscillator
		{
			Length = StochasticLength,
			K = { Length = StochasticSlowing },
			D = { Length = StochasticDLength }
		};

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_cci = new CommodityChannelIndex { Length = CciLength };
		_williams = new WilliamsR { Length = WilliamsLength };
		_bollinger = new BollingerBands { Length = BollingerLength, Width = BollingerDeviation };
		_envelopeMa = new SimpleMovingAverage { Length = EnvelopeLength };
		_alligatorJaw = new SmoothedMovingAverage { Length = AlligatorJawLength };
		_alligatorTeeth = new SmoothedMovingAverage { Length = AlligatorTeethLength };
		_alligatorLips = new SmoothedMovingAverage { Length = AlligatorLipsLength };
		_ama = new KaufmanAdaptiveMovingAverage { Length = AmaLength, FastSCPeriod = AmaFastPeriod, SlowSCPeriod = AmaSlowPeriod };
		_awesome = new AwesomeOscillator { ShortPeriod = AoShortPeriod, LongPeriod = AoLongPeriod };
		_ichimoku = new Ichimoku
		{
			Tenkan = { Length = IchimokuTenkanLength },
			Kijun = { Length = IchimokuKijunLength },
			SenkouB = { Length = IchimokuSenkouLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_fastMa, _slowMa, _macd, _stochastic, _rsi, _cci, _williams, _bollinger, _ama, _awesome, _ichimoku, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue fastMaValue,
		IIndicatorValue slowMaValue,
		IIndicatorValue macdValue,
		IIndicatorValue stochasticValue,
		IIndicatorValue rsiValue,
		IIndicatorValue cciValue,
		IIndicatorValue williamsValue,
		IIndicatorValue bollingerValue,
		IIndicatorValue amaValue,
		IIndicatorValue awesomeValue,
		IIndicatorValue ichimokuValue)
		{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!fastMaValue.IsFinal || !slowMaValue.IsFinal || !macdValue.IsFinal || !stochasticValue.IsFinal || !rsiValue.IsFinal ||
		!cciValue.IsFinal || !williamsValue.IsFinal || !bollingerValue.IsFinal || !amaValue.IsFinal || !awesomeValue.IsFinal ||
		!ichimokuValue.IsFinal)
		{
		return;
		}

		var fastMa = fastMaValue.ToDecimal();
		var slowMa = slowMaValue.ToDecimal();

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal macdSignal)
		return;

		var stochTyped = (StochasticOscillatorValue)stochasticValue;
		if (stochTyped.K is not decimal stochK)
		return;

		var rsi = rsiValue.ToDecimal();
		var cci = cciValue.ToDecimal();
		var wpr = williamsValue.ToDecimal();

		var bbTyped = (BollingerBandsValue)bollingerValue;
		if (bbTyped.UpBand is not decimal bbUpper || bbTyped.LowBand is not decimal bbLower)
		return;

		var ama = amaValue.ToDecimal();
		var ao = awesomeValue.ToDecimal();

		var ichimokuTyped = (IchimokuValue)ichimokuValue;
		if (ichimokuTyped.Tenkan is not decimal tenkan || ichimokuTyped.Kijun is not decimal kijun)
		return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var jawValue = _alligatorJaw.Process(median);
		var teethValue = _alligatorTeeth.Process(median);
		var lipsValue = _alligatorLips.Process(median);

		if (!jawValue.IsFinal || !teethValue.IsFinal || !lipsValue.IsFinal)
		return;

		var jaw = jawValue.GetValue<decimal>();
		var teeth = teethValue.GetValue<decimal>();
		var lips = lipsValue.GetValue<decimal>();

		var envelopeValue = _envelopeMa.Process(candle.ClosePrice);
		if (!envelopeValue.IsFinal)
		return;

		var envelopeMid = envelopeValue.GetValue<decimal>();
		var envelopeFactor = 1m + EnvelopeDeviation / 100m;
		var envelopeUpper = envelopeMid * envelopeFactor;
		var envelopeLower = envelopeMid / envelopeFactor;

		var maSignal = GetMaSignal(fastMa, slowMa);
		var macdSignalValue = GetMacdSignal(macd, macdSignal);
		var stochSignal = GetThresholdSignal(stochK, _prevStochastic, 20m, 80m);
		var rsiSignal = GetThresholdSignal(rsi, _prevRsi, 30m, 70m);
		var cciSignal = GetThresholdSignal(cci, _prevCci, -100m, 100m);
		var wprSignal = GetThresholdSignal(wpr, _prevWpr, -80m, -20m);
		var bollingerSignal = GetBollingerSignal(candle.ClosePrice, bbUpper, bbLower);
		var envelopeSignal = GetEnvelopeSignal(candle.ClosePrice, envelopeUpper, envelopeLower);
		var alligatorSignal = GetAlligatorSignal(jaw, teeth, lips);
		var amaSignal = GetMomentumSignal(ama, ref _prevAma);
		var aoSignal = GetAoSignal(ao);
		var ichimokuSignal = GetIchimokuSignal(tenkan, kijun);

		_prevStochastic = stochK;
		_prevRsi = rsi;
		_prevCci = cci;
		_prevWpr = wpr;
		_prevClose = candle.ClosePrice;
		_prevBollingerUpper = bbUpper;
		_prevBollingerLower = bbLower;
		_prevEnvelopeUpper = envelopeUpper;
		_prevEnvelopeLower = envelopeLower;

		var buySignals = 0;
		var sellSignals = 0;

		CountSignal(maSignal, ref buySignals, ref sellSignals);
		CountSignal(macdSignalValue, ref buySignals, ref sellSignals);
		CountSignal(stochSignal, ref buySignals, ref sellSignals);
		CountSignal(rsiSignal, ref buySignals, ref sellSignals);
		CountSignal(cciSignal, ref buySignals, ref sellSignals);
		CountSignal(wprSignal, ref buySignals, ref sellSignals);
		CountSignal(bollingerSignal, ref buySignals, ref sellSignals);
		CountSignal(envelopeSignal, ref buySignals, ref sellSignals);
		CountSignal(alligatorSignal, ref buySignals, ref sellSignals);
		CountSignal(amaSignal, ref buySignals, ref sellSignals);
		CountSignal(aoSignal, ref buySignals, ref sellSignals);
		CountSignal(ichimokuSignal, ref buySignals, ref sellSignals);

		var longCondition = AllowLong && buySignals >= MinimumConfirmations && buySignals > sellSignals;
		var shortCondition = AllowShort && sellSignals >= MinimumConfirmations && sellSignals > buySignals;

		if (longCondition)
		{
		if (Position < 0)
		{
		BuyMarket(Math.Abs(Position));
		}

		if (Position <= 0)
		{
		BuyMarket(Volume);
		_entryPrice = candle.ClosePrice;
		}
		}
		else if (shortCondition)
		{
		if (Position > 0)
		{
		SellMarket(Position);
		}

		if (Position >= 0)
		{
		SellMarket(Volume);
		_entryPrice = candle.ClosePrice;
		}
		}
		else if (CloseOnOpposite && Position != 0)
		{
		if (Position > 0 && sellSignals >= MinimumConfirmations)
		{
		SellMarket(Position);
		ResetEntryState();
		}
		else if (Position < 0 && buySignals >= MinimumConfirmations)
		{
		BuyMarket(Math.Abs(Position));
		ResetEntryState();
		}
		}

		HandleRisk(candle);
		}

		private int GetMaSignal(decimal fast, decimal slow)
		{
		var signal = 0;
		if (_prevFastMa is decimal prevFast && _prevSlowMa is decimal prevSlow)
		{
		if (prevFast <= prevSlow && fast > slow)
		signal = 1;
		else if (prevFast >= prevSlow && fast < slow)
		signal = -1;
		}

		_prevFastMa = fast;
		_prevSlowMa = slow;
		return signal;
		}

		private int GetMacdSignal(decimal macd, decimal signalLine)
		{
		var signal = 0;
		if (_prevMacd is decimal prevMacd && _prevMacdSignal is decimal prevSignal)
		{
		if (prevMacd <= prevSignal && macd > signalLine)
		signal = 1;
		else if (prevMacd >= prevSignal && macd < signalLine)
		signal = -1;
		}

		_prevMacd = macd;
		_prevMacdSignal = signalLine;
		return signal;
		}

		private int GetThresholdSignal(decimal current, decimal? previous, decimal lower, decimal upper)
		{
		var signal = 0;
		if (previous is decimal prev)
		{
		if (prev <= lower && current > lower)
		signal = 1;
		else if (prev >= upper && current < upper)
		signal = -1;
		}

		return signal;
		}

		private int GetBollingerSignal(decimal close, decimal upper, decimal lower)
		{
		var signal = 0;
		if (_prevClose is decimal prevClose && _prevBollingerUpper is decimal prevUpper && _prevBollingerLower is decimal prevLower)
		{
		if (prevClose <= prevLower && close > lower)
		signal = 1;
		else if (prevClose >= prevUpper && close < upper)
		signal = -1;
		}

		return signal;
		}

		private int GetEnvelopeSignal(decimal close, decimal upper, decimal lower)
		{
		var signal = 0;
		if (_prevClose is decimal prevClose && _prevEnvelopeUpper is decimal prevUpper && _prevEnvelopeLower is decimal prevLower)
		{
		if (prevClose <= prevLower && close > lower)
		signal = 1;
		else if (prevClose >= prevUpper && close < upper)
		signal = -1;
		}

		return signal;
		}

		private static int GetAlligatorSignal(decimal jaw, decimal teeth, decimal lips)
		{
		if (lips > teeth && teeth > jaw)
		return 1;

		if (lips < teeth && teeth < jaw)
		return -1;

		return 0;
		}

		private int GetMomentumSignal(decimal current, ref decimal? previous)
		{
		var signal = 0;
		if (previous is decimal prev)
		{
		if (current > prev)
		signal = 1;
		else if (current < prev)
		signal = -1;
		}

		previous = current;
		return signal;
		}

		private int GetAoSignal(decimal ao)
		{
		var signal = 0;
		if (_prevAo is decimal prevAo)
		{
		if (prevAo <= 0m && ao > 0m)
		signal = 1;
		else if (prevAo >= 0m && ao < 0m)
		signal = -1;
		}

		_prevAo = ao;
		return signal;
		}

		private int GetIchimokuSignal(decimal tenkan, decimal kijun)
		{
		var signal = 0;
		if (_prevTenkan is decimal prevTenkan && _prevKijun is decimal prevKijun)
		{
		var wasAbove = prevTenkan > prevKijun;
		var isAbove = tenkan > kijun;
		if (!wasAbove && isAbove)
		signal = 1;
		else if (wasAbove && !isAbove)
		signal = -1;
		}
		else
		{
		if (tenkan > kijun)
		signal = 1;
		else if (tenkan < kijun)
		signal = -1;
		}

		_prevTenkan = tenkan;
		_prevKijun = kijun;
		return signal;
		}

		private static void CountSignal(int signal, ref int buySignals, ref int sellSignals)
		{
		if (signal > 0)
		buySignals++;
		else if (signal < 0)
		sellSignals++;
		}

		private void HandleRisk(ICandleMessage candle)
		{
		if (Position == 0)
		{
		ResetEntryState();
		return;
		}

		if (_entryPrice is null)
		_entryPrice = candle.ClosePrice;

		var priceStep = GetPriceStep();

		if (Position > 0)
		{
		if (TakeProfitPips > 0m)
		{
		var target = _entryPrice.Value + TakeProfitPips * priceStep;
		if (candle.HighPrice >= target)
		{
		SellMarket(Position);
		ResetEntryState();
		return;
		}
		}

		if (StopLossPips > 0m)
		{
		var stop = _entryPrice.Value - StopLossPips * priceStep;
		if (candle.LowPrice <= stop)
		{
		SellMarket(Position);
		ResetEntryState();
		}
		}
		}
		else if (Position < 0)
		{
		if (TakeProfitPips > 0m)
		{
		var target = _entryPrice.Value - TakeProfitPips * priceStep;
		if (candle.LowPrice <= target)
		{
		BuyMarket(Math.Abs(Position));
		ResetEntryState();
		return;
		}
		}

		if (StopLossPips > 0m)
		{
		var stop = _entryPrice.Value + StopLossPips * priceStep;
		if (candle.HighPrice >= stop)
		{
		BuyMarket(Math.Abs(Position));
		ResetEntryState();
		}
		}
		}
		}

		private void ResetEntryState()
		{
		_entryPrice = null;
		}

		private decimal GetPriceStep()
		{
		var step = Security?.PriceStep;
		return step is null or 0m ? 1m : step.Value;
		}
		}
