using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Double Smoothed Stochastic (DSS) Bressert indicator.
/// Trades when DSS line crosses the MIT line.
/// </summary>
public class DssBressertStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _stoPeriod;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevUp;
	private decimal _prevDown;
	
	/// <summary>
	/// EMA period used for smoothing.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}
	
	/// <summary>
	/// Stochastic period.
	/// </summary>
	public int StoPeriod
	{
		get => _stoPeriod.Value;
		set => _stoPeriod.Value = value;
	}
	
	/// <summary>
	/// Take profit level in percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}
	
	/// <summary>
	/// Stop loss level in percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	/// Initializes strategy parameters.
	/// </summary>
	public DssBressertStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 8)
		.SetGreaterThanZero()
		.SetDisplay("EMA Period", "EMA smoothing period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);
		
		_stoPeriod = Param(nameof(StoPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic Period", "Stochastic calculation period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 1);
		
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit %", "Take profit level in percent", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 0.5m);
		
		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss %", "Stop loss level in percent", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 5m, 0.5m);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevUp = 0m;
		_prevDown = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var dss = new DssBressertIndicator
		{
			EmaPeriod = EmaPeriod,
			StoPeriod = StoPeriod
		};
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(dss, ProcessCandle)
		.Start();
		
		StartProtection(
		takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
		stopLoss: new Unit(StopLossPercent, UnitTypes.Percent));
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, dss);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var dssValue = (DssBressertValue)value;
		var up = dssValue.Up;
		var down = dssValue.Down;
		
		var crossBuy = _prevUp > _prevDown && up <= down;
		var crossSell = _prevDown > _prevUp && down <= up;
		
		if (crossBuy && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (crossSell && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		
		_prevUp = up;
		_prevDown = down;
	}
}

/// <summary>
/// Double Smoothed Stochastic Bressert indicator.
/// Calculates DSS and MIT lines.
/// </summary>
public class DssBressertIndicator : BaseIndicator<decimal>
{
	/// <summary>
	/// EMA smoothing period.
	/// </summary>
	public int EmaPeriod { get; set; } = 8;
	
	/// <summary>
	/// Stochastic period.
	/// </summary>
	public int StoPeriod { get; set; } = 13;
	
	private readonly Queue<decimal> _high = new();
	private readonly Queue<decimal> _low = new();
	private readonly Queue<decimal> _close = new();
	private readonly Queue<decimal> _mit = new();
	
	private decimal _prevMit = 50m;
	private decimal _prevDss = 50m;
	
	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();
		_high.Clear();
		_low.Clear();
		_close.Clear();
		_mit.Clear();
		_prevMit = 50m;
		_prevDss = 50m;
	}
	
	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
		return new DecimalIndicatorValue(this, default, input.Time);
		
		_high.Enqueue(candle.HighPrice);
		_low.Enqueue(candle.LowPrice);
		_close.Enqueue(candle.ClosePrice);
		
		if (_high.Count > StoPeriod)
		{
			_high.Dequeue();
			_low.Dequeue();
			_close.Dequeue();
		}
		
		var highRange = GetMax(_high);
		var lowRange = GetMin(_low);
		if (highRange == lowRange)
		return new DssBressertValue(this, input, _prevDss, _prevMit);
		
		var delta = candle.ClosePrice - lowRange;
		var mitRaw = delta / (highRange - lowRange) * 100m;
		var coeff = 2m / (1m + EmaPeriod);
		var mitValue = _prevMit + coeff * (mitRaw - _prevMit);
		_prevMit = mitValue;
		
		_mit.Enqueue(mitValue);
		if (_mit.Count > StoPeriod)
		_mit.Dequeue();
		
		var highMit = GetMax(_mit);
		var lowMit = GetMin(_mit);
		if (highMit == lowMit)
		return new DssBressertValue(this, input, _prevDss, mitValue);
		
		var deltaMit = mitValue - lowMit;
		var dssRaw = deltaMit / (highMit - lowMit) * 100m;
		var dssValue = _prevDss + coeff * (dssRaw - _prevDss);
		_prevDss = dssValue;
		
		return new DssBressertValue(this, input, dssValue, mitValue);
	}
	
	private static decimal GetMax(IEnumerable<decimal> values)
	{
		var max = decimal.MinValue;
		foreach (var v in values)
		{
			if (v > max)
			max = v;
		}
		return max;
	}
	
	private static decimal GetMin(IEnumerable<decimal> values)
	{
		var min = decimal.MaxValue;
		foreach (var v in values)
		{
			if (v < min)
			min = v;
		}
		return min;
	}
}

/// <summary>
/// Indicator value for <see cref="DssBressertIndicator"/>.
/// </summary>
public class DssBressertValue : ComplexIndicatorValue
{
	public DssBressertValue(IIndicator indicator, IIndicatorValue input, decimal up, decimal down)
	: base(indicator, input, (nameof(Up), up), (nameof(Down), down))
	{
	}
	
	/// <summary>
	/// DSS line value.
	/// </summary>
	public decimal Up => (decimal)GetValue(nameof(Up));
	
	/// <summary>
	/// MIT line value.
	/// </summary>
	public decimal Down => (decimal)GetValue(nameof(Down));
}
