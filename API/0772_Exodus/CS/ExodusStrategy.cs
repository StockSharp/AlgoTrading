using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class ExodusStrategy : Strategy
{
	private readonly StrategyParam<int> _vwmoMomentum;
	private readonly StrategyParam<int> _vwmoVolume;
	private readonly StrategyParam<int> _vwmoSmooth;
	private readonly StrategyParam<decimal> _vwmoThreshold;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _tpMultiplier;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private readonly AverageTrueRange _atr;
	private readonly AverageDirectionalIndex _adx;
	private readonly SimpleMovingAverage _volumeSma;
	private readonly SimpleMovingAverage _vwmoSma;

	private decimal _prevClose;

	public int VwmoMomentum { get => _vwmoMomentum.Value; set => _vwmoMomentum.Value = value; }
	public int VwmoVolume { get => _vwmoVolume.Value; set => _vwmoVolume.Value = value; }
	public int VwmoSmooth { get => _vwmoSmooth.Value; set => _vwmoSmooth.Value = value; }
	public decimal VwmoThreshold { get => _vwmoThreshold.Value; set => _vwmoThreshold.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal TpMultiplier { get => _tpMultiplier.Value; set => _tpMultiplier.Value = value; }
	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	public int Volume { get => _volume.Value; set => _volume.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ExodusStrategy()
	{
		_vwmoMomentum = Param(nameof(VwmoMomentum), 21)
			.SetGreaterThanZero()
			.SetDisplay("VWMO Momentum", "Momentum lookback", "VWMO");

		_vwmoVolume = Param(nameof(VwmoVolume), 30)
			.SetGreaterThanZero()
			.SetDisplay("VWMO Volume", "Volume lookback", "VWMO");

		_vwmoSmooth = Param(nameof(VwmoSmooth), 9)
			.SetGreaterThanZero()
			.SetDisplay("VWMO Smooth", "Smoothing", "VWMO");

		_vwmoThreshold = Param(nameof(VwmoThreshold), 10m)
			.SetDisplay("VWMO Threshold", "Signal threshold", "VWMO");

		_atrLength = Param(nameof(AtrLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2.1m)
			.SetDisplay("ATR Mult", "Stop ATR multiplier", "Risk");

		_tpMultiplier = Param(nameof(TpMultiplier), 4.1m)
			.SetDisplay("TP Mult", "Take profit ATR multiplier", "Risk");

		_adxLength = Param(nameof(AdxLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("ADX Length", "ADX period", "Filters");

		_adxThreshold = Param(nameof(AdxThreshold), 27m)
			.SetDisplay("ADX Threshold", "Minimum ADX", "Filters");

		_volume = Param(nameof(Volume), 1)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_atr = new AverageTrueRange { Length = AtrLength };
		_adx = new AverageDirectionalIndex { Length = AdxLength };
		_volumeSma = new SimpleMovingAverage { Length = VwmoVolume };
		_vwmoSma = new SimpleMovingAverage { Length = VwmoSmooth };
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr.Length = AtrLength;
		_adx.Length = AdxLength;
		_volumeSma.Length = VwmoVolume;
		_vwmoSma.Length = VwmoSmooth;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_atr, _adx, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var avgVol = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();
		var priceMom = _prevClose == 0m ? 0m : candle.ClosePrice - _prevClose;
		var volWeight = avgVol == 0m ? 0m : candle.TotalVolume / avgVol;
		var vwmo = _vwmoSma.Process(priceMom * volWeight, candle.ServerTime, true).ToDecimal();

		_prevClose = candle.ClosePrice;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position <= 0 && vwmo > VwmoThreshold && adxValue > AdxThreshold)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (Position >= 0 && vwmo < -VwmoThreshold && adxValue > AdxThreshold)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else
		{
			if (Position > 0 && vwmo < 0)
				SellMarket(Math.Abs(Position));
			else if (Position < 0 && vwmo > 0)
				BuyMarket(Math.Abs(Position));
		}
	}
}
