namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Adaptive perceptron based strategy converted from the MQL4 expert "Auto_RXD_V1.67".
/// Combines three neural style moving average perceptrons with several optional indicator filters.
/// </summary>
public class AutoRxdV167Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<AutoRxdMode> _mode;
	private readonly StrategyParam<bool> _newOrderAllowed;
	private readonly StrategyParam<bool> _enableHourTrade;
	private readonly StrategyParam<int> _hourTradeStart;
	private readonly StrategyParam<int> _hourTradeStop;
	private readonly StrategyParam<bool> _enableIndicatorManager;
	private readonly StrategyParam<bool> _enableOrderCloseManager;
	private readonly StrategyParam<bool> _atrTpSlEnable;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _longTakeProfitPoints;
	private readonly StrategyParam<decimal> _longStopLossPoints;
	private readonly StrategyParam<decimal> _shortTakeProfitPoints;
	private readonly StrategyParam<decimal> _shortStopLossPoints;
	private readonly StrategyParam<int> _supervisorMaLength;
	private readonly StrategyParam<int> _supervisorShift;
	private readonly StrategyParam<int> _supervisorX1;
	private readonly StrategyParam<int> _supervisorX2;
	private readonly StrategyParam<int> _supervisorX3;
	private readonly StrategyParam<int> _supervisorX4;
	private readonly StrategyParam<int> _supervisorThreshold;
	private readonly StrategyParam<int> _longMaLength;
	private readonly StrategyParam<int> _longShift;
	private readonly StrategyParam<int> _longX1;
	private readonly StrategyParam<int> _longX2;
	private readonly StrategyParam<int> _longX3;
	private readonly StrategyParam<int> _longX4;
	private readonly StrategyParam<int> _longThreshold;
	private readonly StrategyParam<int> _shortMaLength;
	private readonly StrategyParam<int> _shortShift;
	private readonly StrategyParam<int> _shortX1;
	private readonly StrategyParam<int> _shortX2;
	private readonly StrategyParam<int> _shortX3;
	private readonly StrategyParam<int> _shortX4;
	private readonly StrategyParam<int> _shortThreshold;
	private readonly StrategyParam<bool> _adxControl;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<bool> _macdControl;
	private readonly StrategyParam<bool> _macdCrossNeeded;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<bool> _sarControl;
	private readonly StrategyParam<bool> _sarCrossNeeded;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaximum;
	private readonly StrategyParam<bool> _rsiControl;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<bool> _cciControl;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<bool> _osmaControl;
	private readonly StrategyParam<int> _osmaFast;
	private readonly StrategyParam<int> _osmaSlow;
	private readonly StrategyParam<int> _osmaSignal;
	private readonly StrategyParam<bool> _aoControl;
	private readonly StrategyParam<bool> _acControl;

	private LinearWeightedMovingAverage? _supervisorCloseWma;
	private LinearWeightedMovingAverage? _supervisorWeightedWma;
	private LinearWeightedMovingAverage? _longCloseWma;
	private LinearWeightedMovingAverage? _longWeightedWma;
	private LinearWeightedMovingAverage? _shortCloseWma;
	private LinearWeightedMovingAverage? _shortWeightedWma;
	private AverageTrueRange? _atr;
	private RelativeStrengthIndex? _rsi;
	private CommodityChannelIndex? _cci;
	private MovingAverageConvergenceDivergenceSignal? _macd;
	private AverageDirectionalIndex? _adx;
	private ParabolicStopAndReverse? _sar;
	private AwesomeOscillator? _ao;
	private AcceleratorOscillator? _ac;

	private readonly ShiftBuffer _supervisorHistory = new();
	private readonly ShiftBuffer _longHistory = new();
	private readonly ShiftBuffer _shortHistory = new();

	private decimal? _previousMacd;
	private decimal? _previousSignal;
	private decimal? _previousSar;
	private decimal? _previousClose;
	private decimal _point;

	/// <summary>
	/// Initializes a new instance of the <see cref="AutoRxdV167Strategy"/> class.
	/// </summary>
	public AutoRxdV167Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe used for signal calculations.", "General");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order volume", "Base position size for every entry.", "Trading");

		_mode = Param(nameof(Mode), AutoRxdMode.Indicator)
			.SetDisplay("Mode", "Selects which perceptron or indicator block drives trading decisions.", "Trading");

		_newOrderAllowed = Param(nameof(NewOrderAllowed), true)
			.SetDisplay("Allow new orders", "Disable to block opening of new market positions.", "Trading");

		_enableHourTrade = Param(nameof(EnableHourTrade), false)
			.SetDisplay("Limit trading hours", "Restrict entries to a specific intraday time window.", "Trading");

		_hourTradeStart = Param(nameof(HourTradeStartTime), 18)
			.SetDisplay("Start hour", "Hour (0-23) when trading window opens.", "Trading");

		_hourTradeStop = Param(nameof(HourTradeStopTime), 23)
			.SetDisplay("Stop hour", "Hour (0-23) when trading window closes.", "Trading");

		_enableIndicatorManager = Param(nameof(EnableIndicatorManager), true)
			.SetDisplay("Use indicator filters", "Enable the confirmation block that checks supporting indicators.", "Filters");

		_enableOrderCloseManager = Param(nameof(EnableOrderCloseManager), true)
			.SetDisplay("Order close manager", "Close positions when the protective Parabolic SAR is violated.", "Risk");

		_atrTpSlEnable = Param(nameof(AtrTpSlEnable), false)
			.SetDisplay("ATR based exits", "Derive stop-loss and take-profit distances from the ATR indicator.", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR period", "Look-back length for ATR based risk management.", "Risk");

		_longTakeProfitPoints = Param(nameof(LongTakeProfitPoints), 1000m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Long TP (points)", "Static take-profit distance for long trades expressed in points.", "Risk");

		_longStopLossPoints = Param(nameof(LongStopLossPoints), 1000m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Long SL (points)", "Static stop-loss distance for long trades expressed in points.", "Risk");

		_shortTakeProfitPoints = Param(nameof(ShortTakeProfitPoints), 1000m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Short TP (points)", "Static take-profit distance for short trades expressed in points.", "Risk");

		_shortStopLossPoints = Param(nameof(ShortStopLossPoints), 1000m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Short SL (points)", "Static stop-loss distance for short trades expressed in points.", "Risk");

		_supervisorMaLength = Param(nameof(SupervisorMaLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Supervisor MA length", "Length of the LWMA used by the supervisor perceptron.", "Perceptrons");

		_supervisorShift = Param(nameof(SupervisorShift), 5)
			.SetGreaterThanZero()
			.SetDisplay("Supervisor shift", "Shift step for supervisor LWMA comparisons.", "Perceptrons");

		_supervisorX1 = Param(nameof(SupervisorX1), 100)
			.SetDisplay("Supervisor weight 1", "First weight of the supervisor perceptron.", "Perceptrons");

		_supervisorX2 = Param(nameof(SupervisorX2), 100)
			.SetDisplay("Supervisor weight 2", "Second weight of the supervisor perceptron.", "Perceptrons");

		_supervisorX3 = Param(nameof(SupervisorX3), 100)
			.SetDisplay("Supervisor weight 3", "Third weight of the supervisor perceptron.", "Perceptrons");

		_supervisorX4 = Param(nameof(SupervisorX4), 100)
			.SetDisplay("Supervisor weight 4", "Fourth weight of the supervisor perceptron.", "Perceptrons");

		_supervisorThreshold = Param(nameof(SupervisorThreshold), 100)
			.SetDisplay("Supervisor threshold", "Activation threshold applied to the supervisor output.", "Perceptrons");

		_longMaLength = Param(nameof(LongMaLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Long MA length", "LWMA length used by the long perceptron.", "Perceptrons");

		_longShift = Param(nameof(LongShift), 5)
			.SetGreaterThanZero()
			.SetDisplay("Long shift", "Shift step for the long perceptron LWMA chain.", "Perceptrons");

		_longX1 = Param(nameof(LongX1), 100)
			.SetDisplay("Long weight 1", "First weight of the long perceptron.", "Perceptrons");

		_longX2 = Param(nameof(LongX2), 100)
			.SetDisplay("Long weight 2", "Second weight of the long perceptron.", "Perceptrons");

		_longX3 = Param(nameof(LongX3), 100)
			.SetDisplay("Long weight 3", "Third weight of the long perceptron.", "Perceptrons");

		_longX4 = Param(nameof(LongX4), 100)
			.SetDisplay("Long weight 4", "Fourth weight of the long perceptron.", "Perceptrons");

		_longThreshold = Param(nameof(LongThreshold), 100)
			.SetDisplay("Long threshold", "Activation threshold applied to the long perceptron.", "Perceptrons");

		_shortMaLength = Param(nameof(ShortMaLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Short MA length", "LWMA length used by the short perceptron.", "Perceptrons");

		_shortShift = Param(nameof(ShortShift), 5)
			.SetGreaterThanZero()
			.SetDisplay("Short shift", "Shift step for the short perceptron LWMA chain.", "Perceptrons");

		_shortX1 = Param(nameof(ShortX1), 100)
			.SetDisplay("Short weight 1", "First weight of the short perceptron.", "Perceptrons");

		_shortX2 = Param(nameof(ShortX2), 100)
			.SetDisplay("Short weight 2", "Second weight of the short perceptron.", "Perceptrons");

		_shortX3 = Param(nameof(ShortX3), 100)
			.SetDisplay("Short weight 3", "Third weight of the short perceptron.", "Perceptrons");

		_shortX4 = Param(nameof(ShortX4), 100)
			.SetDisplay("Short weight 4", "Fourth weight of the short perceptron.", "Perceptrons");

		_shortThreshold = Param(nameof(ShortThreshold), 100)
			.SetDisplay("Short threshold", "Activation threshold applied to the short perceptron.", "Perceptrons");

		_adxControl = Param(nameof(AdxControl), false)
			.SetDisplay("ADX filter", "Confirm trends with ADX before entering trades.", "Filters");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX period", "Length used by the ADX indicator.", "Filters");

		_adxThreshold = Param(nameof(AdxThreshold), 21m)
			.SetGreaterOrEqualZero()
			.SetDisplay("ADX threshold", "Minimum ADX value required for trading.", "Filters");

		_macdControl = Param(nameof(MacdControl), false)
			.SetDisplay("MACD filter", "Use MACD alignment for signal confirmation.", "Filters");

		_macdCrossNeeded = Param(nameof(MacdCrossNeeded), false)
			.SetDisplay("MACD crossover", "Require a MACD/Signal crossover to validate entries.", "Filters");

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD fast", "Fast EMA length for MACD.", "Filters");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD slow", "Slow EMA length for MACD.", "Filters");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD signal", "Signal SMA length for MACD.", "Filters");

		_sarControl = Param(nameof(SarControl), false)
			.SetDisplay("Parabolic SAR filter", "Confirm signals with the Parabolic SAR trend direction.", "Filters");

		_sarCrossNeeded = Param(nameof(SarCrossNeeded), false)
			.SetDisplay("SAR crossover", "Require price to cross SAR before validating signals.", "Filters");

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR step", "Acceleration factor for Parabolic SAR.", "Filters");

		_sarMaximum = Param(nameof(SarMaximum), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR maximum", "Maximum acceleration factor for Parabolic SAR.", "Filters");

		_rsiControl = Param(nameof(RsiControl), false)
			.SetDisplay("RSI filter", "Confirm momentum using the RSI oscillator.", "Filters");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI period", "Length used by the RSI oscillator.", "Filters");

		_cciControl = Param(nameof(CciControl), false)
			.SetDisplay("CCI filter", "Confirm direction with Commodity Channel Index thresholds.", "Filters");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI period", "Length used by the CCI indicator.", "Filters");

		_osmaControl = Param(nameof(OsmaControl), false)
			.SetDisplay("OsMA filter", "Confirm momentum via the MACD histogram (OsMA).", "Filters");

		_osmaFast = Param(nameof(OsmaFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("OsMA fast", "Fast EMA length for the OsMA calculation.", "Filters");

		_osmaSlow = Param(nameof(OsmaSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("OsMA slow", "Slow EMA length for the OsMA calculation.", "Filters");

		_osmaSignal = Param(nameof(OsmaSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("OsMA signal", "Signal SMA length for the OsMA calculation.", "Filters");

		_aoControl = Param(nameof(AoControl), false)
			.SetDisplay("Awesome Oscillator", "Confirm entries with the Awesome Oscillator sign.", "Filters");

		_acControl = Param(nameof(AcControl), false)
			.SetDisplay("Accelerator Oscillator", "Confirm entries with the Accelerator Oscillator sign.", "Filters");
	}

	/// <summary>
	/// Timeframe used for data subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base trade volume in lots.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Primary operating mode for the strategy.
	/// </summary>
	public AutoRxdMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Disables new entries when set to false.
	/// </summary>
	public bool NewOrderAllowed
	{
		get => _newOrderAllowed.Value;
		set => _newOrderAllowed.Value = value;
	}

	/// <summary>
	/// Enables intraday trading window limitation.
	/// </summary>
	public bool EnableHourTrade
	{
		get => _enableHourTrade.Value;
		set => _enableHourTrade.Value = value;
	}

	/// <summary>
	/// Opening hour of the trading window.
	/// </summary>
	public int HourTradeStartTime
	{
		get => _hourTradeStart.Value;
		set => _hourTradeStart.Value = value;
	}

	/// <summary>
	/// Closing hour of the trading window.
	/// </summary>
	public int HourTradeStopTime
	{
		get => _hourTradeStop.Value;
		set => _hourTradeStop.Value = value;
	}

	/// <summary>
	/// Enables the confirmation block made of classic indicators.
	/// </summary>
	public bool EnableIndicatorManager
	{
		get => _enableIndicatorManager.Value;
		set => _enableIndicatorManager.Value = value;
	}

	/// <summary>
	/// Enables the SAR based close manager.
	/// </summary>
	public bool EnableOrderCloseManager
	{
		get => _enableOrderCloseManager.Value;
		set => _enableOrderCloseManager.Value = value;
	}

	/// <summary>
	/// Enables ATR based stop and target calculations.
	/// </summary>
	public bool AtrTpSlEnable
	{
		get => _atrTpSlEnable.Value;
		set => _atrTpSlEnable.Value = value;
	}

	/// <summary>
	/// ATR look-back length.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Take-profit distance for long trades expressed in points.
	/// </summary>
	public decimal LongTakeProfitPoints
	{
		get => _longTakeProfitPoints.Value;
		set => _longTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long trades expressed in points.
	/// </summary>
	public decimal LongStopLossPoints
	{
		get => _longStopLossPoints.Value;
		set => _longStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance for short trades expressed in points.
	/// </summary>
	public decimal ShortTakeProfitPoints
	{
		get => _shortTakeProfitPoints.Value;
		set => _shortTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short trades expressed in points.
	/// </summary>
	public decimal ShortStopLossPoints
	{
		get => _shortStopLossPoints.Value;
		set => _shortStopLossPoints.Value = value;
	}

	/// <summary>
	/// Supervisor perceptron LWMA length.
	/// </summary>
	public int SupervisorMaLength
	{
		get => _supervisorMaLength.Value;
		set => _supervisorMaLength.Value = value;
	}

	/// <summary>
	/// Supervisor perceptron shift step.
	/// </summary>
	public int SupervisorShift
	{
		get => _supervisorShift.Value;
		set => _supervisorShift.Value = value;
	}

	/// <summary>
	/// Supervisor perceptron first weight.
	/// </summary>
	public int SupervisorX1
	{
		get => _supervisorX1.Value;
		set => _supervisorX1.Value = value;
	}

	/// <summary>
	/// Supervisor perceptron second weight.
	/// </summary>
	public int SupervisorX2
	{
		get => _supervisorX2.Value;
		set => _supervisorX2.Value = value;
	}

	/// <summary>
	/// Supervisor perceptron third weight.
	/// </summary>
	public int SupervisorX3
	{
		get => _supervisorX3.Value;
		set => _supervisorX3.Value = value;
	}

	/// <summary>
	/// Supervisor perceptron fourth weight.
	/// </summary>
	public int SupervisorX4
	{
		get => _supervisorX4.Value;
		set => _supervisorX4.Value = value;
	}

	/// <summary>
	/// Supervisor perceptron activation threshold.
	/// </summary>
	public int SupervisorThreshold
	{
		get => _supervisorThreshold.Value;
		set => _supervisorThreshold.Value = value;
	}

	/// <summary>
	/// Long perceptron LWMA length.
	/// </summary>
	public int LongMaLength
	{
		get => _longMaLength.Value;
		set => _longMaLength.Value = value;
	}

	/// <summary>
	/// Long perceptron shift step.
	/// </summary>
	public int LongShift
	{
		get => _longShift.Value;
		set => _longShift.Value = value;
	}

	/// <summary>
	/// Long perceptron first weight.
	/// </summary>
	public int LongX1
	{
		get => _longX1.Value;
		set => _longX1.Value = value;
	}

	/// <summary>
	/// Long perceptron second weight.
	/// </summary>
	public int LongX2
	{
		get => _longX2.Value;
		set => _longX2.Value = value;
	}

	/// <summary>
	/// Long perceptron third weight.
	/// </summary>
	public int LongX3
	{
		get => _longX3.Value;
		set => _longX3.Value = value;
	}

	/// <summary>
	/// Long perceptron fourth weight.
	/// </summary>
	public int LongX4
	{
		get => _longX4.Value;
		set => _longX4.Value = value;
	}

	/// <summary>
	/// Long perceptron activation threshold.
	/// </summary>
	public int LongThreshold
	{
		get => _longThreshold.Value;
		set => _longThreshold.Value = value;
	}

	/// <summary>
	/// Short perceptron LWMA length.
	/// </summary>
	public int ShortMaLength
	{
		get => _shortMaLength.Value;
		set => _shortMaLength.Value = value;
	}

	/// <summary>
	/// Short perceptron shift step.
	/// </summary>
	public int ShortShift
	{
		get => _shortShift.Value;
		set => _shortShift.Value = value;
	}

	/// <summary>
	/// Short perceptron first weight.
	/// </summary>
	public int ShortX1
	{
		get => _shortX1.Value;
		set => _shortX1.Value = value;
	}

	/// <summary>
	/// Short perceptron second weight.
	/// </summary>
	public int ShortX2
	{
		get => _shortX2.Value;
		set => _shortX2.Value = value;
	}

	/// <summary>
	/// Short perceptron third weight.
	/// </summary>
	public int ShortX3
	{
		get => _shortX3.Value;
		set => _shortX3.Value = value;
	}

	/// <summary>
	/// Short perceptron fourth weight.
	/// </summary>
	public int ShortX4
	{
		get => _shortX4.Value;
		set => _shortX4.Value = value;
	}

	/// <summary>
	/// Short perceptron activation threshold.
	/// </summary>
	public int ShortThreshold
	{
		get => _shortThreshold.Value;
		set => _shortThreshold.Value = value;
	}

	/// <summary>
	/// Enables ADX confirmation.
	/// </summary>
	public bool AdxControl
	{
		get => _adxControl.Value;
		set => _adxControl.Value = value;
	}

	/// <summary>
	/// ADX look-back length.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Minimum ADX value for trading.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Enables MACD confirmation.
	/// </summary>
	public bool MacdControl
	{
		get => _macdControl.Value;
		set => _macdControl.Value = value;
	}

	/// <summary>
	/// Requires MACD/Signal crossovers to validate entries.
	/// </summary>
	public bool MacdCrossNeeded
	{
		get => _macdCrossNeeded.Value;
		set => _macdCrossNeeded.Value = value;
	}

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Enables Parabolic SAR confirmation.
	/// </summary>
	public bool SarControl
	{
		get => _sarControl.Value;
		set => _sarControl.Value = value;
	}

	/// <summary>
	/// Requires SAR crossovers when enabled.
	/// </summary>
	public bool SarCrossNeeded
	{
		get => _sarCrossNeeded.Value;
		set => _sarCrossNeeded.Value = value;
	}

	/// <summary>
	/// SAR acceleration factor.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// SAR maximum acceleration factor.
	/// </summary>
	public decimal SarMaximum
	{
		get => _sarMaximum.Value;
		set => _sarMaximum.Value = value;
	}

	/// <summary>
	/// Enables RSI confirmation.
	/// </summary>
	public bool RsiControl
	{
		get => _rsiControl.Value;
		set => _rsiControl.Value = value;
	}

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Enables CCI confirmation.
	/// </summary>
	public bool CciControl
	{
		get => _cciControl.Value;
		set => _cciControl.Value = value;
	}

	/// <summary>
	/// CCI length.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Enables OsMA confirmation.
	/// </summary>
	public bool OsmaControl
	{
		get => _osmaControl.Value;
		set => _osmaControl.Value = value;
	}

	/// <summary>
	/// OsMA fast period.
	/// </summary>
	public int OsmaFast
	{
		get => _osmaFast.Value;
		set => _osmaFast.Value = value;
	}

	/// <summary>
	/// OsMA slow period.
	/// </summary>
	public int OsmaSlow
	{
		get => _osmaSlow.Value;
		set => _osmaSlow.Value = value;
	}

	/// <summary>
	/// OsMA signal period.
	/// </summary>
	public int OsmaSignal
	{
		get => _osmaSignal.Value;
		set => _osmaSignal.Value = value;
	}

	/// <summary>
	/// Enables Awesome Oscillator confirmation.
	/// </summary>
	public bool AoControl
	{
		get => _aoControl.Value;
		set => _aoControl.Value = value;
	}

	/// <summary>
	/// Enables Accelerator Oscillator confirmation.
	/// </summary>
	public bool AcControl
	{
		get => _acControl.Value;
		set => _acControl.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_supervisorHistory.Clear();
		_longHistory.Clear();
		_shortHistory.Clear();
		_previousMacd = null;
		_previousSignal = null;
		_previousSar = null;
		_previousClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_point = Security?.PriceStep ?? 0.0001m;

		_supervisorCloseWma = new LinearWeightedMovingAverage
		{
			Length = SupervisorMaLength,
			CandlePrice = CandlePrice.Close
		};

		_supervisorWeightedWma = new LinearWeightedMovingAverage
		{
			Length = SupervisorMaLength,
			CandlePrice = CandlePrice.Weighted
		};

		_longCloseWma = new LinearWeightedMovingAverage
		{
			Length = LongMaLength,
			CandlePrice = CandlePrice.Close
		};

		_longWeightedWma = new LinearWeightedMovingAverage
		{
			Length = LongMaLength,
			CandlePrice = CandlePrice.Weighted
		};

		_shortCloseWma = new LinearWeightedMovingAverage
		{
			Length = ShortMaLength,
			CandlePrice = CandlePrice.Close
		};

		_shortWeightedWma = new LinearWeightedMovingAverage
		{
			Length = ShortMaLength,
			CandlePrice = CandlePrice.Weighted
		};

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow }
			},
			SignalMa = { Length = MacdSignal }
		};

		_adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_sar = new ParabolicStopAndReverse { Step = SarStep, MaxStep = SarMaximum };
		_ao = new AwesomeOscillator();
		_ac = new AcceleratorOscillator();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(
				supervisor: _supervisorCloseWma!,
				supervisorWeighted: _supervisorWeightedWma!,
				longClose: _longCloseWma!,
				longWeighted: _longWeightedWma!,
				shortClose: _shortCloseWma!,
				shortWeighted: _shortWeightedWma!,
				atr: _atr!,
				rsi: _rsi!,
				cci: _cci!,
				macd: _macd!,
				adx: _adx!,
				sar: _sar!,
				ao: _ao!,
				ac: _ac!,
				ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _supervisorCloseWma!);
			DrawIndicator(area, _longCloseWma!);
			DrawIndicator(area, _shortCloseWma!);
			DrawIndicator(area, _macd!);
			DrawIndicator(area, _adx!);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue supervisorValue,
		IIndicatorValue supervisorWeightedValue,
		IIndicatorValue longValue,
		IIndicatorValue longWeightedValue,
		IIndicatorValue shortValue,
		IIndicatorValue shortWeightedValue,
		IIndicatorValue atrValue,
		IIndicatorValue rsiValue,
		IIndicatorValue cciValue,
		IIndicatorValue macdValue,
		IIndicatorValue adxValue,
		IIndicatorValue sarValue,
		IIndicatorValue aoValue,
		IIndicatorValue acValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!NewOrderAllowed)
			return;

		if (EnableHourTrade && !IsWithinTradeWindow(candle.OpenTime.TimeOfDay))
			return;

		var supervisorClose = supervisorValue.GetValue<decimal>();
		var supervisorWeighted = supervisorWeightedValue.GetValue<decimal>();
		var longClose = longValue.GetValue<decimal>();
		var longWeighted = longWeightedValue.GetValue<decimal>();
		var shortClose = shortValue.GetValue<decimal>();
		var shortWeighted = shortWeightedValue.GetValue<decimal>();
		var atr = atrValue.IsFinal ? atrValue.GetValue<decimal>() : (decimal?)null;
		var rsi = rsiValue.IsFinal ? rsiValue.GetValue<decimal>() : (decimal?)null;
		var cci = cciValue.IsFinal ? cciValue.GetValue<decimal>() : (decimal?)null;
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdMain = macdTyped.Macd;
		var macdSignal = macdTyped.Signal;
		var macdHistogram = macdTyped.Histogram;
		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		var sar = sarValue.IsFinal ? sarValue.GetValue<decimal>() : (decimal?)null;
		var ao = aoValue.IsFinal ? aoValue.GetValue<decimal>() : (decimal?)null;
		var ac = acValue.IsFinal ? acValue.GetValue<decimal>() : (decimal?)null;

		var supervisorReady = _supervisorHistory.Add(supervisorWeighted, SupervisorShift * 4 + 5);
		var longReady = _longHistory.Add(longWeighted, LongShift * 4 + 5);
		var shortReady = _shortHistory.Add(shortWeighted, ShortShift * 4 + 5);

		if (!supervisorReady || !longReady || !shortReady)
			return;

		var filters = EvaluateFilters(candle.ClosePrice, macdMain, macdSignal, macdHistogram, sar, adxTyped, rsi, cci, ao, ac);

		var decision = EvaluateDecision(supervisorClose, longClose, shortClose, atr, filters);

		HandleOrderCloseManager(candle, sar);

		ExecuteDecision(candle, atr, decision);

		_previousMacd = macdMain;
		_previousSignal = macdSignal;
		_previousSar = sar;
		_previousClose = candle.ClosePrice;
	}

	private TradeDecision EvaluateDecision(decimal supervisorClose, decimal longClose, decimal shortClose, decimal? atr, IndicatorFilters filters)
	{
		var decision = new TradeDecision(0, null, null);

		decimal? stop = null;
		decimal? take = null;

		void EvaluateStops(bool isLong)
		{
			var distances = CalculateProtection(isLong, atr);
			take = distances.take;
			stop = distances.stop;
		}

		bool IsAllowed(bool isLong)
		{
			if (!EnableIndicatorManager)
				return true;

			return isLong ? filters.LongAllowed : filters.ShortAllowed;
		}

		decimal SupervisorScore()
		{
			var w1 = SupervisorX1 - 100m;
			var w2 = SupervisorX2 - 100m;
			var w3 = SupervisorX3 - 100m;
			var w4 = SupervisorX4 - 100m;

			var shift = Math.Max(1, SupervisorShift);
			if (!_supervisorHistory.TryGetShift(shift, out var s1) ||
				!_supervisorHistory.TryGetShift(shift * 2, out var s2) ||
				!_supervisorHistory.TryGetShift(shift * 3, out var s3) ||
				!_supervisorHistory.TryGetShift(shift * 4, out var s4))
			{
				return 0m;
			}

			var a1 = supervisorClose - s1;
			var a2 = s1 - s2;
			var a3 = s2 - s3;
			var a4 = s3 - s4;
			return w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4 + (SupervisorThreshold - 100m);
		}

		decimal LongScore()
		{
			var w1 = LongX1 - 100m;
			var w2 = LongX2 - 100m;
			var w3 = LongX3 - 100m;
			var w4 = LongX4 - 100m;

			var shift = Math.Max(1, LongShift);
			if (!_longHistory.TryGetShift(shift, out var s1) ||
				!_longHistory.TryGetShift(shift * 2, out var s2) ||
				!_longHistory.TryGetShift(shift * 3, out var s3) ||
				!_longHistory.TryGetShift(shift * 4, out var s4))
			{
				return 0m;
			}

			var a1 = longClose - s1;
			var a2 = s1 - s2;
			var a3 = s2 - s3;
			var a4 = s3 - s4;
			return w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4 + (LongThreshold - 100m);
		}

		decimal ShortScore()
		{
			var w1 = ShortX1 - 100m;
			var w2 = ShortX2 - 100m;
			var w3 = ShortX3 - 100m;
			var w4 = ShortX4 - 100m;

			var shift = Math.Max(1, ShortShift);
			if (!_shortHistory.TryGetShift(shift, out var s1) ||
				!_shortHistory.TryGetShift(shift * 2, out var s2) ||
				!_shortHistory.TryGetShift(shift * 3, out var s3) ||
				!_shortHistory.TryGetShift(shift * 4, out var s4))
			{
				return 0m;
			}

			var a1 = shortClose - s1;
			var a2 = s1 - s2;
			var a3 = s2 - s3;
			var a4 = s3 - s4;
			return w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4 + (ShortThreshold - 100m);
		}

		switch (Mode)
		{
			case AutoRxdMode.Indicator:
			{
				if (EnableIndicatorManager && filters.LongAllowed)
				{
					EvaluateStops(true);
					decision = new TradeDecision(1, take, stop);
					break;
				}

				if (EnableIndicatorManager && filters.ShortAllowed)
				{
					EvaluateStops(false);
					decision = new TradeDecision(-1, take, stop);
				}

				break;
			}

			case AutoRxdMode.Grid:
				break;

			case AutoRxdMode.AiShort:
			{
				var score = ShortScore();
				if (score < 0m && IsAllowed(false))
				{
					EvaluateStops(false);
					decision = new TradeDecision(-1, take, stop);
				}
				break;
			}

			case AutoRxdMode.AiLong:
			{
				var score = LongScore();
				if (score > 0m && IsAllowed(true))
				{
					EvaluateStops(true);
					decision = new TradeDecision(1, take, stop);
				}
				break;
			}

			case AutoRxdMode.AiFilter:
			{
				var score = SupervisorScore();
				if (score > 0m && LongScore() > 0m && IsAllowed(true))
				{
					EvaluateStops(true);
					decision = new TradeDecision(1, take, stop);
				}
				else if (score < 0m && ShortScore() < 0m && IsAllowed(false))
				{
					EvaluateStops(false);
					decision = new TradeDecision(-1, take, stop);
				}
				break;
			}
		}

		return decision;
	}

	private (decimal? take, decimal? stop) CalculateProtection(bool isLong, decimal? atr)
	{
		if (AtrTpSlEnable && atr.HasValue)
		{
			var target = 4m * atr.Value * (isLong ? LongTakeProfitPoints : ShortTakeProfitPoints) / 100m;
			var stop = 3m * atr.Value * (isLong ? LongStopLossPoints : ShortStopLossPoints) / 100m;
			return (NormalizeDistance(target), NormalizeDistance(stop));
		}

		var take = (isLong ? LongTakeProfitPoints : ShortTakeProfitPoints) * _point;
		var stopLoss = (isLong ? LongStopLossPoints : ShortStopLossPoints) * _point;
		return (NormalizeDistance(take), NormalizeDistance(stopLoss));
	}

	private IndicatorFilters EvaluateFilters(
		decimal closePrice,
		decimal macd,
		decimal signal,
		decimal histogram,
		decimal? sar,
		AverageDirectionalIndexValue adx,
		decimal? rsi,
		decimal? cci,
		decimal? ao,
		decimal? ac)
	{
		var longOk = true;
		var shortOk = true;

		if (AdxControl)
		{
			var adxValue = adx.MovingAverage;
			var plus = adx.PlusDI;
			var minus = adx.MinusDI;

			var hasTrend = adxValue >= AdxThreshold;
			longOk &= hasTrend && plus > minus;
			shortOk &= hasTrend && minus > plus;
		}

		if (MacdControl)
		{
			var longCond = macd > signal;
			var shortCond = macd < signal;

			if (MacdCrossNeeded)
			{
				var prevMacd = _previousMacd ?? macd;
				var prevSignal = _previousSignal ?? signal;
				longCond &= prevMacd <= prevSignal;
				shortCond &= prevMacd >= prevSignal;
			}
			else
			{
				longCond &= macd > 0m;
				shortCond &= macd < 0m;
			}

			longOk &= longCond;
			shortOk &= shortCond;
		}

		if (OsmaControl)
		{
			longOk &= histogram > 0m;
			shortOk &= histogram < 0m;
		}

		if (SarControl && sar.HasValue)
		{
			var longCond = closePrice > sar.Value;
			var shortCond = closePrice < sar.Value;

			if (SarCrossNeeded && _previousSar.HasValue && _previousClose.HasValue)
			{
				longCond &= _previousClose.Value <= _previousSar.Value;
				shortCond &= _previousClose.Value >= _previousSar.Value;
			}

			longOk &= longCond;
			shortOk &= shortCond;
		}

		if (RsiControl && rsi.HasValue)
		{
			longOk &= rsi.Value > 50m;
			shortOk &= rsi.Value < 50m;
		}

		if (CciControl && cci.HasValue)
		{
			longOk &= cci.Value > 100m;
			shortOk &= cci.Value < -100m;
		}

		if (AoControl && ao.HasValue)
		{
			longOk &= ao.Value > 0m;
			shortOk &= ao.Value < 0m;
		}

		if (AcControl && ac.HasValue)
		{
			longOk &= ac.Value > 0m;
			shortOk &= ac.Value < 0m;
		}

		return new IndicatorFilters(longOk, shortOk);
	}

	private void ExecuteDecision(ICandleMessage candle, decimal? atr, TradeDecision decision)
	{
		if (decision.Direction == 0)
			return;

		if (decision.Direction > 0)
		{
			if (Position < 0)
				ClosePosition();

			if (Position <= 0)
			{
				BuyMarket(OrderVolume + Math.Abs(Position));

				if (decision.TakeProfit.HasValue && decision.TakeProfit.Value > 0m)
					SetTakeProfit(decision.TakeProfit.Value, candle.ClosePrice, Position);

				if (decision.StopLoss.HasValue && decision.StopLoss.Value > 0m)
					SetStopLoss(decision.StopLoss.Value, candle.ClosePrice, Position);
			}
		}
		else
		{
			if (Position > 0)
				ClosePosition();

			if (Position >= 0)
			{
				SellMarket(OrderVolume + Math.Max(Position, 0m));

				if (decision.TakeProfit.HasValue && decision.TakeProfit.Value > 0m)
					SetTakeProfit(decision.TakeProfit.Value, candle.ClosePrice, Position);

				if (decision.StopLoss.HasValue && decision.StopLoss.Value > 0m)
					SetStopLoss(decision.StopLoss.Value, candle.ClosePrice, Position);
			}
		}
	}

	private void HandleOrderCloseManager(ICandleMessage candle, decimal? sar)
	{
		if (!EnableOrderCloseManager || sar is null)
			return;

		if (Position > 0 && sar.Value >= candle.HighPrice)
			ClosePosition();

		if (Position < 0 && sar.Value <= candle.LowPrice)
			ClosePosition();
	}

	private bool IsWithinTradeWindow(TimeSpan current)
	{
		var start = TimeSpan.FromHours(Math.Clamp(HourTradeStartTime, 0, 23));
		var stop = TimeSpan.FromHours(Math.Clamp(HourTradeStopTime, 0, 23));

		if (start <= stop)
			return current >= start && current <= stop;

		return current >= start || current <= stop;
	}

	private decimal? NormalizeDistance(decimal value)
	{
		if (value <= 0m)
			return null;

		var step = Security?.PriceStep ?? _point;
		if (step <= 0m)
			return value;

		return Math.Round(value / step) * step;
	}

	private readonly struct IndicatorFilters
	{
		public IndicatorFilters(bool longAllowed, bool shortAllowed)
		{
			LongAllowed = longAllowed;
			ShortAllowed = shortAllowed;
		}

		public bool LongAllowed { get; }

		public bool ShortAllowed { get; }
	}

	private readonly struct TradeDecision
	{
		public TradeDecision(int direction, decimal? takeProfit, decimal? stopLoss)
		{
			Direction = direction;
			TakeProfit = takeProfit;
			StopLoss = stopLoss;
		}

		public int Direction { get; }

		public decimal? TakeProfit { get; }

		public decimal? StopLoss { get; }
	}

	private sealed class ShiftBuffer
	{
		private readonly List<decimal> _values = new();

		public bool Add(decimal value, int maxCapacity)
		{
			_values.Add(value);

			if (_values.Count > maxCapacity)
				_values.RemoveAt(0);

			return _values.Count >= Math.Min(maxCapacity, 4);
		}

		public bool TryGetShift(int shift, out decimal value)
		{
			var index = _values.Count - 1 - shift;
			if (index < 0 || index >= _values.Count)
			{
				value = default;
				return false;
			}

			value = _values[index];
			return true;
		}

		public void Clear()
		{
			_values.Clear();
		}
	}
}

/// <summary>
/// Operation modes for the Auto RXD strategy.
/// </summary>
public enum AutoRxdMode
{
	/// <summary>
	/// Uses only the indicator confirmation block.
	/// </summary>
	Indicator = 0,

	/// <summary>
	/// Grid mode is disabled in the port and therefore does not generate signals.
	/// </summary>
	Grid = 1,

	/// <summary>
	/// Short perceptron generates entries.
	/// </summary>
	AiShort = 2,

	/// <summary>
	/// Long perceptron generates entries.
	/// </summary>
	AiLong = 3,

	/// <summary>
	/// Supervisor perceptron validates long and short perceptrons.
	/// </summary>
	AiFilter = 4
}
