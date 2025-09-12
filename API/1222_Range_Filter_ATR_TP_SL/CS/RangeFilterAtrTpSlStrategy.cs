using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class RangeFilterAtrTpSlStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _tpMultiplier;
	private readonly StrategyParam<decimal> _slMultiplier;
	
	private decimal? _prevClose;
	private decimal? _prevUpper;
	private decimal? _prevLower;
	private bool? _uptrend;
	private bool? _downtrend;
	private decimal _takeProfit;
	private decimal _stopLoss;
	
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	public int RangeFilterLength
	{
		get => _length.Value;
		set => _length.Value = value;
	}
	
	public decimal RangeFilterMultiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}
	
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}
	
	public decimal TakeProfitMultiplier
	{
		get => _tpMultiplier.Value;
		set => _tpMultiplier.Value = value;
	}
	
	public decimal StopLossMultiplier
	{
		get => _slMultiplier.Value;
		set => _slMultiplier.Value = value;
	}
	
	public RangeFilterAtrTpSlStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
		
		_length = Param(nameof(RangeFilterLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("RF Length", "Range filter length", "Range Filter")
		.SetCanOptimize(true)
		.SetOptimize(10, 50, 5);
		
		_multiplier = Param(nameof(RangeFilterMultiplier), 1.5m)
		.SetRange(0.1m, 5m)
		.SetDisplay("RF Multiplier", "StdDev multiplier", "Range Filter")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.1m);
		
		_atrLength = Param(nameof(AtrLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "ATR period", "ATR")
		.SetCanOptimize(true)
		.SetOptimize(7, 28, 1);
		
		_tpMultiplier = Param(nameof(TakeProfitMultiplier), 1.5m)
		.SetRange(0m, 10m)
		.SetDisplay("TP Mult", "ATR multiplier for take profit", "Risk Management");
		
		_slMultiplier = Param(nameof(StopLossMultiplier), 1.5m)
		.SetRange(0m, 10m)
		.SetDisplay("SL Mult", "ATR multiplier for stop loss", "Risk Management");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = null;
		_prevUpper = null;
		_prevLower = null;
		_uptrend = null;
		_downtrend = null;
		_takeProfit = 0;
		_stopLoss = 0;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var sma = new SimpleMovingAverage { Length = RangeFilterLength };
		var stdDev = new StandardDeviation { Length = RangeFilterLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(sma, stdDev, atr, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, stdDev);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal smooth, decimal deviation, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var upper = smooth + RangeFilterMultiplier * deviation;
		var lower = smooth - RangeFilterMultiplier * deviation;
		
		var longCondition = _prevClose is decimal prevClose && _prevUpper is decimal prevUpper && _uptrend is bool prevUptrend &&
		prevClose <= prevUpper && candle.ClosePrice > upper && !prevUptrend;
		
		var shortCondition = _prevClose is decimal prevClose2 && _prevLower is decimal prevLower && _downtrend is bool prevDowntrend &&
		prevClose2 >= prevLower && candle.ClosePrice < lower && !prevDowntrend;
		
		var newUptrend = candle.ClosePrice > upper && ((_uptrend is null) || _uptrend.Value);
		var newDowntrend = candle.ClosePrice < lower && ((_downtrend is null) || _downtrend.Value);
		
		if (longCondition && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_takeProfit = candle.ClosePrice + atr * TakeProfitMultiplier;
			_stopLoss = candle.ClosePrice - atr * StopLossMultiplier;
		}
		else if (shortCondition && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_takeProfit = candle.ClosePrice - atr * TakeProfitMultiplier;
			_stopLoss = candle.ClosePrice + atr * StopLossMultiplier;
		}
		
		if (Position > 0)
		{
			if (candle.HighPrice >= _takeProfit || candle.LowPrice <= _stopLoss)
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (candle.LowPrice <= _takeProfit || candle.HighPrice >= _stopLoss)
			BuyMarket(Math.Abs(Position));
		}
		
		_uptrend = newUptrend;
		_downtrend = newDowntrend;
		_prevClose = candle.ClosePrice;
		_prevUpper = upper;
		_prevLower = lower;
	}
}
