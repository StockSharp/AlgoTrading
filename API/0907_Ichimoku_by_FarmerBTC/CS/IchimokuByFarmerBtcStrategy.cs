namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on Ichimoku cloud with high timeframe SMA and volume filter.
/// </summary>
public class IchimokuByFarmerBtcStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<bool> _useLongs;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _htfCandleType;
	private readonly StrategyParam<int> _volumeLength;
	private readonly StrategyParam<decimal> _volumeMultiplier;

	private SimpleMovingAverage _volumeSma;
	private SimpleMovingAverage _htfSma;
	private decimal? _htfSmaValue;

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
	/// Enable long positions.
	/// </summary>
	public bool UseLongs
	{
		get => _useLongs.Value;
		set => _useLongs.Value = value;
	}

	/// <summary>
	/// Length for SMA on higher timeframe.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Primary candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type.
	/// </summary>
	public DataType HtfCandleType
	{
		get => _htfCandleType.Value;
		set => _htfCandleType.Value = value;
	}

	/// <summary>
	/// Volume moving average length.
	/// </summary>
	public int VolumeLength
	{
		get => _volumeLength.Value;
		set => _volumeLength.Value = value;
	}

	/// <summary>
	/// Volume multiplier for filter.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="IchimokuByFarmerBtcStrategy"/>.
	/// </summary>
	public IchimokuByFarmerBtcStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 10)
			.SetDisplay("Conversion Line Period", "Tenkan-sen period", "Ichimoku")
			.SetCanOptimize(true);

		_kijunPeriod = Param(nameof(KijunPeriod), 30)
			.SetDisplay("Base Line Period", "Kijun-sen period", "Ichimoku")
			.SetCanOptimize(true);

		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 53)
			.SetDisplay("Lagging Span Period", "Senkou Span B period", "Ichimoku")
			.SetCanOptimize(true);

		_useLongs = Param(nameof(UseLongs), true)
			.SetDisplay("Enable Long Positions", "Allow opening long positions", "Trading")
			.SetCanOptimize(false);

		_smaLength = Param(nameof(SmaLength), 13)
			.SetDisplay("HTF SMA Length", "Length of SMA on higher timeframe", "High Timeframe")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_htfCandleType = Param(nameof(HtfCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("HTF Candle Type", "Higher timeframe for SMA", "High Timeframe");

		_volumeLength = Param(nameof(VolumeLength), 20)
			.SetDisplay("Volume MA Length", "Length for volume moving average", "Volume")
			.SetCanOptimize(true);

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.5m)
			.SetDisplay("Volume Multiplier", "Multiplier for volume filter", "Volume")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_volumeSma = new SimpleMovingAverage { Length = VolumeLength };
		_htfSma = new SimpleMovingAverage { Length = SmaLength };

		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanBPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(ichimoku, ProcessCandle).Start();

		var htfSubscription = SubscribeCandles(HtfCandleType);
		htfSubscription.Bind(_htfSma, ProcessHtfCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ichimoku);
			DrawIndicator(area, _htfSma);

			var volumeArea = CreateChartArea();
			if (volumeArea != null)
				DrawIndicator(volumeArea, _volumeSma);

			DrawOwnTrades(area);
		}
	}

	private void ProcessHtfCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_htfSmaValue = smaValue;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeValue = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, true);
		if (!volumeValue.IsFinal || _htfSmaValue is not decimal htfSma)
			return;

		var volumeAvg = volumeValue.ToDecimal();
		var volumeCondition = candle.TotalVolume > volumeAvg * VolumeMultiplier;

		var ichimokuTyped = (IchimokuValue)ichimokuValue;
		if (ichimokuTyped.SenkouA is not decimal spanA || ichimokuTyped.SenkouB is not decimal spanB)
			return;

		var priceAboveCloud = candle.ClosePrice > Math.Max(spanA, spanB);
		var priceBelowCloud = candle.ClosePrice < Math.Min(spanA, spanB);
		var bullishCloud = spanA > spanB;
		var priceAboveSma = candle.ClosePrice > htfSma;

		var longCondition = UseLongs && priceAboveCloud && bullishCloud && priceAboveSma && volumeCondition;

		if (longCondition && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			LogInfo($"Enter long at {candle.ClosePrice}");
		}
		else if (priceBelowCloud && Position > 0)
		{
			SellMarket(Position);
			LogInfo($"Exit long at {candle.ClosePrice}");
		}
	}
}
