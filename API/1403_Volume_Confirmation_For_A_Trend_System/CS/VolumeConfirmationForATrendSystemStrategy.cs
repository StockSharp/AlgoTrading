namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Long-only strategy using ADX, Trend Thrust Indicator (TTI) and Volume Price Confirmation Indicator (VPCI).
/// Enters long when ADX > 30, TTI above signal and VPCI positive. Exits when VPCI turns negative.
/// </summary>
public class VolumeConfirmationForATrendSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<int> _adxSmooth;
	private readonly StrategyParam<int> _ttiFast;
	private readonly StrategyParam<int> _ttiSlow;
	private readonly StrategyParam<int> _ttiSignalLength;
	private readonly StrategyParam<int> _vpciShort;
	private readonly StrategyParam<int> _vpciLong;
	private readonly StrategyParam<DataType> _candleType;

	private readonly SimpleMovingAverage _ttiSignal = new();
	private readonly SimpleMovingAverage _vpciShortVolume = new();
	private readonly SimpleMovingAverage _vpciLongVolume = new();

	public VolumeConfirmationForATrendSystemStrategy()
	{
		_adxLength = Param(nameof(AdxLength), 14)
			.SetDisplay("ADX Length", "ADX calculation length", "General");
		_adxSmooth = Param(nameof(AdxSmooth), 14)
			.SetDisplay("ADX Smoothing", "ADX smoothing length", "General");
		_ttiFast = Param(nameof(TtiFast), 13)
			.SetDisplay("TTI Fast Average", "TTI fast VWMA length", "General");
		_ttiSlow = Param(nameof(TtiSlow), 26)
			.SetDisplay("TTI Slow Average", "TTI slow VWMA length", "General");
		_ttiSignalLength = Param(nameof(TtiSignalLength), 9)
			.SetDisplay("TTI Signal Length", "TTI signal SMA period", "General");
		_vpciShort = Param(nameof(VpciShort), 5)
			.SetDisplay("VPCI Short Avg", "VPCI short-term average", "General");
		_vpciLong = Param(nameof(VpciLong), 25)
			.SetDisplay("VPCI Long Avg", "VPCI long-term average", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");
	}

	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }
	public int AdxSmooth { get => _adxSmooth.Value; set => _adxSmooth.Value = value; }
	public int TtiFast { get => _ttiFast.Value; set => _ttiFast.Value = value; }
	public int TtiSlow { get => _ttiSlow.Value; set => _ttiSlow.Value = value; }
	public int TtiSignalLength { get => _ttiSignalLength.Value; set => _ttiSignalLength.Value = value; }
	public int VpciShort { get => _vpciShort.Value; set => _vpciShort.Value = value; }
	public int VpciLong { get => _vpciLong.Value; set => _vpciLong.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_ttiSignal.Length = TtiSignalLength;
		_vpciShortVolume.Length = VpciShort;
		_vpciLongVolume.Length = VpciLong;

		var adx = new AverageDirectionalIndex { Length = AdxLength, Smooth = AdxSmooth };
		var fastTti = new VolumeWeightedMovingAverage { Length = TtiFast };
		var slowTti = new VolumeWeightedMovingAverage { Length = TtiSlow };
		var vpciLongVwma = new VolumeWeightedMovingAverage { Length = VpciLong };
		var vpciLongSma = new SimpleMovingAverage { Length = VpciLong };
		var vpciShortVwma = new VolumeWeightedMovingAverage { Length = VpciShort };
		var vpciShortSma = new SimpleMovingAverage { Length = VpciShort };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, fastTti, slowTti, vpciLongVwma, vpciLongSma, vpciShortVwma, vpciShortSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawIndicator(area, fastTti);
			DrawIndicator(area, slowTti);
			DrawIndicator(area, vpciLongVwma);
			DrawIndicator(area, vpciLongSma);
			DrawIndicator(area, vpciShortVwma);
			DrawIndicator(area, vpciShortSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue adxValue,
		IIndicatorValue fastTtiValue,
		IIndicatorValue slowTtiValue,
		IIndicatorValue vpciLongVwmaValue,
		IIndicatorValue vpciLongSmaValue,
		IIndicatorValue vpciShortVwmaValue,
		IIndicatorValue vpciShortSmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var shortVol = _vpciShortVolume.Process(candle.TotalVolume, candle.OpenTime, true).ToDecimal();
		var longVol = _vpciLongVolume.Process(candle.TotalVolume, candle.OpenTime, true).ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading() || !_vpciShortVolume.IsFormed || !_vpciLongVolume.IsFormed)
			return;

		if (adxValue is not AverageDirectionalIndexValue adx ||
			fastTtiValue is not DecimalIndicatorValue fastVal ||
			slowTtiValue is not DecimalIndicatorValue slowVal ||
			vpciLongVwmaValue is not DecimalIndicatorValue vpciLongVwmaVal ||
			vpciLongSmaValue is not DecimalIndicatorValue vpciLongSmaVal ||
			vpciShortVwmaValue is not DecimalIndicatorValue vpciShortVwmaVal ||
			vpciShortSmaValue is not DecimalIndicatorValue vpciShortSmaVal)
			return;

		if (adx.MovingAverage is not decimal adxMa)
			return;

		var fast = fastVal.Value;
		var slow = slowVal.Value;
		var vMult = (fast / slow) * (fast / slow);
		var tti = fast * vMult - slow / vMult;
		var signal = _ttiSignal.Process(new DecimalIndicatorValue(_ttiSignal, tti, candle.OpenTime)).ToDecimal();

		var vpc = vpciLongVwmaVal.Value - vpciLongSmaVal.Value;
		var vpr = vpciShortVwmaVal.Value / vpciShortSmaVal.Value;
		var vm = shortVol / longVol;
		var vpci = vpc * vpr * vm;

		if (adxMa > 30m && tti > signal && vpci > 0 && Position <= 0)
		{
			BuyMarket();
		}
		else if (vpci < 0 && Position > 0)
		{
			ClosePosition();
		}
	}
}
