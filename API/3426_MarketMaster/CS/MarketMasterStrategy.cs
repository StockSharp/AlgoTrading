
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
/// High-level port of the Market Master expert advisor.
/// Combines ATR, Money Flow Index, Bulls/Bears Power, Stochastic and Parabolic SAR filters.
/// </summary>
public class MarketMasterStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _useAutoVolume;
	private readonly StrategyParam<decimal> _riskMultiplier;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<decimal> _minHedgeVolume;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _atrHedgePeriod;
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<int> _bullBearPeriod;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<decimal> _stochasticBuyLevel;
	private readonly StrategyParam<decimal> _stochasticSellLevel;
	private readonly StrategyParam<bool> _useStochasticFilter;
	private readonly StrategyParam<bool> _usePsarFilter;
	private readonly StrategyParam<bool> _usePsarConfirmation;
	private readonly StrategyParam<decimal> _psarStep;
	private readonly StrategyParam<decimal> _psarMaxStep;
	private readonly StrategyParam<decimal> _psarConfirmStep;
	private readonly StrategyParam<decimal> _psarConfirmMaxStep;
	private readonly StrategyParam<bool> _allowSameSignalEntries;
	private readonly StrategyParam<bool> _allowOppositeEntries;
	private readonly StrategyParam<bool> _useTradingWindow;
	private readonly StrategyParam<TimeSpan> _tradingStart;
	private readonly StrategyParam<TimeSpan> _tradingEnd;
	private readonly StrategyParam<bool> _useTradingBreak;
	private readonly StrategyParam<TimeSpan> _breakStart;
	private readonly StrategyParam<TimeSpan> _breakEnd;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr = null!;
	private AverageTrueRange _atrHedge = null!;
	private MoneyFlowIndex _mfi = null!;
	private BullsPower _bulls = null!;
	private BearsPower _bears = null!;
	private StochasticOscillator _stochastic = null!;
	private ParabolicSar _psar = null!;
	private ParabolicSar _psarConfirm = null!;

	private decimal? _prevAtr;
	private decimal? _prevAtrHedge;
	private decimal? _prevMfi;
	private decimal? _prevStochasticMain;
	private decimal? _prevStochasticSignal;

	/// <summary>
	/// Trading volume for the initial order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Automatically scale volume based on portfolio size.
	/// </summary>
	public bool UseAutoVolume
	{
		get => _useAutoVolume.Value;
		set => _useAutoVolume.Value = value;
	}

	/// <summary>
	/// Risk multiplier used by the auto volume block.
	/// </summary>
	public decimal RiskMultiplier
	{
		get => _riskMultiplier.Value;
		set => _riskMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier applied when pyramiding into existing positions.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Minimum volume on the trading timeframe required to open the first position.
	/// </summary>
	public decimal MinVolume
	{
		get => _minVolume.Value;
		set => _minVolume.Value = value;
	}

	/// <summary>
	/// Minimum volume on the trading timeframe required to add or hedge positions.
	/// </summary>
	public decimal MinHedgeVolume
	{
		get => _minHedgeVolume.Value;
		set => _minHedgeVolume.Value = value;
	}

	/// <summary>
	/// ATR period used for the primary trade filter.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR period used when additional or hedge entries are considered.
	/// </summary>
	public int AtrHedgePeriod
	{
		get => _atrHedgePeriod.Value;
		set => _atrHedgePeriod.Value = value;
	}

	/// <summary>
	/// Money Flow Index period.
	/// </summary>
	public int MfiPeriod
	{
		get => _mfiPeriod.Value;
		set => _mfiPeriod.Value = value;
	}

	/// <summary>
	/// Bulls/Bears Power period.
	/// </summary>
	public int BullBearPeriod
	{
		get => _bullBearPeriod.Value;
		set => _bullBearPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D period.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic slowing period.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <summary>
	/// Maximum Stochastic value that still allows long entries.
	/// </summary>
	public decimal StochasticBuyLevel
	{
		get => _stochasticBuyLevel.Value;
		set => _stochasticBuyLevel.Value = value;
	}

	/// <summary>
	/// Minimum Stochastic value that confirms short setups.
	/// </summary>
	public decimal StochasticSellLevel
	{
		get => _stochasticSellLevel.Value;
		set => _stochasticSellLevel.Value = value;
	}

	/// <summary>
	/// Enable Stochastic filter.
	/// </summary>
	public bool UseStochasticFilter
	{
		get => _useStochasticFilter.Value;
		set => _useStochasticFilter.Value = value;
	}

	/// <summary>
	/// Enable Parabolic SAR confirmation on the trading timeframe.
	/// </summary>
	public bool UsePsarFilter
	{
		get => _usePsarFilter.Value;
		set => _usePsarFilter.Value = value;
	}

	/// <summary>
	/// Enable a secondary Parabolic SAR confirmation.
	/// </summary>
	public bool UsePsarConfirmation
	{
		get => _usePsarConfirmation.Value;
		set => _usePsarConfirmation.Value = value;
	}

	/// <summary>
	/// Step for the primary Parabolic SAR.
	/// </summary>
	public decimal PsarStep
	{
		get => _psarStep.Value;
		set => _psarStep.Value = value;
	}

	/// <summary>
	/// Maximum step for the primary Parabolic SAR.
	/// </summary>
	public decimal PsarMaxStep
	{
		get => _psarMaxStep.Value;
		set => _psarMaxStep.Value = value;
	}

	/// <summary>
	/// Step for the confirmation Parabolic SAR.
	/// </summary>
	public decimal PsarConfirmStep
	{
		get => _psarConfirmStep.Value;
		set => _psarConfirmStep.Value = value;
	}

	/// <summary>
	/// Maximum step for the confirmation Parabolic SAR.
	/// </summary>
	public decimal PsarConfirmMaxStep
	{
		get => _psarConfirmMaxStep.Value;
		set => _psarConfirmMaxStep.Value = value;
	}

	/// <summary>
	/// Allow adding to existing positions in the same direction.
	/// </summary>
	public bool AllowSameSignalEntries
	{
		get => _allowSameSignalEntries.Value;
		set => _allowSameSignalEntries.Value = value;
	}

	/// <summary>
	/// Allow reversing positions immediately after an opposite signal.
	/// </summary>
	public bool AllowOppositeEntries
	{
		get => _allowOppositeEntries.Value;
		set => _allowOppositeEntries.Value = value;
	}

	/// <summary>
	/// Restrict trading to a specific intraday window.
	/// </summary>
	public bool UseTradingWindow
	{
		get => _useTradingWindow.Value;
		set => _useTradingWindow.Value = value;
	}

	/// <summary>
	/// Trading window start.
	/// </summary>
	public TimeSpan TradingStart
	{
		get => _tradingStart.Value;
		set => _tradingStart.Value = value;
	}

	/// <summary>
	/// Trading window end.
	/// </summary>
	public TimeSpan TradingEnd
	{
		get => _tradingEnd.Value;
		set => _tradingEnd.Value = value;
	}

	/// <summary>
	/// Activate an intraday break during which no new orders are allowed.
	/// </summary>
	public bool UseTradingBreak
	{
		get => _useTradingBreak.Value;
		set => _useTradingBreak.Value = value;
	}

	/// <summary>
	/// Start of the break period.
	/// </summary>
	public TimeSpan BreakStart
	{
		get => _breakStart.Value;
		set => _breakStart.Value = value;
	}

	/// <summary>
	/// End of the break period.
	/// </summary>
	public TimeSpan BreakEnd
	{
		get => _breakEnd.Value;
		set => _breakEnd.Value = value;
	}

	/// <summary>
	/// Stop-loss size expressed in instrument points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for trading signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MarketMasterStrategy"/> class.
	/// </summary>
	public MarketMasterStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetDisplay("Volume", "Base order volume", "Trading");

		_useAutoVolume = Param(nameof(UseAutoVolume), true)
		.SetDisplay("Auto Volume", "Derive volume from portfolio balance", "Trading");

		_riskMultiplier = Param(nameof(RiskMultiplier), 10m)
		.SetDisplay("Risk Multiplier", "Risk multiplier applied in auto volume", "Trading");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2m)
		.SetDisplay("Volume Multiplier", "Multiplier when pyramiding", "Trading");

		_minVolume = Param(nameof(MinVolume), 3000m)
		.SetDisplay("Min Volume", "Required volume for the first entry", "Filters");

		_minHedgeVolume = Param(nameof(MinHedgeVolume), 3000m)
		.SetDisplay("Min Hedge Volume", "Required volume for additional entries", "Filters");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetDisplay("ATR Period", "ATR length for main filter", "Indicators")
		.SetGreaterThanZero();

		_atrHedgePeriod = Param(nameof(AtrHedgePeriod), 14)
		.SetDisplay("ATR Hedge Period", "ATR length for hedge filter", "Indicators")
		.SetGreaterThanZero();

		_mfiPeriod = Param(nameof(MfiPeriod), 14)
		.SetDisplay("MFI Period", "Money Flow Index period", "Indicators")
		.SetGreaterThanZero();

		_bullBearPeriod = Param(nameof(BullBearPeriod), 14)
		.SetDisplay("Bull/Bear Period", "Bulls and Bears Power period", "Indicators")
		.SetGreaterThanZero();

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
		.SetDisplay("Stochastic %K", "%K period", "Indicators")
		.SetGreaterThanZero();

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
		.SetDisplay("Stochastic %D", "%D period", "Indicators")
		.SetGreaterThanZero();

		_stochasticSlowing = Param(nameof(StochasticSlowing), 3)
		.SetDisplay("Stochastic Slowing", "Slowing applied to %K", "Indicators")
		.SetGreaterThanZero();

		_stochasticBuyLevel = Param(nameof(StochasticBuyLevel), 60m)
		.SetDisplay("Stochastic Buy", "Maximum stochastic value for longs", "Signals");

		_stochasticSellLevel = Param(nameof(StochasticSellLevel), 40m)
		.SetDisplay("Stochastic Sell", "Minimum stochastic value for shorts", "Signals");

		_useStochasticFilter = Param(nameof(UseStochasticFilter), true)
		.SetDisplay("Use Stochastic", "Enable stochastic filter", "Signals");

		_usePsarFilter = Param(nameof(UsePsarFilter), true)
		.SetDisplay("Use PSAR", "Enable Parabolic SAR filter", "Signals");

		_usePsarConfirmation = Param(nameof(UsePsarConfirmation), true)
		.SetDisplay("Use PSAR Confirm", "Enable confirmation Parabolic SAR", "Signals");

		_psarStep = Param(nameof(PsarStep), 0.02m)
		.SetDisplay("PSAR Step", "Primary PSAR acceleration", "Parabolic SAR");

		_psarMaxStep = Param(nameof(PsarMaxStep), 0.2m)
		.SetDisplay("PSAR Max", "Primary PSAR maximum acceleration", "Parabolic SAR");

		_psarConfirmStep = Param(nameof(PsarConfirmStep), 0.02m)
		.SetDisplay("PSAR Confirm Step", "Confirmation PSAR acceleration", "Parabolic SAR");

		_psarConfirmMaxStep = Param(nameof(PsarConfirmMaxStep), 0.2m)
		.SetDisplay("PSAR Confirm Max", "Confirmation PSAR maximum acceleration", "Parabolic SAR");

		_allowSameSignalEntries = Param(nameof(AllowSameSignalEntries), false)
		.SetDisplay("Allow Pyramid", "Permit same direction additions", "Trading");

		_allowOppositeEntries = Param(nameof(AllowOppositeEntries), true)
		.SetDisplay("Allow Opposite", "Allow immediate reversals", "Trading");

		_useTradingWindow = Param(nameof(UseTradingWindow), false)
		.SetDisplay("Use Trading Window", "Restrict trading hours", "Timing");

		_tradingStart = Param(nameof(TradingStart), new TimeSpan(6, 0, 0))
		.SetDisplay("Start", "Trading window start", "Timing");

		_tradingEnd = Param(nameof(TradingEnd), new TimeSpan(18, 0, 0))
		.SetDisplay("End", "Trading window end", "Timing");

		_useTradingBreak = Param(nameof(UseTradingBreak), false)
		.SetDisplay("Use Break", "Enable intraday break", "Timing");

		_breakStart = Param(nameof(BreakStart), new TimeSpan(6, 0, 1))
		.SetDisplay("Break Start", "Break start time", "Timing");

		_breakEnd = Param(nameof(BreakEnd), new TimeSpan(6, 0, 2))
		.SetDisplay("Break End", "Break end time", "Timing");

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
		.SetDisplay("Stop Loss", "Protective stop in points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Trading timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevAtr = null;
		_prevAtrHedge = null;
		_prevMfi = null;
		_prevStochasticMain = null;
		_prevStochasticSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_atrHedge = new AverageTrueRange { Length = AtrHedgePeriod };
		_mfi = new MoneyFlowIndex { Length = MfiPeriod };
		_bulls = new BullsPower { Length = BullBearPeriod };
		_bears = new BearsPower { Length = BullBearPeriod };
		_stochastic = new StochasticOscillator
		{
			Length = StochasticKPeriod,
			K = { Length = StochasticSlowing },
			D = { Length = StochasticDPeriod }
		};
		_psar = new ParabolicSar
		{
			Acceleration = PsarStep,
			AccelerationMax = PsarMaxStep
		};
		_psarConfirm = new ParabolicSar
		{
			Acceleration = PsarConfirmStep,
			AccelerationMax = PsarConfirmMaxStep
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_stochastic, _atr, _atrHedge, _mfi, _bulls, _bears, _psar, _psarConfirm, ProcessCandle)
		.Start();

		Volume = CalculateBaseVolume();

		var step = Security?.PriceStep;
		if (step is decimal priceStep && priceStep > 0m && StopLossPoints > 0m)
		{
			StartProtection(stopLoss: new Unit(StopLossPoints * priceStep, UnitTypes.Point));
		}
		else
		{
			StartProtection();
		}
	}

	private void ProcessCandle(
	ICandleMessage candle,
	decimal stochasticMain,
	decimal stochasticSignal,
	decimal atrValue,
	decimal atrHedgeValue,
	decimal mfiValue,
	decimal bullsValue,
	decimal bearsValue,
	decimal psarValue,
	decimal psarConfirmValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateMemory(atrValue, atrHedgeValue, mfiValue, stochasticMain, stochasticSignal);
			return;
		}

		if (!_atr.IsFormed || !_atrHedge.IsFormed || !_mfi.IsFormed || !_bulls.IsFormed || !_bears.IsFormed || !_stochastic.IsFormed)
		{
			UpdateMemory(atrValue, atrHedgeValue, mfiValue, stochasticMain, stochasticSignal);
			return;
		}

		if (_prevAtr is null || _prevAtrHedge is null || _prevMfi is null || _prevStochasticMain is null || _prevStochasticSignal is null)
		{
			UpdateMemory(atrValue, atrHedgeValue, mfiValue, stochasticMain, stochasticSignal);
			return;
		}

		var serverTime = candle.OpenTime;
		if (!IsWithinTradingWindow(serverTime))
		{
			UpdateMemory(atrValue, atrHedgeValue, mfiValue, stochasticMain, stochasticSignal);
			return;
		}

		var currentVolume = candle.TotalVolume ?? 0m;
		var requiredVolume = Math.Abs(Position) > 0m ? MinHedgeVolume : MinVolume;
		if (currentVolume < requiredVolume)
		{
			UpdateMemory(atrValue, atrHedgeValue, mfiValue, stochasticMain, stochasticSignal);
			return;
		}

		var baseVolume = CalculateBaseVolume();
		Volume = baseVolume;
		if (baseVolume <= 0m)
		{
			UpdateMemory(atrValue, atrHedgeValue, mfiValue, stochasticMain, stochasticSignal);
			return;
		}

		var atrSource = Math.Abs(Position) > 0m ? atrHedgeValue : atrValue;
		var prevAtr = Math.Abs(Position) > 0m ? _prevAtrHedge.Value : _prevAtr.Value;
		var atrIncreasing = atrSource > prevAtr;
		var mfiRising = mfiValue > _prevMfi.Value;
		var mfiFalling = mfiValue < _prevMfi.Value;
		var bearsPositive = bearsValue > 0m;
		var bullsNegative = bullsValue < 0m;
		var stochMainPrev = _prevStochasticMain.Value;
		var stochSignalPrev = _prevStochasticSignal.Value;

		var stochBull = !UseStochasticFilter || (stochasticMain <= StochasticBuyLevel && stochasticMain > stochasticSignal && stochasticMain > stochMainPrev);
		var stochBear = !UseStochasticFilter || (stochasticSignal >= StochasticSellLevel && stochasticMain < stochasticSignal && stochasticSignal < stochSignalPrev);

		var price = candle.ClosePrice;
		var psarBull = !UsePsarFilter || (price > psarValue && (!UsePsarConfirmation || price > psarConfirmValue));
		var psarBear = !UsePsarFilter || (price < psarValue && (!UsePsarConfirmation || price < psarConfirmValue));

		var buySignal = atrIncreasing && mfiRising && bearsPositive && stochBull && psarBull;
		var sellSignal = atrIncreasing && mfiFalling && bullsNegative && stochBear && psarBear;

		var hadShort = Position < 0m;
		var hadLong = Position > 0m;

		if (buySignal && hadShort)
		{
			BuyMarket(Math.Abs(Position));
			if (!AllowOppositeEntries)
			{
				UpdateMemory(atrValue, atrHedgeValue, mfiValue, stochasticMain, stochasticSignal);
				return;
			}
		}

		if (sellSignal && hadLong)
		{
			SellMarket(Position);
			if (!AllowOppositeEntries)
			{
				UpdateMemory(atrValue, atrHedgeValue, mfiValue, stochasticMain, stochasticSignal);
				return;
			}
		}

		if (buySignal)
		{
			if (Position == 0m)
			{
				var volume = CalculateEntryVolume(baseVolume, false, true);
				if (volume > 0m)
				BuyMarket(volume);
			}
			else if (Position > 0m && AllowSameSignalEntries)
			{
				var volume = CalculateEntryVolume(baseVolume, true, true);
				if (volume > 0m)
				BuyMarket(volume);
			}
		}
		else if (sellSignal)
		{
			if (Position == 0m)
			{
				var volume = CalculateEntryVolume(baseVolume, false, false);
				if (volume > 0m)
				SellMarket(volume);
			}
			else if (Position < 0m && AllowSameSignalEntries)
			{
				var volume = CalculateEntryVolume(baseVolume, true, false);
				if (volume > 0m)
				SellMarket(volume);
			}
		}

		UpdateMemory(atrValue, atrHedgeValue, mfiValue, stochasticMain, stochasticSignal);
	}

	private decimal CalculateBaseVolume()
	{
		var volume = OrderVolume;

		if (UseAutoVolume && Portfolio is not null && Security is not null)
		{
			var balance = Portfolio.CurrentValue ?? Portfolio.BeginValue;
			if (balance is decimal total && total > 0m)
			{
				var step = Security.VolumeStep ?? 1m;
				var minVolume = Security.MinVolume ?? step;
				var maxVolume = Security.MaxVolume ?? decimal.MaxValue;
				var raw = total * RiskMultiplier / 100m / 1000m;
				var steps = Math.Round(raw / step, MidpointRounding.AwayFromZero);
				var normalized = steps * step;
				volume = Math.Max(minVolume, Math.Min(maxVolume, normalized));
			}
		}

		return Math.Max(0m, volume);
	}

	private decimal CalculateEntryVolume(decimal baseVolume, bool isAdditional, bool isLong)
	{
		var volume = baseVolume;

		if (isAdditional && VolumeMultiplier > 1m)
		volume *= VolumeMultiplier;

		if ((isLong && Position < 0m) || (!isLong && Position > 0m))
		volume += Math.Abs(Position);

		return volume;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		if (!UseTradingWindow)
		return true;

		var current = time.TimeOfDay;
		var inWindow = TradingStart <= TradingEnd
		? current >= TradingStart && current <= TradingEnd
		: current >= TradingStart || current <= TradingEnd;

		if (!inWindow)
		return false;

		if (!UseTradingBreak)
		return true;

		var inBreak = BreakStart <= BreakEnd
		? current >= BreakStart && current <= BreakEnd
		: current >= BreakStart || current <= BreakEnd;

		return !inBreak;
	}

	private void UpdateMemory(decimal atr, decimal atrHedge, decimal mfi, decimal stochMain, decimal stochSignal)
	{
		_prevAtr = atr;
		_prevAtrHedge = atrHedge;
		_prevMfi = mfi;
		_prevStochasticMain = stochMain;
		_prevStochasticSignal = stochSignal;
	}
}

