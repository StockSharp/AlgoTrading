using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class TwoMarsOkxStrategy : Strategy
{
	private readonly StrategyParam<int> _basisLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _entryPrice;
	private bool _isInitialized;
	private bool _wasSignalBelowBasis;
	
	public int BasisLength
	{
		get => _basisLength.Value;
		set => _basisLength.Value = value;
	}
	
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}
	
	public int SupertrendPeriod
	{
		get => _supertrendPeriod.Value;
		set => _supertrendPeriod.Value = value;
	}
	
	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
	}
	
	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}
	
	public decimal BbMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}
	
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}
	
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}
	
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	public TwoMarsOkxStrategy()
	{
		_basisLength = Param(nameof(BasisLength), 96)
		.SetGreaterThanZero()
		.SetDisplay("Basis MA Length", "Length of basis moving average", "MA Settings")
		.SetCanOptimize(true)
		.SetOptimize(20, 200, 10);
		
		_signalLength = Param(nameof(SignalLength), 89)
		.SetGreaterThanZero()
		.SetDisplay("Signal MA Length", "Length of signal moving average", "MA Settings")
		.SetCanOptimize(true)
		.SetOptimize(10, 200, 10);
		
		_supertrendPeriod = Param(nameof(SupertrendPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("SuperTrend Period", "Period of SuperTrend", "Trend Filter")
		.SetCanOptimize(true)
		.SetOptimize(10, 50, 5);
		
		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 4m)
		.SetGreaterThanZero()
		.SetDisplay("SuperTrend Multiplier", "Multiplier for SuperTrend", "Trend Filter")
		.SetCanOptimize(true)
		.SetOptimize(1m, 6m, 0.5m);
		
		_bbLength = Param(nameof(BbLength), 30)
		.SetGreaterThanZero()
		.SetDisplay("BB Length", "Bollinger Bands period", "Take Profit")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 5);
		
		_bbMultiplier = Param(nameof(BbMultiplier), 3m)
		.SetGreaterThanZero()
		.SetDisplay("BB Multiplier", "Bollinger Bands width", "Take Profit")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 0.5m);
		
		_atrPeriod = Param(nameof(AtrPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR length", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 5);
		
		_atrMultiplier = Param(nameof(AtrMultiplier), 6m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "ATR multiplier for stop loss", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(1m, 10m, 0.5m);
		
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
		_entryPrice = 0m;
		_isInitialized = false;
	}
	
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var basisMa = new EMA { Length = BasisLength };
		var signalMa = new EMA { Length = SignalLength };
		var supertrend = new SuperTrend { Length = SupertrendPeriod, Multiplier = SupertrendMultiplier };
		var bollinger = new BollingerBands { Length = BbLength, Width = BbMultiplier };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(basisMa, signalMa, supertrend, bollinger, atr, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, basisMa);
			DrawIndicator(area, signalMa);
			DrawIndicator(area, supertrend);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(
	ICandleMessage candle,
	decimal basis,
	decimal signal,
	decimal trend,
	decimal middle,
	decimal upper,
	decimal lower,
	decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (!_isInitialized)
		{
			_wasSignalBelowBasis = signal < basis;
			_isInitialized = true;
			return;
		}
		
		var isSignalBelowBasis = signal < basis;
		var isUpTrend = candle.ClosePrice > trend;
		var isDownTrend = candle.ClosePrice < trend;
		
		if (_wasSignalBelowBasis && !isSignalBelowBasis && Position <= 0 && isUpTrend)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
		}
		else if (!_wasSignalBelowBasis && isSignalBelowBasis && Position >= 0 && isDownTrend)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
		}
		
		_wasSignalBelowBasis = isSignalBelowBasis;
		
		if (Position > 0 && candle.HighPrice >= upper)
		SellMarket(Math.Abs(Position));
		else if (Position < 0 && candle.LowPrice <= lower)
		BuyMarket(Math.Abs(Position));
		
		var stopLoss = atrValue * AtrMultiplier;
		
		if (Position > 0 && candle.LowPrice <= _entryPrice - stopLoss)
		SellMarket(Math.Abs(Position));
		else if (Position < 0 && candle.HighPrice >= _entryPrice + stopLoss)
		BuyMarket(Math.Abs(Position));
	}
}
