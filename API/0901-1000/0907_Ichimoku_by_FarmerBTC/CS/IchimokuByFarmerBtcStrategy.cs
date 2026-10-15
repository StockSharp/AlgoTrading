using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ichimoku by FarmerBTC strategy.
/// Long only. A long opens when the close is above the Ichimoku cloud, the cloud is bullish (Senkou A above Senkou B), the close is above
/// the SmaLength SMA of the higher timeframe and the candle volume exceeds its VolumeLength average multiplied by VolumeMultiplier.
/// The position closes when the close falls below the cloud. There are no stops.
/// </summary>
public class IchimokuByFarmerBtcStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _volumeLength;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _htfCandleType;

	private SimpleMovingAverage _volumeSma;
	private decimal? _htfSma;

	/// <summary>
	/// Tenkan-sen period.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Kijun-sen period.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Senkou Span B period.
	/// </summary>
	public int SenkouSpanBPeriod
	{
		get => _senkouSpanBPeriod.Value;
		set => _senkouSpanBPeriod.Value = value;
	}

	/// <summary>
	/// Higher timeframe SMA period.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Volume moving average period.
	/// </summary>
	public int VolumeLength
	{
		get => _volumeLength.Value;
		set => _volumeLength.Value = value;
	}

	/// <summary>
	/// Factor applied to the volume average.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Working candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type for the trend SMA.
	/// </summary>
	public DataType HtfCandleType
	{
		get => _htfCandleType.Value;
		set => _htfCandleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public IchimokuByFarmerBtcStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Tenkan-sen period", "Ichimoku");

		_kijunPeriod = Param(nameof(KijunPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Kijun-sen period", "Ichimoku");

		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 53)
			.SetGreaterThanZero()
			.SetDisplay("Senkou Span B Period", "Senkou Span B period", "Ichimoku");

		_smaLength = Param(nameof(SmaLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Higher timeframe SMA period", "Trend");

		_volumeLength = Param(nameof(VolumeLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume Length", "Volume moving average period", "Volume");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Multiplier", "Factor applied to the volume average", "Volume");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Working candle type", "General");

		_htfCandleType = Param(nameof(HtfCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("HTF Candle Type", "Higher timeframe candle type for the trend SMA", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, HtfCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_volumeSma = null;
		_htfSma = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_htfSma = null;
		_volumeSma = new SimpleMovingAverage { Length = VolumeLength };

		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanBPeriod },
		};
		var htfSma = new SimpleMovingAverage { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ichimoku, ProcessCandle)
			.Start();

		SubscribeCandles(HtfCandleType)
			.BindEx(htfSma, ProcessHtfCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ichimoku);
			DrawOwnTrades(area);
		}
	}

	private void ProcessHtfCandle(ICandleMessage candle, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished || !smaValue.IsFormed)
			return;

		_htfSma = smaValue.GetValue<decimal>();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volume = candle.TotalVolume;
		var volumeValue = _volumeSma.Process(new DecimalIndicatorValue(_volumeSma, volume, candle.OpenTime) { IsFinal = true });

		if (!_volumeSma.IsFormed || volumeValue.IsEmpty)
			return;

		var averageVolume = volumeValue.GetValue<decimal>();

		if (!ichimokuValue.IsFormed || ichimokuValue is not IchimokuValue { SenkouA: decimal senkouA, SenkouB: decimal senkouB })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var cloudTop = Math.Max(senkouA, senkouB);
		var cloudBottom = Math.Min(senkouA, senkouB);

		if (Position > 0)
		{
			if (close < cloudBottom)
				SellMarket(Position);
			return;
		}

		if (_htfSma is not decimal trendSma)
			return;

		if (close > cloudTop && senkouA > senkouB && close > trendSma && volume > averageVolume * VolumeMultiplier)
			BuyMarket(Volume + Math.Abs(Position));
	}
}
