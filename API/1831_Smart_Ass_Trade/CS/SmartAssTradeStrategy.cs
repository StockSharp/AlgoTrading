using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Smart Ass Trade strategy converted from MQL version.
/// Uses multi-timeframe OsMA (MACD histogram) and moving averages with Williams %R filter.
/// </summary>
public class SmartAssTradeStrategy : Strategy
{
	private readonly StrategyParam<bool> _hedging;
	private readonly StrategyParam<bool> _lotsOptimization;
	private readonly StrategyParam<decimal> _lots;
	private readonly StrategyParam<bool> _automaticTakeProfit;
	private readonly StrategyParam<decimal> _minimumTakeProfit;
	private readonly StrategyParam<bool> _automaticStopLoss;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;
	
	private MovingAverageConvergenceDivergence _macd5;
	private MovingAverageConvergenceDivergence _macd15;
	private MovingAverageConvergenceDivergence _macd30;
	private SimpleMovingAverage _sma5;
	private SimpleMovingAverage _sma15;
	private SimpleMovingAverage _sma30;
	private WilliamsR _wpr;
	
	private bool _osmaUp5;
	private bool _osmaUp15;
	private bool _osmaUp30;
	private bool _maUp5;
	private bool _maUp15;
	private bool _maUp30;
	private decimal _wprVal;
	
	private decimal _prevHist5;
	private decimal _prevHist15;
	private decimal _prevHist30;
	private decimal _prevMa5;
	private decimal _prevMa15;
	private decimal _prevMa30;
	
	/// <summary>
	/// Allow opening opposite positions simultaneously.
	/// </summary>
	public bool Hedging { get => _hedging.Value; set => _hedging.Value = value; }
	
	/// <summary>
	/// Enable automatic lot sizing based on account value.
	/// </summary>
	public bool LotsOptimization { get => _lotsOptimization.Value; set => _lotsOptimization.Value = value; }
	
	/// <summary>
	/// Fixed trading volume when optimization is disabled.
	/// </summary>
	public decimal Lots { get => _lots.Value; set => _lots.Value = value; }
	
	/// <summary>
	/// Use dynamic profit target from Bollinger width (not implemented, kept for compatibility).
	/// </summary>
	public bool AutomaticTakeProfit { get => _automaticTakeProfit.Value; set => _automaticTakeProfit.Value = value; }
	
	/// <summary>
	/// Fallback profit target in points when automatic mode is off.
	/// </summary>
	public decimal MinimumTakeProfit { get => _minimumTakeProfit.Value; set => _minimumTakeProfit.Value = value; }
	
	/// <summary>
	/// Use dynamic stop loss (not implemented, kept for compatibility).
	/// </summary>
	public bool AutomaticStopLoss { get => _automaticStopLoss.Value; set => _automaticStopLoss.Value = value; }
	
	/// <summary>
	/// Fallback stop loss in points when automatic mode is off.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	
	/// <summary>
	/// Base candle type for the strategy (5 minute by default).
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="SmartAssTradeStrategy"/> class.
	/// </summary>
	public SmartAssTradeStrategy()
	{
		_hedging = Param(nameof(Hedging), false)
		.SetDisplay("Hedging", "Allow opening opposite positions", "General");
		
		_lotsOptimization = Param(nameof(LotsOptimization), false)
		.SetDisplay("Lots Optimization", "Enable automatic lot sizing", "Trading");
		
		_lots = Param(nameof(Lots), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Lots", "Fixed trading volume", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 5m, 0.1m);
		
		_automaticTakeProfit = Param(nameof(AutomaticTakeProfit), false)
		.SetDisplay("Automatic Take Profit", "Use dynamic profit target", "Trading");
		
		_minimumTakeProfit = Param(nameof(MinimumTakeProfit), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Minimum Take Profit", "Fallback profit target in points", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(5m, 100m, 5m);
		
		_automaticStopLoss = Param(nameof(AutomaticStopLoss), true)
		.SetDisplay("Automatic Stop Loss", "Use dynamic stop loss", "Trading");
		
		_stopLoss = Param(nameof(StopLoss), 350m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Fallback stop loss in points", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(50m, 500m, 10m);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Base timeframe", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (Security, TimeSpan.FromMinutes(15).TimeFrame());
		yield return (Security, TimeSpan.FromMinutes(30).TimeFrame());
		yield return (Security, TimeSpan.FromDays(1).TimeFrame());
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();
		
		_macd5 = new MovingAverageConvergenceDivergence();
		_macd15 = new MovingAverageConvergenceDivergence();
		_macd30 = new MovingAverageConvergenceDivergence();
		_sma5 = new SimpleMovingAverage { Length = 20 };
		_sma15 = new SimpleMovingAverage { Length = 20 };
		_sma30 = new SimpleMovingAverage { Length = 20 };
		_wpr = new WilliamsR { Length = 26 };
		
		var sub5 = SubscribeCandles(TimeSpan.FromMinutes(5).TimeFrame());
		sub5.Bind(_macd5, _sma5, Process5).Start();
		
		var sub15 = SubscribeCandles(TimeSpan.FromMinutes(15).TimeFrame());
		sub15.Bind(_macd15, _sma15, Process15).Start();
		
		var sub30 = SubscribeCandles(TimeSpan.FromMinutes(30).TimeFrame());
		sub30.Bind(_macd30, _sma30, Process30).Start();
		
		var subDay = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		subDay.Bind(_wpr, ProcessDaily).Start();
	}
	
	private void Process5(ICandleMessage candle, decimal macd, decimal signal, decimal hist, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		// Check histogram slope and moving average direction on 5m timeframe
		_osmaUp5 = hist > _prevHist5;
		_prevHist5 = hist;
		_maUp5 = ma > _prevMa5;
		_prevMa5 = ma;
		
		TryTrade();
	}
	
	private void Process15(ICandleMessage candle, decimal macd, decimal signal, decimal hist, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		// Check histogram slope and moving average direction on 15m timeframe
		_osmaUp15 = hist > _prevHist15;
		_prevHist15 = hist;
		_maUp15 = ma > _prevMa15;
		_prevMa15 = ma;
		
		TryTrade();
	}
	
	private void Process30(ICandleMessage candle, decimal macd, decimal signal, decimal hist, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		// Check histogram slope and moving average direction on 30m timeframe
		_osmaUp30 = hist > _prevHist30;
		_prevHist30 = hist;
		_maUp30 = ma > _prevMa30;
		_prevMa30 = ma;
		
		TryTrade();
	}
	
	private void ProcessDaily(ICandleMessage candle, decimal wpr)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		// Store Williams %R value from daily timeframe
		_wprVal = wpr;
		TryTrade();
	}
	
	private void TryTrade()
	{
		var upCount = (_osmaUp5 ? 1 : 0) + (_osmaUp15 ? 1 : 0) + (_osmaUp30 ? 1 : 0);
		var maUpCount = (_maUp5 ? 1 : 0) + (_maUp15 ? 1 : 0) + (_maUp30 ? 1 : 0);
		var downCount = 3 - upCount;
		var maDownCount = 3 - maUpCount;
		
		// Williams %R ranges from -100 to 0, so values close to 0 mean overbought
		var upward = upCount == 3 && maUpCount >= 2 && _wprVal > -98m;
		var downward = downCount == 3 && maDownCount >= 2 && _wprVal < -2m;
		
		var volume = Lots;
		if (LotsOptimization)
		volume = Math.Max(0.1m, (Portfolio?.CurrentValue ?? 0m) / 10000m);
		
		if (upward && (!Hedging ? Position <= 0 : true))
		{
			BuyMarket(volume + (!Hedging ? Math.Abs(Position) : 0m));
		}
		else if (downward && (!Hedging ? Position >= 0 : true))
		{
			SellMarket(volume + (!Hedging ? Math.Abs(Position) : 0m));
		}
	}
}
