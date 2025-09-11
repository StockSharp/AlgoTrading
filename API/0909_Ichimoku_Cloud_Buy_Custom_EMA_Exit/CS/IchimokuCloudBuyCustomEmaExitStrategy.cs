using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ichimoku Cloud Buy strategy with EMA-based exit and volume filter.
/// Buys when price is above both Senkou spans and volume exceeds its average.
/// Optionally requires price above EMA. Exits when price falls below EMA.
/// </summary>
public class IchimokuCloudBuyCustomEmaExitStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanPeriod;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _volumeAvgPeriod;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _requireAboveEma;
	private readonly StrategyParam<DataType> _candleType;

	private Ichimoku _ichimoku;
	private ExponentialMovingAverage _ema;
	private SimpleMovingAverage _volumeMa;

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
	/// EMA length for exit.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Period for average volume.
	/// </summary>
	public int VolumeAvgPeriod
	{
		get => _volumeAvgPeriod.Value;
		set => _volumeAvgPeriod.Value = value;
	}

	/// <summary>
	/// Use stop-loss.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Stop-loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Require price above EMA to buy.
	/// </summary>
	public bool RequireAboveEma
	{
		get => _requireAboveEma.Value;
		set => _requireAboveEma.Value = value;
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
	/// Initialize strategy.
	/// </summary>
	public IchimokuCloudBuyCustomEmaExitStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetDisplay("Tenkan Period", "Tenkan-sen period", "Ichimoku");

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetDisplay("Kijun Period", "Kijun-sen period", "Ichimoku");

		_senkouSpanPeriod = Param(nameof(SenkouSpanPeriod), 52)
			.SetDisplay("Senkou Span B Period", "Senkou Span B period", "Ichimoku");

		_emaLength = Param(nameof(EmaLength), 44)
			.SetDisplay("EMA Length", "EMA length for exit", "EMA");

		_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 10)
			.SetDisplay("Volume Average Period", "Period for volume SMA", "Volume");

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Enable stop-loss protection", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetDisplay("Stop Loss (%)", "Stop-loss percent", "Risk");

		_requireAboveEma = Param(nameof(RequireAboveEma), true)
			.SetDisplay("Require Above EMA", "Buy only if price above EMA", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanPeriod }
		};

		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_volumeMa = new SimpleMovingAverage { Length = VolumeAvgPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_ichimoku, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ichimoku);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}

		if (UseStopLoss)
			StartProtection(null, new(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ich = (IchimokuValue)ichimokuValue;

		if (ich.SenkouA is not decimal senkouA)
			return;

		if (ich.SenkouB is not decimal senkouB)
			return;

		var emaVal = _ema.Process(candle.ClosePrice, candle.ServerTime, true).ToDecimal();
		var avgVol = _volumeMa.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();

		var priceAboveCloud = candle.ClosePrice > Math.Max(senkouA, senkouB);
		var volumeAboveAvg = candle.TotalVolume > avgVol;
		var aboveEma = !RequireAboveEma || candle.ClosePrice > emaVal;

		if (priceAboveCloud && volumeAboveAvg && aboveEma && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (Position > 0 && candle.ClosePrice < emaVal)
		{
			SellMarket(Position);
		}
	}
}