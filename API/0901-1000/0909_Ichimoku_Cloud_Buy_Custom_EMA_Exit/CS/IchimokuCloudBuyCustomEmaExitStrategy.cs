using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ichimoku cloud buy with custom EMA exit strategy.
/// Long only. A long opens when the close is above the Ichimoku cloud and the candle volume exceeds its VolumeAvgPeriod average,
/// optionally also requiring the close above the EmaLength EMA. The position closes when the close falls below the EMA,
/// and a percent stop loss limits the loss.
/// </summary>
public class IchimokuCloudBuyCustomEmaExitStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanPeriod;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _volumeAvgPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _useEmaFilter;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeSma;

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
	public int SenkouSpanPeriod
	{
		get => _senkouSpanPeriod.Value;
		set => _senkouSpanPeriod.Value = value;
	}

	/// <summary>
	/// Exit EMA period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Volume average period.
	/// </summary>
	public int VolumeAvgPeriod
	{
		get => _volumeAvgPeriod.Value;
		set => _volumeAvgPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Require the close above the EMA for entries.
	/// </summary>
	public bool UseEmaFilter
	{
		get => _useEmaFilter.Value;
		set => _useEmaFilter.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public IchimokuCloudBuyCustomEmaExitStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Tenkan-sen period", "Ichimoku");

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Kijun-sen period", "Ichimoku");

		_senkouSpanPeriod = Param(nameof(SenkouSpanPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Senkou Span Period", "Senkou Span B period", "Ichimoku");

		_emaLength = Param(nameof(EmaLength), 44)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Exit EMA period", "Exit");

		_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Volume Avg Period", "Volume average period", "Volume");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_useEmaFilter = Param(nameof(UseEmaFilter), true)
			.SetDisplay("Use EMA Filter", "Require the close above the EMA for entries", "Exit");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_volumeSma = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_volumeSma = new SimpleMovingAverage { Length = VolumeAvgPeriod };

		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanPeriod },
		};
		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ichimoku, ema, ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ichimoku);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volume = candle.TotalVolume;
		var volumeValue = _volumeSma.Process(new DecimalIndicatorValue(_volumeSma, volume, candle.OpenTime) { IsFinal = true });

		if (!_volumeSma.IsFormed || volumeValue.IsEmpty || !emaValue.IsFormed)
			return;

		if (!ichimokuValue.IsFormed || ichimokuValue is not IchimokuValue { SenkouA: decimal senkouA, SenkouB: decimal senkouB })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var averageVolume = volumeValue.GetValue<decimal>();
		var ema = emaValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (close < ema)
				SellMarket(Position);
			return;
		}

		var aboveCloud = close > Math.Max(senkouA, senkouB);
		var emaOk = !UseEmaFilter || close > ema;

		if (aboveCloud && volume > averageVolume && emaOk)
			BuyMarket(Volume + Math.Abs(Position));
	}
}
