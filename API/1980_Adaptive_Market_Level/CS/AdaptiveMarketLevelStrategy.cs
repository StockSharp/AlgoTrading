using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Adaptive Market Level indicator.
/// </summary>
public class AdaptiveMarketLevelStrategy : Strategy
{
	private readonly StrategyParam<int> _fractal;
	private readonly StrategyParam<int> _lag;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<DataType> _candleType;
	
	private AdaptiveMarketLevel _aml = null!;
	private decimal? _prevAml;
	private int _prevDirection;
	
	public AdaptiveMarketLevelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
		
		_fractal = Param(nameof(Fractal), 6)
		.SetDisplay("Fractal", "Fractal period", "Indicator")
		.SetCanOptimize(true);
		
		_lag = Param(nameof(Lag), 7)
		.SetDisplay("Lag", "Lag period", "Indicator")
		.SetCanOptimize(true);
		
		_stopLossTicks = Param(nameof(StopLossTicks), 1000)
		.SetDisplay("Stop Loss (ticks)", "Stop loss in ticks", "Risk")
		.SetCanOptimize(true);
		
		_takeProfitTicks = Param(nameof(TakeProfitTicks), 2000)
		.SetDisplay("Take Profit (ticks)", "Take profit in ticks", "Risk")
		.SetCanOptimize(true);
		
		_buyPosOpen = Param(nameof(BuyPosOpen), true)
		.SetDisplay("Allow Long Entry", "Enable opening long positions", "Trading");
		
		_sellPosOpen = Param(nameof(SellPosOpen), true)
		.SetDisplay("Allow Short Entry", "Enable opening short positions", "Trading");
		
		_buyPosClose = Param(nameof(BuyPosClose), true)
		.SetDisplay("Allow Long Exit", "Enable closing long positions", "Trading");
		
		_sellPosClose = Param(nameof(SellPosClose), true)
		.SetDisplay("Allow Short Exit", "Enable closing short positions", "Trading");
	}
	
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	public int Fractal
	{
		get => _fractal.Value;
		set => _fractal.Value = value;
	}
	
	public int Lag
	{
		get => _lag.Value;
		set => _lag.Value = value;
	}
	
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}
	
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}
	
	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}
	
	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}
	
	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}
	
	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
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
		
		_prevAml = null;
		_prevDirection = 0;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_aml = new AdaptiveMarketLevel
		{
			Fractal = Fractal,
			Lag = Lag,
			Step = Security.PriceStep ?? 1m
		};
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_aml, ProcessCandle)
		.Start();
		
		var step = Security.PriceStep ?? 1m;
		
		StartProtection(
		takeProfit: new Unit(TakeProfitTicks * step, UnitTypes.Point),
		stopLoss: new Unit(StopLossTicks * step, UnitTypes.Point));
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _aml);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal aml)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var up = _prevAml is decimal prev && aml > prev;
		var down = _prevAml is decimal prev2 && aml < prev2;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevAml = aml;
			_prevDirection = up ? 1 : down ? -1 : 0;
			return;
		}
		
		if (up && _prevDirection <= 0)
		{
			if (BuyPosOpen && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
			
			if (SellPosClose && Position < 0)
			BuyMarket(Math.Abs(Position));
			
			_prevDirection = 1;
		}
		else if (down && _prevDirection >= 0)
		{
			if (SellPosOpen && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
			
			if (BuyPosClose && Position > 0)
			SellMarket(Math.Abs(Position));
			
			_prevDirection = -1;
		}
		
		_prevAml = aml;
	}
	
	private class AdaptiveMarketLevel : BaseIndicator
	{
		public int Fractal { get; set; }
		public int Lag { get; set; }
		public decimal Step { get; set; } = 1m;
		
		private readonly List<decimal> _high = new();
		private readonly List<decimal> _low = new();
		private readonly List<decimal> _open = new();
		private readonly List<decimal> _close = new();
		private readonly List<decimal> _smooth = new();
		private decimal _aml;
		
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			if (candle == null)
			return new DecimalIndicatorValue(this);
			
			_high.Insert(0, candle.HighPrice);
			_low.Insert(0, candle.LowPrice);
			_open.Insert(0, candle.OpenPrice);
			_close.Insert(0, candle.ClosePrice);
			
			var maxLength = Math.Max(Lag + 1, Fractal * 2);
			if (_high.Count > maxLength)
			{
				_high.RemoveAt(_high.Count - 1);
				_low.RemoveAt(_low.Count - 1);
				_open.RemoveAt(_open.Count - 1);
				_close.RemoveAt(_close.Count - 1);
			}
			
			if (_high.Count < maxLength)
			return new DecimalIndicatorValue(this);
			
			var r1 = Range(Fractal, 0) / Fractal;
			var r2 = Range(Fractal, Fractal) / Fractal;
			var r3 = Range(Fractal * 2, 0) / (Fractal * 2);
			
			var dim = 0m;
			if (r1 + r2 > 0 && r3 > 0)
			dim = (decimal)((Math.Log((double)(r1 + r2)) - Math.Log((double)r3)) * 1.44269504088896);
			
			var alpha = (decimal)Math.Exp(-(double)Lag * ((double)dim - 1.0));
			alpha = Math.Min(alpha, 1m);
			alpha = Math.Max(alpha, 0.01m);
			
			var price = (_high[0] + _low[0] + 2m * _open[0] + 2m * _close[0]) / 6m;
			var prevSmooth = _smooth.Count > 0 ? _smooth[0] : price;
			var newSmooth = alpha * price + (1m - alpha) * prevSmooth;
			
			_smooth.Insert(0, newSmooth);
			if (_smooth.Count > Lag + 1)
			_smooth.RemoveAt(_smooth.Count - 1);
			
			var condition = _smooth.Count > Lag && Math.Abs(newSmooth - _smooth[Lag]) >= Lag * Lag * Step;
			_aml = condition ? newSmooth : _aml;
			
			return new DecimalIndicatorValue(this, _aml);
		}
		
		private decimal Range(int period, int start)
		{
			var max = _high[start];
			var min = _low[start];
			for (var i = start + 1; i < start + period && i < _high.Count; i++)
			{
				if (_high[i] > max)
				max = _high[i];
				if (_low[i] < min)
				min = _low[i];
			}
			return max - min;
		}
	}
}
