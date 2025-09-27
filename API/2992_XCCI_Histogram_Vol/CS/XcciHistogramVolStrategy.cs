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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the MQL5 XCCI Histogram Vol expert using StockSharp high level API.
/// It multiplies CCI values by volume, smooths the result and reacts to color-coded zones.
/// </summary>
public class XcciHistogramVolStrategy : Strategy
{

	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _highLevel2;
	private readonly StrategyParam<decimal> _highLevel1;
	private readonly StrategyParam<decimal> _lowLevel1;
	private readonly StrategyParam<decimal> _lowLevel2;
	private readonly StrategyParam<int> _signalBarOffset;
	private readonly StrategyParam<SmoothingMethods> _smoothingMethod;
	private readonly StrategyParam<bool> _allowLongEntries;
	private readonly StrategyParam<bool> _allowShortEntries;
	private readonly StrategyParam<bool> _allowLongExits;
	private readonly StrategyParam<bool> _allowShortExits;
	private readonly StrategyParam<decimal> _primaryEntryVolume;
	private readonly StrategyParam<decimal> _secondaryEntryVolume;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _colorExtremePositive;
	private readonly StrategyParam<int> _colorPositive;
	private readonly StrategyParam<int> _colorNeutral;
	private readonly StrategyParam<int> _colorNegative;
	private readonly StrategyParam<int> _colorExtremeNegative;

	private CommodityChannelIndex _cci;
	private LengthIndicator<decimal> _cciVolumeAverage;
	private LengthIndicator<decimal> _volumeAverage;
	private readonly List<int> _colorHistory = new();
	private bool _primaryLongActive;
	private bool _secondaryLongActive;
	private bool _primaryShortActive;
	private bool _secondaryShortActive;

	/// <summary>
	/// Initializes a new instance of <see cref="XcciHistogramVolStrategy"/>.
	/// </summary>
	public XcciHistogramVolStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 14)
		.SetDisplay("CCI Period", "Length of the Commodity Channel Index", "Indicator")
		.SetRange(5, 200)
		.SetCanOptimize(true);

		_maLength = Param(nameof(MaLength), 12)
		.SetDisplay("Smoothing Length", "Length for smoothing the volume weighted CCI", "Indicator")
		.SetRange(1, 200)
		.SetCanOptimize(true);

		_highLevel2 = Param(nameof(HighLevel2), 100m)
		.SetDisplay("High Level 2", "Upper extreme multiplier applied to smoothed volume", "Thresholds")
		.SetRange(0m, 500m)
		.SetCanOptimize(true);

		_highLevel1 = Param(nameof(HighLevel1), 80m)
		.SetDisplay("High Level 1", "Upper warning multiplier applied to smoothed volume", "Thresholds")
		.SetRange(0m, 500m)
		.SetCanOptimize(true);

		_lowLevel1 = Param(nameof(LowLevel1), -80m)
		.SetDisplay("Low Level 1", "Lower warning multiplier applied to smoothed volume", "Thresholds")
		.SetRange(-500m, 0m)
		.SetCanOptimize(true);

		_lowLevel2 = Param(nameof(LowLevel2), -100m)
		.SetDisplay("Low Level 2", "Lower extreme multiplier applied to smoothed volume", "Thresholds")
		.SetRange(-500m, 0m)
		.SetCanOptimize(true);

		_signalBarOffset = Param(nameof(SignalBarOffset), 1)
		.SetDisplay("Signal Offset", "Number of closed candles to wait before acting", "Trading")
		.SetRange(0, 10);
		_colorExtremePositive = Param(nameof(ColorExtremePositive), 0)
			.SetDisplay("Extreme Positive Color", "Chart color index for the strongest bullish zone", "Visualization");

		_colorPositive = Param(nameof(ColorPositive), 1)
			.SetDisplay("Positive Color", "Chart color index for positive zone", "Visualization");

		_colorNeutral = Param(nameof(ColorNeutral), 2)
			.SetDisplay("Neutral Color", "Chart color index for neutral zone", "Visualization");

		_colorNegative = Param(nameof(ColorNegative), 3)
			.SetDisplay("Negative Color", "Chart color index for negative zone", "Visualization");

		_colorExtremeNegative = Param(nameof(ColorExtremeNegative), 4)
			.SetDisplay("Extreme Negative Color", "Chart color index for the strongest bearish zone", "Visualization");


		_smoothingMethod = Param(nameof(Smoothing), SmoothingMethods.Simple)
		.SetDisplay("Smoothing", "Moving average used to smooth indicator and volume", "Indicator");

		_allowLongEntries = Param(nameof(AllowLongEntries), true)
		.SetDisplay("Allow Long Entries", "Enable opening long positions", "Trading");

		_allowShortEntries = Param(nameof(AllowShortEntries), true)
		.SetDisplay("Allow Short Entries", "Enable opening short positions", "Trading");

		_allowLongExits = Param(nameof(AllowLongExits), true)
		.SetDisplay("Allow Long Exits", "Enable closing existing long positions", "Trading");

		_allowShortExits = Param(nameof(AllowShortExits), true)
		.SetDisplay("Allow Short Exits", "Enable closing existing short positions", "Trading");

		_primaryEntryVolume = Param(nameof(PrimaryEntryVolume), 0.1m)
		.SetDisplay("Primary Entry Volume", "Volume used for the first entry tier", "Trading")
		.SetRange(0m, 100m);

		_secondaryEntryVolume = Param(nameof(SecondaryEntryVolume), 0.2m)
		.SetDisplay("Secondary Entry Volume", "Volume used for the second entry tier", "Trading")
		.SetRange(0m, 100m);

		_useStopLoss = Param(nameof(UseStopLoss), false)
		.SetDisplay("Use Stop Loss", "Enable protective stop loss", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
		.SetDisplay("Stop Loss Points", "Stop loss distance in price points", "Risk")
		.SetGreaterThanZero();

		_useTakeProfit = Param(nameof(UseTakeProfit), false)
		.SetDisplay("Use Take Profit", "Enable protective take profit", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
		.SetDisplay("Take Profit Points", "Take profit distance in price points", "Risk")
		.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Candle timeframe for calculations", "General");
	}

	/// <summary>
	/// Period of the Commodity Channel Index.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Length of the smoothing moving averages.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Multiplier for the upper extreme level.
	/// </summary>
	public decimal HighLevel2
	{
		get => _highLevel2.Value;
		set => _highLevel2.Value = value;
	}

	/// <summary>
	/// Multiplier for the upper warning level.
	/// </summary>
	public decimal HighLevel1
	{
		get => _highLevel1.Value;
		set => _highLevel1.Value = value;
	}

	/// <summary>
	/// Multiplier for the lower warning level.
	/// </summary>
	public decimal LowLevel1
	{
		get => _lowLevel1.Value;
		set => _lowLevel1.Value = value;
	}

	/// <summary>
	/// Multiplier for the lower extreme level.
	/// </summary>
	public decimal LowLevel2
	{
		get => _lowLevel2.Value;
		set => _lowLevel2.Value = value;
	}

	/// <summary>
	/// Number of closed candles to delay signals.
	/// </summary>
	public int SignalBarOffset
	{
		get => _signalBarOffset.Value;
		set => _signalBarOffset.Value = value;
	}

	/// <summary>
	/// Moving average type used for smoothing.
	/// </summary>
	public SmoothingMethods Smoothing
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Color index used when histogram is in the extreme positive zone.
	/// </summary>
	public int ColorExtremePositive
	{
		get => _colorExtremePositive.Value;
		set => _colorExtremePositive.Value = value;
	}

	/// <summary>
	/// Color index used when histogram is in the positive zone.
	/// </summary>
	public int ColorPositive
	{
		get => _colorPositive.Value;
		set => _colorPositive.Value = value;
	}

	/// <summary>
	/// Color index used when histogram is in the neutral zone.
	/// </summary>
	public int ColorNeutral
	{
		get => _colorNeutral.Value;
		set => _colorNeutral.Value = value;
	}

	/// <summary>
	/// Color index used when histogram is in the negative zone.
	/// </summary>
	public int ColorNegative
	{
		get => _colorNegative.Value;
		set => _colorNegative.Value = value;
	}

	/// <summary>
	/// Color index used when histogram is in the extreme negative zone.
	/// </summary>
	public int ColorExtremeNegative
	{
		get => _colorExtremeNegative.Value;
		set => _colorExtremeNegative.Value = value;
	}

	/// <summary>
	/// Enable or disable long entries.
	/// </summary>
	public bool AllowLongEntries
	{
		get => _allowLongEntries.Value;
		set => _allowLongEntries.Value = value;
	}

	/// <summary>
	/// Enable or disable short entries.
	/// </summary>
	public bool AllowShortEntries
	{
		get => _allowShortEntries.Value;
		set => _allowShortEntries.Value = value;
	}

	/// <summary>
	/// Enable or disable closing of long positions.
	/// </summary>
	public bool AllowLongExits
	{
		get => _allowLongExits.Value;
		set => _allowLongExits.Value = value;
	}

	/// <summary>
	/// Enable or disable closing of short positions.
	/// </summary>
	public bool AllowShortExits
	{
		get => _allowShortExits.Value;
		set => _allowShortExits.Value = value;
	}

	/// <summary>
	/// Volume used for the first entry tier.
	/// </summary>
	public decimal PrimaryEntryVolume
	{
		get => _primaryEntryVolume.Value;
		set => _primaryEntryVolume.Value = value;
	}

	/// <summary>
	/// Volume used for the second entry tier.
	/// </summary>
	public decimal SecondaryEntryVolume
	{
		get => _secondaryEntryVolume.Value;
		set => _secondaryEntryVolume.Value = value;
	}

	/// <summary>
	/// Enable protective stop loss.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Enable protective take profit.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Take profit distance in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type to use for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_cci?.Reset();
		_cciVolumeAverage.Reset();
		_volumeAverage.Reset();
		_colorHistory.Clear();
		_primaryLongActive = false;
		_secondaryLongActive = false;
		_primaryShortActive = false;
		_secondaryShortActive = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_cciVolumeAverage = CreateMovingAverage(Smoothing, MaLength);
		_volumeAverage = CreateMovingAverage(Smoothing, MaLength);

		_colorHistory.Clear();
		_primaryLongActive = false;
		_secondaryLongActive = false;
		_primaryShortActive = false;
		_secondaryShortActive = false;

		if (UseStopLoss || UseTakeProfit)
		{
			StartProtection(
			takeProfit: UseTakeProfit ? new Unit(TakeProfitPoints, UnitTypes.Absolute) : null,
			stopLoss: UseStopLoss ? new Unit(StopLossPoints, UnitTypes.Absolute) : null,
			isStopTrailing: false,
			useMarketOrders: true);
		}

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(_cci, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cciVolumeAverage);
			DrawOwnTrades(area);

			var volumeArea = CreateChartArea();
			if (volumeArea != null)
			{
				DrawIndicator(volumeArea, _volumeAverage);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_cciVolumeAverage == null || _volumeAverage == null)
		return;

		// Reset entry flags when flat or positioned to the opposite side.
		if (Position <= 0)
		{
			_primaryLongActive = false;
			_secondaryLongActive = false;
		}

		if (Position >= 0)
		{
			_primaryShortActive = false;
			_secondaryShortActive = false;
		}

		// Multiply CCI by volume to reproduce indicator calculations.
		var volume = candle.TotalVolume;
		var cciVolume = cciValue * volume;

		var cciVolumeValue = _cciVolumeAverage.Process(cciVolume, candle.OpenTime, true);
		var volumeValue = _volumeAverage.Process(volume, candle.OpenTime, true);

		if (cciVolumeValue is not DecimalIndicatorValue { IsFinal: true, Value: var smoothedCciVolume } ||
		volumeValue is not DecimalIndicatorValue { IsFinal: true, Value: var smoothedVolume })
		{
			return;
		}

		if (!_cciVolumeAverage.IsFormed || !_volumeAverage.IsFormed)
		return;

		var maxLevel = HighLevel2 * smoothedVolume;
		var upperLevel = HighLevel1 * smoothedVolume;
		var lowerLevel = LowLevel1 * smoothedVolume;
		var minLevel = LowLevel2 * smoothedVolume;

		var color = DetermineColor(smoothedCciVolume, maxLevel, upperLevel, lowerLevel, minLevel);

		_colorHistory.Insert(0, color);
		var historyLimit = Math.Max(SignalBarOffset + 3, 5);
		if (_colorHistory.Count > historyLimit)
		{
			_colorHistory.RemoveRange(historyLimit, _colorHistory.Count - historyLimit);
		}

		if (_colorHistory.Count <= SignalBarOffset + 1)
		return;

		var signalColor = _colorHistory[SignalBarOffset];
		var previousColor = _colorHistory[SignalBarOffset + 1];

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		// Close existing positions before generating new entries.
		if (AllowLongExits && (signalColor == ColorNegative || signalColor == ColorExtremeNegative) && Position > 0)
		{
			SellMarket(Position);
			_primaryLongActive = false;
			_secondaryLongActive = false;
			LogInfo($"Closing long position because indicator moved to bearish zone {signalColor}.");
		}

		if (AllowShortExits && (signalColor == ColorPositive || signalColor == ColorExtremePositive) && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			_primaryShortActive = false;
			_secondaryShortActive = false;
			LogInfo($"Closing short position because indicator moved to bullish zone {signalColor}.");
		}

		// Long entries correspond to upward transitions into bullish colors.
		if (AllowLongEntries)
		{
			if (signalColor == ColorPositive && previousColor > ColorPositive && !_primaryLongActive)
			{
				var volumeToTrade = PrepareLongVolume(PrimaryEntryVolume);
				if (volumeToTrade > 0)
				{
					BuyMarket(volumeToTrade);
					_primaryLongActive = true;
					_primaryShortActive = false;
					_secondaryShortActive = false;
					LogInfo($"Opening primary long position with volume {volumeToTrade}.");
				}
			}

			if (signalColor == ColorExtremePositive && previousColor > ColorExtremePositive && !_secondaryLongActive)
			{
				var volumeToTrade = PrepareLongVolume(SecondaryEntryVolume);
				if (volumeToTrade > 0)
				{
					BuyMarket(volumeToTrade);
					_secondaryLongActive = true;
					_primaryShortActive = false;
					_secondaryShortActive = false;
					LogInfo($"Opening secondary long position with volume {volumeToTrade}.");
				}
			}
		}

		// Short entries correspond to downward transitions into bearish colors.
		if (AllowShortEntries)
		{
			if (signalColor == ColorNegative && previousColor < ColorNegative && !_primaryShortActive)
			{
				var volumeToTrade = PrepareShortVolume(PrimaryEntryVolume);
				if (volumeToTrade > 0)
				{
					SellMarket(volumeToTrade);
					_primaryShortActive = true;
					_primaryLongActive = false;
					_secondaryLongActive = false;
					LogInfo($"Opening primary short position with volume {volumeToTrade}.");
				}
			}

			if (signalColor == ColorExtremeNegative && previousColor < ColorExtremeNegative && !_secondaryShortActive)
			{
				var volumeToTrade = PrepareShortVolume(SecondaryEntryVolume);
				if (volumeToTrade > 0)
				{
					SellMarket(volumeToTrade);
					_secondaryShortActive = true;
					_primaryLongActive = false;
					_secondaryLongActive = false;
					LogInfo($"Opening secondary short position with volume {volumeToTrade}.");
				}
			}
		}
	}

	private static int DetermineColor(decimal value, decimal maxLevel, decimal upperLevel, decimal lowerLevel, decimal minLevel)
	{
		if (value > maxLevel)
		return ColorExtremePositive;

		if (value > upperLevel)
		return ColorPositive;

		if (value < minLevel)
		return ColorExtremeNegative;

		if (value < lowerLevel)
		return ColorNegative;

		return ColorNeutral;
	}

	private decimal PrepareLongVolume(decimal baseVolume)
	{
		if (baseVolume <= 0)
		return 0m;

		var volume = baseVolume;
		if (Position < 0)
		{
			volume += Math.Abs(Position);
		}

		return volume;
	}

	private decimal PrepareShortVolume(decimal baseVolume)
	{
		if (baseVolume <= 0)
		return 0m;

		var volume = baseVolume;
		if (Position > 0)
		{
			volume += Position;
		}

		return volume;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(SmoothingMethods method, int length)
	{
		LengthIndicator<decimal> indicator = method switch
		{
			SmoothingMethods.Exponential => new ExponentialMovingAverage(),
			SmoothingMethods.Smoothed => new SmoothedMovingAverage(),
			SmoothingMethods.Weighted => new WeightedMovingAverage(),
			SmoothingMethods.Hull => new HullMovingAverage(),
			SmoothingMethods.VolumeWeighted => new VolumeWeightedMovingAverage(),
			_ => new SimpleMovingAverage(),
		};

		indicator.Length = length;
		return indicator;
	}

	/// <summary>
	/// Supported smoothing methods.
	/// </summary>
	public enum SmoothingMethods
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted,
		Hull,
		VolumeWeighted
	}
}