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
/// Conversion of the "Combo_EA4FSFrUpdated5" MetaTrader expert advisor.
/// The strategy combines moving averages, RSI, stochastic oscillator, parabolic SAR and zero-lag MACD confirmations.
/// All enabled modules must agree on the direction before a position is opened.
/// </summary>
public class ComboEa4FsfrUpdated5Strategy : Strategy
{
	private readonly StrategyParam<bool> _useMa;
	private readonly StrategyParam<MaSignalModes> _maMode;
	private readonly StrategyParam<int> _ma1Period;
	private readonly StrategyParam<int> _ma2Period;
	private readonly StrategyParam<int> _ma3Period;
	private readonly StrategyParam<int> _ma1BufferPeriod;
	private readonly StrategyParam<int> _ma2BufferPeriod;
	private readonly StrategyParam<MovingAverageMethods> _ma1Method;
	private readonly StrategyParam<MovingAverageMethods> _ma2Method;
	private readonly StrategyParam<MovingAverageMethods> _ma3Method;
	private readonly StrategyParam<AppliedPrices> _ma1Price;
	private readonly StrategyParam<AppliedPrices> _ma2Price;
	private readonly StrategyParam<AppliedPrices> _ma3Price;

	private readonly StrategyParam<bool> _useRsi;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<RsiSignalModes> _rsiMode;
	private readonly StrategyParam<decimal> _rsiBuyLevel;
	private readonly StrategyParam<decimal> _rsiSellLevel;
	private readonly StrategyParam<decimal> _rsiBuyZone;
	private readonly StrategyParam<decimal> _rsiSellZone;

	private readonly StrategyParam<bool> _useStochastic;
	private readonly StrategyParam<int> _stochasticK;
	private readonly StrategyParam<int> _stochasticD;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<bool> _useStochasticHighLow;
	private readonly StrategyParam<decimal> _stochasticHigh;
	private readonly StrategyParam<decimal> _stochasticLow;

	private readonly StrategyParam<bool> _useSar;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;

	private readonly StrategyParam<bool> _useMacd;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<AppliedPrices> _macdPrice;
	private readonly StrategyParam<MacdSignalModes> _macdMode;

	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStop;

	private readonly StrategyParam<bool> _useStaticVolume;
	private readonly StrategyParam<decimal> _staticVolume;
	private readonly StrategyParam<decimal> _riskPercent;

	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;

	private readonly StrategyParam<bool> _autoClose;
	private readonly StrategyParam<bool> _openOppositeAfterClose;
	private readonly StrategyParam<bool> _useMaClosing;
	private readonly StrategyParam<MaSignalModes> _maModeClosing;
	private readonly StrategyParam<bool> _useMacdClosing;
	private readonly StrategyParam<MacdSignalModes> _macdModeClosing;
	private readonly StrategyParam<bool> _useRsiClosing;
	private readonly StrategyParam<RsiSignalModes> _rsiModeClosing;
	private readonly StrategyParam<bool> _useStochasticClosing;
	private readonly StrategyParam<bool> _useSarClosing;

	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _ma1 = null!;
	private IIndicator _ma2 = null!;
	private IIndicator _ma3 = null!;
	private AverageTrueRange _atr = null!;
	private AverageTrueRange _ma1BufferAtr = null!;
	private AverageTrueRange _ma2BufferAtr = null!;
	private RelativeStrengthIndex _rsi = null!;
	private StochasticOscillator _stochastic = null!;
	private ParabolicSar _sar = null!;
	private ZeroLagExponentialMovingAverage _fastZlema = null!;
	private ZeroLagExponentialMovingAverage _slowZlema = null!;
	private ExponentialMovingAverage _macdEma1 = null!;
	private ExponentialMovingAverage _macdEma2 = null!;

	private decimal? _ma1Current;
	private decimal? _ma1Previous;
	private decimal? _ma2Current;
	private decimal? _ma2Previous;
	private decimal? _ma3Current;
	private decimal? _ma3Previous;
	private decimal? _ma1BufferValue;
	private decimal? _ma2BufferValue;
	private decimal? _atrValue;
	private decimal? _rsiCurrent;
	private decimal? _rsiPrevious;
	private decimal? _sarValue;
	private decimal? _stochasticValue;
	private decimal? _stochasticSignal;
	private decimal? _macdLineCurrent;
	private decimal? _macdLinePrevious;
	private decimal? _macdSignalCurrent;
	private decimal? _macdSignalPrevious;
	private decimal? _prevOpen;
	private decimal? _prevClose;

	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;
	private decimal? _pendingLongAtr;
	private decimal? _pendingShortAtr;
	private SignalDirections? _pendingOppositeEntry;
	private bool _longExitRequested;
	private bool _shortExitRequested;

	/// <summary>
	/// Enables moving average confirmation for entries.
	/// </summary>
	public bool UseMa
	{
		get => _useMa.Value;
		set => _useMa.Value = value;
	}

	/// <summary>
	/// Moving average confirmation mode.
	/// </summary>
	public MaSignalModes MaMode
	{
		get => _maMode.Value;
		set => _maMode.Value = value;
	}

	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int Ma1Period
	{
		get => _ma1Period.Value;
		set => _ma1Period.Value = value;
	}

	/// <summary>
	/// Medium moving average length.
	/// </summary>
	public int Ma2Period
	{
		get => _ma2Period.Value;
		set => _ma2Period.Value = value;
	}

	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int Ma3Period
	{
		get => _ma3Period.Value;
		set => _ma3Period.Value = value;
	}

	/// <summary>
	/// ATR period used as buffer for MA1 crossing logic.
	/// </summary>
	public int Ma1BufferPeriod
	{
		get => _ma1BufferPeriod.Value;
		set => _ma1BufferPeriod.Value = value;
	}

	/// <summary>
	/// ATR period used as buffer for MA2 crossing logic.
	/// </summary>
	public int Ma2BufferPeriod
	{
		get => _ma2BufferPeriod.Value;
		set => _ma2BufferPeriod.Value = value;
	}

	/// <summary>
	/// Method used for the fast moving average.
	/// </summary>
	public MovingAverageMethods Ma1Method
	{
		get => _ma1Method.Value;
		set => _ma1Method.Value = value;
	}

	/// <summary>
	/// Method used for the medium moving average.
	/// </summary>
	public MovingAverageMethods Ma2Method
	{
		get => _ma2Method.Value;
		set => _ma2Method.Value = value;
	}

	/// <summary>
	/// Method used for the slow moving average.
	/// </summary>
	public MovingAverageMethods Ma3Method
	{
		get => _ma3Method.Value;
		set => _ma3Method.Value = value;
	}

	/// <summary>
	/// Applied price for the fast moving average.
	/// </summary>
	public AppliedPrices Ma1Price
	{
		get => _ma1Price.Value;
		set => _ma1Price.Value = value;
	}

	/// <summary>
	/// Applied price for the medium moving average.
	/// </summary>
	public AppliedPrices Ma2Price
	{
		get => _ma2Price.Value;
		set => _ma2Price.Value = value;
	}

	/// <summary>
	/// Applied price for the slow moving average.
	/// </summary>
	public AppliedPrices Ma3Price
	{
		get => _ma3Price.Value;
		set => _ma3Price.Value = value;
	}

	/// <summary>
	/// Enables RSI confirmation for entries.
	/// </summary>
	public bool UseRsi
	{
		get => _useRsi.Value;
		set => _useRsi.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI confirmation mode.
	/// </summary>
	public RsiSignalModes RsiMode
	{
		get => _rsiMode.Value;
		set => _rsiMode.Value = value;
	}

	/// <summary>
	/// RSI buy threshold for overbought/oversold logic.
	/// </summary>
	public decimal RsiBuyLevel
	{
		get => _rsiBuyLevel.Value;
		set => _rsiBuyLevel.Value = value;
	}

	/// <summary>
	/// RSI sell threshold for overbought/oversold logic.
	/// </summary>
	public decimal RsiSellLevel
	{
		get => _rsiSellLevel.Value;
		set => _rsiSellLevel.Value = value;
	}

	/// <summary>
	/// RSI buy zone level used in zone confirmation.
	/// </summary>
	public decimal RsiBuyZone
	{
		get => _rsiBuyZone.Value;
		set => _rsiBuyZone.Value = value;
	}

	/// <summary>
	/// RSI sell zone level used in zone confirmation.
	/// </summary>
	public decimal RsiSellZone
	{
		get => _rsiSellZone.Value;
		set => _rsiSellZone.Value = value;
	}

	/// <summary>
	/// Enables stochastic oscillator confirmation for entries.
	/// </summary>
	public bool UseStochastic
	{
		get => _useStochastic.Value;
		set => _useStochastic.Value = value;
	}

	/// <summary>
	/// %K period of the stochastic oscillator.
	/// </summary>
	public int StochasticK
	{
		get => _stochasticK.Value;
		set => _stochasticK.Value = value;
	}

	/// <summary>
	/// %D period of the stochastic oscillator.
	/// </summary>
	public int StochasticD
	{
		get => _stochasticD.Value;
		set => _stochasticD.Value = value;
	}

	/// <summary>
	/// Slowing factor of the stochastic oscillator.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <summary>
	/// Enables stochastic high/low band filtering.
	/// </summary>
	public bool UseStochasticHighLow
	{
		get => _useStochasticHighLow.Value;
		set => _useStochasticHighLow.Value = value;
	}

	/// <summary>
	/// Upper stochastic threshold used when high/low filtering is enabled.
	/// </summary>
	public decimal StochasticHigh
	{
		get => _stochasticHigh.Value;
		set => _stochasticHigh.Value = value;
	}

	/// <summary>
	/// Lower stochastic threshold used when high/low filtering is enabled.
	/// </summary>
	public decimal StochasticLow
	{
		get => _stochasticLow.Value;
		set => _stochasticLow.Value = value;
	}

	/// <summary>
	/// Enables parabolic SAR confirmation for entries.
	/// </summary>
	public bool UseSar
	{
		get => _useSar.Value;
		set => _useSar.Value = value;
	}

	/// <summary>
	/// Parabolic SAR acceleration step.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum acceleration.
	/// </summary>
	public decimal SarMax
	{
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}

	/// <summary>
	/// Enables MACD confirmation for entries.
	/// </summary>
	public bool UseMacd
	{
		get => _useMacd.Value;
		set => _useMacd.Value = value;
	}

	/// <summary>
	/// Fast length of the zero-lag MACD.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow length of the zero-lag MACD.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Signal length of the zero-lag MACD.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Applied price used for the zero-lag MACD.
	/// </summary>
	public AppliedPrices MacdPrice
	{
		get => _macdPrice.Value;
		set => _macdPrice.Value = value;
	}

	/// <summary>
	/// MACD confirmation mode.
	/// </summary>
	public MacdSignalModes MacdMode
	{
		get => _macdMode.Value;
		set => _macdMode.Value = value;
	}

	/// <summary>
	/// Enables trailing stop management.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in points.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Switches the strategy to fixed volume mode.
	/// </summary>
	public bool UseStaticVolume
	{
		get => _useStaticVolume.Value;
		set => _useStaticVolume.Value = value;
	}

	/// <summary>
	/// Fixed trading volume.
	/// </summary>
	public decimal StaticVolume
	{
		get => _staticVolume.Value;
		set => _staticVolume.Value = value;
	}

	/// <summary>
	/// Risk percentage used when dynamic position sizing is enabled.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier used to derive stop-loss and take-profit distances.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Enables automatic closing logic.
	/// </summary>
	public bool AutoClose
	{
		get => _autoClose.Value;
		set => _autoClose.Value = value;
	}

	/// <summary>
	/// Automatically opens the opposite position after a signal based exit.
	/// </summary>
	public bool OpenOppositeAfterClose
	{
		get => _openOppositeAfterClose.Value;
		set => _openOppositeAfterClose.Value = value;
	}

	/// <summary>
	/// Enables moving average confirmation for exits.
	/// </summary>
	public bool UseMaClosing
	{
		get => _useMaClosing.Value;
		set => _useMaClosing.Value = value;
	}

	/// <summary>
	/// Moving average confirmation mode for exits.
	/// </summary>
	public MaSignalModes MaModeClosing
	{
		get => _maModeClosing.Value;
		set => _maModeClosing.Value = value;
	}

	/// <summary>
	/// Enables MACD confirmation for exits.
	/// </summary>
	public bool UseMacdClosing
	{
		get => _useMacdClosing.Value;
		set => _useMacdClosing.Value = value;
	}

	/// <summary>
	/// MACD confirmation mode for exits.
	/// </summary>
	public MacdSignalModes MacdModeClosing
	{
		get => _macdModeClosing.Value;
		set => _macdModeClosing.Value = value;
	}

	/// <summary>
	/// Enables RSI confirmation for exits.
	/// </summary>
	public bool UseRsiClosing
	{
		get => _useRsiClosing.Value;
		set => _useRsiClosing.Value = value;
	}

	/// <summary>
	/// RSI confirmation mode for exits.
	/// </summary>
	public RsiSignalModes RsiModeClosing
	{
		get => _rsiModeClosing.Value;
		set => _rsiModeClosing.Value = value;
	}

	/// <summary>
	/// Enables stochastic confirmation for exits.
	/// </summary>
	public bool UseStochasticClosing
	{
		get => _useStochasticClosing.Value;
		set => _useStochasticClosing.Value = value;
	}

	/// <summary>
	/// Enables parabolic SAR confirmation for exits.
	/// </summary>
	public bool UseSarClosing
	{
		get => _useSarClosing.Value;
		set => _useSarClosing.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ComboEa4FsfrUpdated5Strategy"/> class.
	/// </summary>
	public ComboEa4FsfrUpdated5Strategy()
	{
		_useMa = Param(nameof(UseMa), true).SetDisplay("Use MA", "Enable moving averages", "Entries");
		_maMode = Param(nameof(MaMode), MaSignalModes.AllCombined).SetDisplay("MA Mode", "Moving average mode", "Entries");
		_ma1Period = Param(nameof(Ma1Period), 5).SetDisplay("MA1 Period", "Fast MA length", "Indicators").SetCanOptimize(true);
		_ma2Period = Param(nameof(Ma2Period), 13).SetDisplay("MA2 Period", "Medium MA length", "Indicators").SetCanOptimize(true);
		_ma3Period = Param(nameof(Ma3Period), 62).SetDisplay("MA3 Period", "Slow MA length", "Indicators").SetCanOptimize(true);
		_ma1BufferPeriod = Param(nameof(Ma1BufferPeriod), 14).SetDisplay("MA1 Buffer", "ATR buffer for MA1", "Indicators").SetCanOptimize(true);
		_ma2BufferPeriod = Param(nameof(Ma2BufferPeriod), 14).SetDisplay("MA2 Buffer", "ATR buffer for MA2", "Indicators").SetCanOptimize(true);
		_ma1Method = Param(nameof(Ma1Method), MovingAverageMethods.Exponential).SetDisplay("MA1 Method", "Fast MA method", "Indicators");
		_ma2Method = Param(nameof(Ma2Method), MovingAverageMethods.Exponential).SetDisplay("MA2 Method", "Medium MA method", "Indicators");
		_ma3Method = Param(nameof(Ma3Method), MovingAverageMethods.Exponential).SetDisplay("MA3 Method", "Slow MA method", "Indicators");
		_ma1Price = Param(nameof(Ma1Price), AppliedPrices.Close).SetDisplay("MA1 Price", "Fast MA price", "Indicators");
		_ma2Price = Param(nameof(Ma2Price), AppliedPrices.Close).SetDisplay("MA2 Price", "Medium MA price", "Indicators");
		_ma3Price = Param(nameof(Ma3Price), AppliedPrices.Close).SetDisplay("MA3 Price", "Slow MA price", "Indicators");

		_useRsi = Param(nameof(UseRsi), true).SetDisplay("Use RSI", "Enable RSI", "Entries");
		_rsiPeriod = Param(nameof(RsiPeriod), 21).SetDisplay("RSI Period", "RSI length", "Indicators").SetCanOptimize(true);
		_rsiMode = Param(nameof(RsiMode), RsiSignalModes.OverboughtOversold).SetDisplay("RSI Mode", "RSI logic", "Entries");
		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 12m).SetDisplay("RSI Buy", "Oversold threshold", "Entries").SetCanOptimize(true);
		_rsiSellLevel = Param(nameof(RsiSellLevel), 88m).SetDisplay("RSI Sell", "Overbought threshold", "Entries").SetCanOptimize(true);
		_rsiBuyZone = Param(nameof(RsiBuyZone), 55m).SetDisplay("RSI Buy Zone", "Upper zone", "Entries");
		_rsiSellZone = Param(nameof(RsiSellZone), 45m).SetDisplay("RSI Sell Zone", "Lower zone", "Entries");

		_useStochastic = Param(nameof(UseStochastic), true).SetDisplay("Use Stochastic", "Enable stochastic", "Entries");
		_stochasticK = Param(nameof(StochasticK), 5).SetDisplay("Stochastic %K", "K period", "Indicators").SetCanOptimize(true);
		_stochasticD = Param(nameof(StochasticD), 3).SetDisplay("Stochastic %D", "D period", "Indicators").SetCanOptimize(true);
		_stochasticSlowing = Param(nameof(StochasticSlowing), 3).SetDisplay("Stochastic Slowing", "Slowing", "Indicators").SetCanOptimize(true);
		_useStochasticHighLow = Param(nameof(UseStochasticHighLow), false).SetDisplay("Use Stoch Bands", "Require bands", "Entries");
		_stochasticHigh = Param(nameof(StochasticHigh), 80m).SetDisplay("Stoch High", "Upper band", "Entries");
		_stochasticLow = Param(nameof(StochasticLow), 20m).SetDisplay("Stoch Low", "Lower band", "Entries");

		_useSar = Param(nameof(UseSar), true).SetDisplay("Use SAR", "Enable parabolic SAR", "Entries");
		_sarStep = Param(nameof(SarStep), 0.02m).SetDisplay("SAR Step", "Acceleration step", "Indicators");
		_sarMax = Param(nameof(SarMax), 0.2m).SetDisplay("SAR Max", "Maximum acceleration", "Indicators");

		_useMacd = Param(nameof(UseMacd), true).SetDisplay("Use MACD", "Enable MACD", "Entries");
		_macdFast = Param(nameof(MacdFast), 12).SetDisplay("MACD Fast", "Fast period", "Indicators").SetCanOptimize(true);
		_macdSlow = Param(nameof(MacdSlow), 24).SetDisplay("MACD Slow", "Slow period", "Indicators").SetCanOptimize(true);
		_macdSignal = Param(nameof(MacdSignal), 9).SetDisplay("MACD Signal", "Signal period", "Indicators").SetCanOptimize(true);
		_macdPrice = Param(nameof(MacdPrice), AppliedPrices.Close).SetDisplay("MACD Price", "Applied price", "Indicators");
		_macdMode = Param(nameof(MacdMode), MacdSignalModes.ZeroCross).SetDisplay("MACD Mode", "MACD logic", "Entries");

		_useTrailingStop = Param(nameof(UseTrailingStop), false).SetDisplay("Use Trailing", "Enable trailing stop", "Risk");
		_trailingStop = Param(nameof(TrailingStop), 198m).SetDisplay("Trailing", "Trailing distance", "Risk");

		_useStaticVolume = Param(nameof(UseStaticVolume), false).SetDisplay("Static Volume", "Use fixed volume", "Risk");
		_staticVolume = Param(nameof(StaticVolume), 0.1m).SetDisplay("Volume", "Fixed volume", "Risk");
		_riskPercent = Param(nameof(RiskPercent), 5m).SetDisplay("Risk %", "Risk percentage", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 191).SetDisplay("ATR Period", "ATR length", "Risk").SetCanOptimize(true);
		_atrMultiplier = Param(nameof(AtrMultiplier), 7m).SetDisplay("ATR Mult", "ATR multiplier", "Risk").SetCanOptimize(true);

		_autoClose = Param(nameof(AutoClose), true).SetDisplay("Auto Close", "Enable exit confirmations", "Exits");
		_openOppositeAfterClose = Param(nameof(OpenOppositeAfterClose), false).SetDisplay("Flip Position", "Open opposite after exit", "Exits");
		_useMaClosing = Param(nameof(UseMaClosing), false).SetDisplay("Close MA", "Use MA for exits", "Exits");
		_maModeClosing = Param(nameof(MaModeClosing), MaSignalModes.MediumSlow).SetDisplay("MA Exit Mode", "MA logic for exits", "Exits");
		_useMacdClosing = Param(nameof(UseMacdClosing), true).SetDisplay("Close MACD", "Use MACD for exits", "Exits");
		_macdModeClosing = Param(nameof(MacdModeClosing), MacdSignalModes.Combined).SetDisplay("MACD Exit Mode", "MACD logic for exits", "Exits");
		_useRsiClosing = Param(nameof(UseRsiClosing), false).SetDisplay("Close RSI", "Use RSI for exits", "Exits");
		_rsiModeClosing = Param(nameof(RsiModeClosing), RsiSignalModes.Trend).SetDisplay("RSI Exit Mode", "RSI logic for exits", "Exits");
		_useStochasticClosing = Param(nameof(UseStochasticClosing), true).SetDisplay("Close Stoch", "Use stochastic for exits", "Exits");
		_useSarClosing = Param(nameof(UseSarClosing), true).SetDisplay("Close SAR", "Use SAR for exits", "Exits");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Primary timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		_ma1 = CreateMovingAverage(Ma1Method, Ma1Period);
		_ma2 = CreateMovingAverage(Ma2Method, Ma2Period);
		_ma3 = CreateMovingAverage(Ma3Method, Ma3Period);
		_atr = new AverageTrueRange { Length = AtrPeriod };
		_ma1BufferAtr = new AverageTrueRange { Length = Ma1BufferPeriod };
		_ma2BufferAtr = new AverageTrueRange { Length = Ma2BufferPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_stochastic = new StochasticOscillator
		{
			Length = StochasticK,
			K = { Length = StochasticK },
			D = { Length = StochasticD },
			Slowing = StochasticSlowing
		};
		_sar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationMax = SarMax
		};
		_fastZlema = new ZeroLagExponentialMovingAverage { Length = MacdFast };
		_slowZlema = new ZeroLagExponentialMovingAverage { Length = MacdSlow };
		_macdEma1 = new ExponentialMovingAverage { Length = MacdSignal };
		_macdEma2 = new ExponentialMovingAverage { Length = MacdSignal };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		base.OnStarted(time);
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0)
		{
			_longStopPrice = null;
			_longTakePrice = null;
			_shortStopPrice = null;
			_shortTakePrice = null;
			_longExitRequested = false;
			_shortExitRequested = false;

			if (_pendingOppositeEntry is { } direction && _atrValue is decimal atr)
			{
				_pendingOppositeEntry = null;

				switch (direction)
				{
					case SignalDirections.Buy:
					_pendingLongAtr = atr;
					BuyMarket(GetTradeVolume());
					break;
					case SignalDirections.Sell:
					_pendingShortAtr = atr;
					SellMarket(GetTradeVolume());
					break;
				}
			}

			return;
		}

		if (Position > 0 && delta > 0)
		{
			SetupLongProtection();
			_longExitRequested = false;
			_shortExitRequested = false;
		}
		else if (Position < 0 && delta < 0)
		{
			SetupShortProtection();
			_shortExitRequested = false;
			_longExitRequested = false;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateIndicators(candle);
		ManageStops(candle);

		if (!AreIndicatorsReady())
		{
			_prevOpen = candle.OpenPrice;
			_prevClose = candle.ClosePrice;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevOpen = candle.OpenPrice;
			_prevClose = candle.ClosePrice;
			return;
		}

		var exitSignal = CalculateCloseSignal();
		HandleSignalBasedExit(exitSignal);

		if (Position == 0 && !_longExitRequested && !_shortExitRequested && _pendingOppositeEntry is null)
		{
			var entrySignal = CalculateEntrySignal();
			switch (entrySignal)
			{
				case SignalDirections.Buy:
				_pendingLongAtr = _atrValue;
				BuyMarket(GetTradeVolume());
				break;
				case SignalDirections.Sell:
				_pendingShortAtr = _atrValue;
				SellMarket(GetTradeVolume());
				break;
			}
		}

		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
	}

	private void UpdateIndicators(ICandleMessage candle)
	{
		var time = candle.CloseTime;
		var ma1Price = GetAppliedPrice(candle, Ma1Price);
		var ma2Price = GetAppliedPrice(candle, Ma2Price);
		var ma3Price = GetAppliedPrice(candle, Ma3Price);

		var ma1Value = _ma1.Process(ma1Price, time, true);
		if (ma1Value.IsFinal)
		{
			_ma1Previous = _ma1Current;
			_ma1Current = ma1Value.ToDecimal();
		}

		var ma2Value = _ma2.Process(ma2Price, time, true);
		if (ma2Value.IsFinal)
		{
			_ma2Previous = _ma2Current;
			_ma2Current = ma2Value.ToDecimal();
		}

		var ma3Value = _ma3.Process(ma3Price, time, true);
		if (ma3Value.IsFinal)
		{
			_ma3Previous = _ma3Current;
			_ma3Current = ma3Value.ToDecimal();
		}

		var atrValue = _atr.Process(candle);
		if (atrValue.IsFinal)
		_atrValue = atrValue.ToDecimal();

		var buffer1 = _ma1BufferAtr.Process(candle);
		if (buffer1.IsFinal)
		_ma1BufferValue = buffer1.ToDecimal();

		var buffer2 = _ma2BufferAtr.Process(candle);
		if (buffer2.IsFinal)
		_ma2BufferValue = buffer2.ToDecimal();

		var rsiValue = _rsi.Process(candle.ClosePrice, time, true);
		if (rsiValue.IsFinal)
		{
			_rsiPrevious = _rsiCurrent;
			_rsiCurrent = rsiValue.ToDecimal();
		}

		var stochasticValue = (StochasticOscillatorValue)_stochastic.Process(candle);
		if (stochasticValue.K is decimal k && stochasticValue.D is decimal d)
		{
			_stochasticValue = k;
			_stochasticSignal = d;
		}

		var sarValue = _sar.Process(candle);
		if (sarValue.IsFinal)
		_sarValue = sarValue.ToDecimal();

		var macdInput = GetAppliedPrice(candle, MacdPrice);
		var fastValue = _fastZlema.Process(macdInput, time, true);
		var slowValue = _slowZlema.Process(macdInput, time, true);
		if (fastValue.IsFinal && slowValue.IsFinal)
		{
			var fast = fastValue.ToDecimal();
			var slow = slowValue.ToDecimal();
			var macdLine = fast - slow;
			var ema1 = _macdEma1.Process(macdLine, time, true).ToDecimal();
			var ema2 = _macdEma2.Process(ema1, time, true).ToDecimal();
			var signal = 2m * ema1 - ema2;

			_macdLinePrevious = _macdLineCurrent;
			_macdLineCurrent = macdLine;
			_macdSignalPrevious = _macdSignalCurrent;
			_macdSignalCurrent = signal;
		}
	}

	private void ManageStops(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longExitRequested)
			return;

			var stopHit = _longStopPrice is decimal stop && candle.LowPrice <= stop;
			var takeHit = _longTakePrice is decimal take && candle.HighPrice >= take;

			if ((stopHit || takeHit) && Position > 0)
			{
				SellMarket(Position);
				_longExitRequested = true;
				_pendingOppositeEntry = null;
				return;
			}

			if (UseTrailingStop && TrailingStop > 0)
			{
				var trailingDistance = TrailingStop * CalculatePointValue();
				var newStop = candle.ClosePrice - trailingDistance;
				if (_longStopPrice is decimal currentStop)
				_longStopPrice = Math.Max(currentStop, newStop);
				else
				_longStopPrice = newStop;
			}
		}
		else if (Position < 0)
		{
			if (_shortExitRequested)
			return;

			var stopHit = _shortStopPrice is decimal stop && candle.HighPrice >= stop;
			var takeHit = _shortTakePrice is decimal take && candle.LowPrice <= take;

			if ((stopHit || takeHit) && Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				_shortExitRequested = true;
				_pendingOppositeEntry = null;
				return;
			}

			if (UseTrailingStop && TrailingStop > 0)
			{
				var trailingDistance = TrailingStop * CalculatePointValue();
				var newStop = candle.ClosePrice + trailingDistance;
				if (_shortStopPrice is decimal currentStop)
				_shortStopPrice = Math.Min(currentStop, newStop);
				else
				_shortStopPrice = newStop;
			}
		}
	}

	private void HandleSignalBasedExit(SignalDirections exitSignal)
	{
		if (!AutoClose)
		return;

		switch (exitSignal)
		{
			case SignalDirections.Sell when Position > 0 && !_longExitRequested:
			SellMarket(Position);
			_longExitRequested = true;
			_pendingOppositeEntry = OpenOppositeAfterClose ? SignalDirections.Sell : null;
			break;
			case SignalDirections.Buy when Position < 0 && !_shortExitRequested:
			BuyMarket(Math.Abs(Position));
			_shortExitRequested = true;
			_pendingOppositeEntry = OpenOppositeAfterClose ? SignalDirections.Buy : null;
			break;
		}
	}

	private SignalDirections CalculateEntrySignal()
	{
		var selected = 0;
		var up = 0;
		var down = 0;

		if (UseRsi)
		{
			selected++;
			UpdateCounters(EvaluateRsi(RsiMode), ref up, ref down);
		}

		if (UseStochastic)
		{
			selected++;
			UpdateCounters(EvaluateStochastic(), ref up, ref down);
		}

		if (UseSar)
		{
			selected++;
			UpdateCounters(EvaluateSar(), ref up, ref down);
		}

		if (UseMa)
		{
			selected++;
			UpdateCounters(EvaluateMa(MaMode), ref up, ref down);
		}

		if (UseMacd)
		{
			selected++;
			UpdateCounters(EvaluateMacd(MacdMode), ref up, ref down);
		}

		return ResolveSignal(selected, up, down);
	}

	private SignalDirections CalculateCloseSignal()
	{
		var selected = 0;
		var up = 0;
		var down = 0;

		if (UseRsiClosing)
		{
			selected++;
			UpdateCounters(EvaluateRsi(RsiModeClosing), ref up, ref down);
		}

		if (UseStochasticClosing)
		{
			selected++;
			UpdateCounters(EvaluateStochastic(), ref up, ref down);
		}

		if (UseSarClosing)
		{
			selected++;
			UpdateCounters(EvaluateSar(), ref up, ref down);
		}

		if (UseMaClosing)
		{
			selected++;
			UpdateCounters(EvaluateMa(MaModeClosing), ref up, ref down);
		}

		if (UseMacdClosing)
		{
			selected++;
			UpdateCounters(EvaluateMacd(MacdModeClosing), ref up, ref down);
		}

		return ResolveSignal(selected, up, down);
	}

	private void UpdateCounters(SignalDirections direction, ref int up, ref int down)
	{
		switch (direction)
		{
			case SignalDirections.Buy:
			up++;
			break;
			case SignalDirections.Sell:
			down++;
			break;
		}
	}

	private static SignalDirections ResolveSignal(int selected, int up, int down)
	{
		if (selected == 0)
		return SignalDirections.None;

		if (up == selected)
		return SignalDirections.Buy;

		if (down == selected)
		return SignalDirections.Sell;

		return SignalDirections.None;
	}

	private SignalDirections EvaluateMa(MaSignalModes mode)
	{
		if (_ma1Current is not decimal ma1 || _ma1Previous is not decimal ma1Prev ||
		_ma2Current is not decimal ma2 || _ma2Previous is not decimal ma2Prev ||
		_ma3Current is not decimal ma3 || _ma3Previous is not decimal ma3Prev)
		return SignalDirections.None;

		var buffer1 = _ma1BufferValue ?? 0m;
		var buffer2 = _ma2BufferValue ?? 0m;

		var ma12 = EvaluateMaCross(ma1, ma1Prev, ma2, ma2Prev, buffer1);
		var ma23 = EvaluateMaCross(ma2, ma2Prev, ma3, ma3Prev, buffer2);
		var ma13 = EvaluateMaCross(ma1, ma1Prev, ma3, ma3Prev, buffer1);

		return mode switch
		{
			MaSignalModes.FastMedium => ma12,
			MaSignalModes.MediumSlow => ma23,
			MaSignalModes.FastMediumCombined => CombineSignals(ma12, ma23),
			MaSignalModes.FastSlow => ma13,
			MaSignalModes.AllCombined => CombineSignals(CombineSignals(ma12, ma23), ma13),
			_ => SignalDirections.None
		};
	}

	private static SignalDirections EvaluateMaCross(decimal fast, decimal fastPrev, decimal slow, decimal slowPrev, decimal buffer)
	{
		if (fast >= slow + buffer && fastPrev < slowPrev + buffer)
		return SignalDirections.Buy;

		if (fast <= slow - buffer && fastPrev > slowPrev - buffer)
		return SignalDirections.Sell;

		return SignalDirections.None;
	}

	private static SignalDirections CombineSignals(SignalDirections first, SignalDirections second)
	{
		if (first == SignalDirections.None || second == SignalDirections.None)
		return SignalDirections.None;

		return first == second ? first : SignalDirections.None;
	}

	private SignalDirections EvaluateRsi(RsiSignalModes mode)
	{
		if (_rsiCurrent is not decimal current || _rsiPrevious is not decimal previous)
		return SignalDirections.None;

		return mode switch
		{
			RsiSignalModes.OverboughtOversold => current < RsiBuyLevel ? SignalDirections.Buy : current > RsiSellLevel ? SignalDirections.Sell : SignalDirections.None,
			RsiSignalModes.Trend => EvaluateRsiTrend(current, previous),
			RsiSignalModes.Combined => CombineSignals(EvaluateRsiTrend(current, previous), current < RsiBuyLevel ? SignalDirections.Buy : current > RsiSellLevel ? SignalDirections.Sell : SignalDirections.None),
			RsiSignalModes.Zone => EvaluateRsiZone(current, previous),
			_ => SignalDirections.None
		};
	}

	private SignalDirections EvaluateRsiTrend(decimal current, decimal previous)
	{
		if (_prevOpen is not decimal openPrev || _prevClose is not decimal closePrev)
		return SignalDirections.None;

		if (current > previous && Security?.LastTick?.Price is decimal lastPrice && lastPrice > closePrev)
		return SignalDirections.Buy;

		if (current < previous && Security?.LastTick?.Price is decimal last && last < closePrev)
		return SignalDirections.Sell;

		if (current > previous && openPrev < closePrev)
		return SignalDirections.Buy;

		if (current < previous && openPrev > closePrev)
		return SignalDirections.Sell;

		return SignalDirections.None;
	}

	private SignalDirections EvaluateRsiZone(decimal current, decimal previous)
	{
		if (current > previous && (current > 50m || previous > RsiSellZone))
		return SignalDirections.Buy;

		if (current < previous && (current < 50m || previous < RsiBuyZone))
		return SignalDirections.Sell;

		return SignalDirections.None;
	}

	private SignalDirections EvaluateStochastic()
	{
		if (_stochasticValue is not decimal value || _stochasticSignal is not decimal signal)
		return SignalDirections.None;

		if (UseStochasticHighLow)
		{
			if (value > signal && value > StochasticHigh)
			return SignalDirections.Buy;

			if (value < signal && value < StochasticLow)
			return SignalDirections.Sell;

			return SignalDirections.None;
		}

		if (value > signal)
		return SignalDirections.Buy;

		if (value < signal)
		return SignalDirections.Sell;

		return SignalDirections.None;
	}

	private SignalDirections EvaluateSar()
	{
		if (_sarValue is not decimal sar || _prevClose is not decimal prevClose)
		return SignalDirections.None;

		return sar < prevClose ? SignalDirections.Buy : SignalDirections.Sell;
	}

	private SignalDirections EvaluateMacd(MacdSignalModes mode)
	{
		if (_macdLineCurrent is not decimal current || _macdLinePrevious is not decimal previous ||
		_macdSignalCurrent is not decimal signal || _macdSignalPrevious is not decimal signalPrev)
		return SignalDirections.None;

		return mode switch
		{
			MacdSignalModes.Trend => EvaluateMacdTrend(current, previous, signal, signalPrev),
			MacdSignalModes.ZeroCross => EvaluateMacdCross(current, previous, signal, signalPrev),
			MacdSignalModes.Combined => CombineSignals(EvaluateMacdTrend(current, previous, signal, signalPrev), EvaluateMacdCross(current, previous, signal, signalPrev)),
			_ => SignalDirections.None
		};
	}

	private static SignalDirections EvaluateMacdTrend(decimal current, decimal previous, decimal signal, decimal signalPrev)
	{
		if (current > previous && signal > signalPrev && current > signal)
		return SignalDirections.Buy;

		if (current < previous && signal < signalPrev && current < signal)
		return SignalDirections.Sell;

		return SignalDirections.None;
	}

	private static SignalDirections EvaluateMacdCross(decimal current, decimal previous, decimal signal, decimal signalPrev)
	{
		var crossUp = previous <= signalPrev && current > signal;
		var crossDown = previous >= signalPrev && current < signal;

		if (crossUp && current < 0m && previous < 0m)
		return SignalDirections.Buy;

		if (crossDown && current > 0m && previous > 0m)
		return SignalDirections.Sell;

		return SignalDirections.None;
	}

	private decimal GetTradeVolume()
	{
		if (UseStaticVolume)
		return StaticVolume;

		if (Portfolio is null || Security is null)
		return Volume;

		var balance = Portfolio.CurrentValue ?? Portfolio.BeginValue;
		if (balance is null || balance == 0m)
		return Volume;

		var price = Security.LastTick?.Price ?? Security.BestBid?.Price ?? Security.BestAsk?.Price ?? 0m;
		if (price <= 0m)
		return Volume;

		var riskAmount = balance.Value * RiskPercent / 100m;
		var step = Security.StepPrice ?? 1m;
		var volume = riskAmount / (step == 0m ? price : price * step);
		return Math.Max(volume, Volume);
	}

	private void SetupLongProtection()
	{
		if (Security == null)
		return;

		var atr = _pendingLongAtr ?? _atrValue;
		_pendingLongAtr = null;
		if (atr is null)
		return;

		var point = CalculatePointValue();
		var buffer = 2m * point;
		var atrRange = atr.Value * AtrMultiplier;
		var stopDistance = Math.Max(atrRange - buffer, point);
		var takeDistance = atrRange + buffer;
		var entryPrice = PositionPrice;

		_longStopPrice = entryPrice - stopDistance;
		_longTakePrice = entryPrice + takeDistance;
	}

	private void SetupShortProtection()
	{
		if (Security == null)
		return;

		var atr = _pendingShortAtr ?? _atrValue;
		_pendingShortAtr = null;
		if (atr is null)
		return;

		var point = CalculatePointValue();
		var buffer = 2m * point;
		var atrRange = atr.Value * AtrMultiplier;
		var stopDistance = atrRange + buffer;
		var takeDistance = Math.Max(atrRange - buffer, point);
		var entryPrice = PositionPrice;

		_shortStopPrice = entryPrice + stopDistance;
		_shortTakePrice = entryPrice - takeDistance;
	}

	private bool AreIndicatorsReady()
	{
		return _ma1Current.HasValue && _ma1Previous.HasValue &&
		_ma2Current.HasValue && _ma2Previous.HasValue &&
		_ma3Current.HasValue && _ma3Previous.HasValue &&
		_ma1BufferValue.HasValue && _ma2BufferValue.HasValue &&
		_atrValue.HasValue &&
		_rsiCurrent.HasValue && _rsiPrevious.HasValue &&
		_sarValue.HasValue &&
		_stochasticValue.HasValue && _stochasticSignal.HasValue &&
		_macdLineCurrent.HasValue && _macdLinePrevious.HasValue &&
		_macdSignalCurrent.HasValue && _macdSignalPrevious.HasValue &&
		_prevOpen.HasValue && _prevClose.HasValue;
	}

	private decimal CalculatePointValue()
	{
		var step = Security?.StepPrice ?? 0.0001m;
		var decimals = Security?.Decimals ?? 4;
		var factor = decimals is 3 or 5 ? 10m : 1m;
		return step * factor;
	}

	private static IIndicator CreateMovingAverage(MovingAverageMethods method, int period)
	{
		return method switch
		{
			MovingAverageMethods.Simple => new SimpleMovingAverage { Length = period },
			MovingAverageMethods.Exponential => new ExponentialMovingAverage { Length = period },
			MovingAverageMethods.Smoothed => new SmoothedMovingAverage { Length = period },
			MovingAverageMethods.LinearWeighted => new WeightedMovingAverage { Length = period },
			_ => new SimpleMovingAverage { Length = period }
		};
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPrices price)
	{
		return price switch
		{
			AppliedPrices.Close => candle.ClosePrice,
			AppliedPrices.Open => candle.OpenPrice,
			AppliedPrices.High => candle.HighPrice,
			AppliedPrices.Low => candle.LowPrice,
			AppliedPrices.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrices.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrices.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}

	private enum SignalDirections
	{
		None,
		Buy,
		Sell
	}

	/// <summary>
	/// Moving average confirmation modes.
	/// </summary>
	public enum MaSignalModes
	{
		FastMedium = 1,
		MediumSlow = 2,
		FastMediumCombined = 3,
		FastSlow = 4,
		AllCombined = 5
	}

	/// <summary>
	/// RSI confirmation modes.
	/// </summary>
	public enum RsiSignalModes
	{
		OverboughtOversold = 1,
		Trend = 2,
		Combined = 3,
		Zone = 4
	}

	/// <summary>
	/// MACD confirmation modes.
	/// </summary>
	public enum MacdSignalModes
	{
		Trend = 1,
		ZeroCross = 2,
		Combined = 3
	}

	/// <summary>
	/// Moving average calculation methods.
	/// </summary>
	public enum MovingAverageMethods
	{
		Simple = 0,
		Exponential = 1,
		Smoothed = 2,
		LinearWeighted = 3
	}

	/// <summary>
	/// Available price sources.
	/// </summary>
	public enum AppliedPrices
	{
		Close = 0,
		Open = 1,
		High = 2,
		Low = 3,
		Median = 4,
		Typical = 5,
		Weighted = 6
	}
}
