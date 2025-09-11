namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// EMA crossover strategy with volume filter.
/// Includes stacked take profits and trailing stop.
/// </summary>
public class EmaCrossoverVolumeStackedTpTrailingSlStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _tp1Multiplier;
	private readonly StrategyParam<decimal> _tp2Multiplier;
	private readonly StrategyParam<decimal> _trailOffsetMultiplier;
	private readonly StrategyParam<decimal> _trailTriggerMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	
	private bool _wasFastAboveSlow;
	private decimal _entryPrice;
	private bool _tp1Hit;
	private bool _tp2Hit;
	private bool _trailActive;
	private decimal? _trailStop;
	
	private EMA _fastEma;
	private EMA _slowEma;
	private ATR _atr;
	private SMA _volumeSma;
	
	public EmaCrossoverVolumeStackedTpTrailingSlStrategy()
	{
		_fastLength = Param(nameof(FastLength), 21)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Fast EMA length", "General");
		
		_slowLength = Param(nameof(SlowLength), 55)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Slow EMA length", "General");
		
		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.2m)
		.SetGreaterThanZero()
		.SetDisplay("Volume Multiplier", "Volume threshold multiplier", "General");
		
		_atrLength = Param(nameof(AtrLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "ATR period", "General");
		
		_tp1Multiplier = Param(nameof(Tp1Multiplier), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("TP1 Multiplier", "First take profit ATR multiplier", "Exit");
		
		_tp2Multiplier = Param(nameof(Tp2Multiplier), 2.5m)
		.SetGreaterThanZero()
		.SetDisplay("TP2 Multiplier", "Second take profit ATR multiplier", "Exit");
		
		_trailOffsetMultiplier = Param(nameof(TrailOffsetMultiplier), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Trail Offset Mult", "Trailing stop offset ATR multiplier", "Exit");
		
		_trailTriggerMultiplier = Param(nameof(TrailTriggerMultiplier), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Trail Trigger Mult", "Activation threshold ATR multiplier", "Exit");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Candle time frame", "General");
	}
	
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal VolumeMultiplier { get => _volumeMultiplier.Value; set => _volumeMultiplier.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal Tp1Multiplier { get => _tp1Multiplier.Value; set => _tp1Multiplier.Value = value; }
	public decimal Tp2Multiplier { get => _tp2Multiplier.Value; set => _tp2Multiplier.Value = value; }
	public decimal TrailOffsetMultiplier { get => _trailOffsetMultiplier.Value; set => _trailOffsetMultiplier.Value = value; }
	public decimal TrailTriggerMultiplier { get => _trailTriggerMultiplier.Value; set => _trailTriggerMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		
		_wasFastAboveSlow = false;
		_entryPrice = 0m;
		_tp1Hit = false;
		_tp2Hit = false;
		_trailActive = false;
		_trailStop = null;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_fastEma = new EMA { Length = FastLength };
		_slowEma = new EMA { Length = SlowLength };
		_atr = new ATR { Length = AtrLength };
		_volumeSma = new SMA { Length = 20 };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_fastEma, _slowEma, _atr, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var avgVolume = _volumeSma.Process(candle.TotalVolume, candle.OpenTime, true).ToDecimal();
		var volumeOk = candle.TotalVolume > avgVolume * VolumeMultiplier;
		
		var fastAboveSlow = fast > slow;
		var crossUp = fastAboveSlow && !_wasFastAboveSlow;
		var crossDown = !fastAboveSlow && _wasFastAboveSlow;
		
		if (crossUp && volumeOk && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			_entryPrice = candle.ClosePrice;
			_tp1Hit = false;
			_tp2Hit = false;
			_trailActive = false;
			_trailStop = null;
			BuyMarket(volume);
		}
		else if (crossDown && volumeOk && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			_entryPrice = candle.ClosePrice;
			_tp1Hit = false;
			_tp2Hit = false;
			_trailActive = false;
			_trailStop = null;
			SellMarket(volume);
		}
		else
		{
			if (Position > 0)
			{
				var tp1Price = _entryPrice + atr * Tp1Multiplier;
				var tp2Price = _entryPrice + atr * Tp2Multiplier;
				
				if (!_tp1Hit && candle.HighPrice >= tp1Price)
				{
					SellMarket(Position / 3m);
					_tp1Hit = true;
				}
				
				if (!_tp2Hit && candle.HighPrice >= tp2Price)
				{
					SellMarket(Position / 2m);
					_tp2Hit = true;
				}
				
				if (!_trailActive && candle.ClosePrice - _entryPrice >= atr * TrailTriggerMultiplier)
				{
					_trailActive = true;
					_trailStop = candle.ClosePrice - atr * TrailOffsetMultiplier;
				}
				
				if (_trailActive && _trailStop != null)
				{
					var newStop = candle.ClosePrice - atr * TrailOffsetMultiplier;
					if (newStop > _trailStop)
					_trailStop = newStop;
					
					if (candle.LowPrice <= _trailStop)
					{
						SellMarket(Position);
						_trailActive = false;
						_trailStop = null;
					}
				}
			}
			else if (Position < 0)
			{
				var tp1Price = _entryPrice - atr * Tp1Multiplier;
				var tp2Price = _entryPrice - atr * Tp2Multiplier;
				
				if (!_tp1Hit && candle.LowPrice <= tp1Price)
				{
					BuyMarket(Math.Abs(Position) / 3m);
					_tp1Hit = true;
				}
				
				if (!_tp2Hit && candle.LowPrice <= tp2Price)
				{
					BuyMarket(Math.Abs(Position) / 2m);
					_tp2Hit = true;
				}
				
				if (!_trailActive && _entryPrice - candle.ClosePrice >= atr * TrailTriggerMultiplier)
				{
					_trailActive = true;
					_trailStop = candle.ClosePrice + atr * TrailOffsetMultiplier;
				}
				
				if (_trailActive && _trailStop != null)
				{
					var newStop = candle.ClosePrice + atr * TrailOffsetMultiplier;
					if (newStop < _trailStop)
					_trailStop = newStop;
					
					if (candle.HighPrice >= _trailStop)
					{
						BuyMarket(Math.Abs(Position));
						_trailActive = false;
						_trailStop = null;
					}
				}
			}
		}
		
		if (Position == 0)
		{
			_tp1Hit = false;
			_tp2Hit = false;
			_trailActive = false;
			_trailStop = null;
		}
		
		_wasFastAboveSlow = fastAboveSlow;
	}
}
