using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fractal Breakout Trend Following strategy.
/// Places a buy stop at an activated fractal level when volatility is low.
/// </summary>
public class FractalBreakoutTrendFollowingStrategy : Strategy
{
	private const int AtrLen = 13;
	private const int AtrLookback = 52;
	
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _atrThreshold;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _tradeStart;
	private readonly StrategyParam<DateTimeOffset> _tradeStop;
	
	private AverageTrueRange _atr;
	private SmoothedMovingAverage _teethSmma;
	
	private readonly decimal?[] _teethBuffer = new decimal?[6];
	private int _teethCount;
	
	private decimal _h1, _h2, _h3, _h4, _h5;
	private decimal _l1, _l2, _l3, _l4, _l5;
	private decimal? _upFractalLevel;
	private decimal? _downFractalLevel;
	private decimal? _upFractalActivation;
	private decimal? _downFractalActivation;
	
	private decimal _prevHigh;
	
	private readonly Queue<decimal> _atrQueue = new();
	private readonly Queue<decimal> _atrNormQueue = new();
	private decimal _atrNormSum;
	
	/// <summary>
	/// Percent stop-loss.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}
	
	/// <summary>
	/// ATR percentile threshold.
	/// </summary>
	public decimal AtrThreshold
	{
		get => _atrThreshold.Value;
		set => _atrThreshold.Value = value;
	}
	
	/// <summary>
	/// Number of bars for ATR percentile averaging.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}
	
	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Start of trading period.
	/// </summary>
	public DateTimeOffset TradeStart
	{
		get => _tradeStart.Value;
		set => _tradeStart.Value = value;
	}
	
	/// <summary>
	/// End of trading period.
	/// </summary>
	public DateTimeOffset TradeStop
	{
		get => _tradeStop.Value;
		set => _tradeStop.Value = value;
	}
	
	/// <summary>
	/// Initialize <see cref="FractalBreakoutTrendFollowingStrategy"/>.
	/// </summary>
	public FractalBreakoutTrendFollowingStrategy()
	{
		_stopLossPercent = Param(nameof(StopLossPercent), 0.03m)
		.SetDisplay("Stop Loss (%)", "Percent stop-loss", "Risk");
		
		_atrThreshold = Param(nameof(AtrThreshold), 50m)
		.SetDisplay("ATR Threshold", "ATR percentile threshold", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(10m, 90m, 5m);
		
		_atrPeriod = Param(nameof(AtrPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Bars for ATR average", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(1, 20, 1);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
		
		_tradeStart = Param(nameof(TradeStart), new DateTimeOffset(new DateTime(2023, 1, 1), TimeSpan.Zero))
		.SetDisplay("Trade Start", "Start trading date", "Time");
		
		_tradeStop = Param(nameof(TradeStop), new DateTimeOffset(new DateTime(2025, 1, 1), TimeSpan.Zero))
		.SetDisplay("Trade Stop", "End trading date", "Time");
		
		Volume = 1;
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
		
		Array.Clear(_teethBuffer);
		_teethCount = 0;
		_h1 = _h2 = _h3 = _h4 = _h5 = 0m;
		_l1 = _l2 = _l3 = _l4 = _l5 = 0m;
		_upFractalLevel = null;
		_downFractalLevel = null;
		_upFractalActivation = null;
		_downFractalActivation = null;
		_prevHigh = 0m;
		_atrQueue.Clear();
		_atrNormQueue.Clear();
		_atrNormSum = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_atr = new AverageTrueRange { Length = AtrLen };
		_teethSmma = new SmoothedMovingAverage { Length = 8 };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_atr, _teethSmma, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _teethSmma);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal teethRaw)
	{
		// Update Alligator teeth buffer (shift by 5 bars)
		for (var i = 0; i < 5; i++)
		_teethBuffer[i] = _teethBuffer[i + 1];
		_teethBuffer[5] = teethRaw;
		decimal? teeth = null;
		if (_teethCount >= 5)
		teeth = _teethBuffer[0];
		else
		_teethCount++;
		
		// Update highs and lows for fractals
		_h1 = _h2; _h2 = _h3; _h3 = _h4; _h4 = _h5; _h5 = candle.HighPrice;
		_l1 = _l2; _l2 = _l3; _l3 = _l4; _l4 = _l5; _l5 = candle.LowPrice;
		
		if (candle.State != CandleStates.Finished)
		{
			_prevHigh = candle.HighPrice;
			return;
		}
		
		// Detect fractals
		if (_h3 > _h1 && _h3 > _h2 && _h3 > _h4 && _h3 > _h5)
		_upFractalLevel = _h3;
		else if (_upFractalLevel is decimal uf && _prevHigh <= uf && candle.HighPrice > uf)
		_upFractalLevel = null;
		
		if (_l3 < _l1 && _l3 < _l2 && _l3 < _l4 && _l3 < _l5)
		_downFractalLevel = _l3;
		
		if (teeth is not decimal t)
		{
			_prevHigh = candle.HighPrice;
			return;
		}
		
		_upFractalActivation = _upFractalLevel is decimal uf2 && uf2 >= t ? uf2 : null;
		if (_downFractalLevel is decimal df && df <= t)
		_downFractalActivation = df;
		
		// ATR percentile rank
		_atrQueue.Enqueue(atrValue);
		if (_atrQueue.Count > AtrLookback)
		_atrQueue.Dequeue();
		var lessOrEqual = 0;
		foreach (var v in _atrQueue)
		{
			if (v <= atrValue)
			lessOrEqual++;
		}
		var atrNormalized = (decimal)lessOrEqual / _atrQueue.Count * 100m;
		
		_atrNormQueue.Enqueue(atrNormalized);
		_atrNormSum += atrNormalized;
		if (_atrNormQueue.Count > AtrPeriod)
		_atrNormSum -= _atrNormQueue.Dequeue();
		if (_atrNormQueue.Count < AtrPeriod)
		{
			_prevHigh = candle.HighPrice;
			return;
		}
		var atrAvg = _atrNormSum / _atrNormQueue.Count;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevHigh = candle.HighPrice;
			return;
		}
		
		if (_upFractalActivation is decimal activation && atrAvg <= AtrThreshold &&
		candle.ServerTime >= TradeStart && candle.ServerTime <= TradeStop && Position <= 0)
		{
			CancelActiveOrders();
			BuyStop(Volume + Math.Abs(Position), activation);
		}
		
		if (Position > 0)
		{
			var stopPrice = Math.Max(PositionAvgPrice * (1m - StopLossPercent), _downFractalActivation ?? decimal.MinValue);
			if (candle.LowPrice <= stopPrice)
			SellMarket(Position);
		}
		
		_prevHigh = candle.HighPrice;
	}
}
