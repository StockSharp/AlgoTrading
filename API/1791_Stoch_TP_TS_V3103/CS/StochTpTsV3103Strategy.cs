using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic crossover strategy with trailing stop and take profit.
/// </summary>
public class StochTpTsV3103Strategy : Strategy
{
	private readonly StrategyParam<int> _triggerMinute;
	private readonly StrategyParam<decimal> _signalThreshold;
	private readonly StrategyParam<decimal> _startOffset;
	private readonly StrategyParam<decimal> _trailStopOffset;
	private readonly StrategyParam<decimal> _stopLossOffset;
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<DataType> _candleType;
	
	private StochasticOscillator _stochastic = null!;
	private int _lastTradeHour = -1;
	private decimal _entryPrice;
	private decimal _longStopPrice;
	private decimal _longTakeProfit;
	private decimal _shortStopPrice;
	private decimal _shortTakeProfit;
	
	public int TriggerMinute { get => _triggerMinute.Value; set => _triggerMinute.Value = value; }
	public decimal SignalThreshold { get => _signalThreshold.Value; set => _signalThreshold.Value = value; }
	public decimal StartOffset { get => _startOffset.Value; set => _startOffset.Value = value; }
	public decimal TrailStopOffset { get => _trailStopOffset.Value; set => _trailStopOffset.Value = value; }
	public decimal StopLossOffset { get => _stopLossOffset.Value; set => _stopLossOffset.Value = value; }
	public int StochLength { get => _stochLength.Value; set => _stochLength.Value = value; }
	public int KPeriod { get => _kPeriod.Value; set => _kPeriod.Value = value; }
	public int DPeriod { get => _dPeriod.Value; set => _dPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	public StochTpTsV3103Strategy()
	{
		_triggerMinute = Param(nameof(TriggerMinute), 56).SetDisplay("Trigger Minute", "Minute of hour to evaluate", "General");
		_signalThreshold = Param(nameof(SignalThreshold), 0m).SetDisplay("Signal Threshold", "Minimum difference between %K and %D", "Logic");
		_startOffset = Param(nameof(StartOffset), 95m).SetDisplay("Start Offset", "Profit in price units to activate trailing", "Risk");
		_trailStopOffset = Param(nameof(TrailStopOffset), 15m).SetDisplay("Trail Stop Offset", "Trailing stop distance in price units", "Risk");
		_stopLossOffset = Param(nameof(StopLossOffset), 830m).SetDisplay("Stop Loss Offset", "Initial stop loss in price units", "Risk");
		_stochLength = Param(nameof(StochLength), 5).SetDisplay("Stochastic Length", "Stochastic oscillator length", "Indicators");
		_kPeriod = Param(nameof(KPeriod), 3).SetDisplay("K Period", "Smoothing period for %K", "Indicators");
		_dPeriod = Param(nameof(DPeriod), 3).SetDisplay("D Period", "Smoothing period for %D", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", "Type of candles to use", "General");
	}
	
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];
	
	protected override void OnReseted()
	{
		base.OnReseted();
		_lastTradeHour = -1;
		_entryPrice = 0m;
		_longStopPrice = _longTakeProfit = 0m;
		_shortStopPrice = _shortTakeProfit = 0m;
	}
	
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();
		
		_stochastic = new StochasticOscillator
		{
			Length = StochLength,
			K = { Length = KPeriod },
			D = { Length = DPeriod }
		};
		
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_stochastic, ProcessCandle).Start();
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var stoch = (StochasticOscillatorValue)stochValue;
		var k = stoch.K;
		var d = stoch.D;
		
		var currentMinute = candle.OpenTime.Minute;
		var currentHour = candle.OpenTime.Hour;
		
		if (currentMinute == TriggerMinute && currentHour != _lastTradeHour)
		{
			_lastTradeHour = currentHour;
			
			if (k >= 20m && k <= 80m)
			{
				if (d - k >= SignalThreshold && Position <= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
					_entryPrice = candle.ClosePrice;
					_shortStopPrice = _entryPrice + StopLossOffset;
					_shortTakeProfit = _entryPrice - StartOffset * 3m;
					_longStopPrice = _longTakeProfit = 0m;
				}
				else if (k - d > SignalThreshold && Position >= 0)
				{
					BuyMarket(Volume + Math.Abs(Position));
					_entryPrice = candle.ClosePrice;
					_longStopPrice = _entryPrice - StopLossOffset;
					_longTakeProfit = _entryPrice + StartOffset * 3m;
					_shortStopPrice = _shortTakeProfit = 0m;
				}
			}
		}
		
		if (Position > 0)
		{
			if (_longTakeProfit > 0m && candle.HighPrice >= _longTakeProfit)
			{
				SellMarket(Position);
				_longTakeProfit = _longStopPrice = 0m;
				return;
			}
			
			if (candle.ClosePrice - _entryPrice >= StartOffset)
			{
				var newStop = candle.ClosePrice - TrailStopOffset;
				_longStopPrice = Math.Max(_longStopPrice, newStop);
				_longTakeProfit = 0m;
			}
			
			if (_longStopPrice > 0m && candle.LowPrice <= _longStopPrice)
			{
				SellMarket(Position);
				_longStopPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if (_shortTakeProfit > 0m && candle.LowPrice <= _shortTakeProfit)
			{
				BuyMarket(Math.Abs(Position));
				_shortTakeProfit = _shortStopPrice = 0m;
				return;
			}
			
			if (_entryPrice - candle.ClosePrice >= StartOffset)
			{
				var newStop = candle.ClosePrice + TrailStopOffset;
				_shortStopPrice = _shortStopPrice == 0m ? newStop : Math.Min(_shortStopPrice, newStop);
				_shortTakeProfit = 0m;
			}
			
			if (_shortStopPrice > 0m && candle.HighPrice >= _shortStopPrice)
			{
				BuyMarket(Math.Abs(Position));
				_shortStopPrice = 0m;
			}
		}
	}
}
