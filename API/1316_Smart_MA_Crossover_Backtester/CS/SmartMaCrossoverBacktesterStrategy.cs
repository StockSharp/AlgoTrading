using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy with ATR-based risk management.
/// Uses a 200-period SMA as trend filter and ATR for stop and take-profit levels.
/// </summary>
public class SmartMaCrossoverBacktesterStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal? _stop;
	private decimal? _target;
	
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int TrendLength { get => _trendLength.Value; set => _trendLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	public SmartMaCrossoverBacktesterStrategy()
	{
		_fastLength = Param(nameof(FastLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA Length", "Period of the fast moving average", "MA Settings")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);
		
		_slowLength = Param(nameof(SlowLength), 21)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA Length", "Period of the slow moving average", "MA Settings")
		.SetCanOptimize(true)
		.SetOptimize(20, 60, 1);
		
		_trendLength = Param(nameof(TrendLength), 200)
		.SetGreaterThanZero()
		.SetDisplay("Trend MA Length", "Period of the trend filter", "MA Settings");
		
		_atrLength = Param(nameof(AtrLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "ATR period for stop and target", "Risk Management");
		
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "Multiplier for ATR based stop", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(1m, 4m, 0.5m);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}
	
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0m;
		_prevSlow = 0m;
		_stop = null;
		_target = null;
	}
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var fastMa = new SMA { Length = FastLength };
		var slowMa = new SMA { Length = SlowLength };
		var trendMa = new SMA { Length = TrendLength };
		var atr = new ATR { Length = AtrLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(fastMa, slowMa, trendMa, atr, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawIndicator(area, trendMa);
			DrawOwnTrades(area);
		}
	}
	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal, decimal trendVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		
		var crossUp = _prevFast <= _prevSlow && fastVal > slowVal && candle.ClosePrice > trendVal;
		var crossDown = _prevFast >= _prevSlow && fastVal < slowVal && candle.ClosePrice < trendVal;
		
		if (Position == 0)
		{
			if (crossUp)
			{
				BuyMarket();
				var dist = atrVal * AtrMultiplier;
				_stop = candle.ClosePrice - dist;
				_target = candle.ClosePrice + dist * 2m;
			}
			else if (crossDown)
			{
				SellMarket();
				var dist = atrVal * AtrMultiplier;
				_stop = candle.ClosePrice + dist;
				_target = candle.ClosePrice - dist * 2m;
			}
		}
		else if (Position > 0)
		{
			if (_stop.HasValue && candle.LowPrice <= _stop)
			{
				SellMarket(Math.Abs(Position));
				_stop = null;
				_target = null;
			}
			else if (_target.HasValue && candle.HighPrice >= _target)
			{
				SellMarket(Math.Abs(Position));
				_stop = null;
				_target = null;
			}
		}
		else if (Position < 0)
		{
			if (_stop.HasValue && candle.HighPrice >= _stop)
			{
				BuyMarket(Math.Abs(Position));
				_stop = null;
				_target = null;
			}
			else if (_target.HasValue && candle.LowPrice <= _target)
			{
				BuyMarket(Math.Abs(Position));
				_stop = null;
				_target = null;
			}
		}
		
		_prevFast = fastVal;
		_prevSlow = slowVal;
	}
}
