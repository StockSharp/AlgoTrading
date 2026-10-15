namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Trades strong, rising ADX with a directional DI spread; exits on weakening or an ATR stop.
/// </summary>
public class DmiPowerMoveStrategy : Strategy
{
	private readonly StrategyParam<int> _dmiPeriod;
	private readonly StrategyParam<decimal> _diDifferenceThreshold;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _adxExitThreshold;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private decimal? _previousAdx;
	private decimal _stopPrice;

	public int DmiPeriod { get => _dmiPeriod.Value; set => _dmiPeriod.Value = value; }
	public decimal DiDifferenceThreshold { get => _diDifferenceThreshold.Value; set => _diDifferenceThreshold.Value = value; }
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	public decimal AdxExitThreshold { get => _adxExitThreshold.Value; set => _adxExitThreshold.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public DmiPowerMoveStrategy()
	{
		_dmiPeriod = Param(nameof(DmiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("DMI Period", "Period for DMI and ATR", "Indicators");
		_diDifferenceThreshold = Param(nameof(DiDifferenceThreshold), 5m)
			.SetRange(0m, 100m)
			.SetDisplay("DI Difference Threshold", "Minimum directional DI spread", "Trading");
		_adxThreshold = Param(nameof(AdxThreshold), 30m)
			.SetRange(0m, 100m)
			.SetDisplay("ADX Threshold", "Minimum ADX for entry", "Trading");
		_adxExitThreshold = Param(nameof(AdxExitThreshold), 25m)
			.SetRange(0m, 100m)
			.SetDisplay("ADX Exit Threshold", "Close when ADX falls below this value", "Trading");
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Stop distance in entry ATR multiples", "Protection");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousAdx = null;
		_stopPrice = 0m;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var dmi = new AverageDirectionalIndex { Length = DmiPeriod };
		var atr = new AverageTrueRange { Length = DmiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(dmi, atr, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, dmi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue dmiValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished || !dmiValue.IsFormed || !atrValue.IsFormed)
			return;
		if (dmiValue is not AverageDirectionalIndexValue dmi
			|| dmi.MovingAverage is not decimal adx || dmi.Dx.Plus is not decimal plus || dmi.Dx.Minus is not decimal minus)
			return;

		var rising = _previousAdx is decimal previous && adx > previous;
		_previousAdx = adx;
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var spread = plus - minus;
		var stopped = _stopPrice != 0m && (Position > 0 ? candle.ClosePrice <= _stopPrice : candle.ClosePrice >= _stopPrice);
		if (Position > 0 && (stopped || adx < AdxExitThreshold || spread <= DiDifferenceThreshold))
		{
			SellMarket(Position);
			_stopPrice = 0m;
			return;
		}
		if (Position < 0 && (stopped || adx < AdxExitThreshold || spread >= -DiDifferenceThreshold))
		{
			BuyMarket(-Position);
			_stopPrice = 0m;
			return;
		}

		var atr = atrValue.GetValue<decimal>();
		if (Position != 0 || !rising || adx <= AdxThreshold || atr <= 0)
			return;
		if (spread > DiDifferenceThreshold)
		{
			BuyMarket();
			_stopPrice = candle.ClosePrice - AtrMultiplier * atr;
		}
		else if (spread < -DiDifferenceThreshold)
		{
			SellMarket();
			_stopPrice = candle.ClosePrice + AtrMultiplier * atr;
		}
	}
}
