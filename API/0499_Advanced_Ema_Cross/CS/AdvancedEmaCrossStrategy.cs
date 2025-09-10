using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Advanced EMA Cross Strategy - uses normalized ATR, ADX and SuperTrend filters.
/// </summary>
public class AdvancedEmaCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaShortLength;
	private readonly StrategyParam<int> _emaLongLength;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _atrRange;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxHighLevel;
	private readonly StrategyParam<int> _usdEmaLength;
	private readonly StrategyParam<int> _volumeSmaLength;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<decimal> _bullAtrThreshold;
	private readonly StrategyParam<decimal> _bearAtrThreshold;
	
	private ExponentialMovingAverage _emaShort;
	private ExponentialMovingAverage _emaLong;
	private AverageTrueRange _atr;
	private Highest _highestAtr;
	private Lowest _lowestAtr;
	private AverageDirectionalIndex _adx;
	private ExponentialMovingAverage _usdEma;
	private SimpleMovingAverage _volumeSma;
	private SuperTrend _supertrend;
	
	private decimal _prevEmaShort;
	private decimal _prevEmaLong;
	private bool _hasBought;
	private int _barCount;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	
	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Short EMA length.
	/// </summary>
	public int EmaShortLength
	{
		get => _emaShortLength.Value;
		set => _emaShortLength.Value = value;
	}
	
	/// <summary>
	/// Long EMA length.
	/// </summary>
	public int EmaLongLength
	{
		get => _emaLongLength.Value;
		set => _emaLongLength.Value = value;
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
	/// Lookback range for ATR normalization.
	/// </summary>
	public int AtrRange
	{
		get => _atrRange.Value;
		set => _atrRange.Value = value;
	}
	
	/// <summary>
	/// ADX calculation period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}
	
	/// <summary>
	/// ADX threshold for exit.
	/// </summary>
	public decimal AdxHighLevel
	{
		get => _adxHighLevel.Value;
		set => _adxHighLevel.Value = value;
	}
	
	/// <summary>
	/// EMA length for USD strength calculation.
	/// </summary>
	public int UsdEmaLength
	{
		get => _usdEmaLength.Value;
		set => _usdEmaLength.Value = value;
	}
	
	/// <summary>
	/// SMA length for volume filter.
	/// </summary>
	public int VolumeSmaLength
	{
		get => _volumeSmaLength.Value;
		set => _volumeSmaLength.Value = value;
	}
	
	/// <summary>
	/// Volume multiplier.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}
	
	/// <summary>
	/// SuperTrend ATR period.
	/// </summary>
	public int SupertrendPeriod
	{
		get => _supertrendPeriod.Value;
		set => _supertrendPeriod.Value = value;
	}
	
	/// <summary>
	/// SuperTrend ATR multiplier.
	/// </summary>
	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
	}
	
	/// <summary>
	/// Normalized ATR threshold for bull market.
	/// </summary>
	public decimal BullAtrThreshold
	{
		get => _bullAtrThreshold.Value;
		set => _bullAtrThreshold.Value = value;
	}
	
	/// <summary>
	/// Normalized ATR threshold for bear market.
	/// </summary>
	public decimal BearAtrThreshold
	{
		get => _bearAtrThreshold.Value;
		set => _bearAtrThreshold.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="AdvancedEmaCrossStrategy"/>.
	/// </summary>
	public AdvancedEmaCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
		
		_emaShortLength = Param(nameof(EmaShortLength), 8)
		.SetGreaterThanZero()
		.SetDisplay("EMA Short Length", "Short EMA period", "EMA");
		
		_emaLongLength = Param(nameof(EmaLongLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("EMA Long Length", "Long EMA period", "EMA");
		
		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR calculation period", "ATR");
		
		_atrRange = Param(nameof(AtrRange), 20)
		.SetGreaterThanZero()
		.SetDisplay("ATR Range", "ATR normalization range", "ATR");
		
		_adxPeriod = Param(nameof(AdxPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ADX Period", "ADX calculation period", "ADX");
		
		_adxHighLevel = Param(nameof(AdxHighLevel), 30m)
		.SetRange(1m, 100m)
		.SetDisplay("ADX High Level", "ADX threshold for exit", "ADX");
		
		_usdEmaLength = Param(nameof(UsdEmaLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("USD EMA Length", "EMA length for USD strength", "Filters");
		
		_volumeSmaLength = Param(nameof(VolumeSmaLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Volume SMA Length", "Volume SMA length", "Volume");
		
		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.5m)
		.SetRange(0.1m, 10m)
		.SetDisplay("Volume Multiplier", "Volume threshold multiplier", "Volume");
		
		_supertrendPeriod = Param(nameof(SupertrendPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("SuperTrend Period", "ATR period for SuperTrend", "SuperTrend");
		
		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 4m)
		.SetRange(0.1m, 10m)
		.SetDisplay("SuperTrend Multiplier", "ATR multiplier for SuperTrend", "SuperTrend");
		
		_bullAtrThreshold = Param(nameof(BullAtrThreshold), 0.2m)
		.SetRange(0m, 1m)
		.SetDisplay("Bull ATR Threshold", "Normalized ATR threshold in bull market", "ATR");
		
		_bearAtrThreshold = Param(nameof(BearAtrThreshold), 0.5m)
		.SetRange(0m, 1m)
		.SetDisplay("Bear ATR Threshold", "Normalized ATR threshold in bear market", "ATR");
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
		
		_prevEmaShort = default;
		_prevEmaLong = default;
		_hasBought = default;
		_barCount = default;
		_entryPrice = default;
		_stopPrice = default;
		_takePrice = default;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_emaShort = new() { Length = EmaShortLength };
		_emaLong = new() { Length = EmaLongLength };
		_atr = new() { Length = AtrPeriod };
		_highestAtr = new() { Length = AtrRange };
		_lowestAtr = new() { Length = AtrRange };
		_adx = new() { Length = AdxPeriod };
		_usdEma = new() { Length = UsdEmaLength };
		_volumeSma = new() { Length = VolumeSmaLength };
		_supertrend = new() { Length = SupertrendPeriod, Multiplier = SupertrendMultiplier };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_emaShort, _emaLong, _atr, _usdEma, _supertrend, _adx, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaShort);
			DrawIndicator(area, _emaLong);
			DrawIndicator(area, _supertrend);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaShortValue, IIndicatorValue emaLongValue, IIndicatorValue atrValue, IIndicatorValue usdEmaValue, IIndicatorValue supertrendValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var emaShort = emaShortValue.ToDecimal();
		var emaLong = emaLongValue.ToDecimal();
		var atr = atrValue.ToDecimal();
		var usdEma = usdEmaValue.ToDecimal();
		var supertrend = supertrendValue.ToDecimal();
		
		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adx)
		return;
		
		var volumeSma = _volumeSma.Process(candle.TotalVolume).ToDecimal();
		var maxAtr = _highestAtr.Process(atr).ToDecimal();
		var minAtr = _lowestAtr.Process(atr).ToDecimal();
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (!_volumeSma.IsFormed || !_highestAtr.IsFormed || !_lowestAtr.IsFormed || !_adx.IsFormed)
		return;
		
		var normalizedAtr = maxAtr != minAtr ? (atr - minAtr) / (maxAtr - minAtr) : 0m;
		var usdStrength = usdEma != 0m ? candle.ClosePrice / usdEma - 1m : 0m;
		var stopLossPercent = usdStrength > 0 ? 3m : 4m;
		var takeProfitPercent = usdStrength > 0 ? 6m : 8m;
		var volumeThreshold = volumeSma * VolumeMultiplier;
		var isVolumeHigh = candle.TotalVolume > volumeThreshold;
		
		var direction = candle.ClosePrice > supertrend ? -1 : 1;
		var isBull = direction < 0;
		var isBear = direction > 0;
		
		var crossover = _prevEmaShort <= _prevEmaLong && emaShort > emaLong;
		var crossunder = _prevEmaShort >= _prevEmaLong && emaShort < emaLong;
		
		_prevEmaShort = emaShort;
		_prevEmaLong = emaLong;
		
		var buyCondition = (isBull && crossover && normalizedAtr > BullAtrThreshold) ||
		(isBear && crossover && normalizedAtr > BearAtrThreshold);
		var isAdxHigh = adx > AdxHighLevel;
		var sellCondition = (isBull && (crossunder || isAdxHigh)) ||
		(isBear && (crossunder || isAdxHigh));
		
		if (buyCondition && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_hasBought = true;
			_barCount = 0;
			_stopPrice = _entryPrice * (1m - stopLossPercent / 100m);
			_takePrice = _entryPrice * (1m + takeProfitPercent / 100m);
		}
		
		if (_hasBought)
		_barCount++;
		
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				ClosePosition();
				_hasBought = false;
				_barCount = 0;
				return;
			}
		}
		
		if (sellCondition && _hasBought && _barCount >= 3 && isVolumeHigh)
		{
			ClosePosition();
			_hasBought = false;
			_barCount = 0;
		}
	}
}
