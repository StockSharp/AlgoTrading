using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified port of the Tester v0.14 MQL strategy for EURUSD H4.
/// </summary>
public class TesterV014Strategy : Strategy
{
	private readonly StrategyParam<int> _minSignSum;
	private readonly StrategyParam<decimal> _risk;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _barsNumber;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _lastSma;
	private decimal _lastMacd;
	private int _barsCounter;
	private bool _positionOpened;
	
	public int MinSignSum { get => _minSignSum.Value; set => _minSignSum.Value = value; }
	public decimal Risk { get => _risk.Value; set => _risk.Value = value; }
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public int BarsNumber { get => _barsNumber.Value; set => _barsNumber.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	public TesterV014Strategy()
	{
		_minSignSum = Param(nameof(MinSignSum), 5)
		.SetDisplay("Min Sign Sum", "Minimum number of signals before entry", "General");
		
		_risk = Param(nameof(Risk), 0.02m)
		.SetDisplay("Risk", "Account risk used for money management", "General");
		
		_takeProfit = Param(nameof(TakeProfit), 300)
		.SetDisplay("Take Profit", "Take profit in points", "General");
		
		_stopLoss = Param(nameof(StopLoss), 300)
		.SetDisplay("Stop Loss", "Stop loss in points", "General");
		
		_barsNumber = Param(nameof(BarsNumber), 1)
		.SetDisplay("Bars Number", "Holding period in bars", "General");
		
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromHours(4)))
		.SetDisplay("Candle Type", "Time frame used by the strategy", "General");
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection();
		
		var sma14 = new SimpleMovingAverage { Length = 14 };
		var macd = new MovingAverageConvergenceDivergence();
		
		SubscribeCandles(CandleType)
		.Bind(sma14, macd, ProcessCandle)
		.Start();
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal sma14Value, decimal macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		_lastSma = sma14Value;
		_lastMacd = macdValue;
		
		// Approximation of original signal rules: buy when price is above SMA and MACD is positive.
		if (candle.ClosePrice > _lastSma && _lastMacd > 0m && Position == 0)
		{
			BuyMarket();
			_barsCounter = 0;
			_positionOpened = true;
			return;
		}
		
		// Sell when price is below SMA and MACD is negative.
		if (candle.ClosePrice < _lastSma && _lastMacd < 0m && Position == 0)
		{
			SellMarket();
			_barsCounter = 0;
			_positionOpened = true;
			return;
		}
		
		// Manage open position: close after specified number of bars.
		if (_positionOpened)
		{
			_barsCounter++;
			
			if (_barsCounter >= BarsNumber)
			ClosePosition();
		}
	}
}
