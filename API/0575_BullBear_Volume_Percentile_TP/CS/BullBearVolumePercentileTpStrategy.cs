using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bull/Bear Power strategy with volume and percentile adjusted take profit levels.
/// </summary>
public class BullBearVolumePercentileTpStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _zLength;
	private readonly StrategyParam<decimal> _zThreshold;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMult1;
	private readonly StrategyParam<decimal> _atrMult2;
	private readonly StrategyParam<decimal> _atrMult3;
	private readonly StrategyParam<decimal> _tp1Percent;
	private readonly StrategyParam<decimal> _tp2Percent;
	private readonly StrategyParam<decimal> _tp3Percent;
	private readonly StrategyParam<int> _volPeriod;
	private readonly StrategyParam<decimal> _volHighMult;
	private readonly StrategyParam<decimal> _volMedMult;
	private readonly StrategyParam<decimal> _volLowMult;
	private readonly StrategyParam<decimal> _volHighFactor;
	private readonly StrategyParam<decimal> _volMedFactor;
	private readonly StrategyParam<decimal> _volLowFactor;
	private readonly StrategyParam<int> _percPeriod;
	private readonly StrategyParam<decimal> _percHigh;
	private readonly StrategyParam<decimal> _percMed;
	private readonly StrategyParam<decimal> _percLow;
	private readonly StrategyParam<decimal> _percHighFactor;
	private readonly StrategyParam<decimal> _percMedFactor;
	private readonly StrategyParam<decimal> _percLowFactor;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;
	private AverageTrueRange _atr;
	private SimpleMovingAverage _volSma;
	private SimpleMovingAverage _bbpMean;
	private StandardDeviation _bbpStd;

	private readonly Queue<decimal> _priceValues = [];
	private readonly Queue<decimal> _volumeValues = [];

	private decimal _entryPrice;
	private decimal _entryVolume;
	private decimal _tp1Price;
	private decimal _tp2Price;
	private decimal _tp3Price;
	private decimal _tp1Qty;
	private decimal _tp2Qty;
	private decimal _tp3Qty;
	private bool _tp1Done;
	private bool _tp2Done;
	private bool _tp3Done;
	private decimal _prevZScore;

	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int ZLength { get => _zLength.Value; set => _zLength.Value = value; }
	public decimal ZThreshold { get => _zThreshold.Value; set => _zThreshold.Value = value; }
	public bool UseTakeProfit { get => _useTakeProfit.Value; set => _useTakeProfit.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMult1 { get => _atrMult1.Value; set => _atrMult1.Value = value; }
	public decimal AtrMult2 { get => _atrMult2.Value; set => _atrMult2.Value = value; }
	public decimal AtrMult3 { get => _atrMult3.Value; set => _atrMult3.Value = value; }
	public decimal Tp1Percent { get => _tp1Percent.Value; set => _tp1Percent.Value = value; }
	public decimal Tp2Percent { get => _tp2Percent.Value; set => _tp2Percent.Value = value; }
	public decimal Tp3Percent { get => _tp3Percent.Value; set => _tp3Percent.Value = value; }
	public int VolPeriod { get => _volPeriod.Value; set => _volPeriod.Value = value; }
	public decimal VolHighMult { get => _volHighMult.Value; set => _volHighMult.Value = value; }
	public decimal VolMedMult { get => _volMedMult.Value; set => _volMedMult.Value = value; }
	public decimal VolLowMult { get => _volLowMult.Value; set => _volLowMult.Value = value; }
	public decimal VolHighFactor { get => _volHighFactor.Value; set => _volHighFactor.Value = value; }
	public decimal VolMedFactor { get => _volMedFactor.Value; set => _volMedFactor.Value = value; }
	public decimal VolLowFactor { get => _volLowFactor.Value; set => _volLowFactor.Value = value; }
	public int PercPeriod { get => _percPeriod.Value; set => _percPeriod.Value = value; }
	public decimal PercHigh { get => _percHigh.Value; set => _percHigh.Value = value; }
	public decimal PercMed { get => _percMed.Value; set => _percMed.Value = value; }
	public decimal PercLow { get => _percLow.Value; set => _percLow.Value = value; }
	public decimal PercHighFactor { get => _percHighFactor.Value; set => _percHighFactor.Value = value; }
	public decimal PercMedFactor { get => _percMedFactor.Value; set => _percMedFactor.Value = value; }
	public decimal PercLowFactor { get => _percLowFactor.Value; set => _percLowFactor.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BullBearVolumePercentileTpStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 21)
		.SetGreaterThanZero()
		.SetDisplay("EMA Length", "EMA length for Bull/Bear Power", "Parameters");

		_zLength = Param(nameof(ZLength), 252)
		.SetGreaterThanZero()
		.SetDisplay("ZScore Length", "Lookback for Z-Score", "Parameters");

		_zThreshold = Param(nameof(ZThreshold), 1.618m)
		.SetGreaterThanZero()
		.SetDisplay("ZScore Threshold", "Entry threshold", "Parameters");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
		.SetDisplay("Use Take Profit", "Enable take profit", "Take Profit");

		_atrPeriod = Param(nameof(AtrPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR calculation period", "Take Profit");

		_atrMult1 = Param(nameof(AtrMult1), 1.618m)
		.SetDisplay("TP1 ATR Multiplier", "First take profit multiplier", "Take Profit");

		_atrMult2 = Param(nameof(AtrMult2), 2.382m)
		.SetDisplay("TP2 ATR Multiplier", "Second take profit multiplier", "Take Profit");

		_atrMult3 = Param(nameof(AtrMult3), 3.618m)
		.SetDisplay("TP3 ATR Multiplier", "Third take profit multiplier", "Take Profit");

		_tp1Percent = Param(nameof(Tp1Percent), 13m)
		.SetDisplay("TP1 Position %", "Portion to close at TP1", "Take Profit");

		_tp2Percent = Param(nameof(Tp2Percent), 13m)
		.SetDisplay("TP2 Position %", "Portion to close at TP2", "Take Profit");

		_tp3Percent = Param(nameof(Tp3Percent), 13m)
		.SetDisplay("TP3 Position %", "Portion to close at TP3", "Take Profit");

		_volPeriod = Param(nameof(VolPeriod), 100)
		.SetGreaterThanZero()
		.SetDisplay("Volume MA Period", "Volume moving average period", "Volume");

		_volHighMult = Param(nameof(VolHighMult), 2m)
		.SetDisplay("High Volume Multiplier", "High volume threshold", "Volume");

		_volMedMult = Param(nameof(VolMedMult), 1.5m)
		.SetDisplay("Medium Volume Multiplier", "Medium volume threshold", "Volume");

		_volLowMult = Param(nameof(VolLowMult), 1m)
		.SetDisplay("Low Volume Multiplier", "Low volume threshold", "Volume");

		_volHighFactor = Param(nameof(VolHighFactor), 1.5m)
		.SetDisplay("High Volume Factor", "Take profit factor for high volume", "Volume");

		_volMedFactor = Param(nameof(VolMedFactor), 1.3m)
		.SetDisplay("Medium Volume Factor", "Take profit factor for medium volume", "Volume");

		_volLowFactor = Param(nameof(VolLowFactor), 1m)
		.SetDisplay("Low Volume Factor", "Take profit factor for low volume", "Volume");

		_percPeriod = Param(nameof(PercPeriod), 100)
		.SetGreaterThanZero()
		.SetDisplay("Percentile Period", "History length for percentiles", "Percentile");

		_percHigh = Param(nameof(PercHigh), 90m)
		.SetDisplay("High Percentile", "High percentile threshold", "Percentile");

		_percMed = Param(nameof(PercMed), 80m)
		.SetDisplay("Medium Percentile", "Medium percentile threshold", "Percentile");

		_percLow = Param(nameof(PercLow), 70m)
		.SetDisplay("Low Percentile", "Low percentile threshold", "Percentile");

		_percHighFactor = Param(nameof(PercHighFactor), 1.5m)
		.SetDisplay("High Percentile Factor", "Factor for high percentile", "Percentile");

		_percMedFactor = Param(nameof(PercMedFactor), 1.3m)
		.SetDisplay("Medium Percentile Factor", "Factor for medium percentile", "Percentile");

		_percLowFactor = Param(nameof(PercLowFactor), 1m)
		.SetDisplay("Low Percentile Factor", "Factor for low percentile", "Percentile");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Time frame", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_priceValues.Clear();
		_volumeValues.Clear();
		ResetTp();
		_prevZScore = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_atr = new AverageTrueRange { Length = AtrPeriod };
		_volSma = new SimpleMovingAverage { Length = VolPeriod };
		_bbpMean = new SimpleMovingAverage { Length = ZLength };
		_bbpStd = new StandardDeviation { Length = ZLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var emaValue = _ema.Process(candle.ClosePrice, candle.ServerTime, true).ToDecimal();
		var bullPower = candle.HighPrice - emaValue;
		var bearPower = candle.LowPrice - emaValue;
		var bbp = bullPower + bearPower;

		var bbpMean = _bbpMean.Process(bbp, candle.ServerTime, true).ToDecimal();
		var bbpStd = _bbpStd.Process(bbp, candle.ServerTime, true).ToDecimal();
		if (!_bbpStd.IsFormed || bbpStd == 0)
		{
			_prevZScore = 0m;
			return;
		}

		var zscore = (bbp - bbpMean) / bbpStd;

		var volume = candle.TotalVolume ?? 0m;
		var volMa = _volSma.Process(volume, candle.ServerTime, true).ToDecimal();
		var volMult = volMa == 0 ? 0 : volume / volMa;

		_priceValues.Enqueue(candle.ClosePrice);
		if (_priceValues.Count > PercPeriod)
		_priceValues.Dequeue();

		_volumeValues.Enqueue(volume);
		if (_volumeValues.Count > PercPeriod)
		_volumeValues.Dequeue();

		var pricePerc = GetPercentile(_priceValues, candle.ClosePrice);
		var volPerc = GetPercentile(_volumeValues, volume);

		var volScore = volMult > VolHighMult ? VolHighFactor : volMult > VolMedMult ? VolMedFactor : volMult > VolLowMult ? VolLowFactor : 0.8m;
		var priceScore = pricePerc > PercHigh ? PercHighFactor : pricePerc > PercMed ? PercMedFactor : pricePerc > PercLow ? PercLowFactor : 0.8m;
		var tpFactor = (volScore + priceScore) / 2m;

		var atrValue = _atr.Process(candle).ToDecimal();

		if (Position == 0)
		{
			if (zscore < -ZThreshold && _prevZScore >= -ZThreshold)
			{
				var qty = Volume + Math.Abs(Position);
				BuyMarket(qty);
				_entryPrice = candle.ClosePrice;
				_entryVolume = qty;
				PrepareTpLevels(true, atrValue, tpFactor);
			}
			else if (zscore > ZThreshold && _prevZScore <= ZThreshold)
			{
				var qty = Volume + Math.Abs(Position);
				SellMarket(qty);
				_entryPrice = candle.ClosePrice;
				_entryVolume = qty;
				PrepareTpLevels(false, atrValue, tpFactor);
			}
		}
		else
		{
			if (Position > 0 && _prevZScore > 0 && zscore <= 0)
			{
				SellMarket(Position);
				ResetTp();
			}
			else if (Position < 0 && _prevZScore < 0 && zscore >= 0)
			{
				BuyMarket(Math.Abs(Position));
				ResetTp();
			}
			else if (UseTakeProfit)
			{
				ManageTakeProfit(candle);
			}
		}

		_prevZScore = zscore;
	}

	private void PrepareTpLevels(bool isLong, decimal atr, decimal factor)
	{
		if (!UseTakeProfit || atr == 0)
		return;

		if (isLong)
		{
			_tp1Price = _entryPrice + atr * AtrMult1 * factor;
			_tp2Price = _entryPrice + atr * AtrMult2 * factor;
			_tp3Price = _entryPrice + atr * AtrMult3 * factor;
		}
		else
		{
			_tp1Price = _entryPrice - atr * AtrMult1 * factor;
			_tp2Price = _entryPrice - atr * AtrMult2 * factor;
			_tp3Price = _entryPrice - atr * AtrMult3 * factor;
		}

		_tp1Qty = _entryVolume * Tp1Percent / 100m;
		_tp2Qty = _entryVolume * Tp2Percent / 100m;
		_tp3Qty = _entryVolume * Tp3Percent / 100m;
		_tp1Done = _tp2Done = _tp3Done = false;
	}

	private void ManageTakeProfit(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (!_tp1Done && candle.HighPrice >= _tp1Price)
			{
				SellMarket(Math.Min(_tp1Qty, Position));
				_tp1Done = true;
			}

			if (!_tp2Done && candle.HighPrice >= _tp2Price)
			{
				SellMarket(Math.Min(_tp2Qty, Position));
				_tp2Done = true;
			}

			if (!_tp3Done && candle.HighPrice >= _tp3Price)
			{
				SellMarket(Math.Abs(Position));
				ResetTp();
			}
		}
		else if (Position < 0)
		{
			var absPos = Math.Abs(Position);

			if (!_tp1Done && candle.LowPrice <= _tp1Price)
			{
				BuyMarket(Math.Min(_tp1Qty, absPos));
				_tp1Done = true;
			}

			if (!_tp2Done && candle.LowPrice <= _tp2Price)
			{
				BuyMarket(Math.Min(_tp2Qty, Math.Abs(Position)));
				_tp2Done = true;
			}

			if (!_tp3Done && candle.LowPrice <= _tp3Price)
			{
				BuyMarket(absPos);
				ResetTp();
			}
		}
	}

	private void ResetTp()
	{
		_tp1Price = _tp2Price = _tp3Price = 0m;
		_tp1Qty = _tp2Qty = _tp3Qty = 0m;
		_tp1Done = _tp2Done = _tp3Done = false;
	}

	private static decimal GetPercentile(IEnumerable<decimal> values, decimal current)
	{
		var total = 0;
		var count = 0;
		foreach (var v in values)
		{
			total++;
			if (v <= current)
			count++;
		}
		if (total == 0)
		return 50m;
		return (decimal)count / total * 100m;
	}
}
