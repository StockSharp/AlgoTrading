using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA and VWAP based scalping strategy with optional volume and price action filters and ATR exits.
/// </summary>
public class UltimateScalpingV2Strategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<bool> _allowLongs;
	private readonly StrategyParam<bool> _allowShorts;
	private readonly StrategyParam<bool> _usePriceAction;
	private readonly StrategyParam<bool> _useVolume;
	private readonly StrategyParam<int> _volumeMaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _slAtrMult;
	private readonly StrategyParam<decimal> _tpAtrMult;
	private readonly StrategyParam<bool> _useExitOnOpposite;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevEmaFast;
	private decimal _prevEmaSlow;
	private decimal _prevOpen;
	private decimal _prevClose;
	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortStop;
	private decimal _shortTake;
	private SimpleMovingAverage _volumeMa;
	
	/// <summary>
	/// Fast EMA period.
	/// </summary>
public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }

/// <summary>
/// Slow EMA period.
/// </summary>
public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }

/// <summary>
/// Allow long trades.
/// </summary>
public bool AllowLongs { get => _allowLongs.Value; set => _allowLongs.Value = value; }

/// <summary>
/// Allow short trades.
/// </summary>
public bool AllowShorts { get => _allowShorts.Value; set => _allowShorts.Value = value; }

/// <summary>
/// Use engulfing candle confirmation.
/// </summary>
public bool UsePriceAction { get => _usePriceAction.Value; set => _usePriceAction.Value = value; }

/// <summary>
/// Use volume spike confirmation.
/// </summary>
public bool UseVolume { get => _useVolume.Value; set => _useVolume.Value = value; }

/// <summary>
/// Volume moving average length.
/// </summary>
public int VolumeMaLength { get => _volumeMaLength.Value; set => _volumeMaLength.Value = value; }

/// <summary>
/// ATR period.
/// </summary>
public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

/// <summary>
/// Stop loss ATR multiplier.
/// </summary>
public decimal SlAtrMult { get => _slAtrMult.Value; set => _slAtrMult.Value = value; }

/// <summary>
/// Take profit ATR multiplier.
/// </summary>
public decimal TpAtrMult { get => _tpAtrMult.Value; set => _tpAtrMult.Value = value; }

/// <summary>
/// Exit on opposite signal.
/// </summary>
public bool UseExitOnOpposite { get => _useExitOnOpposite.Value; set => _useExitOnOpposite.Value = value; }

/// <summary>
/// Candle type.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public UltimateScalpingV2Strategy()
{
	_fastEmaLength = Param(nameof(FastEmaLength), 9).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA length", "Indicators").SetCanOptimize(true).SetOptimize(5, 15, 2);
	_slowEmaLength = Param(nameof(SlowEmaLength), 21).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA length", "Indicators").SetCanOptimize(true).SetOptimize(15, 30, 3);
	_allowLongs = Param(nameof(AllowLongs), true).SetDisplay("Allow Longs", "Enable long trades", "Trading");
	_allowShorts = Param(nameof(AllowShorts), true).SetDisplay("Allow Shorts", "Enable short trades", "Trading");
	_usePriceAction = Param(nameof(UsePriceAction), false).SetDisplay("Use Price Action", "Require engulfing pattern", "Filters");
	_useVolume = Param(nameof(UseVolume), false).SetDisplay("Use Volume", "Require volume spike", "Filters");
	_volumeMaLength = Param(nameof(VolumeMaLength), 20).SetGreaterThanZero().SetDisplay("Volume MA", "Volume MA length", "Filters");
	_atrLength = Param(nameof(AtrLength), 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "Risk");
	_slAtrMult = Param(nameof(SlAtrMult), 1.5m).SetGreaterThanZero().SetDisplay("SL ATR Mult", "Stop ATR multiplier", "Risk");
	_tpAtrMult = Param(nameof(TpAtrMult), 2m).SetGreaterThanZero().SetDisplay("TP ATR Mult", "Take profit ATR multiplier", "Risk");
	_useExitOnOpposite = Param(nameof(UseExitOnOpposite), true).SetDisplay("Exit On Opposite", "Close on opposite signal", "Risk");
	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General");
}

public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
	return [(Security, CandleType)];
}

protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);
	
	var emaFast = new ExponentialMovingAverage { Length = FastEmaLength };
	var emaSlow = new ExponentialMovingAverage { Length = SlowEmaLength };
	var vwap = new VolumeWeightedMovingAverage();
	var atr = new AverageTrueRange { Length = AtrLength };
	
	_volumeMa = new SimpleMovingAverage { Length = VolumeMaLength };
	
	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(emaFast, emaSlow, vwap, atr, ProcessCandle).Start();
	
	var area = CreateChartArea();
	if (area != null)
	{
		DrawCandles(area, subscription);
		DrawIndicator(area, emaFast);
		DrawIndicator(area, emaSlow);
		DrawIndicator(area, vwap);
		DrawOwnTrades(area);
	}
}

private void ProcessCandle(ICandleMessage candle, decimal emaFastVal, decimal emaSlowVal, decimal vwapVal, decimal atrVal)
{
	var volMa = _volumeMa.Process(candle.TotalVolume).ToDecimal();
	
	if (candle.State != CandleStates.Finished)
	return;
	
	bool longCross = _prevEmaFast <= _prevEmaSlow && emaFastVal > emaSlowVal;
	bool shortCross = _prevEmaFast >= _prevEmaSlow && emaFastVal < emaSlowVal;
	
	bool bullishEngulf = candle.ClosePrice > candle.OpenPrice && _prevClose < _prevOpen && candle.ClosePrice > _prevOpen && candle.OpenPrice < _prevClose;
	bool bearishEngulf = candle.ClosePrice < candle.OpenPrice && _prevClose > _prevOpen && candle.ClosePrice < _prevOpen && candle.OpenPrice > _prevClose;
	bool volSpike = candle.TotalVolume > volMa;
	
	bool longCond = longCross && candle.ClosePrice > vwapVal && (!UsePriceAction || bullishEngulf) && (!UseVolume || volSpike);
	bool shortCond = shortCross && candle.ClosePrice < vwapVal && (!UsePriceAction || bearishEngulf) && (!UseVolume || volSpike);
	
	if (longCond && AllowLongs && Position <= 0)
	{
		var volume = Volume + Math.Abs(Position);
		BuyMarket(volume);
		_longStop = candle.ClosePrice - atrVal * SlAtrMult;
		_longTake = candle.ClosePrice + atrVal * TpAtrMult;
	}
	else if (shortCond && AllowShorts && Position >= 0)
	{
		var volume = Volume + Math.Abs(Position);
		SellMarket(volume);
		_shortStop = candle.ClosePrice + atrVal * SlAtrMult;
		_shortTake = candle.ClosePrice - atrVal * TpAtrMult;
	}
	else
	{
		if (Position > 0)
		{
			if (candle.LowPrice <= _longStop || candle.HighPrice >= _longTake || (UseExitOnOpposite && shortCond))
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _shortStop || candle.LowPrice <= _shortTake || (UseExitOnOpposite && longCond))
			BuyMarket(Math.Abs(Position));
		}
	}
	
	_prevEmaFast = emaFastVal;
	_prevEmaSlow = emaSlowVal;
	_prevOpen = candle.OpenPrice;
	_prevClose = candle.ClosePrice;
}
}
