using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Liquid Pulse strategy.
/// Detects high volume spikes confirmed by MACD and ADX.
/// ATR defines stop and take profit; limits trades per day.
/// </summary>
public class LiquidPulseStrategy : Strategy
{
	public enum VolumeSensitivityLevel { Low, Medium, High }
	public enum MacdSpeedOption { Fast, Medium, Slow }
	
	private readonly StrategyParam<VolumeSensitivityLevel> _volumeSensitivity;
	private readonly StrategyParam<MacdSpeedOption> _macdSpeed;
	private readonly StrategyParam<int> _dailyTradeLimit;
	private readonly StrategyParam<int> _volume;
	private readonly StrategyParam<int> _adxTrendThreshold;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;
	
	private MovingAverageConvergenceDivergenceSignal _macd;
	private AverageDirectionalIndex _adx;
	private AverageTrueRange _atr;
	private SimpleMovingAverage _volSma;
	
	private decimal _prevMacd, _prevSignal, _entryPrice, _stop, _tp;
	private DateTime _day;
	private int _trades;
	
	public VolumeSensitivityLevel VolumeSensitivity { get => _volumeSensitivity.Value; set => _volumeSensitivity.Value = value; }
	public MacdSpeedOption MacdSpeed { get => _macdSpeed.Value; set => _macdSpeed.Value = value; }
	public int DailyTradeLimit { get => _dailyTradeLimit.Value; set => _dailyTradeLimit.Value = value; }
	public int Volume { get => _volume.Value; set => _volume.Value = value; }
	public int AdxTrendThreshold { get => _adxTrendThreshold.Value; set => _adxTrendThreshold.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	public LiquidPulseStrategy()
	{
		_volumeSensitivity = Param(nameof(VolumeSensitivity), VolumeSensitivityLevel.Medium)
		.SetDisplay("Volume Sensitivity", "Volume sensitivity", "General");
		_macdSpeed = Param(nameof(MacdSpeed), MacdSpeedOption.Medium)
		.SetDisplay("MACD Speed", "MACD speed", "General");
		_dailyTradeLimit = Param(nameof(DailyTradeLimit), 20)
		.SetDisplay("Daily Trade Limit", "Max trades per day", "Risk");
		_volume = Param(nameof(Volume), 1)
		.SetDisplay("Volume", "Order volume", "General")
		.SetGreaterThanZero();
		_adxTrendThreshold = Param(nameof(AdxTrendThreshold), 41)
		.SetDisplay("ADX Trend Threshold", "Trend threshold", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 9)
		.SetDisplay("ATR Period", "ATR period", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe", "General");
	}
	
	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMacd = _prevSignal = 0m;
		_entryPrice = _stop = _tp = 0m;
		_day = default;
		_trades = 0;
	}
	
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var (lookback, threshold) = VolumeSensitivity switch
		{
			VolumeSensitivityLevel.Low => (30, 1.5m),
			VolumeSensitivityLevel.Medium => (20, 1.8m),
			_ => (11, 2m)
		};
		_volSma = new SimpleMovingAverage { Length = lookback };
		
		var (fast, slow, signal) = MacdSpeed switch
		{
			MacdSpeedOption.Fast => (2, 7, 5),
			MacdSpeedOption.Medium => (5, 13, 9),
			_ => (12, 26, 9)
		};
		_macd = new()
		{
			Macd = { ShortMa = { Length = fast }, LongMa = { Length = slow } },
			SignalMa = { Length = signal }
		};
		_adx = new() { Length = 14 };
		_atr = new() { Length = AtrPeriod };
		
		var sub = SubscribeCandles(CandleType);
		sub.BindEx(_macd, _adx, _atr, ProcessCandle).Start();
		
		StartProtection();
		
		_volParams = () => (lookback, threshold);
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawOwnTrades(area);
		}
	}
	
	private Func<(int, decimal)> _volParams;
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdVal, IIndicatorValue adxVal, IIndicatorValue atrVal)
	{
		if (candle.State != CandleStates.Finished || !macdVal.IsFinal || !adxVal.IsFinal || !atrVal.IsFinal)
		return;
		
		var day = candle.OpenTime.Date;
		if (day != _day)
		{
			_day = day;
			_trades = 0;
		}
		
		var (lookback, threshold) = _volParams();
		_volSma.Length = lookback;
		var avgVol = _volSma.Process(candle.TotalVolume).ToDecimal();
		var highVol = avgVol != 0m && candle.TotalVolume >= threshold * avgVol;
		
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdVal;
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
		return;
		
		var adxTyped = (AverageDirectionalIndexValue)adxVal;
		if (adxTyped.MovingAverage is not decimal adx ||
		adxTyped.Dx.Plus is not decimal plusDi ||
		adxTyped.Dx.Minus is not decimal minusDi)
		return;
		
		var atr = atrVal.ToDecimal();
		
		var bull = _prevMacd <= _prevSignal && macd > signal && plusDi > minusDi && adx >= AdxTrendThreshold;
		var bear = _prevMacd >= _prevSignal && macd < signal && minusDi > plusDi && adx >= AdxTrendThreshold;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevMacd = macd;
			_prevSignal = signal;
			return;
		}
		
		if (highVol && _trades < DailyTradeLimit)
		{
			if (bull && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_entryPrice = candle.ClosePrice;
				_stop = _entryPrice - atr * 1.5m;
				_tp = _entryPrice + atr * 2m;
				_trades++;
			}
			else if (bear && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_entryPrice = candle.ClosePrice;
				_stop = _entryPrice + atr * 1.5m;
				_tp = _entryPrice - atr * 2m;
				_trades++;
			}
		}
		
		if (Position > 0 && (candle.LowPrice <= _stop || candle.HighPrice >= _tp))
		{
			SellMarket(Math.Abs(Position));
			_entryPrice = _stop = _tp = 0m;
		}
		else if (Position < 0 && (candle.HighPrice >= _stop || candle.LowPrice <= _tp))
		{
			BuyMarket(Math.Abs(Position));
			_entryPrice = _stop = _tp = 0m;
		}
		
		_prevMacd = macd;
		_prevSignal = signal;
	}
}
