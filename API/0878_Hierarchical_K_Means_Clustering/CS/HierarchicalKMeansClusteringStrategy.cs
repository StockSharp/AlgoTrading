using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SuperTrend strategy with volatility clustering and volume based exits.
/// </summary>
public class HierarchicalKMeansClusteringStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _superTrendFactor;
	private readonly StrategyParam<int> _trainingDataLength;
	private readonly StrategyParam<int> _startYear;
	private readonly StrategyParam<int> _startMonth;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<bool> _maFilter;
	private readonly StrategyParam<int> _trendStrengthPeriod;
	private readonly StrategyParam<int> _trendStrengthThreshold;
	private readonly StrategyParam<bool> _useTrendStrength;
	private readonly StrategyParam<bool> _volumeSlEnabled;
	private readonly StrategyParam<decimal> _volumeRatioThreshold;
	private readonly StrategyParam<int> _delayBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _bullVolumes = new decimal[3];
	private readonly decimal[] _bearVolumes = new decimal[3];

	private Highest _atrHighest;
	private Lowest _atrLowest;

	private int _volumeIndex;
	private int _barIndex;
	private int _entryBar;
	private bool _inLongTrade;
	private bool _inShortTrade;
	private decimal _entryVolumeRatio;
	private int _prevDir;
	private decimal _prevVolatility;

	/// <summary>
	/// ATR period for SuperTrend.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// SuperTrend multiplier.
	/// </summary>
	public decimal SuperTrendFactor
	{
		get => _superTrendFactor.Value;
		set => _superTrendFactor.Value = value;
	}

	/// <summary>
	/// Length of training data for volatility clustering.
	/// </summary>
	public int TrainingDataLength
	{
		get => _trainingDataLength.Value;
		set => _trainingDataLength.Value = value;
	}

	/// <summary>
	/// Year to start trading.
	/// </summary>
	public int StartYear
	{
		get => _startYear.Value;
		set => _startYear.Value = value;
	}

	/// <summary>
	/// Month to start trading.
	/// </summary>
	public int StartMonth
	{
		get => _startMonth.Value;
		set => _startMonth.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Use moving average filter.
	/// </summary>
	public bool MaFilter
	{
		get => _maFilter.Value;
		set => _maFilter.Value = value;
	}

	/// <summary>
	/// ADX calculation period.
	/// </summary>
	public int TrendStrengthPeriod
	{
		get => _trendStrengthPeriod.Value;
		set => _trendStrengthPeriod.Value = value;
	}

	/// <summary>
	/// ADX threshold for strong trend.
	/// </summary>
	public int TrendStrengthThreshold
	{
		get => _trendStrengthThreshold.Value;
		set => _trendStrengthThreshold.Value = value;
	}

	/// <summary>
	/// Use ADX filter.
	/// </summary>
	public bool UseTrendStrength
	{
		get => _useTrendStrength.Value;
		set => _useTrendStrength.Value = value;
	}

	/// <summary>
	/// Enable volume based stop loss / take profit.
	/// </summary>
	public bool VolumeSlEnabled
	{
		get => _volumeSlEnabled.Value;
		set => _volumeSlEnabled.Value = value;
	}

	/// <summary>
	/// Bull/bear volume ratio threshold.
	/// </summary>
	public decimal VolumeRatioThreshold
	{
		get => _volumeRatioThreshold.Value;
		set => _volumeRatioThreshold.Value = value;
	}

	/// <summary>
	/// Bars delay before monitoring volume ratio.
	/// </summary>
	public int DelayBars
	{
		get => _delayBars.Value;
		set => _delayBars.Value = value;
	}

	/// <summary>
	/// Candle type used by strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public HierarchicalKMeansClusteringStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 11)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "ATR calculation period", "SuperTrend Settings");

		_superTrendFactor = Param(nameof(SuperTrendFactor), 3m)
		.SetGreaterThanZero()
		.SetDisplay("SuperTrend Factor", "Multiplier for SuperTrend", "SuperTrend Settings");

		_trainingDataLength = Param(nameof(TrainingDataLength), 200)
		.SetGreaterThanZero()
		.SetDisplay("Training Data Length", "Period for volatility clustering", "Clustering Settings");

		_startYear = Param(nameof(StartYear), 2020)
		.SetDisplay("Start Year", "Year to start trading", "Time Settings");

		_startMonth = Param(nameof(StartMonth), 1)
		.SetDisplay("Start Month", "Month to start trading", "Time Settings");

		_maLength = Param(nameof(MaLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("Moving Average Length", "Length of the moving average", "Filter Settings");

		_maFilter = Param(nameof(MaFilter), true)
		.SetDisplay("Use Moving Average Filter", "Enable MA filter", "Filter Settings");

		_trendStrengthPeriod = Param(nameof(TrendStrengthPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Trend Strength Period", "ADX calculation period", "Filter Settings");

		_trendStrengthThreshold = Param(nameof(TrendStrengthThreshold), 20)
		.SetGreaterThanZero()
		.SetDisplay("Trend Strength Threshold", "ADX value for strong trend", "Filter Settings");

		_useTrendStrength = Param(nameof(UseTrendStrength), true)
		.SetDisplay("Use Trend Strength Filter", "Enable ADX filter", "Filter Settings");

		_volumeSlEnabled = Param(nameof(VolumeSlEnabled), true)
		.SetDisplay("Enable Volume SL/TP", "Enable volume ratio exit", "Volume Stop Loss Settings");

		_volumeRatioThreshold = Param(nameof(VolumeRatioThreshold), 0.9m)
		.SetDisplay("Volume Ratio Threshold", "Bull/Bear volume ratio threshold", "Volume Stop Loss Settings");

		_delayBars = Param(nameof(DelayBars), 4)
		.SetGreaterThanZero()
		.SetDisplay("Monitoring Delay Bars", "Bars before checking volume ratio", "Volume Stop Loss Settings");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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
		_volumeIndex = 0;
		_barIndex = 0;
		_entryBar = 0;
		_inLongTrade = false;
		_inShortTrade = false;
		_entryVolumeRatio = 0m;
		_prevDir = 0;
		_prevVolatility = 0m;
		Array.Clear(_bullVolumes, 0, _bullVolumes.Length);
		Array.Clear(_bearVolumes, 0, _bearVolumes.Length);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var atr = new ATR { Length = AtrLength };
		var supertrend = new SuperTrend { Period = AtrLength, Multiplier = SuperTrendFactor };
		var ma = new SMA { Length = MaLength };
		var adx = new AverageDirectionalIndex { Length = TrendStrengthPeriod };

		_atrHighest = new Highest { Length = TrainingDataLength };
		_atrLowest = new Lowest { Length = TrainingDataLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
		.BindEx(atr, supertrend, ma, adx, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, supertrend);
			DrawIndicator(area, ma);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(
	ICandleMessage candle,
	IIndicatorValue atrVal,
	IIndicatorValue supertrendVal,
	IIndicatorValue maVal,
	IIndicatorValue adxVal)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		_barIndex++;

		if (atrVal is not DecimalIndicatorValue atrValue ||
		maVal is not DecimalIndicatorValue maValue ||
		supertrendVal is not SuperTrendIndicatorValue stValue ||
		adxVal is not AverageDirectionalIndexValue adxValue)
		return;

		var volatility = atrValue.Value;
		var maLine = maValue.Value;
		var isStrongTrend = adxValue.MovingAverage > TrendStrengthThreshold;

		var highVal = _atrHighest.Process(atrValue);
		var lowVal = _atrLowest.Process(atrValue);

		if (highVal is not DecimalIndicatorValue high || lowVal is not DecimalIndicatorValue low)
		return;

		if (!highVal.IsFinal || !lowVal.IsFinal)
		return;

		var upper = high.Value;
		var lower = low.Value;
		var range = (upper - lower) / 3m;
		var lowCenter = lower + range / 2m;
		var midCenter = lower + range * 1.5m;
		var highCenter = lower + range * 2.5m;

		var distToLow = Math.Abs(volatility - lowCenter);
		var distToMid = Math.Abs(volatility - midCenter);
		var distToHigh = Math.Abs(volatility - highCenter);

		int clusterId;
		if (distToLow < distToMid && distToLow < distToHigh)
		clusterId = 0;
		else if (distToMid < distToLow && distToMid < distToHigh)
		clusterId = 1;
		else
		clusterId = 2;

		var trend = clusterId == 0 ? -1 : clusterId == 2 ? 1 : (volatility > _prevVolatility ? 1 : -1);
		_prevVolatility = volatility;

		var dir = stValue.IsUpTrend ? -1 : 1;
		var isCrossUnder = _prevDir > 0 && dir < 0;
		var isCrossOver = _prevDir < 0 && dir > 0;
		_prevDir = dir;

		var isAfterStartTime = candle.OpenTime.Year > StartYear ||
		(candle.OpenTime.Year == StartYear && candle.OpenTime.Month >= StartMonth);

		var maConditionLong = !MaFilter || candle.ClosePrice > maLine;
		var maConditionShort = !MaFilter || candle.ClosePrice < maLine;
		var trendStrengthCondition = !UseTrendStrength || isStrongTrend;

		var bullVolume = candle.Volume * (candle.ClosePrice > candle.OpenPrice ? 1m : 0m);
		var bearVolume = candle.Volume * (candle.ClosePrice < candle.OpenPrice ? 1m : 0m);
		_bullVolumes[_volumeIndex % 3] = bullVolume;
		_bearVolumes[_volumeIndex % 3] = bearVolume;
		_volumeIndex++;

		var bullSum = _bullVolumes[0] + _bullVolumes[1] + _bullVolumes[2];
		var bearSum = _bearVolumes[0] + _bearVolumes[1] + _bearVolumes[2];
		var volumeRatio = bearSum <= 0m ? 0m : bullSum / Math.Max(bearSum, 0.01m);

		var longCondition = isCrossUnder && isAfterStartTime && maConditionLong && trendStrengthCondition && trend > 0;
		var shortCondition = isCrossOver && isAfterStartTime && maConditionShort && trendStrengthCondition && trend < 0;

		if (longCondition)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryBar = _barIndex;
			_entryVolumeRatio = volumeRatio;
			_inLongTrade = true;
			_inShortTrade = false;
		}
		else if (shortCondition)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryBar = _barIndex;
			_entryVolumeRatio = volumeRatio;
			_inLongTrade = false;
			_inShortTrade = true;
		}

		if (VolumeSlEnabled && _volumeIndex >= 3)
		{
			var longVolumeExit = _inLongTrade && (_barIndex - _entryBar >= DelayBars) &&
			Math.Abs(volumeRatio - 1m) <= (1m - VolumeRatioThreshold);
			var shortVolumeExit = _inShortTrade && (_barIndex - _entryBar >= DelayBars) &&
			Math.Abs(volumeRatio - 1m) <= (1m - VolumeRatioThreshold);

			if (longVolumeExit)
			{
				SellMarket(Math.Abs(Position));
				_inLongTrade = false;
			}
			if (shortVolumeExit)
			{
				BuyMarket(Math.Abs(Position));
				_inShortTrade = false;
			}
		}
	}
}
