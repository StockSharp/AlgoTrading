using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ichimoku Cloud strategy with EMA and volume filters.
/// Buys above the cloud with strong volume and sells below the cloud.
/// </summary>
public class IchimokuCloudBuySellStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _avgVolumeLength;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _requireAboveEma;
	private readonly StrategyParam<bool> _requireBelowEma;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevAvgVolume;

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
	/// EMA length for exit filter.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Length for average volume calculation.
	/// </summary>
	public int AvgVolumeLength
	{
		get => _avgVolumeLength.Value;
		set => _avgVolumeLength.Value = value;
	}

	/// <summary>
	/// Enable stop-loss protection.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Require price above EMA for long entry.
	/// </summary>
	public bool RequireAboveEma
	{
		get => _requireAboveEma.Value;
		set => _requireAboveEma.Value = value;
	}

	/// <summary>
	/// Require price below EMA for short entry.
	/// </summary>
	public bool RequireBelowEma
	{
		get => _requireBelowEma.Value;
		set => _requireBelowEma.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public IchimokuCloudBuySellStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Tenkan-sen periods", "Ichimoku Settings")
			.SetCanOptimize(true)
			.SetOptimize(7, 11, 1);

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Kijun-sen periods", "Ichimoku Settings")
			.SetCanOptimize(true)
			.SetOptimize(20, 30, 2);

		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Senkou Span B Period", "Senkou Span B periods", "Ichimoku Settings")
			.SetCanOptimize(true)
			.SetOptimize(40, 60, 4);

		_emaPeriod = Param(nameof(EmaPeriod), 44)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA length for exit", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(20, 80, 5);

		_avgVolumeLength = Param(nameof(AvgVolumeLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Average Volume Length", "Length for average volume filter", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Enable stop-loss exits", "Risk Management");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_requireAboveEma = Param(nameof(RequireAboveEma), true)
			.SetDisplay("Only Buy Above EMA", "Require price above EMA for long entries", "Filters");

		_requireBelowEma = Param(nameof(RequireBelowEma), true)
			.SetDisplay("Only Sell Below EMA", "Require price below EMA for short entries", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevAvgVolume = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanBPeriod }
		};

		var ema = new EMA { Length = EmaPeriod };
		var volumeMa = new SimpleMovingAverage { Length = AvgVolumeLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(ichimoku, ema, volumeMa, ProcessCandle)
			.Start();

		if (UseStopLoss)
			StartProtection(stopLoss: new Unit(StopLossPercent, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ichimoku);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue, IIndicatorValue emaValue, IIndicatorValue volumeValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!emaValue.IsFinal || !volumeValue.IsFinal)
			return;

		var ema = emaValue.ToDecimal();
		var avgVol = volumeValue.ToDecimal();

		var volCondition = _prevAvgVolume > 0 && candle.TotalVolume > _prevAvgVolume;
		_prevAvgVolume = avgVol;

		var ichi = (IchimokuValue)ichimokuValue;

		if (ichi.SenkouA is not decimal senkouA ||
			ichi.SenkouB is not decimal senkouB)
			return;

		var upperKumo = Math.Max(senkouA, senkouB);
		var lowerKumo = Math.Min(senkouA, senkouB);

		var buyCondition = candle.ClosePrice > upperKumo && volCondition && (!RequireAboveEma || candle.ClosePrice > ema);
		var sellCondition = candle.ClosePrice < lowerKumo && volCondition && (!RequireBelowEma || candle.ClosePrice < ema);

		if (buyCondition && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (sellCondition && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		if (Position > 0 && candle.ClosePrice < ema)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0 && candle.ClosePrice > ema)
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}

