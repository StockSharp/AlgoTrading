
using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Combined strategy that trades on Skyscraper Fix and Color AML signals with MMRec position sizing.
/// </summary>
public class ExpSkyscraperFixColorAmlMmrecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _skyscraperCandleType;
	private readonly StrategyParam<int> _skyscraperLength;
	private readonly StrategyParam<decimal> _skyscraperKv;
	private readonly StrategyParam<decimal> _skyscraperPercentage;
	private readonly StrategyParam<SkyscraperCalculationMode> _skyscraperMode;
	private readonly StrategyParam<int> _skyscraperSignalBar;
	private readonly StrategyParam<bool> _skyscraperEnableLongEntry;
	private readonly StrategyParam<bool> _skyscraperEnableShortEntry;
	private readonly StrategyParam<bool> _skyscraperEnableLongExit;
	private readonly StrategyParam<bool> _skyscraperEnableShortExit;
	private readonly StrategyParam<int> _skyscraperBuyLossTrigger;
	private readonly StrategyParam<int> _skyscraperSellLossTrigger;
	private readonly StrategyParam<decimal> _skyscraperSmallMm;
	private readonly StrategyParam<decimal> _skyscraperMm;
	private readonly StrategyParam<MoneyManagementMode> _skyscraperMmMode;
	private readonly StrategyParam<int> _skyscraperStopLossTicks;
	private readonly StrategyParam<int> _skyscraperTakeProfitTicks;

	private readonly StrategyParam<DataType> _colorAmlCandleType;
	private readonly StrategyParam<int> _colorAmlFractal;
	private readonly StrategyParam<int> _colorAmlLag;
	private readonly StrategyParam<int> _colorAmlSignalBar;
	private readonly StrategyParam<bool> _colorAmlEnableLongEntry;
	private readonly StrategyParam<bool> _colorAmlEnableShortEntry;
	private readonly StrategyParam<bool> _colorAmlEnableLongExit;
	private readonly StrategyParam<bool> _colorAmlEnableShortExit;
	private readonly StrategyParam<int> _colorAmlBuyLossTrigger;
	private readonly StrategyParam<int> _colorAmlSellLossTrigger;
	private readonly StrategyParam<decimal> _colorAmlSmallMm;
	private readonly StrategyParam<decimal> _colorAmlMm;
	private readonly StrategyParam<MoneyManagementMode> _colorAmlMmMode;
	private readonly StrategyParam<int> _colorAmlStopLossTicks;
	private readonly StrategyParam<int> _colorAmlTakeProfitTicks;

	private SkyscraperFixIndicator? _skyscraperIndicator;
	private ColorAmlIndicator? _colorAmlIndicator;

	private readonly Queue<int> _skyscraperTrendQueue = new();
	private readonly Queue<int> _colorAmlQueue = new();

	private int? _lastSkyscraperTrend;
	private int? _lastColorAmlValue;

	private readonly MoneyManager _skyscraperBuyManager = new();
	private readonly MoneyManager _skyscraperSellManager = new();
	private readonly MoneyManager _colorAmlBuyManager = new();
	private readonly MoneyManager _colorAmlSellManager = new();

	private decimal? _longStop;
	private decimal? _longTarget;
	private decimal? _shortStop;
	private decimal? _shortTarget;

	private decimal _signedPosition;
	private Sides? _lastEntrySide;
	private decimal _lastEntryPrice;
	private TradeModule _lastEntryModule = TradeModule.None;
	private TradeModule? _pendingEntryModule;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpSkyscraperFixColorAmlMmrecStrategy"/> class.
	/// </summary>
	public ExpSkyscraperFixColorAmlMmrecStrategy()
	{
		_skyscraperCandleType = Param(nameof(SkyscraperCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Skyscraper Candle", "Timeframe used by the Skyscraper Fix indicator", "Skyscraper Fix");

		_skyscraperLength = Param(nameof(SkyscraperLength), 10)
			.SetDisplay("Length", "ATR window used to calculate the adaptive channel", "Skyscraper Fix")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(5, 25, 5);

		_skyscraperKv = Param(nameof(SkyscraperKv), 0.9m)
			.SetDisplay("Sensitivity", "Multiplier that scales the ATR based channel step", "Skyscraper Fix");

		_skyscraperPercentage = Param(nameof(SkyscraperPercentage), 0m)
			.SetDisplay("Midline Offset %", "Percentage offset applied to the middle line", "Skyscraper Fix");

		_skyscraperMode = Param(nameof(SkyscraperMode), SkyscraperCalculationMode.HighLow)
			.SetDisplay("Price Mode", "Price source used to build the channel", "Skyscraper Fix");

		_skyscraperSignalBar = Param(nameof(SkyscraperSignalBar), 1)
			.SetDisplay("Signal Delay", "Number of closed candles to delay Skyscraper signals", "Skyscraper Fix")
			.SetNotNegative();

		_skyscraperEnableLongEntry = Param(nameof(SkyscraperEnableLongEntry), true)
			.SetDisplay("Allow Long Entries", "Open long trades when the channel turns bullish", "Skyscraper Fix");

		_skyscraperEnableShortEntry = Param(nameof(SkyscraperEnableShortEntry), true)
			.SetDisplay("Allow Short Entries", "Open short trades when the channel turns bearish", "Skyscraper Fix");

		_skyscraperEnableLongExit = Param(nameof(SkyscraperEnableLongExit), true)
			.SetDisplay("Close Long Positions", "Exit long trades on bearish Skyscraper signals", "Skyscraper Fix");

		_skyscraperEnableShortExit = Param(nameof(SkyscraperEnableShortExit), true)
			.SetDisplay("Close Short Positions", "Exit short trades on bullish Skyscraper signals", "Skyscraper Fix");

		_skyscraperBuyLossTrigger = Param(nameof(SkyscraperBuyLossTrigger), 2)
			.SetDisplay("Long Loss Trigger", "Consecutive long losses before reducing volume", "Skyscraper Fix")
			.SetNotNegative();

		_skyscraperSellLossTrigger = Param(nameof(SkyscraperSellLossTrigger), 2)
			.SetDisplay("Short Loss Trigger", "Consecutive short losses before reducing volume", "Skyscraper Fix")
			.SetNotNegative();

		_skyscraperSmallMm = Param(nameof(SkyscraperSmallMm), 0.01m)
			.SetDisplay("Reduced Volume", "Volume applied after hitting the long/short loss trigger", "Skyscraper Fix")
			.SetGreaterThanZero();

		_skyscraperMm = Param(nameof(SkyscraperMm), 0.1m)
			.SetDisplay("Base Volume", "Standard order volume for Skyscraper signals", "Skyscraper Fix")
			.SetGreaterThanZero();

		_skyscraperMmMode = Param(nameof(SkyscraperMmMode), MoneyManagementMode.Lot)
			.SetDisplay("Volume Mode", "Money management mode used by the original expert", "Skyscraper Fix");

		_skyscraperStopLossTicks = Param(nameof(SkyscraperStopLossTicks), 1000)
			.SetDisplay("Stop Loss (ticks)", "Protective stop for Skyscraper trades in price steps", "Skyscraper Fix")
			.SetNotNegative();

		_skyscraperTakeProfitTicks = Param(nameof(SkyscraperTakeProfitTicks), 2000)
			.SetDisplay("Take Profit (ticks)", "Profit target for Skyscraper trades in price steps", "Skyscraper Fix")
			.SetNotNegative();

		_colorAmlCandleType = Param(nameof(ColorAmlCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Color AML Candle", "Timeframe used by the Color AML indicator", "Color AML");

		_colorAmlFractal = Param(nameof(ColorAmlFractal), 6)
			.SetDisplay("Fractal Period", "Window used to measure volatility for AML", "Color AML")
			.SetGreaterThanZero();

		_colorAmlLag = Param(nameof(ColorAmlLag), 7)
			.SetDisplay("Lag", "Smoothing depth applied to AML", "Color AML")
			.SetGreaterThanZero();

		_colorAmlSignalBar = Param(nameof(ColorAmlSignalBar), 1)
			.SetDisplay("Signal Delay", "Number of closed candles to delay Color AML signals", "Color AML")
			.SetNotNegative();

		_colorAmlEnableLongEntry = Param(nameof(ColorAmlEnableLongEntry), true)
			.SetDisplay("Allow Long Entries", "Open long trades on bullish Color AML", "Color AML");

		_colorAmlEnableShortEntry = Param(nameof(ColorAmlEnableShortEntry), true)
			.SetDisplay("Allow Short Entries", "Open short trades on bearish Color AML", "Color AML");

		_colorAmlEnableLongExit = Param(nameof(ColorAmlEnableLongExit), true)
			.SetDisplay("Close Long Positions", "Exit longs when Color AML turns bearish", "Color AML");

		_colorAmlEnableShortExit = Param(nameof(ColorAmlEnableShortExit), true)
			.SetDisplay("Close Short Positions", "Exit shorts when Color AML turns bullish", "Color AML");

		_colorAmlBuyLossTrigger = Param(nameof(ColorAmlBuyLossTrigger), 2)
			.SetDisplay("Long Loss Trigger", "Consecutive long losses before reducing volume", "Color AML")
			.SetNotNegative();

		_colorAmlSellLossTrigger = Param(nameof(ColorAmlSellLossTrigger), 2)
			.SetDisplay("Short Loss Trigger", "Consecutive short losses before reducing volume", "Color AML")
			.SetNotNegative();

		_colorAmlSmallMm = Param(nameof(ColorAmlSmallMm), 0.01m)
			.SetDisplay("Reduced Volume", "Volume applied after hitting the loss trigger", "Color AML")
			.SetGreaterThanZero();

		_colorAmlMm = Param(nameof(ColorAmlMm), 0.1m)
			.SetDisplay("Base Volume", "Standard order volume for Color AML signals", "Color AML")
			.SetGreaterThanZero();

		_colorAmlMmMode = Param(nameof(ColorAmlMmMode), MoneyManagementMode.Lot)
			.SetDisplay("Volume Mode", "Money management mode used by the original expert", "Color AML");

		_colorAmlStopLossTicks = Param(nameof(ColorAmlStopLossTicks), 1000)
			.SetDisplay("Stop Loss (ticks)", "Protective stop for Color AML trades in price steps", "Color AML")
			.SetNotNegative();

		_colorAmlTakeProfitTicks = Param(nameof(ColorAmlTakeProfitTicks), 2000)
			.SetDisplay("Take Profit (ticks)", "Profit target for Color AML trades in price steps", "Color AML")
			.SetNotNegative();
	}

	/// <summary>
	/// Timeframe used to calculate the Skyscraper Fix channel.
	/// </summary>
	public DataType SkyscraperCandleType { get => _skyscraperCandleType.Value; set => _skyscraperCandleType.Value = value; }

	/// <summary>
	/// ATR window used to derive the Skyscraper Fix step.
	/// </summary>
	public int SkyscraperLength { get => _skyscraperLength.Value; set => _skyscraperLength.Value = value; }

	/// <summary>
	/// Sensitivity multiplier applied to the Skyscraper Fix step.
	/// </summary>
	public decimal SkyscraperKv { get => _skyscraperKv.Value; set => _skyscraperKv.Value = value; }

	/// <summary>
	/// Percentage offset applied to the Skyscraper midline.
	/// </summary>
	public decimal SkyscraperPercentage { get => _skyscraperPercentage.Value; set => _skyscraperPercentage.Value = value; }

	/// <summary>
	/// Price mode used to build the Skyscraper channel.
	/// </summary>
	public SkyscraperCalculationMode SkyscraperMode { get => _skyscraperMode.Value; set => _skyscraperMode.Value = value; }

	/// <summary>
	/// Number of closed candles used to delay Skyscraper signals.
	/// </summary>
	public int SkyscraperSignalBar { get => _skyscraperSignalBar.Value; set => _skyscraperSignalBar.Value = value; }

	/// <summary>
	/// Enable long entries triggered by Skyscraper Fix.
	/// </summary>
	public bool SkyscraperEnableLongEntry { get => _skyscraperEnableLongEntry.Value; set => _skyscraperEnableLongEntry.Value = value; }

	/// <summary>
	/// Enable short entries triggered by Skyscraper Fix.
	/// </summary>
	public bool SkyscraperEnableShortEntry { get => _skyscraperEnableShortEntry.Value; set => _skyscraperEnableShortEntry.Value = value; }

	/// <summary>
	/// Close long positions when Skyscraper Fix turns bearish.
	/// </summary>
	public bool SkyscraperEnableLongExit { get => _skyscraperEnableLongExit.Value; set => _skyscraperEnableLongExit.Value = value; }

	/// <summary>
	/// Close short positions when Skyscraper Fix turns bullish.
	/// </summary>
	public bool SkyscraperEnableShortExit { get => _skyscraperEnableShortExit.Value; set => _skyscraperEnableShortExit.Value = value; }

	/// <summary>
	/// Consecutive long losses before reducing Skyscraper trade volume.
	/// </summary>
	public int SkyscraperBuyLossTrigger { get => _skyscraperBuyLossTrigger.Value; set => _skyscraperBuyLossTrigger.Value = value; }

	/// <summary>
	/// Consecutive short losses before reducing Skyscraper trade volume.
	/// </summary>
	public int SkyscraperSellLossTrigger { get => _skyscraperSellLossTrigger.Value; set => _skyscraperSellLossTrigger.Value = value; }

	/// <summary>
	/// Volume applied after the loss trigger is met.
	/// </summary>
	public decimal SkyscraperSmallMm { get => _skyscraperSmallMm.Value; set => _skyscraperSmallMm.Value = value; }

	/// <summary>
	/// Default Skyscraper trade volume.
	/// </summary>
	public decimal SkyscraperMm { get => _skyscraperMm.Value; set => _skyscraperMm.Value = value; }

	/// <summary>
	/// Money management mode for Skyscraper trades.
	/// </summary>
	public MoneyManagementMode SkyscraperMmMode { get => _skyscraperMmMode.Value; set => _skyscraperMmMode.Value = value; }

	/// <summary>
	/// Stop-loss distance for Skyscraper trades expressed in price steps.
	/// </summary>
	public int SkyscraperStopLossTicks { get => _skyscraperStopLossTicks.Value; set => _skyscraperStopLossTicks.Value = value; }

	/// <summary>
	/// Take-profit distance for Skyscraper trades expressed in price steps.
	/// </summary>
	public int SkyscraperTakeProfitTicks { get => _skyscraperTakeProfitTicks.Value; set => _skyscraperTakeProfitTicks.Value = value; }

	/// <summary>
	/// Timeframe used to calculate the Color AML indicator.
	/// </summary>
	public DataType ColorAmlCandleType { get => _colorAmlCandleType.Value; set => _colorAmlCandleType.Value = value; }

	/// <summary>
	/// Fractal period used by Color AML.
	/// </summary>
	public int ColorAmlFractal { get => _colorAmlFractal.Value; set => _colorAmlFractal.Value = value; }

	/// <summary>
	/// Lag depth used in the Color AML smoother.
	/// </summary>
	public int ColorAmlLag { get => _colorAmlLag.Value; set => _colorAmlLag.Value = value; }

	/// <summary>
	/// Number of closed candles used to delay Color AML signals.
	/// </summary>
	public int ColorAmlSignalBar { get => _colorAmlSignalBar.Value; set => _colorAmlSignalBar.Value = value; }

	/// <summary>
	/// Enable long entries triggered by Color AML.
	/// </summary>
	public bool ColorAmlEnableLongEntry { get => _colorAmlEnableLongEntry.Value; set => _colorAmlEnableLongEntry.Value = value; }

	/// <summary>
	/// Enable short entries triggered by Color AML.
	/// </summary>
	public bool ColorAmlEnableShortEntry { get => _colorAmlEnableShortEntry.Value; set => _colorAmlEnableShortEntry.Value = value; }

	/// <summary>
	/// Close long positions when Color AML turns bearish.
	/// </summary>
	public bool ColorAmlEnableLongExit { get => _colorAmlEnableLongExit.Value; set => _colorAmlEnableLongExit.Value = value; }

	/// <summary>
	/// Close short positions when Color AML turns bullish.
	/// </summary>
	public bool ColorAmlEnableShortExit { get => _colorAmlEnableShortExit.Value; set => _colorAmlEnableShortExit.Value = value; }

	/// <summary>
	/// Consecutive long losses before reducing Color AML trade volume.
	/// </summary>
	public int ColorAmlBuyLossTrigger { get => _colorAmlBuyLossTrigger.Value; set => _colorAmlBuyLossTrigger.Value = value; }

	/// <summary>
	/// Consecutive short losses before reducing Color AML trade volume.
	/// </summary>
	public int ColorAmlSellLossTrigger { get => _colorAmlSellLossTrigger.Value; set => _colorAmlSellLossTrigger.Value = value; }

	/// <summary>
	/// Volume applied after the Color AML loss trigger is met.
	/// </summary>
	public decimal ColorAmlSmallMm { get => _colorAmlSmallMm.Value; set => _colorAmlSmallMm.Value = value; }

	/// <summary>
	/// Default Color AML trade volume.
	/// </summary>
	public decimal ColorAmlMm { get => _colorAmlMm.Value; set => _colorAmlMm.Value = value; }

	/// <summary>
	/// Money management mode for Color AML trades.
	/// </summary>
	public MoneyManagementMode ColorAmlMmMode { get => _colorAmlMmMode.Value; set => _colorAmlMmMode.Value = value; }

	/// <summary>
	/// Stop-loss distance for Color AML trades expressed in price steps.
	/// </summary>
	public int ColorAmlStopLossTicks { get => _colorAmlStopLossTicks.Value; set => _colorAmlStopLossTicks.Value = value; }

	/// <summary>
	/// Take-profit distance for Color AML trades expressed in price steps.
	/// </summary>
	public int ColorAmlTakeProfitTicks { get => _colorAmlTakeProfitTicks.Value; set => _colorAmlTakeProfitTicks.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
			yield break;

		yield return (Security, SkyscraperCandleType);

		if (!Equals(SkyscraperCandleType, ColorAmlCandleType))
			yield return (Security, ColorAmlCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_skyscraperIndicator = null;
		_colorAmlIndicator = null;

		_skyscraperTrendQueue.Clear();
		_colorAmlQueue.Clear();
		_lastSkyscraperTrend = null;
		_lastColorAmlValue = null;

		_skyscraperBuyManager.Reset();
		_skyscraperSellManager.Reset();
		_colorAmlBuyManager.Reset();
		_colorAmlSellManager.Reset();

		ResetRiskLevels();

		_signedPosition = 0m;
		_lastEntrySide = null;
		_lastEntryPrice = 0m;
		_lastEntryModule = TradeModule.None;
		_pendingEntryModule = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var step = Security?.PriceStep ?? 0m;
		var volume = Math.Max(SkyscraperMm, ColorAmlMm);
		Volume = NormalizeVolume(volume);

		_skyscraperIndicator = new SkyscraperFixIndicator
		{
			Length = SkyscraperLength,
			Kv = SkyscraperKv,
			Percentage = SkyscraperPercentage,
			Mode = SkyscraperMode,
			PriceStep = step
		};

		_colorAmlIndicator = new ColorAmlIndicator
		{
			Fractal = ColorAmlFractal,
			Lag = ColorAmlLag,
			PriceStep = step
		};

		var skyscraperSubscription = SubscribeCandles(SkyscraperCandleType);
		skyscraperSubscription
			.BindEx(_skyscraperIndicator, ProcessSkyscraper)
			.Start();

		var colorAmlSubscription = SubscribeCandles(ColorAmlCandleType);
		colorAmlSubscription
			.BindEx(_colorAmlIndicator, ProcessColorAml)
			.Start();
	}

	private void ProcessSkyscraper(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (CheckProtectiveLevels(candle))
			return;

		if (indicatorValue is not SkyscraperFixValue value || !value.HasValue || value.Trend is null)
			return;

		_skyscraperTrendQueue.Enqueue(value.Trend.Value);

		while (_skyscraperTrendQueue.Count > SkyscraperSignalBar + 1)
			_skyscraperTrendQueue.Dequeue();

		if (_skyscraperTrendQueue.Count <= SkyscraperSignalBar)
			return;

		var trend = _skyscraperTrendQueue.Dequeue();
		var previous = _lastSkyscraperTrend;
		_lastSkyscraperTrend = trend;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (trend > 0)
		{
			if (SkyscraperEnableShortExit && Position < 0)
				CloseShortPosition();

			if (SkyscraperEnableLongEntry && (previous is null || previous <= 0))
				OpenLong(TradeModule.SkyscraperLong, candle, SkyscraperStopLossTicks, SkyscraperTakeProfitTicks);
		}
		else if (trend < 0)
		{
			if (SkyscraperEnableLongExit && Position > 0)
				CloseLongPosition();

			if (SkyscraperEnableShortEntry && (previous is null || previous >= 0))
				OpenShort(TradeModule.SkyscraperShort, candle, SkyscraperStopLossTicks, SkyscraperTakeProfitTicks);
		}
	}

	private void ProcessColorAml(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (CheckProtectiveLevels(candle))
			return;

		if (indicatorValue is not ColorAmlValue value || !value.HasValue || value.Color is null)
			return;

		_colorAmlQueue.Enqueue(value.Color.Value);

		while (_colorAmlQueue.Count > ColorAmlSignalBar + 1)
			_colorAmlQueue.Dequeue();

		if (_colorAmlQueue.Count <= ColorAmlSignalBar)
			return;

		var color = _colorAmlQueue.Dequeue();
		var previous = _lastColorAmlValue;
		_lastColorAmlValue = color;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (color == 2)
		{
			if (ColorAmlEnableShortExit && Position < 0)
				CloseShortPosition();

			if (ColorAmlEnableLongEntry && previous != 2)
				OpenLong(TradeModule.ColorAmlLong, candle, ColorAmlStopLossTicks, ColorAmlTakeProfitTicks);
		}
		else if (color == 0)
		{
			if (ColorAmlEnableLongExit && Position > 0)
				CloseLongPosition();

			if (ColorAmlEnableShortEntry && previous != 0)
				OpenShort(TradeModule.ColorAmlShort, candle, ColorAmlStopLossTicks, ColorAmlTakeProfitTicks);
		}
	}

	private void OpenLong(TradeModule module, ICandleMessage candle, int stopTicks, int takeTicks)
	{
		if (Position > 0)
			return;

		if (Position < 0)
			EnsureFlatPosition();

		var volume = SelectVolume(module);
		if (volume <= 0m)
			return;

		_pendingEntryModule = module;
		BuyMarket(volume);
		SetProtectiveLevels(module, candle.ClosePrice, true, stopTicks, takeTicks);
	}

	private void OpenShort(TradeModule module, ICandleMessage candle, int stopTicks, int takeTicks)
	{
		if (Position < 0)
			return;

		if (Position > 0)
			EnsureFlatPosition();

		var volume = SelectVolume(module);
		if (volume <= 0m)
			return;

		_pendingEntryModule = module;
		SellMarket(volume);
		SetProtectiveLevels(module, candle.ClosePrice, false, stopTicks, takeTicks);
	}

	private void CloseLongPosition()
	{
		if (Position <= 0)
			return;

		_pendingEntryModule = null;
		CancelActiveOrders();
		ClosePosition();
		ResetRiskLevels();
	}

	private void CloseShortPosition()
	{
		if (Position >= 0)
			return;

		_pendingEntryModule = null;
		CancelActiveOrders();
		ClosePosition();
		ResetRiskLevels();
	}

	private void EnsureFlatPosition()
	{
		_pendingEntryModule = null;
		CancelActiveOrders();
		ClosePosition();
		ResetRiskLevels();
	}

	private decimal SelectVolume(TradeModule module)
	{
		UpdateMoneyManagers();

		return module switch
		{
			TradeModule.SkyscraperLong => _skyscraperBuyManager.SelectVolume(NormalizeVolume, SkyscraperMmMode),
			TradeModule.SkyscraperShort => _skyscraperSellManager.SelectVolume(NormalizeVolume, SkyscraperMmMode),
			TradeModule.ColorAmlLong => _colorAmlBuyManager.SelectVolume(NormalizeVolume, ColorAmlMmMode),
			TradeModule.ColorAmlShort => _colorAmlSellManager.SelectVolume(NormalizeVolume, ColorAmlMmMode),
			_ => 0m
		};
	}

	private void UpdateMoneyManagers()
	{
		_skyscraperBuyManager.NormalVolume = SkyscraperMm;
		_skyscraperBuyManager.SmallVolume = SkyscraperSmallMm;
		_skyscraperBuyManager.LossTrigger = SkyscraperBuyLossTrigger;

		_skyscraperSellManager.NormalVolume = SkyscraperMm;
		_skyscraperSellManager.SmallVolume = SkyscraperSmallMm;
		_skyscraperSellManager.LossTrigger = SkyscraperSellLossTrigger;

		_colorAmlBuyManager.NormalVolume = ColorAmlMm;
		_colorAmlBuyManager.SmallVolume = ColorAmlSmallMm;
		_colorAmlBuyManager.LossTrigger = ColorAmlBuyLossTrigger;

		_colorAmlSellManager.NormalVolume = ColorAmlMm;
		_colorAmlSellManager.SmallVolume = ColorAmlSmallMm;
		_colorAmlSellManager.LossTrigger = ColorAmlSellLossTrigger;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		if (volume < step)
			volume = step;

		var steps = Math.Floor(volume / step);
		if (steps < 1m)
			steps = 1m;

		return steps * step;
	}

	private void SetProtectiveLevels(TradeModule module, decimal entryPrice, bool isLong, int stopTicks, int takeTicks)
	{
		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		decimal? stop = stopTicks > 0 ? stopTicks * step : null;
		decimal? target = takeTicks > 0 ? takeTicks * step : null;

		if (isLong)
		{
			_longStop = stop.HasValue ? entryPrice - stop.Value : null;
			_longTarget = target.HasValue ? entryPrice + target.Value : null;
			_shortStop = null;
			_shortTarget = null;
		}
		else
		{
			_shortStop = stop.HasValue ? entryPrice + stop.Value : null;
			_shortTarget = target.HasValue ? entryPrice - target.Value : null;
			_longStop = null;
			_longTarget = null;
		}
	}

	private bool CheckProtectiveLevels(ICandleMessage candle)
	{
		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var triggered = false;

		if (Position > 0)
		{
			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value + step / 2m)
			{
				CancelActiveOrders();
				ClosePosition();
				ResetRiskLevels();
				triggered = true;
			}
			else if (_longTarget.HasValue && candle.HighPrice >= _longTarget.Value - step / 2m)
			{
				CancelActiveOrders();
				ClosePosition();
				ResetRiskLevels();
				triggered = true;
			}
		}
		else if (Position < 0)
		{
			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value - step / 2m)
			{
				CancelActiveOrders();
				ClosePosition();
				ResetRiskLevels();
				triggered = true;
			}
			else if (_shortTarget.HasValue && candle.LowPrice <= _shortTarget.Value + step / 2m)
			{
				CancelActiveOrders();
				ClosePosition();
				ResetRiskLevels();
				triggered = true;
			}
		}

		return triggered;
	}

	private void ResetRiskLevels()
	{
		_longStop = null;
		_longTarget = null;
		_shortStop = null;
		_shortTarget = null;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var volume = trade.Trade.Volume;
		if (volume <= 0m)
			return;

		var delta = trade.Order.Side == Sides.Buy ? volume : -volume;
		var previous = _signedPosition;
		_signedPosition += delta;

		if (previous == 0m && _signedPosition != 0m)
		{
			_lastEntrySide = delta > 0m ? Sides.Buy : Sides.Sell;
			_lastEntryPrice = trade.Trade.Price;
			_lastEntryModule = _pendingEntryModule ?? TradeModule.None;
			_pendingEntryModule = null;
		}
		else if (previous != 0m && _signedPosition == 0m)
		{
			var exitPrice = trade.Trade.Price;
			if (_lastEntrySide != null && _lastEntryModule != TradeModule.None && _lastEntryPrice != 0m)
			{
				var profit = _lastEntrySide == Sides.Buy
					? exitPrice - _lastEntryPrice
					: _lastEntryPrice - exitPrice;
				RegisterTradeResult(_lastEntryModule, profit < 0m);
			}

			_lastEntrySide = null;
			_lastEntryModule = TradeModule.None;
			_lastEntryPrice = 0m;
		}
	}

	private void RegisterTradeResult(TradeModule module, bool loss)
	{
		switch (module)
		{
			case TradeModule.SkyscraperLong:
				_skyscraperBuyManager.RegisterResult(loss);
				break;
			case TradeModule.SkyscraperShort:
				_skyscraperSellManager.RegisterResult(loss);
				break;
			case TradeModule.ColorAmlLong:
				_colorAmlBuyManager.RegisterResult(loss);
				break;
			case TradeModule.ColorAmlShort:
				_colorAmlSellManager.RegisterResult(loss);
				break;
		}
	}
}

/// <summary>
/// Money management mode supported by the MMRec logic.
/// </summary>
public enum MoneyManagementMode
{
	/// <summary>Use a percentage of free margin.</summary>
	FreeMargin,

	/// <summary>Use a percentage of account balance.</summary>
	Balance,

	/// <summary>Risk a percentage of free margin based on the stop.</summary>
	LossFreeMargin,

	/// <summary>Risk a percentage of balance based on the stop.</summary>
	LossBalance,

	/// <summary>Specify the volume directly in lots.</summary>
	Lot
}

/// <summary>
/// Internal identifier for the module that opened the latest position.
/// </summary>
internal enum TradeModule
{
	None,
	SkyscraperLong,
	SkyscraperShort,
	ColorAmlLong,
	ColorAmlShort
}

/// <summary>
/// Money manager that tracks consecutive losses.
/// </summary>
internal sealed class MoneyManager
{
	public int LossTrigger { get; set; }

	public decimal NormalVolume { get; set; }

	public decimal SmallVolume { get; set; }

	private int _lossStreak;

	public decimal SelectVolume(Func<decimal, decimal> normalize, MoneyManagementMode mode)
	{
		var baseVolume = mode == MoneyManagementMode.Lot ? NormalVolume : NormalVolume;
		var reducedVolume = mode == MoneyManagementMode.Lot ? SmallVolume : SmallVolume;

		if (LossTrigger > 0 && _lossStreak >= LossTrigger)
			baseVolume = reducedVolume;

		if (baseVolume <= 0m)
			baseVolume = NormalVolume;

		return normalize(baseVolume);
	}

	public void RegisterResult(bool loss)
	{
		if (LossTrigger <= 0)
		{
			_lossStreak = 0;
			return;
		}

		_lossStreak = loss ? Math.Min(_lossStreak + 1, LossTrigger) : 0;
	}

	public void Reset()
	{
		_lossStreak = 0;
	}
}

/// <summary>
/// Color AML indicator translated from the original MQ5 implementation.
/// </summary>
public class ColorAmlIndicator : BaseIndicator<decimal>
{
	private readonly List<ICandleMessage> _candles = new();
	private readonly List<decimal> _smoothHistory = new();

	private decimal? _previousAml;
	private int? _previousColor;

	/// <summary>
	/// Fractal window used in the dimension calculation.
	/// </summary>
	public int Fractal { get; set; } = 6;

	/// <summary>
	/// Lag depth used for smoothing.
	/// </summary>
	public int Lag { get; set; } = 7;

	/// <summary>
	/// Price step used to translate ticks into absolute prices.
	/// </summary>
	public decimal PriceStep { get; set; } = 1m;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new ColorAmlValue(this, input, false, null, null);

		if (Fractal <= 0 || Lag <= 0 || PriceStep <= 0m)
			return new ColorAmlValue(this, input, false, null, null);

		_candles.Add(candle);
		var maxCandles = Math.Max(Fractal * 2 + Lag + 5, 64);
		if (_candles.Count > maxCandles)
			_candles.RemoveAt(0);

		if (_candles.Count < Fractal * 2)
			return new ColorAmlValue(this, input, false, null, null);

		var count = _candles.Count;
		var range1 = GetRange(count - Fractal, Fractal);
		var range2 = GetRange(count - 2 * Fractal, Fractal);
		var range3 = GetRange(count - 2 * Fractal, 2 * Fractal);

		var dim = 0d;
		if (range1 + range2 > 0m && range3 > 0m)
		{
			dim = (Math.Log((double)(range1 + range2)) - Math.Log((double)range3)) * 1.44269504088896d;
		}

		var alpha = Math.Exp(-Lag * (dim - 1d));
		if (alpha > 1d)
			alpha = 1d;
		if (alpha < 0.01d)
			alpha = 0.01d;

		var price = (candle.HighPrice + candle.LowPrice + 2m * candle.OpenPrice + 2m * candle.ClosePrice) / 6m;
		var previousSmooth = _smoothHistory.Count > 0 ? _smoothHistory[^1] : price;
		var smooth = (decimal)alpha * price + (1m - (decimal)alpha) * previousSmooth;

		_smoothHistory.Add(smooth);
		if (_smoothHistory.Count > Lag + 1)
			_smoothHistory.RemoveAt(0);

		if (_smoothHistory.Count <= Lag)
			return new ColorAmlValue(this, input, false, null, null);

		var lagIndex = _smoothHistory.Count - 1 - Lag;
		if (lagIndex < 0)
			return new ColorAmlValue(this, input, false, null, null);

		var smoothLag = _smoothHistory[lagIndex];
		var threshold = Lag * Lag * PriceStep;

		var aml = Math.Abs(smooth - smoothLag) >= threshold
			? smooth
			: _previousAml ?? smooth;

		var color = _previousColor ?? 1;
		if (_previousAml.HasValue)
		{
			if (aml > _previousAml)
				color = 2;
			else if (aml < _previousAml)
				color = 0;
		}
		else
		{
			color = 1;
		}

		_previousAml = aml;
		_previousColor = color;

		return new ColorAmlValue(this, input, true, aml, color);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();
		_candles.Clear();
		_smoothHistory.Clear();
		_previousAml = null;
		_previousColor = null;
	}

	private decimal GetRange(int start, int length)
	{
		if (start < 0)
			start = 0;

		var end = Math.Min(start + length, _candles.Count);
		var max = decimal.MinValue;
		var min = decimal.MaxValue;

		for (var i = start; i < end; i++)
		{
			var candle = _candles[i];
			if (candle.HighPrice > max)
				max = candle.HighPrice;
			if (candle.LowPrice < min)
				min = candle.LowPrice;
		}

		if (max == decimal.MinValue || min == decimal.MaxValue)
			return 0m;

		return max - min;
	}
}

/// <summary>
/// Indicator value returned by <see cref="ColorAmlIndicator"/>.
/// </summary>
public class ColorAmlValue : ComplexIndicatorValue
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ColorAmlValue"/> class.
	/// </summary>
	public ColorAmlValue(IIndicator indicator, IIndicatorValue input, bool hasValue, decimal? aml, int? color)
		: base(indicator, input, (nameof(Aml), aml), (nameof(Color), color))
	{
		HasValue = hasValue;
	}

	/// <summary>
	/// Indicates whether the indicator output is ready for trading decisions.
	/// </summary>
	public bool HasValue { get; }

	/// <summary>
	/// Adaptive market level line value.
	/// </summary>
	public decimal? Aml => (decimal?)GetValue(nameof(Aml));

	/// <summary>
	/// Color code (0 = bearish, 1 = neutral, 2 = bullish).
	/// </summary>
	public int? Color => (int?)GetValue(nameof(Color));
}
