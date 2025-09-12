using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
namespace StockSharp.Samples.Strategies;
/// <summary>
/// Strategy combining SuperTrend and Smoothed Directional Indicator.
/// </summary>
public class SuperTrendSdiWebhookStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _diLength;
	private readonly StrategyParam<int> _diSmooth;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _trailingPercent;
	private readonly StrategyParam<DataType> _candleType;
	private decimal _entryPrice;
	private decimal _maxPrice;
	private decimal _minPrice;
	private bool _trailingActive;
	private decimal _trailingPrice;
	/// <summary>
	/// ATR period for SuperTrend.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}
	/// <summary>
	/// ATR multiplier for SuperTrend.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}
	/// <summary>
	/// Length for directional indicator.
	/// </summary>
	public int DiLength
	{
		get => _diLength.Value;
		set => _diLength.Value = value;
	}
	/// <summary>
	/// Smoothing period for directional indicator.
	/// </summary>
	public int DiSmooth
	{
		get => _diSmooth.Value;
		set => _diSmooth.Value = value;
	}
	/// <summary>
	/// Take profit in percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}
	/// <summary>
	/// Stop loss in percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}
	/// <summary>
	/// Trailing stop in percent.
	/// </summary>
	public decimal TrailingPercent
	{
		get => _trailingPercent.Value;
		set => _trailingPercent.Value = value;
	}
	/// <summary>
	/// Candle type to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public SuperTrendSdiWebhookStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR length for SuperTrend", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);
		_atrMultiplier = Param(nameof(AtrMultiplier), 1.8m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "ATR multiplier for SuperTrend", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 0.1m);
		_diLength = Param(nameof(DiLength), 3)
		.SetGreaterThanZero()
		.SetDisplay("DI Length", "Length for directional indicator", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(1, 20, 1);
		_diSmooth = Param(nameof(DiSmooth), 7)
		.SetGreaterThanZero()
		.SetDisplay("DI Smooth", "Smoothing for directional indicator", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(1, 20, 1);
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 25m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit %", "Percent take profit", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10m, 50m, 5m);
		_stopLossPercent = Param(nameof(StopLossPercent), 4.8m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss %", "Percent stop loss", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(2m, 10m, 1m);
		_trailingPercent = Param(nameof(TrailingPercent), 1.9m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing %", "Trailing stop percent", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 0.1m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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
		_entryPrice = 0m;
		_maxPrice = 0m;
		_minPrice = 0m;
		_trailingActive = false;
		_trailingPrice = 0m;
	}
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		var supertrend = new SuperTrend { Length = AtrPeriod, Multiplier = AtrMultiplier };
		var adx = new AverageDirectionalIndex { Length = DiLength, Smooth = DiSmooth };
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(supertrend, adx, ProcessCandle)
		.Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, supertrend);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}
	}
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue supertrendValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		var st = (SuperTrendIndicatorValue)supertrendValue;
		var di = (AverageDirectionalIndexValue)adxValue;
		var plus = di.Dx.Plus;
		var minus = di.Dx.Minus;
		var longSignal = plus > minus && st.IsUpTrend;
		var shortSignal = minus > plus && !st.IsUpTrend;
		if (longSignal && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
			_maxPrice = candle.HighPrice;
			_trailingActive = false;
		}
		else if (shortSignal && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
			_minPrice = candle.LowPrice;
			_trailingActive = false;
		}
		if (Position > 0)
		{
			if (candle.HighPrice > _maxPrice)
			_maxPrice = candle.HighPrice;
			var takeProfit = _entryPrice * (1 + TakeProfitPercent / 100m);
			var stopLoss = _entryPrice * (1 - StopLossPercent / 100m);
			if (candle.ClosePrice >= takeProfit)
			{
				SellMarket(Position);
				return;
			}
			if (candle.ClosePrice <= stopLoss)
			{
				SellMarket(Position);
				return;
			}
			var profitPercent = (_maxPrice - _entryPrice) / _entryPrice * 100m;
			if (!_trailingActive && profitPercent >= TrailingPercent)
			{
				_trailingActive = true;
				_trailingPrice = _maxPrice * (1 - TrailingPercent / 100m);
			}
			if (_trailingActive)
			{
				var newStop = _maxPrice * (1 - TrailingPercent / 100m);
				if (newStop > _trailingPrice)
				_trailingPrice = newStop;
				if (candle.ClosePrice <= _trailingPrice)
				SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			if (candle.LowPrice < _minPrice)
			_minPrice = candle.LowPrice;
			var takeProfit = _entryPrice * (1 - TakeProfitPercent / 100m);
			var stopLoss = _entryPrice * (1 + StopLossPercent / 100m);
			if (candle.ClosePrice <= takeProfit)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}
			if (candle.ClosePrice >= stopLoss)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}
			var profitPercent = (_entryPrice - _minPrice) / _entryPrice * 100m;
			if (!_trailingActive && profitPercent >= TrailingPercent)
			{
				_trailingActive = true;
				_trailingPrice = _minPrice * (1 + TrailingPercent / 100m);
			}
			if (_trailingActive)
			{
				var newStop = _minPrice * (1 + TrailingPercent / 100m);
				if (newStop < _trailingPrice)
				_trailingPrice = newStop;
				if (candle.ClosePrice >= _trailingPrice)
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
