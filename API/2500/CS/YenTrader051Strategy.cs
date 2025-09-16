using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the Yen Trader expert advisor that trades a JPY cross with confirmation from a major pair and USDJPY.
/// </summary>
public class YenTrader051Strategy : Strategy
{
	private readonly StrategyParam<Security> _majorSecurity;
	private readonly StrategyParam<Security> _usdJpySecurity;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<YenTraderMajorDirection> _majorDirection;
	private readonly StrategyParam<YenTraderEntryMode> _entryMode;
	private readonly StrategyParam<YenTraderPriceReference> _priceReference;
	private readonly StrategyParam<int> _loopBackBars;
	private readonly StrategyParam<bool> _useRsiFilter;
	private readonly StrategyParam<bool> _useCciFilter;
	private readonly StrategyParam<bool> _useRviFilter;
	private readonly StrategyParam<bool> _useMovingAverageFilter;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<MovingAverageMode> _maMode;
	private readonly StrategyParam<decimal> _fixedLotSize;
	private readonly StrategyParam<decimal> _balancePercentLotSize;
	private readonly StrategyParam<int> _maxOpenPositions;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _breakEvenPips;
	private readonly StrategyParam<int> _profitLockPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<bool> _closeOnOpposite;
	private readonly StrategyParam<bool> _allowHedging;
	private readonly StrategyParam<bool> _enableAtrLevels;
	private readonly StrategyParam<DataType> _atrCandleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrStopLossMultiplier;
	private readonly StrategyParam<decimal> _atrTakeProfitMultiplier;
	private readonly StrategyParam<decimal> _atrTrailingMultiplier;
	private readonly StrategyParam<decimal> _atrBreakEvenMultiplier;
	private readonly StrategyParam<decimal> _atrProfitLockMultiplier;

	private Highest _majorHighest = null!;
	private Lowest _majorLowest = null!;
	private RelativeStrengthIndex _majorRsi = null!;
	private CommodityChannelIndex _majorCci = null!;
	private RelativeVigorIndex _majorRvi = null!;
	private SimpleMovingAverage _majorRviSignal = null!;
	private IIndicator _majorMa = null!;

	private Highest _usdJpyHighest = null!;
	private Lowest _usdJpyLowest = null!;
	private RelativeStrengthIndex _usdJpyRsi = null!;
	private CommodityChannelIndex _usdJpyCci = null!;
	private RelativeVigorIndex _usdJpyRvi = null!;
	private SimpleMovingAverage _usdJpyRviSignal = null!;
	private IIndicator _usdJpyMa = null!;

	private AverageTrueRange? _atr;

	private readonly Queue<decimal> _majorCloses = new();
	private readonly Queue<decimal> _usdJpyCloses = new();

	private decimal? _majorLastClose;
	private decimal? _majorLookbackClose;
	private decimal? _majorHighestValue;
	private decimal? _majorLowestValue;
	private decimal? _majorRsiValue;
	private decimal? _majorCciValue;
	private decimal? _majorRviValue;
	private decimal? _majorRviSignalValue;
	private decimal? _majorMaValue;

	private decimal? _usdJpyLastClose;
	private decimal? _usdJpyLookbackClose;
	private decimal? _usdJpyHighestValue;
	private decimal? _usdJpyLowestValue;
	private decimal? _usdJpyRsiValue;
	private decimal? _usdJpyCciValue;
	private decimal? _usdJpyRviValue;
	private decimal? _usdJpyRviSignalValue;
	private decimal? _usdJpyMaValue;

	private decimal? _atrValue;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private bool _breakEvenActivated;
	private bool _profitLockActivated;
	private decimal _highestSinceEntry;
	private decimal _lowestSinceEntry;

	/// <summary>
	/// Initializes a new instance of the <see cref="YenTrader051Strategy"/> class.
	/// </summary>
	public YenTrader051Strategy()
	{
		_majorSecurity = Param(nameof(MajorSecurity), default(Security))
			.SetDisplay("Major Security", "Major currency pair used for confirmation", "Instruments");
		_usdJpySecurity = Param(nameof(UsdJpySecurity), default(Security))
			.SetDisplay("USDJPY Security", "USDJPY pair used for confirmation", "Instruments");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Signal Candles", "Primary timeframe for signals", "Data");
		_majorDirection = Param(nameof(MajorDirection), YenTraderMajorDirection.Left)
			.SetDisplay("Major Direction", "Alignment between major and cross", "Filters");
		_entryMode = Param(nameof(EntryMode), YenTraderEntryMode.Both)
			.SetDisplay("Entry Mode", "Control averaging or pyramiding behaviour", "Filters");
		_priceReference = Param(nameof(PriceReference), YenTraderPriceReference.HighLow)
			.SetDisplay("Price Reference", "Breakout reference for loop back bars", "Filters");
		_loopBackBars = Param(nameof(LoopBackBars), 2)
			.SetDisplay("Loop Back Bars", "Number of historical bars for breakout logic", "Filters");
		_useRsiFilter = Param(nameof(UseRsiFilter), true)
			.SetDisplay("Use RSI", "Enable RSI confirmation filter", "Indicators");
		_useCciFilter = Param(nameof(UseCciFilter), true)
			.SetDisplay("Use CCI", "Enable CCI confirmation filter", "Indicators");
		_useRviFilter = Param(nameof(UseRviFilter), true)
			.SetDisplay("Use RVI", "Enable RVI confirmation filter", "Indicators");
		_useMovingAverageFilter = Param(nameof(UseMovingAverageFilter), true)
			.SetDisplay("Use Moving Average", "Enable moving average confirmation filter", "Indicators");
		_maPeriod = Param(nameof(MaPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period", "Indicators");
		_maMode = Param(nameof(MaMode), MovingAverageMode.Smoothed)
			.SetDisplay("MA Mode", "Moving average calculation mode", "Indicators");
		_fixedLotSize = Param(nameof(FixedLotSize), 0m)
			.SetDisplay("Fixed Volume", "Fixed volume per trade (0 = disabled)", "Risk");
		_balancePercentLotSize = Param(nameof(BalancePercentLotSize), 1m)
			.SetDisplay("Balance Percent Volume", "Portfolio percent used to size trades when fixed volume is disabled", "Risk");
		_maxOpenPositions = Param(nameof(MaxOpenPositions), 10)
			.SetDisplay("Max Positions", "Maximum number of additive entries", "Risk");
		_stopLossPips = Param(nameof(StopLossPips), 1000)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");
		_takeProfitPips = Param(nameof(TakeProfitPips), 5000)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");
		_breakEvenPips = Param(nameof(BreakEvenPips), 200)
			.SetDisplay("Break Even (pips)", "Distance before moving stop to break even", "Risk");
		_profitLockPips = Param(nameof(ProfitLockPips), 200)
			.SetDisplay("Profit Lock (pips)", "Distance before locking additional profit", "Risk");
		_trailingStopPips = Param(nameof(TrailingStopPips), 200)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");
		_trailingStepPips = Param(nameof(TrailingStepPips), 10)
			.SetDisplay("Trailing Step (pips)", "Minimum trailing stop step in pips", "Risk");
		_closeOnOpposite = Param(nameof(CloseOnOpposite), false)
			.SetDisplay("Close On Opposite", "Close current position when opposite signal appears", "Risk");
		_allowHedging = Param(nameof(AllowHedging), true)
			.SetDisplay("Allow Hedging", "Allow simultaneous trades without closing existing ones", "Risk");
		_enableAtrLevels = Param(nameof(EnableAtrLevels), false)
			.SetDisplay("Use ATR Levels", "Use ATR based distances instead of pips", "Risk");
		_atrCandleType = Param(nameof(AtrCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("ATR Candles", "Timeframe for ATR calculations", "Risk");
		_atrPeriod = Param(nameof(AtrPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR lookback period", "Risk");
		_atrStopLossMultiplier = Param(nameof(AtrStopLossMultiplier), 2m)
			.SetDisplay("ATR SL Multiplier", "ATR multiplier for stop loss", "Risk");
		_atrTakeProfitMultiplier = Param(nameof(AtrTakeProfitMultiplier), 4m)
			.SetDisplay("ATR TP Multiplier", "ATR multiplier for take profit", "Risk");
		_atrTrailingMultiplier = Param(nameof(AtrTrailingMultiplier), 1m)
			.SetDisplay("ATR Trail Multiplier", "ATR multiplier for trailing stop", "Risk");
		_atrBreakEvenMultiplier = Param(nameof(AtrBreakEvenMultiplier), 0.5m)
			.SetDisplay("ATR BE Multiplier", "ATR multiplier for break even distance", "Risk");
		_atrProfitLockMultiplier = Param(nameof(AtrProfitLockMultiplier), 2m)
			.SetDisplay("ATR PL Multiplier", "ATR multiplier for profit lock distance", "Risk");
	}

	/// <summary>
	/// Major pair used for confirmation.
	/// </summary>
	public Security? MajorSecurity
	{
		get => _majorSecurity.Value;
		set => _majorSecurity.Value = value;
	}

	/// <summary>
	/// USDJPY pair used for confirmation.
	/// </summary>
	public Security? UsdJpySecurity
	{
		get => _usdJpySecurity.Value;
		set => _usdJpySecurity.Value = value;
	}

	/// <summary>
	/// Main candle type used for trading signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Relationship between the major pair and the traded cross.
	/// </summary>
	public YenTraderMajorDirection MajorDirection
	{
		get => _majorDirection.Value;
		set => _majorDirection.Value = value;
	}

	/// <summary>
	/// Entry behaviour when stacking orders.
	/// </summary>
	public YenTraderEntryMode EntryMode
	{
		get => _entryMode.Value;
		set => _entryMode.Value = value;
	}

	/// <summary>
	/// Price reference used for breakout detection.
	/// </summary>
	public YenTraderPriceReference PriceReference
	{
		get => _priceReference.Value;
		set => _priceReference.Value = value;
	}

	/// <summary>
	/// Number of bars used for breakout checks.
	/// </summary>
	public int LoopBackBars
	{
		get => _loopBackBars.Value;
		set => _loopBackBars.Value = value;
	}

	/// <summary>
	/// Enable RSI confirmation.
	/// </summary>
	public bool UseRsiFilter
	{
		get => _useRsiFilter.Value;
		set => _useRsiFilter.Value = value;
	}

	/// <summary>
	/// Enable CCI confirmation.
	/// </summary>
	public bool UseCciFilter
	{
		get => _useCciFilter.Value;
		set => _useCciFilter.Value = value;
	}

	/// <summary>
	/// Enable RVI confirmation.
	/// </summary>
	public bool UseRviFilter
	{
		get => _useRviFilter.Value;
		set => _useRviFilter.Value = value;
	}

	/// <summary>
	/// Enable moving average confirmation.
	/// </summary>
	public bool UseMovingAverageFilter
	{
		get => _useMovingAverageFilter.Value;
		set => _useMovingAverageFilter.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Moving average calculation mode.
	/// </summary>
	public MovingAverageMode MaMode
	{
		get => _maMode.Value;
		set => _maMode.Value = value;
	}

	/// <summary>
	/// Fixed volume per trade.
	/// </summary>
	public decimal FixedLotSize
	{
		get => _fixedLotSize.Value;
		set => _fixedLotSize.Value = value;
	}

	/// <summary>
	/// Percentage of portfolio balance used when variable sizing is active.
	/// </summary>
	public decimal BalancePercentLotSize
	{
		get => _balancePercentLotSize.Value;
		set => _balancePercentLotSize.Value = value;
	}

	/// <summary>
	/// Maximum number of additive entries.
	/// </summary>
	public int MaxOpenPositions
	{
		get => _maxOpenPositions.Value;
		set => _maxOpenPositions.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Break even trigger distance in pips.
	/// </summary>
	public int BreakEvenPips
	{
		get => _breakEvenPips.Value;
		set => _breakEvenPips.Value = value;
	}

	/// <summary>
	/// Profit lock trigger distance in pips.
	/// </summary>
	public int ProfitLockPips
	{
		get => _profitLockPips.Value;
		set => _profitLockPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum trailing stop update step in pips.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Close current position when an opposite signal is generated.
	/// </summary>
	public bool CloseOnOpposite
	{
		get => _closeOnOpposite.Value;
		set => _closeOnOpposite.Value = value;
	}

	/// <summary>
	/// Allow adding positions even if an opposite trade is still open.
	/// </summary>
	public bool AllowHedging
	{
		get => _allowHedging.Value;
		set => _allowHedging.Value = value;
	}

	/// <summary>
	/// Use ATR based levels instead of pip distances.
	/// </summary>
	public bool EnableAtrLevels
	{
		get => _enableAtrLevels.Value;
		set => _enableAtrLevels.Value = value;
	}

	/// <summary>
	/// Candle type used for ATR calculations.
	/// </summary>
	public DataType AtrCandleType
	{
		get => _atrCandleType.Value;
		set => _atrCandleType.Value = value;
	}

	/// <summary>
	/// ATR lookback period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop loss.
	/// </summary>
	public decimal AtrStopLossMultiplier
	{
		get => _atrStopLossMultiplier.Value;
		set => _atrStopLossMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier for take profit.
	/// </summary>
	public decimal AtrTakeProfitMultiplier
	{
		get => _atrTakeProfitMultiplier.Value;
		set => _atrTakeProfitMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier for trailing stop distance.
	/// </summary>
	public decimal AtrTrailingMultiplier
	{
		get => _atrTrailingMultiplier.Value;
		set => _atrTrailingMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier for break even activation.
	/// </summary>
	public decimal AtrBreakEvenMultiplier
	{
		get => _atrBreakEvenMultiplier.Value;
		set => _atrBreakEvenMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier for profit lock activation.
	/// </summary>
	public decimal AtrProfitLockMultiplier
	{
		get => _atrProfitLockMultiplier.Value;
		set => _atrProfitLockMultiplier.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);

		if (MajorSecurity != null)
			yield return (MajorSecurity, CandleType);

		if (UsdJpySecurity != null)
			yield return (UsdJpySecurity, CandleType);

		if (EnableAtrLevels && Security != null)
			yield return (Security, AtrCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializeIndicators();

		var tradingSubscription = SubscribeCandles(CandleType);
		tradingSubscription.WhenNew(ProcessTradingCandle).Start();

		if (MajorSecurity != null)
		{
			var majorSubscription = SubscribeCandles(CandleType, true, MajorSecurity);
			majorSubscription.WhenNew(ProcessMajorCandle).Start();
		}

		if (UsdJpySecurity != null)
		{
			var usdJpySubscription = SubscribeCandles(CandleType, true, UsdJpySecurity);
			usdJpySubscription.WhenNew(ProcessUsdJpyCandle).Start();
		}

		if (EnableAtrLevels)
		{
			_atr = new AverageTrueRange { Length = AtrPeriod };
			var atrSubscription = SubscribeCandles(AtrCandleType, true, Security);
			atrSubscription.WhenNew(ProcessAtrCandle).Start();
		}
		else
		{
			_atr = null;
		}

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, tradingSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessMajorCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateBreakoutIndicators(candle, _majorHighest, _majorLowest, ref _majorHighestValue, ref _majorLowestValue);
		UpdateOscillators(candle, _majorRsi, ref _majorRsiValue, _majorCci, ref _majorCciValue, _majorRvi, _majorRviSignal, ref _majorRviValue, ref _majorRviSignalValue);
		_majorMaValue = UpdateMovingAverage(_majorMa, candle);

		_majorLastClose = candle.ClosePrice;
		UpdateLookbackQueue(_majorCloses, LoopBackBars, candle.ClosePrice, ref _majorLookbackClose);
	}

	private void ProcessUsdJpyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateBreakoutIndicators(candle, _usdJpyHighest, _usdJpyLowest, ref _usdJpyHighestValue, ref _usdJpyLowestValue);
		UpdateOscillators(candle, _usdJpyRsi, ref _usdJpyRsiValue, _usdJpyCci, ref _usdJpyCciValue, _usdJpyRvi, _usdJpyRviSignal, ref _usdJpyRviValue, ref _usdJpyRviSignalValue);
		_usdJpyMaValue = UpdateMovingAverage(_usdJpyMa, candle);

		_usdJpyLastClose = candle.ClosePrice;
		UpdateLookbackQueue(_usdJpyCloses, LoopBackBars, candle.ClosePrice, ref _usdJpyLookbackClose);
	}

	private void ProcessAtrCandle(ICandleMessage candle)
	{
		if (!EnableAtrLevels || _atr == null)
			return;

		if (candle.State != CandleStates.Finished)
			return;

		var atrValue = _atr.Process(candle);
		if (atrValue.IsFinal)
			_atrValue = atrValue.ToDecimal();
	}

	private void ProcessTradingCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateRiskManagement(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsSignalReady())
			return;

		var longSignal = CalculateBreakoutSignal(true);
		var shortSignal = CalculateBreakoutSignal(false);

		ApplyEntryMode(candle, ref longSignal, ref shortSignal);
		ApplyIndicatorFilters(ref longSignal, ref shortSignal);

		if (longSignal)
			TryEnterLong(candle);

		if (shortSignal)
			TryEnterShort(candle);
	}

	private void ApplyEntryMode(ICandleMessage candle, ref bool longSignal, ref bool shortSignal)
	{
		if (EntryMode == YenTraderEntryMode.Averaging)
		{
			longSignal &= candle.ClosePrice < candle.OpenPrice;
			shortSignal &= candle.ClosePrice > candle.OpenPrice;
		}
		else if (EntryMode == YenTraderEntryMode.Pyramiding)
		{
			longSignal &= candle.ClosePrice > candle.OpenPrice;
			shortSignal &= candle.ClosePrice < candle.OpenPrice;
		}
	}

	private void ApplyIndicatorFilters(ref bool longSignal, ref bool shortSignal)
	{
		if (!longSignal && !shortSignal)
			return;

		if (UseRsiFilter)
		{
			if (!_majorRsiValue.HasValue || !_usdJpyRsiValue.HasValue)
			{
				longSignal = false;
				shortSignal = false;
			}
			else if (MajorDirection == YenTraderMajorDirection.Left)
			{
				longSignal &= _majorRsiValue > 50m && _usdJpyRsiValue > 50m;
				shortSignal &= _majorRsiValue < 50m && _usdJpyRsiValue < 50m;
			}
			else
			{
				longSignal &= _majorRsiValue < 50m && _usdJpyRsiValue > 50m;
				shortSignal &= _majorRsiValue > 50m && _usdJpyRsiValue < 50m;
			}
		}

		if (UseCciFilter && (longSignal || shortSignal))
		{
			if (!_majorCciValue.HasValue || !_usdJpyCciValue.HasValue)
			{
				longSignal = false;
				shortSignal = false;
			}
			else if (MajorDirection == YenTraderMajorDirection.Left)
			{
				longSignal &= _majorCciValue > 0m && _usdJpyCciValue > 0m;
				shortSignal &= _majorCciValue < 0m && _usdJpyCciValue < 0m;
			}
			else
			{
				longSignal &= _majorCciValue < 0m && _usdJpyCciValue > 0m;
				shortSignal &= _majorCciValue > 0m && _usdJpyCciValue < 0m;
			}
		}

		if (UseRviFilter && (longSignal || shortSignal))
		{
			if (!_majorRviValue.HasValue || !_usdJpyRviValue.HasValue || !_majorRviSignalValue.HasValue || !_usdJpyRviSignalValue.HasValue)
			{
				longSignal = false;
				shortSignal = false;
			}
			else if (MajorDirection == YenTraderMajorDirection.Left)
			{
				longSignal &= _majorRviValue > _majorRviSignalValue && _usdJpyRviValue > _usdJpyRviSignalValue;
				shortSignal &= _majorRviValue < _majorRviSignalValue && _usdJpyRviValue < _usdJpyRviSignalValue;
			}
			else
			{
				longSignal &= _majorRviValue < _majorRviSignalValue && _usdJpyRviValue > _usdJpyRviSignalValue;
				shortSignal &= _majorRviValue > _majorRviSignalValue && _usdJpyRviValue < _usdJpyRviSignalValue;
			}
		}

		if (UseMovingAverageFilter && (longSignal || shortSignal))
		{
			if (!_majorMaValue.HasValue || !_usdJpyMaValue.HasValue || !_majorLastClose.HasValue || !_usdJpyLastClose.HasValue)
			{
				longSignal = false;
				shortSignal = false;
			}
			else if (MajorDirection == YenTraderMajorDirection.Left)
			{
				longSignal &= _majorLastClose > _majorMaValue && _usdJpyLastClose > _usdJpyMaValue;
				shortSignal &= _majorLastClose < _majorMaValue && _usdJpyLastClose < _usdJpyMaValue;
			}
			else
			{
				longSignal &= _majorLastClose < _majorMaValue && _usdJpyLastClose > _usdJpyMaValue;
				shortSignal &= _majorLastClose > _majorMaValue && _usdJpyLastClose < _usdJpyMaValue;
			}
		}
	}

	private bool CalculateBreakoutSignal(bool isLong)
	{
		if (_majorLastClose == null || _usdJpyLastClose == null)
			return false;

		if (LoopBackBars <= 1)
			return true;

		if (PriceReference == YenTraderPriceReference.HighLow)
		{
			if (_majorHighestValue == null || _majorLowestValue == null || _usdJpyHighestValue == null || _usdJpyLowestValue == null)
				return false;

			return MajorDirection == YenTraderMajorDirection.Left
				? isLong
					? _majorLastClose > _majorHighestValue && _usdJpyLastClose > _usdJpyHighestValue
					: _majorLastClose < _majorLowestValue && _usdJpyLastClose < _usdJpyLowestValue
				: isLong
					? _majorLastClose < _majorLowestValue && _usdJpyLastClose > _usdJpyHighestValue
					: _majorLastClose > _majorHighestValue && _usdJpyLastClose < _usdJpyLowestValue;
		}

		if (_majorLookbackClose == null || _usdJpyLookbackClose == null)
			return false;

		return MajorDirection == YenTraderMajorDirection.Left
			? isLong
				? _majorLastClose > _majorLookbackClose && _usdJpyLastClose > _usdJpyLookbackClose
				: _majorLastClose < _majorLookbackClose && _usdJpyLastClose < _usdJpyLookbackClose
			: isLong
				? _majorLastClose < _majorLookbackClose && _usdJpyLastClose > _usdJpyLookbackClose
				: _majorLastClose > _majorLookbackClose && _usdJpyLastClose < _usdJpyLookbackClose;
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		if (!AllowHedging && Position < 0)
			return;

		var orderVolume = GetOrderVolume(candle.ClosePrice, Sides.Buy);
		if (orderVolume <= 0m)
			return;

		var totalVolume = orderVolume;
		if (CloseOnOpposite && Position < 0)
			totalVolume += Math.Abs(Position);

		if (totalVolume <= 0m)
			return;

		BuyMarket(totalVolume);
		InitializePositionState(candle, Sides.Buy);
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		if (!AllowHedging && Position > 0)
			return;

		var orderVolume = GetOrderVolume(candle.ClosePrice, Sides.Sell);
		if (orderVolume <= 0m)
			return;

		var totalVolume = orderVolume;
		if (CloseOnOpposite && Position > 0)
			totalVolume += Math.Abs(Position);

		if (totalVolume <= 0m)
			return;

		SellMarket(totalVolume);
		InitializePositionState(candle, Sides.Sell);
	}

	private void InitializePositionState(ICandleMessage candle, Sides side)
	{
		_entryPrice = candle.ClosePrice;
		_stopPrice = null;
		_takeProfitPrice = null;
		_breakEvenActivated = false;
		_profitLockActivated = false;
		_highestSinceEntry = candle.HighPrice;
		_lowestSinceEntry = candle.LowPrice;

		var atrDistance = EnableAtrLevels ? _atrValue : null;
		var stopDistance = GetDistance(StopLossPips, AtrStopLossMultiplier, atrDistance);
		var takeDistance = GetDistance(TakeProfitPips, AtrTakeProfitMultiplier, atrDistance);

		if (side == Sides.Buy)
		{
			if (stopDistance > 0m)
				_stopPrice = _entryPrice - stopDistance;

			if (takeDistance > 0m)
				_takeProfitPrice = _entryPrice + takeDistance;
		}
		else
		{
			if (stopDistance > 0m)
				_stopPrice = _entryPrice + stopDistance;

			if (takeDistance > 0m)
				_takeProfitPrice = _entryPrice - takeDistance;
		}
	}

	private void UpdateRiskManagement(ICandleMessage candle)
	{
		if (Position == 0)
		{
			ResetPositionState();
			return;
		}

		if (_entryPrice == null)
			return;

		if (Position > 0)
		{
			_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);

			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			ApplyTrailingRules(candle, Sides.Buy);
		}
		else
		{
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, candle.LowPrice);

			if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			ApplyTrailingRules(candle, Sides.Sell);
		}
	}

	private void ApplyTrailingRules(ICandleMessage candle, Sides side)
	{
		if (_entryPrice == null)
			return;

		var atrDistance = EnableAtrLevels ? _atrValue : null;
		var breakEvenDistance = GetDistance(BreakEvenPips, AtrBreakEvenMultiplier, atrDistance);
		var profitLockDistance = GetDistance(ProfitLockPips, AtrProfitLockMultiplier, atrDistance);
		var trailingDistance = GetDistance(TrailingStopPips, AtrTrailingMultiplier, atrDistance);
		var trailingStep = ConvertPipsToPrice(TrailingStepPips);

		if (side == Sides.Buy)
		{
			if (!_breakEvenActivated && breakEvenDistance > 0m && candle.HighPrice >= _entryPrice + breakEvenDistance)
			{
				var newStop = _entryPrice + breakEvenDistance;
				_stopPrice = _stopPrice.HasValue ? Math.Max(_stopPrice.Value, newStop) : newStop;
				_breakEvenActivated = true;
			}

			if (!_profitLockActivated && profitLockDistance > 0m && candle.HighPrice >= _entryPrice + profitLockDistance)
			{
				var newStop = _entryPrice + profitLockDistance;
				_stopPrice = _stopPrice.HasValue ? Math.Max(_stopPrice.Value, newStop) : newStop;
				_profitLockActivated = true;
			}

			if (trailingDistance > 0m)
			{
				var desiredStop = Math.Max(_entryPrice.Value, candle.HighPrice - trailingDistance);
				if (!_stopPrice.HasValue || desiredStop > _stopPrice.Value + trailingStep)
					_stopPrice = desiredStop;
			}
		}
		else
		{
			if (!_breakEvenActivated && breakEvenDistance > 0m && candle.LowPrice <= _entryPrice - breakEvenDistance)
			{
				var newStop = _entryPrice - breakEvenDistance;
				_stopPrice = _stopPrice.HasValue ? Math.Min(_stopPrice.Value, newStop) : newStop;
				_breakEvenActivated = true;
			}

			if (!_profitLockActivated && profitLockDistance > 0m && candle.LowPrice <= _entryPrice - profitLockDistance)
			{
				var newStop = _entryPrice - profitLockDistance;
				_stopPrice = _stopPrice.HasValue ? Math.Min(_stopPrice.Value, newStop) : newStop;
				_profitLockActivated = true;
			}

			if (trailingDistance > 0m)
			{
				var desiredStop = Math.Min(_entryPrice.Value, candle.LowPrice + trailingDistance);
				if (!_stopPrice.HasValue || desiredStop < _stopPrice.Value - trailingStep)
					_stopPrice = desiredStop;
			}
		}
	}

	private decimal GetOrderVolume(decimal price, Sides side)
	{
		var baseVolume = FixedLotSize > 0m ? FixedLotSize : Volume;

		if (FixedLotSize <= 0m && BalancePercentLotSize > 0m && Portfolio != null && price > 0m)
		{
			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			if (portfolioValue > 0m)
				baseVolume = portfolioValue * BalancePercentLotSize / 100m / price;
		}

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m && baseVolume > 0m)
			baseVolume = Math.Max(step, Math.Round(baseVolume / step) * step);

		if (MaxOpenPositions > 0 && Security != null)
		{
			var current = side == Sides.Buy ? Math.Max(0m, Position) : Math.Max(0m, -Position);
			var maxVolume = baseVolume * MaxOpenPositions;
			var available = maxVolume - current;
			if (available <= 0m)
				return 0m;

			baseVolume = Math.Min(baseVolume, available);
		}

		return Math.Max(0m, baseVolume);
	}

	private decimal GetDistance(int pips, decimal multiplier, decimal? atrValue)
	{
		if (EnableAtrLevels && atrValue.HasValue)
			return atrValue.Value * multiplier;

		if (pips <= 0)
			return 0m;

		return ConvertPipsToPrice(pips);
	}

	private decimal ConvertPipsToPrice(int pips)
	{
		var step = Security?.MinPriceStep ?? 0m;
		return step > 0m ? pips * step : pips;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_breakEvenActivated = false;
		_profitLockActivated = false;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
	}

	private void ResetState()
	{
		_majorCloses.Clear();
		_usdJpyCloses.Clear();

		_majorLastClose = null;
		_majorLookbackClose = null;
		_majorHighestValue = null;
		_majorLowestValue = null;
		_majorRsiValue = null;
		_majorCciValue = null;
		_majorRviValue = null;
		_majorRviSignalValue = null;
		_majorMaValue = null;

		_usdJpyLastClose = null;
		_usdJpyLookbackClose = null;
		_usdJpyHighestValue = null;
		_usdJpyLowestValue = null;
		_usdJpyRsiValue = null;
		_usdJpyCciValue = null;
		_usdJpyRviValue = null;
		_usdJpyRviSignalValue = null;
		_usdJpyMaValue = null;

		_atrValue = null;

		ResetPositionState();
	}

	private bool IsSignalReady()
	{
		if (_majorLastClose == null || _usdJpyLastClose == null)
			return false;

		if (LoopBackBars > 1)
		{
			if (PriceReference == YenTraderPriceReference.HighLow)
			{
				if (_majorHighestValue == null || _majorLowestValue == null || _usdJpyHighestValue == null || _usdJpyLowestValue == null)
					return false;
			}
			else
			{
				if (_majorLookbackClose == null || _usdJpyLookbackClose == null)
					return false;
			}
		}

		if (UseRsiFilter && (!_majorRsiValue.HasValue || !_usdJpyRsiValue.HasValue))
			return false;

		if (UseCciFilter && (!_majorCciValue.HasValue || !_usdJpyCciValue.HasValue))
			return false;

		if (UseRviFilter && (!_majorRviValue.HasValue || !_majorRviSignalValue.HasValue || !_usdJpyRviValue.HasValue || !_usdJpyRviSignalValue.HasValue))
			return false;

		if (UseMovingAverageFilter && (!_majorMaValue.HasValue || !_usdJpyMaValue.HasValue))
			return false;

		return true;
	}

	private void InitializeIndicators()
	{
		var breakoutLength = Math.Max(LoopBackBars, 2);

		_majorHighest = new Highest { Length = breakoutLength };
		_majorLowest = new Lowest { Length = breakoutLength };
		_majorRsi = new RelativeStrengthIndex { Length = 14 };
		_majorCci = new CommodityChannelIndex { Length = 14 };
		_majorRvi = new RelativeVigorIndex { Length = 10 };
		_majorRviSignal = new SimpleMovingAverage { Length = 4 };
		_majorMa = CreateMovingAverage(MaMode, MaPeriod);

		_usdJpyHighest = new Highest { Length = breakoutLength };
		_usdJpyLowest = new Lowest { Length = breakoutLength };
		_usdJpyRsi = new RelativeStrengthIndex { Length = 14 };
		_usdJpyCci = new CommodityChannelIndex { Length = 14 };
		_usdJpyRvi = new RelativeVigorIndex { Length = 10 };
		_usdJpyRviSignal = new SimpleMovingAverage { Length = 4 };
		_usdJpyMa = CreateMovingAverage(MaMode, MaPeriod);
	}

	private static void UpdateBreakoutIndicators(ICandleMessage candle, Highest highest, Lowest lowest, ref decimal? highValue, ref decimal? lowValue)
	{
		var highVal = highest.Process(candle);
		if (highVal.IsFinal)
			highValue = highVal.ToDecimal();

		var lowVal = lowest.Process(candle);
		if (lowVal.IsFinal)
			lowValue = lowVal.ToDecimal();
	}

	private static void UpdateOscillators(
		ICandleMessage candle,
		RelativeStrengthIndex rsi,
		ref decimal? rsiValue,
		CommodityChannelIndex cci,
		ref decimal? cciValue,
		RelativeVigorIndex rvi,
		SimpleMovingAverage rviSignal,
		ref decimal? rviMain,
		ref decimal? rviSignalValue)
	{
		var rsiVal = rsi.Process(candle);
		if (rsiVal.IsFinal)
			rsiValue = rsiVal.ToDecimal();

		var cciVal = cci.Process(candle);
		if (cciVal.IsFinal)
			cciValue = cciVal.ToDecimal();

		var rviVal = rvi.Process(candle);
		if (rviVal.IsFinal)
		{
			var main = rviVal.ToDecimal();
			rviMain = main;

			var signalVal = rviSignal.Process(main, candle.CloseTime, true);
			if (signalVal.IsFinal)
				rviSignalValue = signalVal.ToDecimal();
		}
	}

	private static decimal? UpdateMovingAverage(IIndicator indicator, ICandleMessage candle)
	{
		var value = indicator.Process(new DecimalIndicatorValue(indicator, candle.ClosePrice, candle.CloseTime));
		return value.IsFinal ? value.ToDecimal() : null;
	}

	private static void UpdateLookbackQueue(Queue<decimal> queue, int loopBackBars, decimal close, ref decimal? lookback)
	{
		queue.Enqueue(close);

		var maxCount = Math.Max(loopBackBars + 1, 2);
		while (queue.Count > maxCount)
			queue.Dequeue();

		if (loopBackBars > 0 && queue.Count > loopBackBars)
		{
			var values = queue.ToArray();
			var index = values.Length - 1 - loopBackBars;
			if (index >= 0)
				lookback = values[index];
		}
		else
		{
			lookback = null;
		}
	}

	private static IIndicator CreateMovingAverage(MovingAverageMode mode, int period)
	{
		var length = Math.Max(1, period);

		return mode switch
		{
			MovingAverageMode.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMode.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMode.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMode.LinearWeighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Entry stacking behaviour.
	/// </summary>
	public enum YenTraderEntryMode
	{
		/// <summary>Allow both averaging and pyramiding entries.</summary>
		Both,
		/// <summary>Only add to profitable trades.</summary>
		Pyramiding,
		/// <summary>Only add to losing trades.</summary>
		Averaging
	}

	/// <summary>
	/// Mapping between major pair and traded cross.
	/// </summary>
	public enum YenTraderMajorDirection
	{
		/// <summary>Major pair acts as the left component.</summary>
		Left,
		/// <summary>Major pair acts as the right component.</summary>
		Right
	}

	/// <summary>
	/// Breakout reference type.
	/// </summary>
	public enum YenTraderPriceReference
	{
		/// <summary>Use delayed close values.</summary>
		Close,
		/// <summary>Use highest highs and lowest lows.</summary>
		HighLow
	}

	/// <summary>
	/// Moving average calculation modes supported by the strategy.
	/// </summary>
	public enum MovingAverageMode
	{
		/// <summary>Simple moving average.</summary>
		Simple,
		/// <summary>Exponential moving average.</summary>
		Exponential,
		/// <summary>Smoothed moving average.</summary>
		Smoothed,
		/// <summary>Linear weighted moving average.</summary>
		LinearWeighted
	}
}
