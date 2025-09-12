using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// NQ Phantom Scalper Pro strategy based on VWAP bands with optional volume and trend filters.
/// </summary>
public class NqPhantomScalperProStrategy : Strategy
{
	private readonly StrategyParam<decimal> _band1Mult;
	private readonly StrategyParam<decimal> _band2Mult;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrStopMult;
	private readonly StrategyParam<int> _volumeLookback;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<bool> _useVolumeFilter;
	private readonly StrategyParam<bool> _useTrendFilter;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private bool _isLong;

	private readonly StandardDeviation _std = new();
	private readonly SimpleMovingAverage _volumeSma = new();
	private readonly ExponentialMovingAverage _trendEma = new();

	/// <summary>
	/// Multiplier for the first VWAP band.
	/// </summary>
	public decimal Band1Mult
	{
		get => _band1Mult.Value;
		set => _band1Mult.Value = value;
	}

	/// <summary>
	/// Multiplier for the second VWAP band.
	/// </summary>
	public decimal Band2Mult
	{
		get => _band2Mult.Value;
		set => _band2Mult.Value = value;
	}

	/// <summary>
	/// ATR indicator length.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Multiplier for ATR based stop-loss.
	/// </summary>
	public decimal AtrStopMult
	{
		get => _atrStopMult.Value;
		set => _atrStopMult.Value = value;
	}

	/// <summary>
	/// Period for volume SMA.
	/// </summary>
	public int VolumeLookback
	{
		get => _volumeLookback.Value;
		set => _volumeLookback.Value = value;
	}

	/// <summary>
	/// Volume spike multiplier.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Enable volume filter.
	/// </summary>
	public bool UseVolumeFilter
	{
		get => _useVolumeFilter.Value;
		set => _useVolumeFilter.Value = value;
	}

	/// <summary>
	/// Enable trend filter.
	/// </summary>
	public bool UseTrendFilter
	{
		get => _useTrendFilter.Value;
		set => _useTrendFilter.Value = value;
	}

	/// <summary>
	/// Trend EMA length.
	/// </summary>
	public int TrendLength
	{
		get => _trendLength.Value;
		set => _trendLength.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="NqPhantomScalperProStrategy"/>.
	/// </summary>
	public NqPhantomScalperProStrategy()
	{
		_band1Mult = Param(nameof(Band1Mult), 1.0m)
		.SetGreaterThanZero()
		.SetDisplay("Band #1 Mult", "Multiplier for first VWAP band", "VWAP");

		_band2Mult = Param(nameof(Band2Mult), 2.0m)
		.SetGreaterThanZero()
		.SetDisplay("Band #2 Mult", "Multiplier for second VWAP band", "VWAP");

		_atrLength = Param(nameof(AtrLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "ATR calculation period", "Risk Management");

		_atrStopMult = Param(nameof(AtrStopMult), 1.0m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Stop Mult", "Stop-loss multiplier of ATR", "Risk Management");

		_volumeLookback = Param(nameof(VolumeLookback), 20)
		.SetGreaterThanZero()
		.SetDisplay("Volume SMA Period", "Volume moving average period", "Volume");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Volume Spike Mult", "Volume spike multiplier", "Volume");

		_useVolumeFilter = Param(nameof(UseVolumeFilter), true)
		.SetDisplay("Enable Volume Filter", "Use volume filter", "Volume");

		_useTrendFilter = Param(nameof(UseTrendFilter), false)
		.SetDisplay("Enable Trend Filter", "Use trend EMA filter", "Trend Filter");

		_trendLength = Param(nameof(TrendLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("Trend EMA Length", "Period for trend EMA", "Trend Filter");

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
	_entryPrice = 0m;
	_stopPrice = 0m;
	_isLong = false;
	_std.Length = 20;
	_volumeSma.Length = VolumeLookback;
	_trendEma.Length = TrendLength;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_volumeSma.Length = VolumeLookback;
	_trendEma.Length = TrendLength;

	var vwap = new VolumeWeightedMovingAverage();
	var atr = new AverageTrueRange { Length = AtrLength };

	var subscription = SubscribeCandles(CandleType);

	subscription
	.BindEx(vwap, atr, ProcessCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, vwap);
	DrawIndicator(area, atr);
	DrawOwnTrades(area);
	}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue vwapValue, IIndicatorValue atrValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	var vwap = vwapValue.ToDecimal();
	var atr = atrValue.ToDecimal();

	var stdVal = _std.Process(candle.ClosePrice).ToDecimal();
	var volVal = _volumeSma.Process(candle.TotalVolume).ToDecimal();
	var trendVal = _trendEma.Process(candle.ClosePrice).ToDecimal();

	var upper1 = vwap + stdVal * Band1Mult;
	var lower1 = vwap - stdVal * Band1Mult;

	var volOk = !UseVolumeFilter || candle.TotalVolume > volVal * VolumeMultiplier;
	var trendOkLong = !UseTrendFilter || candle.ClosePrice > trendVal;
	var trendOkShort = !UseTrendFilter || candle.ClosePrice < trendVal;

	if (Position <= 0 && candle.ClosePrice > upper1 && volOk && trendOkLong)
	{
	_entryPrice = candle.ClosePrice;
	_stopPrice = _entryPrice - atr * AtrStopMult;
	_isLong = true;
	BuyMarket(Volume + Math.Abs(Position));
	LogInfo($"Long entry at {_entryPrice}, stop {_stopPrice}");
	}
	else if (Position >= 0 && candle.ClosePrice < lower1 && volOk && trendOkShort)
	{
	_entryPrice = candle.ClosePrice;
	_stopPrice = _entryPrice + atr * AtrStopMult;
	_isLong = false;
	SellMarket(Volume + Math.Abs(Position));
	LogInfo($"Short entry at {_entryPrice}, stop {_stopPrice}");
	}

	if (Position > 0)
	{
	if (candle.ClosePrice <= _stopPrice || candle.ClosePrice < vwap)
	{
	SellMarket(Math.Abs(Position));
	LogInfo($"Exit long at {candle.ClosePrice}");
	}
	}
	else if (Position < 0)
	{
	if (candle.ClosePrice >= _stopPrice || candle.ClosePrice > vwap)
	{
	BuyMarket(Math.Abs(Position));
	LogInfo($"Exit short at {candle.ClosePrice}");
	}
	}
	}
}
